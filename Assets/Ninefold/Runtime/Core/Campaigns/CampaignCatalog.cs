using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ninefold.Core.Flow;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Campaigns
{
    public sealed class CampaignProgress
    {
        public string CampaignId { get; }
        public int CompletedMissions { get; }
        public int TotalMissions { get; }
        public bool CanEnter { get; }
        public bool IsComplete { get; }
        public bool RewardsClaimed { get; }
        public IReadOnlyList<string> AvailableMissions { get; }
        internal CampaignProgress(CampaignDefinition definition, PlayerProgress progress, bool canEnter, IEnumerable<string> available)
        {
            CampaignId = definition.Id; TotalMissions = definition.Missions.Count; CanEnter = canEnter;
            CompletedMissions = definition.Missions.Count(m => progress.Missions.ContainsKey(m.MissionId));
            RewardsClaimed = progress.CampaignClaims.Any(c => c.CampaignId == definition.Id);
            IsComplete = RewardsClaimed || definition.Missions.Where(m => m.Required).All(m => progress.Missions.ContainsKey(m.MissionId));
            AvailableMissions = Array.AsReadOnly(available.ToArray());
        }
    }
    /// <summary>Validated authored campaign graph and unit metadata. Contains no player state.</summary>
    public sealed class CampaignCatalog
    {
        private readonly Dictionary<string, CampaignDefinition> campaigns;
        private readonly Dictionary<string, CampaignDefinition> byMission = new Dictionary<string, CampaignDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, RosterUnit> units;
        private readonly Dictionary<string, MissionEntry> entries;
        public IReadOnlyList<CampaignDefinition> Campaigns { get; }
        public CampaignCatalog(IEnumerable<CampaignDefinition> definitions, IEnumerable<MissionEntry> missions, IEnumerable<RosterUnit> roster)
        {
            if (definitions == null || missions == null || roster == null) throw new ArgumentNullException("Catalogs required.");
            var array = definitions.ToArray();
            if (array.Any(c => c == null) || array.Length > 4096 || array.Count(c => c.StarterBonus != null) > 1) throw new ArgumentException("At most one starter campaign allowed.");
            campaigns = array.ToDictionary(c => c.Id, StringComparer.Ordinal);
            units = roster.ToDictionary(u => u.Id, StringComparer.Ordinal); entries = missions.ToDictionary(m => m.Id, StringComparer.Ordinal);
            Campaigns = Array.AsReadOnly(array);
            foreach (var campaign in array)
            {
                foreach (var node in campaign.Missions)
                {
                    if (!entries.TryGetValue(node.MissionId, out var entry) || !entry.IsCampaign || entry.FactionId != campaign.FactionId || byMission.ContainsKey(node.MissionId))
                        throw new ArgumentException("Campaign missions require unique matching authored entries.");
                    byMission.Add(node.MissionId, campaign);
                }
                var bonus = campaign.StarterBonus;
                if (bonus == null) continue;
                if (!units.TryGetValue(bonus.ApexUnitId, out var apex) || !apex.IsApex || apex.FactionId != campaign.FactionId)
                    throw new ArgumentException("Starter Apex must belong to the starter faction.");
                var factions = new HashSet<string>(StringComparer.Ordinal);
                foreach (var candidate in bonus.Candidates)
                {
                    if (!campaigns.TryGetValue(candidate.CampaignId, out var next) || next.FactionId == campaign.FactionId || !factions.Add(next.FactionId) ||
                        candidate.StandardUnits.Any(id => !units.TryGetValue(id, out var unit) || unit.IsApex || unit.FactionId != next.FactionId))
                        throw new ArgumentException("Next campaign requires three standard units of a different faction.");
                }
            }
            if (array.Length != 0 && entries.Values.Any(m => m.IsCampaign && !byMission.ContainsKey(m.Id)))
                throw new ArgumentException("All campaign mission entries must belong to the catalog.");
        }
        public CampaignDefinition Get(string id)
            => id != null && campaigns.TryGetValue(id, out var c) ? c : throw new ArgumentException("Unknown campaign.");
        public bool ContainsMission(string id) => byMission.ContainsKey(id);
        private bool CanEnter(CampaignDefinition c, PlayerProgress p)
            => units.Values.Count(u => !u.IsApex && u.FactionId == c.FactionId && p.Owns(u.Id)) >= 3;
        public bool IsMissionAvailable(string id, PlayerProgress progress)
        {
            if (!byMission.TryGetValue(id, out var campaign)) return true; // Standalone mode uses its own gates.
            if (!CanEnter(campaign, progress)) return false;
            if (progress.Missions.ContainsKey(id)) return true; // Completed missions remain replayable.
            var node = campaign.Missions.First(m => m.MissionId == id);
            return node.Prerequisites.All(progress.Missions.ContainsKey) && entries[id].IsAvailable(progress);
        }
        public CampaignProgress Inspect(string id, PlayerProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            var c = Get(id);
            return new CampaignProgress(c, progress, CanEnter(c, progress), c.Missions.Where(m => IsMissionAvailable(m.MissionId, progress)).Select(m => m.MissionId));
        }
        internal CampaignClaim Prepare(string id, PlayerProgress progress)
        {
            var c = Get(id);
            if (!Inspect(id, progress).IsComplete) throw new InvalidOperationException("Campaign has unfinished required missions.");
            string nextId = null;
            var granted = new List<string>();
            if (c.StarterBonus != null)
            {
                if (progress.CampaignClaims.Any(r => r.IsStarterBonus)) throw new InvalidOperationException("Starter campaign bonus already consumed.");
                var candidates = c.StarterBonus.Candidates.Where(b => !CanEnter(Get(b.CampaignId), progress)).ToArray();
                if (candidates.Length == 0) candidates = c.StarterBonus.Candidates.ToArray();
                candidates = candidates.OrderBy(b => b.CampaignId, StringComparer.Ordinal).ToArray();
                // Stable, account-specific selection: no player target choice or retry reroll.
                using var sha = SHA256.Create();
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(progress.ProfileId.Length + ":" + progress.ProfileId + c.Id));
                int index = 0; foreach (byte value in digest) index = (index * 256 + value) % candidates.Length;
                var next = candidates[index]; nextId = next.CampaignId;
                granted.Add(c.StarterBonus.ApexUnitId); granted.AddRange(next.StandardUnits);
            }
            return new CampaignClaim(c.Id, c.Revision, c.Missions.Where(m => m.Required).Select(m => m.MissionId),
                c.CompletionRewards, granted.Where(id2 => !progress.Owns(id2)), c.StarterBonus != null, nextId);
        }
    }
}
