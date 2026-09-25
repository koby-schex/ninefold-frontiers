using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    public enum FieldFailure
    {
        None, InvalidRequest, InactiveTurn, MissingPosition, IllegalDestination, BlockedPath,
        UnsupportedElevation, InsufficientMovement, InvalidTarget, OutOfRange, Obstructed, HealthRejected
    }

    public sealed class MovementPreview
    {
        public IReadOnlyList<FieldPoint> Path { get; }
        public decimal Cost { get; }
        internal MovementPreview(FieldPoint[] path, decimal cost) { Path = Array.AsReadOnly(path); Cost = cost; }
    }

    /// <summary>Trusted authored single-target profile; clients select a slot and target, not damage/range.</summary>
    public sealed class FieldAbility
    {
        public AbilitySlot Slot { get; }
        public HealthEffectKind Kind { get; }
        public int Amount { get; }
        public decimal Range { get; }
        public bool UsesCover { get; }
        public FieldAbility(AbilitySlot slot, HealthEffectKind kind, int amount, decimal range, bool usesCover)
        {
            if (!Enum.IsDefined(typeof(AbilitySlot),slot) || slot == AbilitySlot.Passive) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!Enum.IsDefined(typeof(HealthEffectKind),kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (amount <= 0 || range < 0m || range > 10000m) throw new ArgumentOutOfRangeException(nameof(amount));
            if (kind == HealthEffectKind.Healing && usesCover) throw new ArgumentException("Healing does not use cover.");
            Slot = slot; Kind = kind; Amount = amount; Range = range; UsesCover = usesCover;
        }
    }

    /// <summary>Single-threaded spatial commands. All previews are pure; confirmations recalculate.</summary>
    public sealed class BattlefieldController
    {
        private sealed class Placement
        {
            internal readonly FieldBody Body;
            internal FieldPoint Position;
            internal readonly Dictionary<AbilitySlot, FieldAbility> Abilities;
            internal readonly HashSet<string> HealingTargets;
            internal Placement(FieldPoint p, FieldBody body, FieldAbility[] abilities, IEnumerable<string> healingTargets)
            {
                Position = p; Body = body; Abilities = abilities.ToDictionary(a => a.Slot);
                HealingTargets = new HashSet<string>(healingTargets ?? Array.Empty<string>(), StringComparer.Ordinal);
            }
        }
        private readonly BattleTurnController turns;
        private readonly Dictionary<string, Placement> units = new Dictionary<string, Placement>(StringComparer.Ordinal);
        public BattlefieldMap Map { get; }
        internal BattlefieldController(BattleTurnController turns, BattlefieldMap map) { this.turns = turns; Map = map; }

        public void Register(string id, FieldPoint position, FieldBody body, IEnumerable<FieldAbility> abilities,
            IEnumerable<string> compatibleHealingTargets = null)
        {
            if (turns.IsBattleEnded || !turns.IsUnitEligible(id) || turns.GetActivationCount(id) != 0 || units.ContainsKey(id))
                throw new InvalidOperationException("Placement must be registered once before its first activation.");
            if (body == null || abilities == null) throw new ArgumentNullException(nameof(body));
            var profiles = abilities.ToArray();
            if (profiles.Any(a => a == null) || profiles.Select(a => a.Slot).Distinct().Count() != profiles.Length)
                throw new ArgumentException("Ability slots must be unique and non-null.");
            var placement = new Placement(position, body, profiles, compatibleHealingTargets);
            if (!LegalPosition(id, placement, position)) throw new ArgumentException("Illegal starting position.");
            units.Add(id, placement);
        }
        public FieldPoint GetPosition(string id) => units[id].Position;

        public bool TryPreviewMovement(long activationId, IEnumerable<FieldPoint> waypoints,
            out MovementPreview preview, out FieldFailure failure)
        {
            preview = null; failure = FieldFailure.None;
            if (!Active(activationId)) return Fail(FieldFailure.InactiveTurn, out failure);
            string id = turns.CurrentActivation.UnitId;
            if (!units.TryGetValue(id, out var actor)) return Fail(FieldFailure.MissingPosition, out failure);
            if (waypoints == null) return Fail(FieldFailure.InvalidRequest, out failure);
            // Bound command size; paths list destinations after the current position.
            var path = waypoints.Take(257).ToArray();
            if (path.Length == 0 || path.Length > 256) return Fail(FieldFailure.InvalidRequest, out failure);
            var from = actor.Position;
            decimal cost = 0m;
            foreach (var to in path)
            {
                if (to.Equals(from)) return Fail(FieldFailure.InvalidRequest, out failure);
                if (to.Y != from.Y) return Fail(FieldFailure.UnsupportedElevation, out failure);
                if (!LegalPosition(id, actor, to)) return Fail(FieldFailure.IllegalDestination, out failure);
                if (Map.Obstacles.Any(w => w.BlocksMovement && Swept(w.Bounds, from, to, actor.Body)))
                    return Fail(FieldFailure.BlockedPath, out failure);
                foreach (var other in units)
                    if (other.Key != id && turns.IsUnitEligible(other.Key) && Swept(other.Value.Body.At(other.Value.Position), from, to, actor.Body))
                        return Fail(FieldFailure.BlockedPath, out failure);
                cost += SegmentCost(from, to, actor.Body);
                if (cost > turns.CurrentActivation.MovementRemaining) return Fail(FieldFailure.InsufficientMovement, out failure);
                from = to;
            }
            preview = new MovementPreview(path, cost); return true;
        }

        public bool TryMove(long activationId, IEnumerable<FieldPoint> waypoints, out MovementPreview result, out FieldFailure failure)
        {
            if (!TryPreviewMovement(activationId, waypoints, out result, out failure)) return false;
            string id = turns.CurrentActivation.UnitId;
            turns.SpendMovement(activationId, result.Cost);
            units[id].Position = result.Path[result.Path.Count-1];
            return true;
        }

        public bool TryPreviewEffect(long activationId, AbilitySlot slot, string targetId,
            out HealthEffectPreview preview, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            preview = null; healthFailure = HealthActionFailure.None;
            if (!BuildAction(activationId, slot, targetId, out var action, out failure)) return false;
            if (turns.Health.TryPreview(activationId, action, out preview, out healthFailure)) return true;
            return Fail(FieldFailure.HealthRejected, out failure);
        }

        public bool TryApplyEffect(long activationId, AbilitySlot slot, string targetId,
            out HealthEffectPreview result, out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            result = null; healthFailure = HealthActionFailure.None;
            if (!BuildAction(activationId, slot, targetId, out var action, out failure)) return false;
            if (turns.Health.TryApply(activationId, action, out result, out healthFailure)) return true;
            return Fail(FieldFailure.HealthRejected, out failure);
        }

        private bool BuildAction(long activationId, AbilitySlot slot, string targetId, out HealthAction action, out FieldFailure failure)
        {
            action = null; failure = FieldFailure.None;
            if (!Active(activationId)) return Fail(FieldFailure.InactiveTurn, out failure);
            if (!units.TryGetValue(turns.CurrentActivation.UnitId, out var actor)) return Fail(FieldFailure.MissingPosition, out failure);
            if (targetId == null || !units.TryGetValue(targetId, out var target) || !turns.IsUnitEligible(targetId))
                return Fail(FieldFailure.InvalidTarget, out failure);
            if (!actor.Abilities.TryGetValue(slot, out var profile)) return Fail(FieldFailure.InvalidRequest, out failure);
            var from = FieldPoint.Add(actor.Position, actor.Body.AttackOffset);
            var to = FieldPoint.Add(target.Position, target.Body.TargetOffset);
            if (FieldPoint.Distance(from,to) > profile.Range) return Fail(FieldFailure.OutOfRange, out failure);
            decimal cover = 0m;
            foreach (var obstacle in Map.Obstacles)
                if (obstacle.Bounds.Intersects(from, to, out _, out _))
                {
                    if (obstacle.BlocksShots) return Fail(FieldFailure.Obstructed, out failure);
                    if (profile.UsesCover) cover = Math.Max(cover, obstacle.CoverReduction);
                }
            var verdict = profile.Kind == HealthEffectKind.Healing && !actor.HealingTargets.Contains(targetId)
                ? TargetingVerdict.Incompatible : TargetingVerdict.Legal;
            action = new HealthAction(slot, profile.Kind, targetId, profile.Amount, verdict, new DamageMitigation(cover,0m));
            return true;
        }

        private bool LegalPosition(string id, Placement actor, FieldPoint point)
        {
            // Check before constructing a translated box so out-of-map input cannot overflow coordinate bounds.
            if (point.X-actor.Body.HalfWidth < Map.Bounds.Min.X || point.X+actor.Body.HalfWidth > Map.Bounds.Max.X
                || point.Z-actor.Body.HalfDepth < Map.Bounds.Min.Z || point.Z+actor.Body.HalfDepth > Map.Bounds.Max.Z
                || point.Y < Map.Bounds.Min.Y || point.Y+actor.Body.Height > Map.Bounds.Max.Y) return false;
            var box = actor.Body.At(point);
            if (Map.Obstacles.Any(w => w.BlocksMovement && w.Bounds.Overlaps(box))) return false;
            return !units.Any(u => u.Key != id && turns.IsUnitEligible(u.Key) && u.Value.Body.At(u.Value.Position).Overlaps(box));
        }
        private static bool Swept(FieldBox box, FieldPoint a, FieldPoint b, FieldBody body)
            => box.Intersects(a,b,out _,out _,body.HalfWidth,body.Height,body.HalfDepth);
        private decimal SegmentCost(FieldPoint from, FieldPoint to, FieldBody body)
        {
            var spans = new List<(decimal Start, decimal End, decimal Rate)>();
            var cuts = new SortedSet<decimal> { 0m,1m };
            foreach (var ground in Map.Ground)
                if (ground.Bounds.Intersects(from,to,out var enter,out var exit,body.HalfWidth,body.Height,body.HalfDepth))
                { spans.Add((enter,exit,ground.CostPerMeter)); cuts.Add(enter); cuts.Add(exit); }
            decimal weighted = 0m; var points = cuts.ToArray();
            for (int i=1; i<points.Length; i++)
            {
                decimal mid = (points[i-1]+points[i])/2m, rate = 1m;
                foreach (var span in spans) if (mid >= span.Start && mid <= span.End) rate = Math.Max(rate,span.Rate);
                weighted += (points[i]-points[i-1])*rate;
            }
            // Round upwards per segment: subdivision cannot buy extra movement.
            return Math.Ceiling(FieldPoint.Distance(from,to)*weighted*1000000m)/1000000m;
        }
        private bool Active(long id) => !turns.IsBattleEnded && turns.CurrentActivation != null && turns.CurrentActivation.ActivationId == id;
        private static bool Fail(FieldFailure reason, out FieldFailure failure) { failure = reason; return false; }
    }
}
