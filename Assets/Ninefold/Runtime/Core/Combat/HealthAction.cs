using System;

namespace Ninefold.Core.Combat
{
    public enum HealthEffectKind { Damage, Healing }
    public enum TargetingVerdict { Legal, OutOfRange, Obstructed, Incompatible }
    public enum HealthActionFailure
    {
        None, InvalidRequest, AbilityUnavailable, ActorHealthMissing, TargetHealthMissing,
        TargetInactive, WrongTeam, OutOfRange, Obstructed, Incompatible, AlreadyFullHealth
    }

    /// <summary>
    /// Trusted resolved effect input, not raw player input. Amount/slot must come from
    /// the future ability definition layer; targeting verdict from its validator.
    /// </summary>
    public sealed class HealthAction
    {
        public AbilitySlot Slot { get; }
        public HealthEffectKind Kind { get; }
        public string TargetId { get; }
        public int Amount { get; }
        public TargetingVerdict Targeting { get; }
        public DamageMitigation Mitigation { get; }

        public HealthAction(AbilitySlot slot, HealthEffectKind kind, string targetId,
            int amount, TargetingVerdict targeting, DamageMitigation mitigation = null)
        {
            if (!Enum.IsDefined(typeof(AbilitySlot), slot) || slot == AbilitySlot.Passive)
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (!Enum.IsDefined(typeof(HealthEffectKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target ID required.", nameof(targetId));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!Enum.IsDefined(typeof(TargetingVerdict), targeting)) throw new ArgumentOutOfRangeException(nameof(targeting));
            Slot = slot;
            Kind = kind;
            TargetId = targetId;
            Amount = amount;
            Targeting = targeting;
            Mitigation = mitigation ?? DamageMitigation.None;
        }
    }

    public sealed class HealthEffectPreview
    {
        public string TargetId { get; }
        public HealthEffectKind Kind { get; }
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        // Damage after mitigation before overkill clamp; healing after maximum-health clamp.
        public int ResolvedAmount { get; }
        public int HealthChanged => Math.Abs(HealthAfter - HealthBefore);
        public bool DefeatsTarget => Kind == HealthEffectKind.Damage && HealthAfter == 0;

        internal HealthEffectPreview(string targetId, HealthEffectKind kind, int before, int after, int resolved)
        {
            TargetId = targetId;
            Kind = kind;
            HealthBefore = before;
            HealthAfter = after;
            ResolvedAmount = resolved;
        }
    }
}
