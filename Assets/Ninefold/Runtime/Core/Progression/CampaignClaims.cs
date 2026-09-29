using System;
using System.Collections.Generic;
using Ninefold.Core.Campaigns;

namespace Ninefold.Core.Progression
{
    public sealed class CampaignClaim
    {
        public string CampaignId { get; }
        public string DefinitionRevision { get; }
        public IReadOnlyList<string> RequiredMissions { get; }
        public IReadOnlyList<ResourceGrant> Grants { get; }
        public IReadOnlyList<string> GrantedUnits { get; }
        public bool IsStarterBonus { get; }
        public string NextCampaignId { get; }
        internal CampaignClaim(string id, string revision, IEnumerable<string> required, IEnumerable<ResourceGrant> grants,
            IEnumerable<string> units, bool starter, string next)
        {
            RewardRules.Id(id); RewardRules.Id(revision);
            CampaignId = id; DefinitionRevision = revision;
            RequiredMissions = CampaignIds.Copy(required); Grants = RewardRules.Bundle(grants); GrantedUnits = CampaignIds.Copy(units);
            if (RequiredMissions.Count == 0 || (!starter && (next != null || GrantedUnits.Count != 0)) || (starter && GrantedUnits.Count > 4))
                throw new ArgumentException("Invalid campaign completion receipt.");
            if (starter) RewardRules.Id(next);
            IsStarterBonus = starter; NextCampaignId = next;
        }
    }
    public sealed class CampaignClaimResult
    {
        public LoadedProgress Saved { get; }
        public CampaignClaim Receipt { get; }
        public bool AlreadyClaimed { get; }
        internal CampaignClaimResult(LoadedProgress saved, CampaignClaim receipt, bool duplicate)
        { Saved = saved; Receipt = receipt; AlreadyClaimed = duplicate; }
    }
}
