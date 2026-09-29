using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Campaigns
{
    public sealed class CampaignMission
    {
        public string MissionId { get; }
        public IReadOnlyList<string> Prerequisites { get; }
        public bool Required { get; }
        public CampaignMission(string missionId, IEnumerable<string> prerequisites = null, bool required = true)
        {
            RewardRules.Id(missionId); MissionId = missionId; Required = required;
            Prerequisites = CampaignIds.Copy(prerequisites ?? Array.Empty<string>());
        }
    }
    public sealed class NextFactionBundle
    {
        public string CampaignId { get; }
        public IReadOnlyList<string> StandardUnits { get; }
        public NextFactionBundle(string campaignId, IEnumerable<string> standardUnits)
        {
            RewardRules.Id(campaignId); CampaignId = campaignId; StandardUnits = CampaignIds.Copy(standardUnits);
            if (StandardUnits.Count != 3) throw new ArgumentException("Next faction bundle requires three distinct standard units.");
        }
    }
    public sealed class StarterCampaignBonus
    {
        public string ApexUnitId { get; }
        public IReadOnlyList<NextFactionBundle> Candidates { get; }
        public StarterCampaignBonus(string apexUnitId, IEnumerable<NextFactionBundle> candidates)
        {
            RewardRules.Id(apexUnitId); ApexUnitId = apexUnitId;
            var array = candidates?.ToArray() ?? throw new ArgumentNullException(nameof(candidates));
            if (array.Length == 0 || array.Length > 4096 || array.Any(x => x == null) || array.Select(x => x.CampaignId).Distinct(StringComparer.Ordinal).Count() != array.Length)
                throw new ArgumentException("Distinct next-campaign candidates required.");
            Candidates = Array.AsReadOnly(array);
        }
    }
    public sealed class CampaignDefinition
    {
        public string Id { get; }
        public string FactionId { get; }
        public string Revision { get; }
        public IReadOnlyList<CampaignMission> Missions { get; }
        public IReadOnlyList<ResourceGrant> CompletionRewards { get; }
        public StarterCampaignBonus StarterBonus { get; }
        public CampaignDefinition(string id, string factionId, string revision, IEnumerable<CampaignMission> missions,
            IEnumerable<ResourceGrant> completionRewards, StarterCampaignBonus starterBonus = null)
        {
            RewardRules.Id(id); RewardRules.Id(factionId); RewardRules.Id(revision);
            Id = id; FactionId = factionId; Revision = revision; StarterBonus = starterBonus;
            var array = missions?.ToArray() ?? throw new ArgumentNullException(nameof(missions));
            if (array.Length == 0 || array.Length > 4096 || array.Any(m => m == null) || !array.Any(m => m.Required))
                throw new ArgumentException("Campaign needs bounded missions and at least one required mission.");
            var earlier = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in array)
            {
                if (m.Prerequisites.Any(p => !earlier.Contains(p)) || !earlier.Add(m.MissionId))
                    throw new ArgumentException("Prerequisites must be earlier missions; duplicate missions and cycles are invalid.");
            }
            Missions = Array.AsReadOnly(array); CompletionRewards = RewardRules.Bundle(completionRewards);
        }
    }
    internal static class CampaignIds
    {
        internal static IReadOnlyList<string> Copy(IEnumerable<string> values)
        {
            var a = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
            foreach (var id in a) RewardRules.Id(id);
            if (a.Length > 4096 || a.Distinct(StringComparer.Ordinal).Count() != a.Length) throw new ArgumentException("Invalid identifier list.");
            return Array.AsReadOnly(a);
        }
    }
}
