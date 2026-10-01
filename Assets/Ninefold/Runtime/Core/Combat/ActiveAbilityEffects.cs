using System;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattleStatusController
    {
        internal bool HasDefinition(string id) => id != null && definitions.ContainsKey(id);
        internal StatusApplicationPreview PreviewApplication(string owner, string id, HealthEffectPreview health)
        {
            var d = definitions[id];
            if (health?.DefeatsTarget == true) return new StatusApplicationPreview(owner, id, false, 0, 0);
            int stacks = active.TryGetValue(owner, out var list) && list.TryGetValue(id, out var state) ? state.Stacks : 0;
            // Damage passives resolve before the authored attachment; include its stack if it shares this ID.
            if (health?.Kind == HealthEffectKind.Damage && health.HealthChanged > 0 && passives.TryGetValue(owner, out var passive)
                && passive.Trigger == PassiveTrigger.OwnerSurvivedDamage && passive.StatusId == id) stacks++;
            stacks = d.Stacking == StatusStacking.Refresh ? 1 : Math.Min(d.MaximumStacks, stacks + 1);
            return new StatusApplicationPreview(owner, id, true, stacks, d.Duration);
        }
    }
    public sealed partial class BattlefieldController
    {
        internal void ValidateStatusReferences()
        {
            foreach (var unit in units.Values)
                foreach (var ability in unit.Abilities.Values)
                    Ninefold.Core.Persistence.SaveIO.Require(ability.StatusId == null || turns.Statuses.HasDefinition(ability.StatusId), "Missing active status definition.");
        }
        /// <summary>Pure compound preview. Costs, targeting and every component validate together.</summary>
        public bool TryPreviewAbility(long activationId, AbilitySlot slot, string targetId,
            out AbilityEffectPreview preview, out FieldFailure failure, out HealthActionFailure healthFailure)
            => PreviewAbilityAt(activationId, slot, targetId, null, out preview, out _, out failure, out healthFailure);

        /// <summary>Revalidate now; never commit a cached preview. No external interleaving between components.</summary>
        public bool TryUseAbility(long activationId, AbilitySlot slot, string targetId,
            out AbilityEffectPreview result, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            result = null;
            if (!PreviewAbilityAt(activationId, slot, targetId, null, out var preview, out var action, out failure, out healthFailure)) return false;
            if (action != null)
            {
                if (!turns.Health.TryApplyCore(activationId, action, out _, out healthFailure, false))
                    return Fail(FieldFailure.HealthRejected, out failure);
            }
            else if (!turns.Abilities.TryUse(activationId, slot, out _))
            { healthFailure = HealthActionFailure.AbilityUnavailable; return Fail(FieldFailure.HealthRejected, out failure); }
            if (preview.Status?.Applies == true) turns.Statuses.Apply(targetId, preview.Status.StatusId);
            turns.Mission?.Evaluate(); result = preview; return true;
        }
        // Compatibility APIs retain their health-shaped result. Status-only abilities must use the compound API.
        public bool TryPreviewEffect(long activationId, AbilitySlot slot, string targetId,
            out HealthEffectPreview preview, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            preview = null;
            if (!TryPreviewAbility(activationId, slot, targetId, out var full, out failure, out healthFailure)) return false;
            if (full.Health == null) return Fail(FieldFailure.InvalidRequest, out failure);
            preview = full.Health; return true;
        }
        public bool TryApplyEffect(long activationId, AbilitySlot slot, string targetId,
            out HealthEffectPreview result, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            result = null;
            if (!TryPreviewEffect(activationId, slot, targetId, out _, out failure, out healthFailure)) return false;
            if (!TryUseAbility(activationId, slot, targetId, out var full, out failure, out healthFailure)) return false;
            result = full.Health; return true;
        }
        private bool PreviewAbilityAt(long activationId, AbilitySlot slot, string targetId, FieldPoint? actorPosition,
            out AbilityEffectPreview preview, out HealthAction action, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            preview = null; action = null; failure = FieldFailure.None; healthFailure = HealthActionFailure.None;
            if (!Active(activationId)) return Fail(FieldFailure.InactiveTurn, out failure);
            string owner = turns.CurrentActivation.UnitId;
            if (!units.TryGetValue(owner, out var actor)) return Fail(FieldFailure.MissingPosition, out failure);
            if (targetId == null || !units.TryGetValue(targetId, out var target) || !turns.IsUnitEligible(targetId)) return Fail(FieldFailure.InvalidTarget, out failure);
            if (!actor.Abilities.TryGetValue(slot, out var profile)) return Fail(FieldFailure.InvalidRequest, out failure);
            if (turns.Abilities.GetAvailability(activationId, slot) != AbilityUseFailure.None)
            { healthFailure = HealthActionFailure.AbilityUnavailable; return Fail(FieldFailure.HealthRejected, out failure); }
            if (!turns.Health.TryGetState(owner, out var actorHealth))
            { healthFailure = HealthActionFailure.ActorHealthMissing; return Fail(FieldFailure.HealthRejected, out failure); }
            if (!turns.Health.TryGetState(targetId, out var targetHealth))
            { healthFailure = HealthActionFailure.TargetHealthMissing; return Fail(FieldFailure.HealthRejected, out failure); }
            if (targetHealth.IsDefeated) return Fail(FieldFailure.InvalidTarget, out failure);
            bool same = actorHealth.TeamId == targetHealth.TeamId, self = owner == targetId;
            bool allowed = profile.Target == AbilityTarget.Enemy ? !same : profile.Target == AbilityTarget.Self ? self
                : profile.Target == AbilityTarget.Ally ? same && !self : same;
            if (!allowed) { healthFailure = HealthActionFailure.WrongTeam; return Fail(FieldFailure.HealthRejected, out failure); }
            // Preserve authored allied compatibility for healing and extend it to allied status-only support.
            if (same && !(profile.Target == AbilityTarget.Self && self) && !actor.HealingTargets.Contains(targetId))
            { healthFailure = HealthActionFailure.Incompatible; return Fail(FieldFailure.HealthRejected, out failure); }
            if (profile.StatusId != null && !turns.Statuses.HasDefinition(profile.StatusId)) return Fail(FieldFailure.InvalidRequest, out failure);
            var from = FieldPoint.Add(actorPosition ?? actor.Position, actor.Body.AttackOffset);
            var to = FieldPoint.Add(self ? actorPosition ?? target.Position : target.Position, target.Body.TargetOffset);
            // Self-targeting needs neither a projectile path through the unit's own body nor range between offsets.
            decimal cover = 0;
            if (!self)
            {
                if (FieldPoint.Distance(from,to) > profile.Range) return Fail(FieldFailure.OutOfRange, out failure);
                foreach (var obstacle in Map.Obstacles)
                    if (obstacle.Bounds.Intersects(from,to,out _,out _))
                    {
                        if (obstacle.BlocksShots) return Fail(FieldFailure.Obstructed, out failure);
                        if (profile.UsesCover) cover = Math.Max(cover, obstacle.CoverReduction);
                    }
            }
            HealthEffectPreview health = null;
            if (profile.HasHealthEffect)
            {
                action = new HealthAction(slot, profile.Kind, targetId, profile.Amount, TargetingVerdict.Legal, new DamageMitigation(cover,0));
                if (!turns.Health.TryPreviewCore(activationId, action, out health, out healthFailure, profile.StatusId != null))
                    return Fail(FieldFailure.HealthRejected, out failure);
            }
            var status = profile.StatusId == null ? null : turns.Statuses.PreviewApplication(targetId, profile.StatusId, health);
            preview = new AbilityEffectPreview(targetId, health, status); return true;
        }
    }
}
