using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Progression
{
    /// <summary>Signed basis-point trade-off. Limits and equal-weight budget are provisional, not final balance.</summary>
    public sealed class CustomizationBonus
    {
        public const int ProvisionalMaximumBasisPoints = 500;
        public int Health { get; }
        public int Armor { get; }
        public int Power { get; }
        public bool IsNeutral => Health == 0 && Armor == 0 && Power == 0;
        public static CustomizationBonus None { get; } = new CustomizationBonus(0, 0, 0);
        public CustomizationBonus(int health, int armor, int power)
        {
            if (health < -500 || health > 500 || armor < -500 || armor > 500 || power < -500 || power > 500)
                throw new ArgumentOutOfRangeException(nameof(health));
            if (health + armor + power != 0) throw new ArgumentException("Customization must trade equal basis points between supported stats.");
            Health = health; Armor = armor; Power = power;
        }
    }
    public sealed class CustomizationOption
    {
        public string Id { get; }
        public CustomizationBonus Bonus { get; }
        public CustomizationOption(string id, CustomizationBonus bonus)
        {
            RewardRules.Id(id);
            if (bonus == null || bonus.IsNeutral) throw new ArgumentException("A choice must contain a trade-off; reset represents neutral.");
            Id = id; Bonus = bonus;
        }
    }
    public sealed class UnitCustomizationDefinition
    {
        public string UnitId { get; }
        public string Revision { get; }
        public IReadOnlyList<CustomizationOption> Options { get; }
        public UnitCustomizationDefinition(string unitId, string revision, IEnumerable<CustomizationOption> options)
        {
            RewardRules.Id(unitId); RewardRules.Id(revision); UnitId = unitId; Revision = revision;
            var array = options?.ToArray() ?? throw new ArgumentNullException(nameof(options));
            if (array.Length == 0 || array.Length > 4096 || array.Any(o => o == null) || array.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() != array.Length)
                throw new ArgumentException("Distinct authored customization options required.");
            Options = Array.AsReadOnly(array);
        }
    }
    public sealed class CustomizationReceipt
    {
        public string OperationId { get; }
        public string UnitId { get; }
        public string OptionId { get; } // null is explicit reset
        public string DefinitionRevision { get; }
        public CustomizationBonus Bonus { get; }
        internal CustomizationReceipt(string operation, string unit, string option, string revision, CustomizationBonus bonus)
        {
            RewardRules.Id(operation); RewardRules.Id(unit); RewardRules.Id(revision);
            if (option != null) RewardRules.Id(option);
            if (bonus == null || ((option == null) != bonus.IsNeutral)) throw new ArgumentException("Invalid customization receipt.");
            OperationId = operation; UnitId = unit; OptionId = option; DefinitionRevision = revision; Bonus = bonus;
        }
    }
    public sealed class UnitCustomizationState
    {
        public string UnitId { get; }
        public string OptionId { get; }
        public CustomizationBonus Bonus { get; }
        internal UnitCustomizationState(string unit, string option, CustomizationBonus bonus) { UnitId = unit; OptionId = option; Bonus = bonus; }
    }
    public sealed class CustomizationResult
    {
        public LoadedProgress Saved { get; }
        public CustomizationReceipt Receipt { get; }
        public bool AlreadyApplied { get; }
        internal CustomizationResult(LoadedProgress saved, CustomizationReceipt receipt, bool duplicate)
        { Saved = saved; Receipt = receipt; AlreadyApplied = duplicate; }
    }
    /// <summary>Add percentages over the original base once; never multiply already improved stats.</summary>
    public sealed class DeploymentModifiers
    {
        public const int ProvisionalMaximumBasisPoints = 1500;
        public const int ProvisionalMinimumBasisPoints = -500;
        public int Health { get; }
        public int Armor { get; }
        public int Power { get; }
        public bool IsNeutral => Health == 0 && Armor == 0 && Power == 0;
        private DeploymentModifiers(int health, int armor, int power)
        {
            foreach (int stat in new[] { health, armor, power })
                if (stat < ProvisionalMinimumBasisPoints || stat > ProvisionalMaximumBasisPoints) throw new ArgumentOutOfRangeException(nameof(health));
            Health = health; Armor = armor; Power = power;
        }
        public static DeploymentModifiers Combine(AdvancementBonus advancement, CustomizationBonus customization)
        {
            if (advancement == null || customization == null) throw new ArgumentNullException("Both stat components required.");
            return new DeploymentModifiers(checked(advancement.Health + customization.Health), checked(advancement.Armor + customization.Armor), checked(advancement.Power + customization.Power));
        }
        internal static int Scale(int value, int basisPoints)
            => checked((int)(value + decimal.Truncate(value * (decimal)basisPoints / 10000m)));
    }
}
