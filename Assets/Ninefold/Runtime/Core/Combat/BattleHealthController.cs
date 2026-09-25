using System;
using System.Collections.Generic;

namespace Ninefold.Core.Combat
{
    /// <summary>Single-target health effects and cost commits, on the battle's single thread.</summary>
    public sealed class BattleHealthController
    {
        private sealed class HealthState
        {
            internal readonly UnitHealthDefinition Definition;
            internal int Current;
            internal HealthState(UnitHealthDefinition definition, int current)
            {
                Definition = definition;
                Current = current;
            }
        }

        private readonly BattleTurnController turns;
        private readonly Dictionary<string, HealthState> units = new Dictionary<string, HealthState>(StringComparer.Ordinal);
        public DamageRules Rules { get; }

        internal BattleHealthController(BattleTurnController turns, DamageRules rules)
        {
            this.turns = turns;
            Rules = rules;
        }

        public void RegisterHealth(string unitId, UnitHealthDefinition definition, int? startingHealth = null)
        {
            if (turns.IsBattleEnded) throw new InvalidOperationException("The battle has ended.");
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!turns.IsUnitEligible(unitId)) throw new InvalidOperationException("Removed units cannot register health.");
            if (units.ContainsKey(unitId)) throw new InvalidOperationException("Health is already registered.");
            if (turns.GetActivationCount(unitId) != 0) throw new InvalidOperationException("Register health before its first turn.");
            int current = startingHealth ?? definition.MaximumHealth;
            if (current <= 0 || current > definition.MaximumHealth) throw new ArgumentOutOfRangeException(nameof(startingHealth));
            units.Add(unitId, new HealthState(definition, current));
        }

        public HealthStateView GetState(string unitId)
        {
            if (unitId == null) throw new ArgumentNullException(nameof(unitId));
            if (!units.TryGetValue(unitId, out var state)) throw new ArgumentException("No registered health.", nameof(unitId));
            return new HealthStateView(unitId, state.Definition, state.Current);
        }

        /// <summary>Validates and previews without spending actions or changing any state.</summary>
        public bool TryPreview(long activationId, HealthAction request,
            out HealthEffectPreview preview, out HealthActionFailure failure)
        {
            preview = null;
            failure = Validate(activationId, request);
            if (failure != HealthActionFailure.None) return false;
            var target = units[request.TargetId];
            int resolved;
            int after;
            if (request.Kind == HealthEffectKind.Damage)
            {
                resolved = Rules.Calculate(request.Amount, target.Definition.Armor, request.Mitigation);
                after = target.Current - Math.Min(target.Current, resolved);
            }
            else
            {
                resolved = Math.Min(request.Amount, target.Definition.MaximumHealth - target.Current);
                after = target.Current + resolved;
            }
            preview = new HealthEffectPreview(request.TargetId, request.Kind, target.Current, after, resolved);
            return true;
        }

        /// <summary>Recomputes a fresh preview then commits ability costs and its health result.</summary>
        public bool TryApply(long activationId, HealthAction request,
            out HealthEffectPreview result, out HealthActionFailure failure)
        {
            result = null;
            if (!TryPreview(activationId, request, out var preview, out failure)) return false;
            if (!turns.Abilities.TryUse(activationId, request.Slot, out _))
            {
                failure = HealthActionFailure.AbilityUnavailable;
                return false;
            }
            // No callbacks or external interleaving between validated costs and this assignment.
            units[request.TargetId].Current = preview.HealthAfter;
            if (preview.DefeatsTarget) turns.RemoveUnit(request.TargetId);
            result = preview;
            turns.Mission?.Evaluate();
            return true;
        }

        private HealthActionFailure Validate(long activationId, HealthAction request)
        {
            if (request == null) return HealthActionFailure.InvalidRequest;
            if (turns.Abilities.GetAvailability(activationId, request.Slot) != AbilityUseFailure.None)
                return HealthActionFailure.AbilityUnavailable;
            if (!units.TryGetValue(turns.CurrentActivation.UnitId, out var actor))
                return HealthActionFailure.ActorHealthMissing;
            if (!units.TryGetValue(request.TargetId, out var target)) return HealthActionFailure.TargetHealthMissing;
            if (!turns.IsUnitEligible(request.TargetId) || target.Current == 0) return HealthActionFailure.TargetInactive;
            bool sameTeam = StringComparer.Ordinal.Equals(actor.Definition.TeamId, target.Definition.TeamId);
            if ((request.Kind == HealthEffectKind.Damage && sameTeam)
                || (request.Kind == HealthEffectKind.Healing && !sameTeam)) return HealthActionFailure.WrongTeam;
            switch (request.Targeting)
            {
                case TargetingVerdict.OutOfRange: return HealthActionFailure.OutOfRange;
                case TargetingVerdict.Obstructed: return HealthActionFailure.Obstructed;
                case TargetingVerdict.Incompatible: return HealthActionFailure.Incompatible;
            }
            if (request.Kind == HealthEffectKind.Healing && target.Current == target.Definition.MaximumHealth)
                return HealthActionFailure.AlreadyFullHealth;
            return HealthActionFailure.None;
        }
    }
}
