using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Progression;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattleTurnController
    {
        /// <summary>Flow calls once on a new factory-built battle, never on resume or later rounds.</summary>
        internal void ApplyDeploymentModifiers(IReadOnlyDictionary<string, DeploymentModifiers> bonuses)
        {
            if (bonuses.Count == 0) return;
            if (IsBattleEnded || RoundNumber > 1 || units.Values.Any(u => !u.Eligible) ||
                units.Any(p => p.Value.ActivationCount != 0 && (active == null || active.UnitId != p.Key || p.Value.ActivationCount != 1)) ||
                (active != null && (!active.PrimaryActionAvailable || active.MovementRemaining != units[active.UnitId].Definition.MovementAllowance)))
                throw new InvalidOperationException("Stat modifiers require a fresh unplayed deployment.");
            var health = Health.PrepareDeploymentModifiers(bonuses);
            if (Battlefield == null && bonuses.Any(p => p.Value.Power != 0)) throw new InvalidOperationException("Modified power needs battlefield ability profiles.");
            var power = Battlefield?.PrepareDeploymentModifiers(bonuses);
            health(); power?.Invoke();
        }
    }
    public sealed partial class BattleHealthController
    {
        internal Action PrepareDeploymentModifiers(IReadOnlyDictionary<string, DeploymentModifiers> bonuses)
        {
            var updates = new Dictionary<string, HealthState>(StringComparer.Ordinal);
            foreach (var pair in bonuses)
            {
                if (!units.TryGetValue(pair.Key, out var state)) throw new InvalidOperationException("Modified unit needs registered health.");
                var bonus = pair.Value;
                var definition = new UnitHealthDefinition(state.Definition.TeamId,
                    DeploymentModifiers.Scale(state.Definition.MaximumHealth, bonus.Health), DeploymentModifiers.Scale(state.Definition.Armor, bonus.Armor));
                updates.Add(pair.Key, new HealthState(definition, DeploymentModifiers.Scale(state.Current, bonus.Health)));
            }
            return () => { foreach (var pair in updates) units[pair.Key] = pair.Value; };
        }
    }
    public sealed partial class BattlefieldController
    {
        internal Action PrepareDeploymentModifiers(IReadOnlyDictionary<string, DeploymentModifiers> bonuses)
        {
            var updates = new Dictionary<string, FieldAbility[]>(StringComparer.Ordinal);
            foreach (var pair in bonuses.Where(p => p.Value.Power != 0))
            {
                if (!units.TryGetValue(pair.Key, out var placement)) throw new InvalidOperationException("Modified unit needs ability profiles.");
                updates.Add(pair.Key, placement.Abilities.Values.Select(a => new FieldAbility(a.Slot, a.Kind,
                    DeploymentModifiers.Scale(a.Amount, pair.Value.Power), a.Range, a.UsesCover)).ToArray());
            }
            return () => { foreach (var pair in updates) foreach (var ability in pair.Value) units[pair.Key].Abilities[ability.Slot] = ability; };
        }
    }
}
