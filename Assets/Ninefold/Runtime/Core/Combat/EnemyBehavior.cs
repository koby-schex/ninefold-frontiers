using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    public enum EnemyStyle { Aggressive, Defensive }
    public enum EnemyTurnFailure { None, InactiveTurn, UnregisteredActor, InvalidPerception }
    public sealed class EnemyBehavior
    {
        public EnemyStyle Style { get; }
        public decimal PreferredDistance { get; }
        public FieldBox GuardArea { get; }
        public int PathNodeLimit { get; }
        public EnemyBehavior(EnemyStyle style, decimal preferredDistance, FieldBox guardArea = null, int pathNodeLimit = 64)
        {
            if (!Enum.IsDefined(typeof(EnemyStyle),style)) throw new ArgumentOutOfRangeException(nameof(style));
            if (preferredDistance <= 0m || preferredDistance > 100m || decimal.Round(preferredDistance,6) != preferredDistance)
                throw new ArgumentOutOfRangeException(nameof(preferredDistance));
            if (style == EnemyStyle.Defensive && guardArea == null) throw new ArgumentNullException(nameof(guardArea));
            if (pathNodeLimit < 2 || pathNodeLimit > 256) throw new ArgumentOutOfRangeException(nameof(pathNodeLimit));
            Style = style; PreferredDistance = preferredDistance; GuardArea = guardArea; PathNodeLimit = pathNodeLimit;
        }
    }
    public sealed class EnemyPlan
    {
        public long ActivationId { get; }
        public string UnitId { get; }
        public MovementPreview Movement { get; }
        public string TargetId { get; }
        public AbilitySlot? Slot { get; }
        public HealthEffectPreview Effect { get; }
        internal EnemyPlan(long activationId, string unitId, MovementPreview movement = null,
            string targetId = null, AbilitySlot? slot = null, HealthEffectPreview effect = null)
        { ActivationId = activationId; UnitId = unitId; Movement = movement; TargetId = targetId; Slot = slot; Effect = effect; }
    }
    public sealed class EnemyTurnResult
    {
        public string UnitId { get; }
        public MovementPreview Movement { get; }
        public HealthEffectPreview Effect { get; }
        public AbilitySlot? UsedSlot { get; }
        public bool BattleEnded { get; }
        internal EnemyTurnResult(string id, MovementPreview movement, HealthEffectPreview effect, AbilitySlot? slot, bool ended)
        { UnitId = id; Movement = movement; Effect = effect; UsedSlot = slot; BattleEnded = ended; }
    }

    public sealed partial class BattlefieldController
    {
        private readonly Dictionary<string, EnemyBehavior> enemies = new Dictionary<string, EnemyBehavior>(StringComparer.Ordinal);
        public void RegisterEnemy(string id, EnemyBehavior behavior)
        {
            if (behavior == null) throw new ArgumentNullException(nameof(behavior));
            if (turns.IsBattleEnded || !turns.IsUnitEligible(id) || turns.GetActivationCount(id) != 0 || enemies.ContainsKey(id))
                throw new InvalidOperationException("Register enemy behavior once before its first activation.");
            if (!units.TryGetValue(id,out var actor)) throw new ArgumentException("Enemy needs a placement.");
            if (turns.Health.GetState(id).IsDefeated) throw new ArgumentException("Enemy must be alive.");
            turns.Abilities.GetState(id);
            if (behavior.GuardArea != null && !behavior.GuardArea.Contains(actor.Body.At(actor.Position)))
                throw new ArgumentException("Enemy starts outside its guard area.");
            enemies.Add(id,behavior);
        }

        /// <summary>Pure bounded planning. Perception is supplied by the future sensing layer.</summary>
        public bool TryPlanEnemyTurn(long activationId, IEnumerable<string> perceivedOpponents,
            out EnemyPlan plan, out EnemyTurnFailure failure)
        {
            plan = null; failure = EnemyTurnFailure.None;
            if (!Active(activationId)) { failure = EnemyTurnFailure.InactiveTurn; return false; }
            var turn = turns.CurrentActivation;
            if (!enemies.TryGetValue(turn.UnitId,out var behavior)) { failure = EnemyTurnFailure.UnregisteredActor; return false; }
            if (perceivedOpponents == null) { failure = EnemyTurnFailure.InvalidPerception; return false; }
            var supplied = perceivedOpponents.Take(9).ToArray();
            if (supplied.Length > 8 || supplied.Any(string.IsNullOrWhiteSpace)) { failure = EnemyTurnFailure.InvalidPerception; return false; }
            var actor = units[turn.UnitId]; var health = turns.Health.GetState(turn.UnitId);
            var targets = supplied.Distinct(StringComparer.Ordinal).OrderBy(id => id,StringComparer.Ordinal)
                .Where(id => units.ContainsKey(id) && turns.IsUnitEligible(id) && HasEnemyHealth(id,health.TeamId)).ToArray();
            plan = new EnemyPlan(activationId,turn.UnitId);
            if (!turn.PrimaryActionAvailable) return true; // Do not disturb a partially executed caller-owned turn.

            EnemyPlan healing = null;
            foreach (var slot in Slots)
                if (PreviewAt(actor.Position,slot,turn.UnitId,out var preview) && preview.Kind == HealthEffectKind.Healing && preview.HealthChanged > 0
                    && (healing == null || preview.HealthChanged > healing.Effect.HealthChanged))
                    healing = new EnemyPlan(activationId,turn.UnitId,targetId:turn.UnitId,slot:slot,effect:preview);
            if (behavior.Style == EnemyStyle.Defensive && healing != null) { plan = healing; return true; }

            var candidates = new HashSet<FieldPoint> { actor.Position };
            decimal radius = Math.Min(turn.MovementRemaining,10000m);
            foreach (decimal fraction in new[] { .5m,1m })
                foreach (var direction in Directions) AddCandidate(actor.Position.X+direction.X*radius*fraction,actor.Position.Z+direction.Z*radius*fraction);
            foreach (string id in targets)
            {
                var target = units[id].Position;
                AddCandidate(target.X-behavior.PreferredDistance,target.Z); AddCandidate(target.X+behavior.PreferredDistance,target.Z);
                AddCandidate(target.X,target.Z-behavior.PreferredDistance); AddCandidate(target.X,target.Z+behavior.PreferredDistance);
            }
            EnemyPlan attack = null, approach = null;
            decimal closest = targets.Length == 0 ? 0m : targets.Min(id => FieldPoint.Distance(actor.Position,units[id].Position));
            foreach (var point in candidates.OrderBy(p => p.X).ThenBy(p => p.Z))
            {
                MovementPreview route = null;
                if (!point.Equals(actor.Position))
                {
                    if (!TryFindPath(activationId,point,out route,out _,behavior.PathNodeLimit)) continue;
                    if (behavior.GuardArea != null && route.Path.Any(p => !behavior.GuardArea.Contains(actor.Body.At(p)))) continue;
                }
                foreach (string id in targets)
                    foreach (var slot in Slots)
                        if (PreviewAt(point,slot,id,out var effect) && effect.Kind == HealthEffectKind.Damage && effect.HealthChanged > 0)
                        {
                            var candidate = new EnemyPlan(activationId,turn.UnitId,route,id,slot,effect);
                            if (BetterAttack(candidate,attack)) attack = candidate;
                        }
                if (behavior.Style == EnemyStyle.Aggressive && route != null && targets.Length > 0)
                {
                    decimal distance = targets.Min(id => FieldPoint.Distance(point,units[id].Position));
                    if (distance < closest) { closest = distance; approach = new EnemyPlan(activationId,turn.UnitId,route); }
                }
            }
            plan = attack ?? healing ?? approach ?? plan;
            return true;

            bool PreviewAt(FieldPoint point, AbilitySlot slot, string target, out HealthEffectPreview preview)
            {
                preview = null;
                if (!PreviewAbilityAt(activationId,slot,target,point,out var full,out _,out _,out _) || full.Health == null) return false;
                preview = full.Health; return true;
            }
            void AddCandidate(decimal x, decimal z)
            {
                x = decimal.Round(x,6); z = decimal.Round(z,6);
                if (x < -10000m || x > 10000m || z < -10000m || z > 10000m) return;
                var point = new FieldPoint(x,actor.Position.Y,z);
                if (LegalPosition(turn.UnitId,actor,point) && (behavior.GuardArea == null || behavior.GuardArea.Contains(actor.Body.At(point)))) candidates.Add(point);
            }
        }

        /// <summary>Replans and uses normal spatial commands; never starts a round or another activation.</summary>
        public bool TryRunEnemyTurn(long activationId, IEnumerable<string> perceivedOpponents,
            out EnemyTurnResult result, out EnemyTurnFailure failure)
        {
            result = null;
            if (!TryPlanEnemyTurn(activationId,perceivedOpponents,out var plan,out failure)) return false;
            MovementPreview movement = null; HealthEffectPreview effect = null; AbilitySlot? usedSlot = null;
            try
            {
                bool moved = plan.Movement == null || TryMove(activationId,plan.Movement.Path,out movement,out _);
                if (moved && Active(activationId) && plan.Slot.HasValue
                    && TryApplyEffect(activationId,plan.Slot.Value,plan.TargetId,out effect,out _,out _)) usedSlot = plan.Slot;
            }
            finally
            {
                if (Active(activationId)) turns.EndActivation(activationId);
            }
            result = new EnemyTurnResult(plan.UnitId,movement,effect,usedSlot,turns.IsBattleEnded);
            return true;
        }
        private bool HasEnemyHealth(string id, string actorTeam)
        {
            // Placement can exist without health (e.g. future noncombat actors); exclude safely.
            return turns.Health.TryGetState(id,out var health) && !health.IsDefeated && health.TeamId != actorTeam;
        }
        private static readonly AbilitySlot[] Slots = { AbilitySlot.NormalAttack, AbilitySlot.Main, AbilitySlot.Signature };
        private static readonly (decimal X, decimal Z)[] Directions =
        { (1m,0m),(-1m,0m),(0m,1m),(0m,-1m),(.707106m,.707106m),(-.707106m,.707106m),(.707106m,-.707106m),(-.707106m,-.707106m) };
        private static bool BetterAttack(EnemyPlan candidate, EnemyPlan previous)
        {
            if (previous == null) return true;
            if (candidate.Effect.DefeatsTarget != previous.Effect.DefeatsTarget) return candidate.Effect.DefeatsTarget;
            if (candidate.Effect.HealthChanged != previous.Effect.HealthChanged) return candidate.Effect.HealthChanged > previous.Effect.HealthChanged;
            decimal cost = candidate.Movement?.Cost ?? 0m, oldCost = previous.Movement?.Cost ?? 0m;
            if (cost != oldCost) return cost < oldCost;
            if (candidate.Slot != previous.Slot) return (int)candidate.Slot.Value < (int)previous.Slot.Value;
            return StringComparer.Ordinal.Compare(candidate.TargetId,previous.TargetId) < 0;
        }
    }
}
