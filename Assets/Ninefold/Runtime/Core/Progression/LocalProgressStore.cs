using System;
using System.IO;
using System.Linq;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;

namespace Ninefold.Core.Progression
{
    public interface IProgressSaveFiles
    {
        byte[] Read(int slot);
        void WriteDurable(int slot, byte[] bytes);
    }
    public sealed class DirectoryProgressSaveFiles : IProgressSaveFiles
    {
        private readonly string directory;
        public DirectoryProgressSaveFiles(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory required.");
            this.directory = Path.GetFullPath(directory);
        }
        private string Name(int slot)
        {
            if (slot < 0 || slot > 1) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, "progress." + slot + ".save");
        }
        public byte[] Read(int slot)
        {
            try
            {
                using var file = new FileStream(Name(slot), FileMode.Open, FileAccess.Read, FileShare.Read);
                if (file.Length > BattleSave.MaximumBytes) throw new InvalidDataException("Progress exceeds size limit.");
                using var bytes = new MemoryStream(); file.CopyTo(bytes); return bytes.ToArray();
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }
        public void WriteDurable(int slot, byte[] bytes)
        {
            Directory.CreateDirectory(directory);
            using var file = new FileStream(Name(slot), FileMode.Create, FileAccess.Write, FileShare.None);
            file.Write(bytes, 0, bytes.Length); file.Flush(true);
        }
    }
    public sealed class LoadedProgress
    {
        public PlayerProgress Progress { get; }
        public long Generation => Progress.Generation;
        public bool Recovered { get; }
        internal LoadedProgress(PlayerProgress progress, bool recovered) { Progress = progress; Recovered = recovered; }
    }
    public sealed class ClaimResult
    {
        public LoadedProgress Saved { get; }
        public MissionClaim Receipt { get; }
        public bool AlreadyClaimed { get; }
        internal ClaimResult(LoadedProgress saved, MissionClaim receipt, bool duplicate)
        { Saved = saved; Receipt = receipt; AlreadyClaimed = duplicate; }
    }

    /// <summary>One store/writer per profile directory. Reloads durable state for every claim; never saves a caller's stale snapshot.</summary>
    public sealed class LocalProgressStore
    {
        private readonly IProgressSaveFiles files;
        private readonly string profileId;
        private readonly object gate = new object();
        private sealed class Record { internal int Slot; internal PlayerProgress Progress; }
        public LocalProgressStore(IProgressSaveFiles files, string profileId)
        {
            this.files = files ?? throw new ArgumentNullException(nameof(files));
            RewardRules.Id(profileId); this.profileId = profileId;
        }
        public LoadedProgress Load()
        {
            lock (gate)
            {
                var record = Latest(out bool recovered);
                return record == null ? null : new LoadedProgress(record.Progress, recovered);
            }
        }
        public LoadedProgress Create(System.Collections.Generic.IEnumerable<string> initiallyOwned = null)
        {
            lock (gate)
            {
                if (Latest(out _) != null) throw new InvalidOperationException("Profile already exists.");
                var progress = new PlayerProgress(profileId, Array.Empty<MissionClaim>(), initiallyOwned);
                files.WriteDurable(0, ProgressSave.Encode(progress));
                return new LoadedProgress(progress, false);
            }
        }
        public ClaimResult Claim(BattleResult result, MissionRewardPolicy policy)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            lock (gate)
            {
                var previous = Latest(out bool recovered) ?? throw new InvalidOperationException("Create or recover the profile before claiming rewards.");
                string fingerprint = ProgressSave.Fingerprint(result);
                var receipt = previous.Progress.Claims.FirstOrDefault(c => c.AttemptId == result.AttemptId);
                if (receipt != null)
                {
                    if (receipt.ResultFingerprint != fingerprint) throw new InvalidOperationException("Attempt identifier reused for a different result.");
                    // Original receipt remains authoritative even if the policy was removed or changed.
                    return new ClaimResult(new LoadedProgress(previous.Progress, recovered), receipt, true);
                }
                if (policy == null) throw new ArgumentNullException(nameof(policy));
                if (policy.MissionId != result.MissionId) throw new ArgumentException("Reward policy belongs to another mission.");
                bool victory = result.Outcome == MissionOutcome.Victory;
                bool first = victory && !previous.Progress.Missions.ContainsKey(result.MissionId);
                var grants = victory ? (first ? policy.FirstClear : policy.Replay) : Array.Empty<ResourceGrant>();
                receipt = new MissionClaim(result.AttemptId, result.MissionId, fingerprint, result.Outcome, first, policy.Revision, grants);
                var progress = new PlayerProgress(profileId, previous.Progress.Claims.Concat(new[] { receipt }), previous.Progress.InitialUnits, previous.Progress.Unlocks);
                files.WriteDurable(1 - previous.Slot, ProgressSave.Encode(progress));
                // Nothing is published until the complete ledger has been flushed.
                return new ClaimResult(new LoadedProgress(progress, recovered), receipt, false);
            }
        }
        public UnlockResult Unlock(string operationId, UnitUnlockDefinition definition)
        {
            RewardRules.Id(operationId);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            lock (gate)
            {
                var previous = Latest(out bool recovered) ?? throw new InvalidOperationException("Profile absent.");
                var prior = previous.Progress.Unlocks.FirstOrDefault(u => u.OperationId == operationId);
                if (prior != null)
                {
                    if (prior.UnitId != definition.UnitId) throw new InvalidOperationException("Unlock operation reused for another unit.");
                    return new UnlockResult(new LoadedProgress(previous.Progress, recovered), prior, true);
                }
                if (previous.Progress.Owns(definition.UnitId)) throw new InvalidOperationException("Unit already owned.");
                if (previous.Progress.Balance(definition.FragmentResourceId) < definition.Cost) throw new InvalidOperationException("Insufficient unit fragments.");
                var receipt = new UnitUnlockReceipt(operationId, definition);
                var progress = new PlayerProgress(profileId, previous.Progress.Claims, previous.Progress.InitialUnits,
                    previous.Progress.Unlocks.Concat(new[] { receipt }));
                files.WriteDurable(1 - previous.Slot, ProgressSave.Encode(progress));
                return new UnlockResult(new LoadedProgress(progress, recovered), receipt, false);
            }
        }
        private Record Latest(out bool recovered)
        {
            bool damaged = false;
            var records = new Record[2];
            for (int slot = 0; slot < 2; slot++)
            {
                try
                {
                    var bytes = files.Read(slot);
                    if (bytes != null) records[slot] = new Record { Slot = slot, Progress = ProgressSave.Decode(bytes, profileId) };
                }
                catch (InvalidDataException) { damaged = true; }
                catch (EndOfStreamException) { damaged = true; }
            }
            var ordered = records.Where(r => r != null).OrderByDescending(r => r.Progress.Generation).ToArray();
            SaveIO.Require(ordered.Length != 0 || !damaged, "No valid progress save remains; do not reset the profile.");
            if (ordered.Length == 2)
            {
                var newer = ordered[0].Progress; var older = ordered[1].Progress;
                SaveIO.Require(newer.Generation == older.Generation + 1 && newer.Claims.Count >= older.Claims.Count &&
                    newer.Unlocks.Count >= older.Unlocks.Count, "Conflicting progress generations.");
                var prefix = new PlayerProgress(profileId, newer.Claims.Take(older.Claims.Count), newer.InitialUnits, newer.Unlocks.Take(older.Unlocks.Count));
                SaveIO.Require(ProgressSave.Encode(prefix).SequenceEqual(ProgressSave.Encode(older)), "Conflicting progress histories.");
            }
            recovered = damaged;
            return ordered.FirstOrDefault();
        }
    }
}
