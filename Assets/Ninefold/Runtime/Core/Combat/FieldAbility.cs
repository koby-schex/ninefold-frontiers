using System;

namespace Ninefold.Core.Combat
{
    public enum AbilityTarget { Enemy, Ally, Self, AllyOrSelf }
    /// <summary>Trusted single-target authored profile: health, status, or both. No caller-supplied effect values.</summary>
    public sealed class FieldAbility
    {
        public AbilitySlot Slot { get; }
        public bool HasHealthEffect { get; }
        public HealthEffectKind Kind { get; }
        public int Amount { get; }
        public decimal Range { get; }
        public bool UsesCover { get; }
        public string StatusId { get; }
        public AbilityTarget Target { get; }
        public FieldAbility(AbilitySlot slot, HealthEffectKind kind, int amount, decimal range, bool usesCover,
            string statusId = null, AbilityTarget? target = null)
        {
            Validate(slot, range);
            if (!Enum.IsDefined(typeof(HealthEffectKind),kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (kind == HealthEffectKind.Healing && usesCover) throw new ArgumentException("Healing does not use cover.");
            Target = target ?? (kind == HealthEffectKind.Damage ? AbilityTarget.Enemy : AbilityTarget.AllyOrSelf);
            if (!Enum.IsDefined(typeof(AbilityTarget),Target) || (kind == HealthEffectKind.Damage) != (Target == AbilityTarget.Enemy))
                throw new ArgumentException("Health effect and target rule disagree.");
            if (statusId != null) StatusIds.Check(statusId);
            Slot = slot; Kind = kind; Amount = amount; Range = range; UsesCover = usesCover; StatusId = statusId; HasHealthEffect = true;
        }
        public FieldAbility(AbilitySlot slot, string statusId, AbilityTarget target, decimal range)
        {
            Validate(slot, range); StatusIds.Check(statusId);
            if (!Enum.IsDefined(typeof(AbilityTarget),target)) throw new ArgumentOutOfRangeException(nameof(target));
            Slot = slot; StatusId = statusId; Target = target; Range = range;
        }
        private static void Validate(AbilitySlot slot, decimal range)
        {
            if (!Enum.IsDefined(typeof(AbilitySlot),slot) || slot == AbilitySlot.Passive) throw new ArgumentOutOfRangeException(nameof(slot));
            if (range < 0 || range > 10000) throw new ArgumentOutOfRangeException(nameof(range));
        }
        internal FieldAbility WithAmount(int amount) => HasHealthEffect
            ? new FieldAbility(Slot, Kind, amount, Range, UsesCover, StatusId, Target) : this;
    }
    public sealed class StatusApplicationPreview
    {
        public string TargetId { get; }
        public string StatusId { get; }
        public bool Applies { get; }
        public int StacksAfter { get; }
        public int RemainingOwnerActivations { get; }
        internal StatusApplicationPreview(string target, string status, bool applies, int stacks, int remaining)
        { TargetId = target; StatusId = status; Applies = applies; StacksAfter = stacks; RemainingOwnerActivations = remaining; }
    }
    public sealed class AbilityEffectPreview
    {
        public string TargetId { get; }
        public HealthEffectPreview Health { get; }
        public StatusApplicationPreview Status { get; }
        internal AbilityEffectPreview(string target, HealthEffectPreview health, StatusApplicationPreview status)
        { TargetId = target; Health = health; Status = status; }
    }
}
