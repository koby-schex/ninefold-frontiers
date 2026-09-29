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

    /// <summary>Immutable earned-only ledger. Balances and completions are derived from the same receipts.</summary>
    public sealed class PlayerProgress
    {
        public string ProfileId { get; }
        public IReadOnlyList<MissionClaim> Claims { get; }
        public IReadOnlyDictionary<string, long> Balances { get; }
        public IReadOnlyDictionary<string, MissionProgress> Missions { get; }
        internal PlayerProgress(string profileId, IEnumerable<MissionClaim> claims)
        {
            RewardRules.Id(profileId); ProfileId = profileId;
            var array = claims.ToArray();
            if (array.Length > RewardRules.MaximumClaims) throw new InvalidOperationException("Claim ledger requires migration; never discard receipts.");
            if (array.Select(c => c.AttemptId).Distinct(StringComparer.Ordinal).Count() != array.Length)
                throw new ArgumentException("Duplicate attempt receipt.");
            var balances = new Dictionary<string, long>(StringComparer.Ordinal);
            var missions = new Dictionary<string, MissionProgress>(StringComparer.Ordinal);
            foreach (var group in array.Where(c => c.Outcome == MissionOutcome.Victory).GroupBy(c => c.MissionId, StringComparer.Ordinal))
            {
                var victories = group.ToArray();
                if (!victories[0].IsFirstClear || victories.Skip(1).Any(c => c.IsFirstClear))
                    throw new ArgumentException("Invalid first-clear history.");
                missions.Add(group.Key, new MissionProgress(group.Key, victories));
            }
            foreach (var grant in array.SelectMany(c => c.Grants))
            {
                balances.TryGetValue(grant.ResourceId, out long amount);
                balances[grant.ResourceId] = checked(amount + grant.Amount);
            }
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
