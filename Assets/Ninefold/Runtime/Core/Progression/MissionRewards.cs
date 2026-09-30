using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Progression
{
    public sealed class ResourceGrant
    {
        public string ResourceId { get; }
        public long Amount { get; }
        public ResourceGrant(string resourceId, long amount)
        {
            RewardRules.Id(resourceId);
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            ResourceId = resourceId; Amount = amount;
        }
    }

    /// <summary>Authored totals: a victory selects exactly one bundle. No implicit additive first-clear bonus.</summary>
    public sealed class MissionRewardPolicy
    {
        public string MissionId { get; }
        public string Revision { get; }
        public IReadOnlyList<ResourceGrant> FirstClear { get; }
        public IReadOnlyList<ResourceGrant> Replay { get; }
        public MissionRewardPolicy(string missionId, string revision,
            IEnumerable<ResourceGrant> firstClear, IEnumerable<ResourceGrant> replay)
        {
            RewardRules.Id(missionId); RewardRules.Id(revision);
            MissionId = missionId; Revision = revision;
            FirstClear = RewardRules.Bundle(firstClear); Replay = RewardRules.Bundle(replay);
        }
    }

    public sealed class MissionClaim
    {
        public string AttemptId { get; }
        public string MissionId { get; }
        public string ResultFingerprint { get; }
        public MissionOutcome Outcome { get; }
        public bool IsFirstClear { get; }
        public string PolicyRevision { get; }
        public IReadOnlyList<ResourceGrant> Grants { get; }
        internal MissionClaim(string attempt, string mission, string fingerprint, MissionOutcome outcome,
            bool firstClear, string revision, IEnumerable<ResourceGrant> grants)
        {
            RewardRules.Id(attempt); RewardRules.Id(mission); RewardRules.Id(revision);
            if (fingerprint == null || fingerprint.Length != 64 || fingerprint.Any(c => !Uri.IsHexDigit(c)))
                throw new ArgumentException("Invalid result fingerprint.");
            if (!Enum.IsDefined(typeof(MissionOutcome), outcome)) throw new ArgumentOutOfRangeException(nameof(outcome));
            AttemptId = attempt; MissionId = mission; ResultFingerprint = fingerprint; Outcome = outcome;
            IsFirstClear = firstClear; PolicyRevision = revision; Grants = RewardRules.Bundle(grants);
            if (outcome == MissionOutcome.Defeat && (firstClear || Grants.Count != 0))
                throw new ArgumentException("Defeat cannot clear a mission or award resources.");
        }
    }

    public sealed class MissionProgress
    {
        public string MissionId { get; }
        public long VictoryCount { get; }
        public string FirstClearAttemptId { get; }
        internal MissionProgress(string mission, MissionClaim[] victories)
        { MissionId = mission; VictoryCount = victories.LongLength; FirstClearAttemptId = victories[0].AttemptId; }
    }

    /// <summary>Immutable reward and unlock ledger. Balances and completions are derived from the same receipts.</summary>
    public sealed class PlayerProgress
    {
        public string ProfileId { get; }
        public IReadOnlyList<MissionClaim> Claims { get; }
        public IReadOnlyList<string> InitialUnits { get; }
        public IReadOnlyList<string> OwnedUnits { get; }
        public IReadOnlyList<UnitUnlockReceipt> Unlocks { get; }
        public IReadOnlyList<CampaignClaim> CampaignClaims { get; }
        public IReadOnlyList<AdvancementReceipt> AdvancementReceipts { get; }
        public IReadOnlyDictionary<string, UnitAdvancementState> Advancements { get; }
        public IReadOnlyList<CustomizationReceipt> CustomizationReceipts { get; }
        public IReadOnlyDictionary<string, UnitCustomizationState> Customizations { get; }
        public UnitCustomizationState GetCustomization(string unitId)
        {
            if (!Owns(unitId)) throw new ArgumentException("Unit is not owned.");
            return Customizations.TryGetValue(unitId, out var state) ? state : new UnitCustomizationState(unitId, null, CustomizationBonus.None);
        }
        public DeploymentModifiers GetDeploymentModifiers(string unitId)
            => DeploymentModifiers.Combine(GetAdvancement(unitId).Bonus, GetCustomization(unitId).Bonus);
        public UnitAdvancementState GetAdvancement(string unitId)
        {
            if (!Owns(unitId)) throw new ArgumentException("Unit is not owned.");
            return Advancements.TryGetValue(unitId, out var state) ? state : new UnitAdvancementState(unitId, 0, AdvancementBonus.None);
        }
        public long Generation => Claims.Count + Unlocks.Count + CampaignClaims.Count + AdvancementReceipts.Count + CustomizationReceipts.Count + 1L;
        public bool Owns(string unitId) => OwnedUnits.Contains(unitId, StringComparer.Ordinal);
        public IReadOnlyDictionary<string, long> Balances { get; }
        public IReadOnlyDictionary<string, MissionProgress> Missions { get; }
        internal PlayerProgress(string profileId, IEnumerable<MissionClaim> claims, IEnumerable<string> initialUnits = null, IEnumerable<UnitUnlockReceipt> unlocks = null, IEnumerable<CampaignClaim> campaignClaims = null, IEnumerable<AdvancementReceipt> advancementReceipts = null, IEnumerable<CustomizationReceipt> customizationReceipts = null)
        {
            RewardRules.Id(profileId); ProfileId = profileId;
            var array = claims.ToArray();
            var initial = (initialUnits ?? Array.Empty<string>()).ToArray();
            var spent = (unlocks ?? Array.Empty<UnitUnlockReceipt>()).ToArray();
            var completions = (campaignClaims ?? Array.Empty<CampaignClaim>()).ToArray();
            if (completions.Any(c => c == null) || completions.Length + array.Length + spent.Length > RewardRules.MaximumClaims ||
                completions.Select(c => c.CampaignId).Distinct(StringComparer.Ordinal).Count() != completions.Length || completions.Count(c => c.IsStarterBonus) > 1)
                throw new ArgumentException("Invalid campaign receipt history.");
            CampaignClaims = Array.AsReadOnly(completions);
            var advances = (advancementReceipts ?? Array.Empty<AdvancementReceipt>()).ToArray();
            if (advances.Any(a => a == null) || array.Length + spent.Length + completions.Length + advances.Length > RewardRules.MaximumClaims ||
                advances.Select(a => a.OperationId).Distinct(StringComparer.Ordinal).Count() != advances.Length)
                throw new ArgumentException("Invalid advancement receipt history.");
            AdvancementReceipts = Array.AsReadOnly(advances);
            var choices = (customizationReceipts ?? Array.Empty<CustomizationReceipt>()).ToArray();
            if (choices.Any(c => c == null) || array.Length + spent.Length + completions.Length + advances.Length + choices.Length > RewardRules.MaximumClaims ||
                choices.Select(c => c.OperationId).Distinct(StringComparer.Ordinal).Count() != choices.Length)
                throw new ArgumentException("Invalid customization receipt history.");
            CustomizationReceipts = Array.AsReadOnly(choices);
            foreach (string id in initial) RewardRules.Id(id);
            if (initial.Length > 4096 || initial.Distinct(StringComparer.Ordinal).Count() != initial.Length)
                throw new ArgumentException("Invalid initial ownership.");
            if (array.Length + spent.Length > RewardRules.MaximumClaims || spent.Any(u => u == null))
                throw new InvalidOperationException("Invalid or full transaction ledger.");
            if (spent.Select(u => u.OperationId).Distinct(StringComparer.Ordinal).Count() != spent.Length)
                throw new ArgumentException("Duplicate unlock operation.");
            var owned = initial.Concat(spent.Select(u => u.UnitId)).Concat(completions.SelectMany(c => c.GrantedUnits)).ToArray();
            if (owned.Distinct(StringComparer.Ordinal).Count() != owned.Length) throw new ArgumentException("Unit unlocked more than once.");
            InitialUnits = Array.AsReadOnly(initial); OwnedUnits = Array.AsReadOnly(owned); Unlocks = Array.AsReadOnly(spent);
            if (array.Length > RewardRules.MaximumClaims) throw new InvalidOperationException("Claim ledger requires migration; never discard receipts.");
            if (array.Select(c => c.AttemptId).Distinct(StringComparer.Ordinal).Count() != array.Length)
                throw new ArgumentException("Duplicate attempt receipt.");
            var advancement = new Dictionary<string, UnitAdvancementState>(StringComparer.Ordinal);
            foreach (var a in advances)
            {
                if (!Owns(a.UnitId)) throw new ArgumentException("Advancement requires ownership.");
                advancement.TryGetValue(a.UnitId, out var prior);
                if (a.Rank != (prior?.Rank ?? 0) + 1 || (prior != null && (!prior.Bonus.Within(a.TotalBonus) || prior.Bonus.Same(a.TotalBonus))))
                    throw new ArgumentException("Invalid advancement sequence.");
                advancement[a.UnitId] = new UnitAdvancementState(a.UnitId, a.Rank, a.TotalBonus);
            }
            Advancements = new ReadOnlyDictionary<string, UnitAdvancementState>(advancement);
            var customization = new Dictionary<string, UnitCustomizationState>(StringComparer.Ordinal);
            foreach (var c in choices)
            {
                if (!Owns(c.UnitId)) throw new ArgumentException("Customization requires ownership.");
                customization[c.UnitId] = new UnitCustomizationState(c.UnitId, c.OptionId, c.Bonus);
            }
            Customizations = new ReadOnlyDictionary<string, UnitCustomizationState>(customization);
            foreach (string unit in customization.Keys) GetDeploymentModifiers(unit); // Validate combined bounds.

            var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var missions = new Dictionary<string, MissionProgress>(StringComparer.Ordinal);
            foreach (var group in array.Where(c => c.Outcome == MissionOutcome.Victory).GroupBy(c => c.MissionId, StringComparer.Ordinal))
            {
                var victories = group.ToArray();
                if (!victories[0].IsFirstClear || victories.Skip(1).Any(c => c.IsFirstClear))
                    throw new ArgumentException("Invalid first-clear history.");
                missions.Add(group.Key, new MissionProgress(group.Key, victories));
            }
            foreach (var completion in completions)
                if (completion.RequiredMissions.Any(id => !missions.ContainsKey(id))) throw new ArgumentException("Campaign receipt missing required mission wins.");
            foreach (var grant in array.SelectMany(c => c.Grants).Concat(completions.SelectMany(c => c.Grants)))
            {
                totals.TryGetValue(grant.ResourceId, out decimal amount);
                totals[grant.ResourceId] = checked(amount + grant.Amount);
            }
            foreach (var unlock in spent)
            {
                totals.TryGetValue(unlock.FragmentResourceId, out decimal amount);
                if (amount < unlock.Cost) throw new ArgumentException("Unlock ledger exceeds earned fragments.");
                totals[unlock.FragmentResourceId] = amount - unlock.Cost;
            }
            foreach (var advance in advances)
            {
                totals.TryGetValue(advance.FragmentResourceId, out decimal amount);
                if (amount < advance.Cost) throw new ArgumentException("Advancement exceeds earned fragments.");
                totals[advance.FragmentResourceId] = amount - advance.Cost;
            }
            var balances = totals.ToDictionary(p => p.Key, p => checked((long)p.Value), StringComparer.Ordinal);
            Claims = Array.AsReadOnly(array);
            Balances = new ReadOnlyDictionary<string, long>(balances);
            Missions = new ReadOnlyDictionary<string, MissionProgress>(missions);
        }
        public long Balance(string resourceId) => Balances.TryGetValue(resourceId, out long value) ? value : 0;
    }

    internal static class RewardRules
    {
        internal const int MaximumClaims = 100000;
        internal static void Id(string id)
        { if (string.IsNullOrWhiteSpace(id) || id.Length > 1024) throw new ArgumentException("Identifier required (maximum 1024 characters)."); }
        internal static IReadOnlyList<ResourceGrant> Bundle(IEnumerable<ResourceGrant> grants)
        {
            if (grants == null) throw new ArgumentNullException(nameof(grants));
            var array = grants.ToArray();
            if (array.Length > 4096 || array.Any(g => g == null) || array.Select(g => g.ResourceId).Distinct(StringComparer.Ordinal).Count() != array.Length)
                throw new ArgumentException("Invalid or duplicate resource grant.");
            return Array.AsReadOnly(array);
        }
    }
}
