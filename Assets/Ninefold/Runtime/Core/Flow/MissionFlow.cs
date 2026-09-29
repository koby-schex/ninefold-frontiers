using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ninefold.Core.Combat;
using Ninefold.Core.Persistence;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Flow
{
    public enum MissionFlowPhase { Selection, Battle, Results }

    /// <summary>Single-owner simulation-thread coordinator. ReadBattle returns a detached snapshot, never live authority.</summary>
    public sealed class MissionFlow
    {
        private readonly LocalBattleStore battles;
        private readonly LocalProgressStore profiles;
        private readonly Dictionary<string, MissionEntry> entries;
        private readonly Dictionary<string, RosterUnit> roster;
        private readonly string revision;
        private byte[] snapshot;
        private bool busy;
        public bool NeedsReload { get; private set; } = true;
        public bool Recovered { get; private set; }
        public MissionFlowPhase Phase { get; private set; }
        public PlayerProgress Progress { get; private set; }
        public MissionClaim Receipt { get; private set; }
        public IReadOnlyList<MissionEntry> Missions { get; }
        public MissionFlow(IBattleSaveFiles battleFiles, IProgressSaveFiles progressFiles,
            string profileId, string contentRevision, IEnumerable<MissionEntry> missions, IEnumerable<RosterUnit> units)
        {
            RewardRules.Id(profileId); RewardRules.Id(contentRevision);
            if (missions == null || units == null) throw new ArgumentNullException("Catalogs required.");
            var catalog = missions.ToArray(); var owned = units.ToArray();
            if (catalog.Any(x => x == null) || owned.Any(x => x == null)) throw new ArgumentException("Null catalog entry.");
            entries = catalog.ToDictionary(m => m.Id, StringComparer.Ordinal);
            roster = owned.ToDictionary(u => u.Id, StringComparer.Ordinal);
            if (owned.Select(u => u.Unlock.FragmentResourceId).Distinct(StringComparer.Ordinal).Count() != owned.Length)
                throw new ArgumentException("Each unit requires its own fragment resource.");
            Missions = Array.AsReadOnly(catalog);
            // Bind battle snapshots to profile identity as well as content version.
            using var sha = SHA256.Create();
            revision = "flow-v1:" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(profileId.Length + ":" + profileId + contentRevision))).Replace("-", "");
            battles = new LocalBattleStore(battleFiles, revision);
            profiles = new LocalProgressStore(progressFiles, profileId);
        }
        public void CreateProfile(IEnumerable<string> starterUnits)
        {
            Enter(false);
            try
            {
                if (battles.Load() != null) throw new InvalidOperationException("Battle exists; recover its profile instead of creating one.");
                if (starterUnits == null) throw new ArgumentNullException(nameof(starterUnits));
                var starters = starterUnits.ToArray();
                if (starters.Length != 3 || starters.Distinct(StringComparer.Ordinal).Count() != 3 ||
                    starters.Any(id => id == null || !roster.ContainsKey(id) || roster[id].IsApex) ||
                    starters.Select(id => roster[id].FactionId).Distinct(StringComparer.Ordinal).Count() != 1)
                    throw new ArgumentException("Starter roster must contain three standard units from one faction.");
                try { profiles.Create(starters); } catch { NeedsReload = true; throw; }
                LoadCore();
            }
            finally { busy = false; }
        }
        public void Open()
        {
            Enter(false);
            try { LoadCore(); }
            finally { busy = false; }
        }
        private void LoadCore()
        {
            NeedsReload = true;
            var profile = profiles.Load() ?? throw new InvalidOperationException("Profile absent; explicitly create a new profile.");
            var saved = battles.Load();
            byte[] next = null; var phase = MissionFlowPhase.Selection;
            if (saved != null)
            {
                var battle = saved.Battle;
                if (battle.Mission == null) throw new InvalidOperationException("Saved battle has no mission.");
                var claim = profile.Progress.Claims.FirstOrDefault(c => c.AttemptId == battle.Mission.Definition.AttemptId);
                if (claim != null)
                {
                    if (claim.MissionId != battle.Mission.Definition.MissionId) throw new InvalidOperationException("Conflicting claimed battle identity.");
                    // A fallback may be an earlier active checkpoint of an already-claimed attempt.
                    if (battle.Mission.Result != null && claim.ResultFingerprint != ProgressSave.Fingerprint(battle.Mission.Result))
                        throw new InvalidOperationException("Conflicting claimed result.");
                }
                else
                {
                    Entry(battle.Mission.Definition.MissionId);
                    if (battle.IsBattleEnded && battle.Mission.Result == null) throw new InvalidOperationException("Closed battle has no claimable result.");
                    next = BattleSave.Capture(battle, revision);
                    phase = battle.Mission.Result == null ? MissionFlowPhase.Battle : MissionFlowPhase.Results;
                }
            }
            snapshot = next; Progress = profile.Progress; Receipt = null; Phase = phase;
            Recovered = profile.Recovered || (saved?.Recovered ?? false); NeedsReload = false;
        }
        public SquadFailure ValidateSquad(string missionId, IEnumerable<string> selected)
        {
            EnsureReady();
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            return Entry(missionId).Validate(Progress, roster, selected.ToArray());
        }
        public void Start(string missionId, IEnumerable<string> selected)
        {
            Enter(true);
            try
            {
                if (Phase != MissionFlowPhase.Selection) throw new InvalidOperationException("Finish the current attempt before starting another.");
                if (selected == null) throw new ArgumentNullException(nameof(selected));
                var squad = selected.ToArray(); var entry = Entry(missionId);
                var failure = entry.Validate(Progress, roster, squad);
                if (failure != SquadFailure.None) throw new InvalidOperationException("Invalid squad: " + failure);
                string attempt = Guid.NewGuid().ToString("N");
                var draft = entry.Build(attempt, Array.AsReadOnly(squad));
                if (draft?.Mission == null || draft.IsBattleEnded || draft.Mission.Result != null || draft.Mission.Definition.MissionId != missionId ||
                    draft.Mission.Definition.AttemptId != attempt || !draft.Mission.Definition.Squad.SequenceEqual(squad))
                    throw new InvalidOperationException("Factory must return a fresh battle matching mission, attempt and squad.");
                Commit(draft);
            }
            finally { busy = false; }
        }
        public BattleTurnController ReadBattle()
        {
            EnsureReady();
            return snapshot == null ? null : BattleSave.Restore(snapshot, revision);
        }
        /// <summary>Execute one completed rules command on a private copy; publish only after its checkpoint succeeds.</summary>
        public void Execute(Action<BattleTurnController> command)
        {
            Enter(true);
            try
            {
                if (Phase != MissionFlowPhase.Battle) throw new InvalidOperationException("No active battle.");
                if (command == null) throw new ArgumentNullException(nameof(command));
                var draft = BattleSave.Restore(snapshot, revision);
                command(draft);
                if (draft.IsBattleEnded && draft.Mission.Result == null) throw new InvalidOperationException("Command closed the battle without a result.");
                Commit(draft);
            }
            finally { busy = false; }
        }
        private void Commit(BattleTurnController draft)
        {
            var bytes = BattleSave.Capture(draft, revision);
            // Detach from references retained by the command or factory.
            var committed = BattleSave.Restore(bytes, revision);
            try { battles.Save(committed); }
            catch { NeedsReload = true; throw; }
            snapshot = bytes; Receipt = null;
            Phase = committed.Mission.Result == null ? MissionFlowPhase.Battle : MissionFlowPhase.Results;
        }
        public MissionClaim ClaimRewards()
        {
            Enter(true);
            try
            {
                if (Phase != MissionFlowPhase.Results) throw new InvalidOperationException("No terminal result.");
                var result = BattleSave.Restore(snapshot, revision).Mission.Result;
                ClaimResult claimed;
                try { claimed = profiles.Claim(result, Entry(result.MissionId).Rewards); }
                catch { NeedsReload = true; throw; }
                Progress = claimed.Saved.Progress; Receipt = claimed.Receipt; Recovered |= claimed.Saved.Recovered;
                return Receipt;
            }
            finally { busy = false; }
        }
        public UnlockResult UnlockUnit(string operationId, string unitId)
        {
            Enter(true);
            try
            {
                if (Phase != MissionFlowPhase.Selection) throw new InvalidOperationException("Return to selection before unlocking units.");
                if (unitId == null || !roster.TryGetValue(unitId, out var unit)) throw new ArgumentException("Unknown unit.");
                UnlockResult unlocked;
                try { unlocked = profiles.Unlock(operationId, unit.Unlock); }
                catch { NeedsReload = true; throw; }
                Progress = unlocked.Saved.Progress; Recovered |= unlocked.Saved.Recovered;
                return unlocked;
            }
            finally { busy = false; }
        }
        public void ReturnToSelection()
        {
            Enter(true);
            try
            {
                if (Phase != MissionFlowPhase.Results || Receipt == null) throw new InvalidOperationException("Claim the result before leaving.");
                // No delete/ack file: the durable receipt makes the old battle completed on every reopen.
                snapshot = null; Receipt = null; Phase = MissionFlowPhase.Selection;
            }
            finally { busy = false; }
        }
        private MissionEntry Entry(string id)
        {
            if (id == null || !entries.TryGetValue(id, out var entry)) throw new ArgumentException("Unknown mission.");
            return entry;
        }
        private void EnsureReady()
        { if (NeedsReload) throw new InvalidOperationException("Open/recover the flow before continuing."); }
        private void Enter(bool ready)
        {
            if (busy) throw new InvalidOperationException("Reentrant flow operation.");
            if (ready) EnsureReady();
            busy = true;
        }
    }
}
