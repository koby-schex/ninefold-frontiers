using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Progression
{
    /// <summary>Cumulative bonus over authored base stats; 10% is a provisional engineering ceiling, not locked balance.</summary>
    public sealed class AdvancementBonus
    {
        public const int ProvisionalMaximumBasisPoints = 1000;
        public int Health { get; }
        public int Armor { get; }
        public int Power { get; }
        public static AdvancementBonus None { get; } = new AdvancementBonus(0, 0, 0);
        public AdvancementBonus(int health, int armor, int power)
        {
            if (health < 0 || armor < 0 || power < 0 || health > ProvisionalMaximumBasisPoints ||
                armor > ProvisionalMaximumBasisPoints || power > ProvisionalMaximumBasisPoints)
                throw new ArgumentOutOfRangeException(nameof(health), "Advancement exceeds provisional bonus bounds.");
            Health = health; Armor = armor; Power = power;
        }
        internal bool Same(AdvancementBonus other) => other != null && Health == other.Health && Armor == other.Armor && Power == other.Power;
        internal bool Within(AdvancementBonus cap) => Health <= cap.Health && Armor <= cap.Armor && Power <= cap.Power;
        internal static int Scale(int value, int basisPoints) => checked((int)decimal.Floor(value * (10000m + basisPoints) / 10000m));
    }
    public sealed class AdvancementStep
    {
        public long FragmentCost { get; }
        public AdvancementBonus TotalBonus { get; }
        public AdvancementStep(long fragmentCost, AdvancementBonus totalBonus)
        {
            if (fragmentCost <= 0) throw new ArgumentOutOfRangeException(nameof(fragmentCost));
            FragmentCost = fragmentCost; TotalBonus = totalBonus ?? throw new ArgumentNullException(nameof(totalBonus));
        }
    }
    public sealed class UnitAdvancementDefinition
    {
        public string UnitId { get; }
        public string FragmentResourceId { get; }
        public string Revision { get; }
        public AdvancementBonus Cap { get; }
        public IReadOnlyList<AdvancementStep> Steps { get; }
        public UnitAdvancementDefinition(string unitId, string fragmentResourceId, string revision,
            AdvancementBonus cap, IEnumerable<AdvancementStep> steps)
        {
            RewardRules.Id(unitId); RewardRules.Id(fragmentResourceId); RewardRules.Id(revision);
            UnitId = unitId; FragmentResourceId = fragmentResourceId; Revision = revision;
            Cap = cap ?? throw new ArgumentNullException(nameof(cap));
            var array = steps?.ToArray() ?? throw new ArgumentNullException(nameof(steps));
            if (array.Length == 0 || array.Length > 4096) throw new ArgumentException("Bounded advancement steps required.");
            var previous = AdvancementBonus.None;
            foreach (var step in array)
            {
                if (step == null || !step.TotalBonus.Within(Cap) || !previous.Within(step.TotalBonus) || previous.Same(step.TotalBonus))
                    throw new ArgumentException("Each cumulative step must improve within its cap without reducing a stat.");
                previous = step.TotalBonus;
            }
            Steps = Array.AsReadOnly(array);
        }
    }
    public sealed class AdvancementReceipt
    {
        public string OperationId { get; }
        public string UnitId { get; }
        public int Rank { get; }
        public string FragmentResourceId { get; }
        public long Cost { get; }
        public string DefinitionRevision { get; }
        public AdvancementBonus TotalBonus { get; }
        internal AdvancementReceipt(string operation, string unit, int rank, string resource, long cost, string revision, AdvancementBonus bonus)
        {
            RewardRules.Id(operation); RewardRules.Id(unit); RewardRules.Id(resource); RewardRules.Id(revision);
            if (rank < 1 || rank > 4096 || cost <= 0 || bonus == null || bonus.Same(AdvancementBonus.None)) throw new ArgumentException("Invalid advancement receipt.");
            OperationId = operation; UnitId = unit; Rank = rank; FragmentResourceId = resource; Cost = cost;
            DefinitionRevision = revision; TotalBonus = bonus;
        }
    }
    public sealed class UnitAdvancementState
    {
        public string UnitId { get; }
        public int Rank { get; }
        public AdvancementBonus Bonus { get; }
        internal UnitAdvancementState(string unit, int rank, AdvancementBonus bonus) { UnitId = unit; Rank = rank; Bonus = bonus; }
    }
    public sealed class AdvancementResult
    {
        public LoadedProgress Saved { get; }
        public AdvancementReceipt Receipt { get; }
        public bool AlreadyApplied { get; }
        internal AdvancementResult(LoadedProgress saved, AdvancementReceipt receipt, bool duplicate)
        { Saved = saved; Receipt = receipt; AlreadyApplied = duplicate; }
    }
}
