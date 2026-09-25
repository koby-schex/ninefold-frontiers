using System;

namespace Ninefold.Core.Combat
{
    /// <summary>Validated modifiers supplied by future geometry/status resolution.</summary>
    public sealed class DamageMitigation
    {
        public static DamageMitigation None { get; } = new DamageMitigation(0m, 0m);
        public decimal CoverReduction { get; }
        public decimal TemporaryReduction { get; }

        public DamageMitigation(decimal coverReduction, decimal temporaryReduction)
        {
            if (coverReduction < 0m || coverReduction > 1m)
                throw new ArgumentOutOfRangeException(nameof(coverReduction));
            if (temporaryReduction < 0m || temporaryReduction > 1m)
                throw new ArgumentOutOfRangeException(nameof(temporaryReduction));
            CoverReduction = coverReduction;
            TemporaryReduction = temporaryReduction;
        }
    }

    /// <summary>Immutable tuning policy. Provisional defaults are not final unit balance.</summary>
    public sealed class DamageRules
    {
        public static DamageRules Provisional { get; } = new DamageRules(100, 0.70m, true);
        public int ArmorScale { get; }
        public decimal MaximumOrdinaryReduction { get; }
        public bool MinimumOneDamage { get; }

        public DamageRules(int armorScale, decimal maximumOrdinaryReduction, bool minimumOneDamage)
        {
            if (armorScale <= 0)
                throw new ArgumentOutOfRangeException(nameof(armorScale));
            if (maximumOrdinaryReduction < 0m || maximumOrdinaryReduction > 1m)
                throw new ArgumentOutOfRangeException(nameof(maximumOrdinaryReduction));
            ArmorScale = armorScale;
            MaximumOrdinaryReduction = maximumOrdinaryReduction;
            MinimumOneDamage = minimumOneDamage;
        }

        public int Calculate(int rawDamage, int armor, DamageMitigation mitigation)
        {
            if (rawDamage < 0) throw new ArgumentOutOfRangeException(nameof(rawDamage));
            if (armor < 0) throw new ArgumentOutOfRangeException(nameof(armor));
            if (mitigation == null) throw new ArgumentNullException(nameof(mitigation));
            if (rawDamage == 0) return 0;
            // Divide last to retain exact halfway cases such as 36 * 100 * .75 / 120.
            decimal reduced = rawDamage * (decimal)ArmorScale
                * (1m - mitigation.CoverReduction) * (1m - mitigation.TemporaryReduction)
                / ((decimal)ArmorScale + armor);
            reduced = Math.Max(rawDamage * (1m - MaximumOrdinaryReduction), reduced);
            int damage = decimal.ToInt32(decimal.Round(reduced, 0, MidpointRounding.AwayFromZero));
            return MinimumOneDamage ? Math.Max(1, damage) : damage;
        }
    }
}
