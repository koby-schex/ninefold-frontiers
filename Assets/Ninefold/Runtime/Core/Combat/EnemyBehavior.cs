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
        public HealthEffectPreview Effect => AbilityEffect?.Health;
        public AbilityEffectPreview AbilityEffect { get; }
        internal EnemyPlan(long activationId, string unitId, MovementPreview movement = null,
            string targetId = null, AbilitySlot? slot = null, AbilityEffectPreview effect = null)
        { ActivationId = activationId; UnitId = unitId; Movement = movement; TargetId = targetId; Slot = slot; AbilityEffect = effect; }
    }
    public sealed class EnemyTurnResult
    {
        public string UnitId { get; }
        public MovementPreview Movement { get; }
        public HealthEffectPreview Effect => AbilityEffect?.Health;
        public AbilityEffectPreview AbilityEffect { get; }
        public AbilitySlot? UsedSlot { get; }
        public bool BattleEnded { get; }
        internal EnemyTurnResult(string id, MovementPreview movement, AbilityEffectPreview effect, AbilitySlot? slot, bool ended)
        { UnitId = id; Movement = movement; AbilityEffect = effect; UsedSlot = slot; BattleEnded = ended; }
    }

    public sealed partial class BattlefieldController
    {
        private readonly Dictionary<string, EnemyBehavior> enemies = new Dictionary<string, EnemyBehavior>(StringComparer.Ordinal);
        public bool IsEnemyControlled(string id) => id != null && enemies.ContainsKey(id);
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
            => TryPlanEnemyTurn(activationId, perceivedOpponents, Array.Empty<string>(), out plan, out failure);

        public bool TryPlanEnemyTurn(long activationId, IEnumerable<string> perceivedOpponents, IEnumerable<string> perceivedAllies,
            out EnemyPlan plan, out EnemyTurnFailure failure)
        {
            plan = null; failure = EnemyTurnFailure.None;
            if (!Active(activationId)) { failure = EnemyTurnFailure.InactiveTurn; return false; }
            var turn = turns.CurrentActivation;
            if (!enemies.TryGetValue(turn.UnitId,out var behavior)) { failure = EnemyTurnFailure.UnregisteredActor; return false; }
            if (perceivedOpponents == null || perceivedAllies == null) { failure = EnemyTurnFailure.InvalidPerception; return false; }
            var supplied = perceivedOpponents.Take(9).ToArray();
            var suppliedAllies = perceivedAllies.Take(9).ToArray();
            if (supplied.Length > 8 || supplied.Any(string.IsNullOrWhiteSpace) || suppliedAllies.Length > 8 || suppliedAllies.Any(string.IsNullOrWhiteSpace)) { failure = EnemyTurnFailure.InvalidPerception; return false; }
            var actor = units[turn.UnitId]; var health = turns.Health.GetState(turn.UnitId);
            var targets = supplied.Distinct(StringComparer.Ordinal).OrderBy(id => id,StringComparer.Ordinal)
                .Where(id => units.ContainsKey(id) && turns.IsUnitEligible(id) && HasEnemyHealth(id,health.TeamId)).ToArray();
            var allies = suppliedAllies.Concat(new[] { turn.UnitId }).Distinct(StringComparer.Ordinal).OrderBy(id => id,StringComparer.Ordinal)
                .Where(id => units.ContainsKey(id) && turns.IsUnitEligible(id) && turns.Health.TryGetState(id,out var hp) && !hp.IsDefeated && hp.TeamId == health.TeamId).ToArray();
            plan = new EnemyPlan(activationId,turn.UnitId);
            if (!turn.PrimaryActionAvailable) return true; // Do not disturb a partially executed caller-owned turn.

            var candidates = new HashSet<FieldPoint> { actor.Position };
            decimal radius = Math.Min(turn.MovementRemaining,10000m);
            foreach (decimal fraction in new[] { .5m,1m })
                foreach (var direction in Directions) AddCandidate(actor.Position.X+direction.X*radius*fraction,actor.Position.Z+direction.Z*radius*fraction);
            foreach (string id in targets.Concat(allies.Where(id => id != turn.UnitId)))
            {
                var target = units[id].Position;
                AddCandidate(target.X-behavior.PreferredDistance,target.Z); AddCandidate(target.X+behavior.PreferredDistance,target.Z);
                AddCandidate(target.X,target.Z-behavior.PreferredDistance); AddCandidate(target.X,target.Z+behavior.PreferredDistance);
            }
            EnemyPlan attack = null, support = null, approach = null;
            decimal attackScore = 0, supportScore = 0;
            decimal closest = targets.Length == 0 ? 0m : targets.Min(id => FieldPoint.Distance(actor.Position,units[id].Position));
            foreach (var point in candidates.OrderBy(p => p.X).ThenBy(p => p.Z))
            {
                MovementPreview route = null;
                if (!point.Equals(actor.Position))
                {
                    if (!TryFindPath(activationId,point,out route,out _,behavior.PathNodeLimit)) continue;
                    if (behavior.GuardArea != null && route.Path.Any(p => !behavior.GuardArea.Contains(actor.Body.At(p)))) continue;
                }
                foreach (string id in targets.Concat(allies))
                    foreach (var slot in Slots)
                    {
                        if (!PreviewAbilityAt(activationId,slot,id,point,out var effect,out _,out _,out _)) continue;
                        bool allied = allies.Contains(id);
                        int power = units[id].Abilities.Values.Where(p => p.HasHealthEffect).Select(p => p.Amount).DefaultIfEmpty(0).Max();
                        decimal score = (effect.Health?.HealthChanged ?? 0)
                            + turns.Statuses.ApplicationUtility(id,effect.Status,allied,turns.Health.GetState(id).Armor,power,id == turn.UnitId);
                        if (score <= 0) continue;
                        var candidate = new EnemyPlan(activationId,turn.UnitId,route,id,slot,effect);
                        if (allied)
                        {
                            if (BetterAction(candidate,score,support,supportScore)) { support = candidate; supportScore = score; }
                        }
                        else if (BetterAction(candidate,score,attack,attackScore)) { attack = candidate; attackScore = score; }
                    }
                if (behavior.Style == EnemyStyle.Aggressive && route != null && targets.Length > 0)
                {
                    decimal distance = targets.Min(id => FieldPoint.Distance(point,units[id].Position));
                    if (distance < closest) { closest = distance; approach = new EnemyPlan(activationId,turn.UnitId,route); }
                }
            }
            plan = behavior.Style == EnemyStyle.Defensive ? support ?? attack ?? plan : attack ?? support ?? approach ?? plan;
            return true;

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
            => TryRunEnemyTurn(activationId, perceivedOpponents, Array.Empty<string>(), out result, out failure);

        public bool TryRunEnemyTurn(long activationId, IEnumerable<string> perceivedOpponents, IEnumerable<string> perceivedAllies,
            out EnemyTurnResult result, out EnemyTurnFailure failure)
        {
            result = null;
            if (!TryPlanEnemyTurn(activationId,perceivedOpponents,perceivedAllies,out var plan,out failure)) return false;
            MovementPreview movement = null; AbilityEffectPreview effect = null; AbilitySlot? usedSlot = null;
            try
            {
                bool moved = plan.Movement == null || TryMove(activationId,plan.Movement.Path,out movement,out _);
                if (moved && Active(activationId) && plan.Slot.HasValue
                    && TryUseAbility(activationId,plan.Slot.Value,plan.TargetId,out effect,out _,out _)) usedSlot = plan.Slot;
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
        private static bool BetterAction(EnemyPlan candidate, decimal score, EnemyPlan previous, decimal previousScore)
        {
            if (previous == null) return true;
            bool lethal = candidate.Effect?.DefeatsTarget == true, oldLethal = previous.Effect?.DefeatsTarget == true;
            if (lethal != oldLethal) return lethal;
            if (score != previousScore) return score > previousScore;
            decimal cost = candidate.Movement?.Cost ?? 0m, oldCost = previous.Movement?.Cost ?? 0m;
            if (cost != oldCost) return cost < oldCost;
            if (candidate.Slot != previous.Slot) return (int)candidate.Slot.Value < (int)previous.Slot.Value;
            return StringComparer.Ordinal.Compare(candidate.TargetId,previous.TargetId) < 0;
        }
    }
}
