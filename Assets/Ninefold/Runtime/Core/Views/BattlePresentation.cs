using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Flow;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Views
{
    public sealed class ReachableSample
    {
        public FieldPoint Position { get; }
        public decimal Cost { get; }
        internal ReachableSample(FieldPoint position, decimal cost) { Position = position; Cost = cost; }
    }
    public sealed class ObjectiveInteractionPreview
    {
        public string Id { get; }
        public InteractionFailure Failure { get; }
        public bool CanInteract => Failure == InteractionFailure.None;
        internal ObjectiveInteractionPreview(string id, InteractionFailure failure) { Id = id; Failure = failure; }
    }
    public sealed class BattlePlanningView
    {
        public IReadOnlyList<ReachableSample> Movement { get; }
        public IReadOnlyList<ObjectiveInteractionPreview> Objectives { get; }
        internal BattlePlanningView(IEnumerable<ReachableSample> movement, IEnumerable<ObjectiveInteractionPreview> objectives)
        { Movement = Array.AsReadOnly(movement.ToArray()); Objectives = Array.AsReadOnly(objectives.ToArray()); }
    }
    /// <summary>Engine-independent presentation facade. Returns data, never invokes animation callbacks inside a save transaction.</summary>
    public sealed class BattlePresentation
    {
        private readonly BattleSession session;
        public BattlePresentation(BattleSession session) { this.session = session ?? throw new ArgumentNullException(nameof(session)); }
        public BattleView Read()
        { var b = session.ReadSnapshot(); return new BattleView(b,session.Phase(b),session.BattleVersion); }
        public bool TryPreviewDestination(long activationId, FieldPoint destination, out MovementPreview preview, out FieldFailure failure, int nodeLimit = 64)
            => session.ReadPlayerSnapshot(activationId).Battlefield.TryFindPath(activationId,destination,out preview,out failure,nodeLimit);
        public bool TryPreviewReachableDestination(long activationId, FieldPoint destination, out MovementPreview preview, out FieldFailure failure, int nodeLimit = 64)
            => session.ReadPlayerSnapshot(activationId).Battlefield.TryFindReachablePath(activationId,destination,out preview,out failure,nodeLimit);
        public bool TryPreviewPath(long activationId, IEnumerable<FieldPoint> path, out MovementPreview preview, out FieldFailure failure)
            => session.ReadPlayerSnapshot(activationId).Battlefield.TryPreviewMovement(activationId,path,out preview,out failure);
        public TargetPreview PreviewTarget(long activationId, AbilitySlot slot, string target)
        {
            var b = session.ReadPlayerSnapshot(activationId);
            bool valid = b.Battlefield.TryPreviewAbility(activationId,slot,target,out var effect,out var field,out var health);
            return new TargetPreview(target,valid,effect,field,health);
        }
        public IReadOnlyList<TargetPreview> PreviewTargets(long activationId, AbilitySlot slot)
        {
            var b = session.ReadPlayerSnapshot(activationId);
            return Array.AsReadOnly(b.InspectUnitIds().Select(target => {
                bool valid = b.Battlefield.TryPreviewAbility(activationId,slot,target,out var effect,out var field,out var health);
                return new TargetPreview(target,valid,effect,field,health);
            }).ToArray());
        }
        public InteractionFailure PreviewInteraction(long activationId, string objective)
            => session.ReadPlayerSnapshot(activationId).Mission.PreviewInteraction(activationId,objective);
        public BattlePlanningView PreviewPlanning(long activationId)
        {
            var b = session.ReadPlayerSnapshot(activationId);
            var samples = new List<ReachableSample>();
            var turn = b.CurrentActivation; var origin = b.Battlefield.GetPosition(turn.UnitId);
            var bounds = b.Battlefield.Map.Bounds;
            // At most 168 queries on one detached snapshot. Dots are verified destinations,
            // not an assertion that unsampled ground is unreachable. No per-frame search.
            decimal spacing = Math.Max(.5m, decimal.Ceiling(turn.MovementRemaining / 6m * 2m) / 2m);
            if (turn.MovementRemaining > 0)
                for (int x=-6;x<=6;x++) for (int z=-6;z<=6;z++)
                {
                    if (x == 0 && z == 0) continue;
                    decimal px=origin.X+x*spacing, pz=origin.Z+z*spacing;
                    if (px < bounds.Min.X || px > bounds.Max.X || pz < bounds.Min.Z || pz > bounds.Max.Z) continue;
                    var point = new FieldPoint(px,origin.Y,pz);
                    if (b.Battlefield.TryFindPath(activationId,point,out var route,out _,64)) samples.Add(new ReachableSample(point,route.Cost));
                }
            var objectives = b.Mission.Definition.Objectives.Where(o=>o.Interaction!=null)
                .Select(o=>new ObjectiveInteractionPreview(o.Id,b.Mission.PreviewInteraction(activationId,o.Id)));
            return new BattlePlanningView(samples,objectives);
        }
        public bool TryMove(long activationId, IEnumerable<FieldPoint> path, out BattleUpdate update, out FieldFailure failure)
        {
            update = null; var before = Read();
            if (!session.TryMove(activationId,path,out var movement,out failure)) return false;
            update = Finish(before,new[] { new BattleEvent(BattleEventKind.Movement,before.Activation.UnitId,movement:movement) }); return true;
        }
        public bool TryUseAbility(long activationId, AbilitySlot slot, string target, out BattleUpdate update,
            out FieldFailure failure, out HealthActionFailure healthFailure)
        {
            update = null; var before = Read();
            if (!session.TryUseAbility(activationId,slot,target,out var effect,out failure,out healthFailure)) return false;
            update = Finish(before,new[] { new BattleEvent(BattleEventKind.AbilityUsed,before.Activation.UnitId,target,slot:slot,effect:effect) }); return true;
        }
        public bool TryInteract(long activationId, string objective, out BattleUpdate update, out InteractionFailure failure)
        {
            update = null; var before = Read();
            if (!session.TryInteract(activationId,objective,out failure)) return false;
            update = Finish(before,new[] { new BattleEvent(BattleEventKind.Interaction,before.Activation.UnitId,objective:objective) }); return true;
        }
        public BattleUpdate EndPlayerTurn(long activationId)
        { var before = Read(); session.EndPlayerTurn(activationId); return Finish(before,Array.Empty<BattleEvent>()); }
        public BattleUpdate Advance(EnemyPerception perception = null)
        {
            var before = Read(); session.AdvanceDetailed(perception,out var enemy);
            var events = new List<BattleEvent>();
            if (enemy?.Movement != null) events.Add(new BattleEvent(BattleEventKind.Movement,enemy.UnitId,movement:enemy.Movement));
            if (enemy?.UsedSlot != null) events.Add(new BattleEvent(BattleEventKind.AbilityUsed,enemy.UnitId,enemy.AbilityEffect.TargetId,slot:enemy.UsedSlot,effect:enemy.AbilityEffect));
            return Finish(before,events);
        }
        private BattleUpdate Finish(BattleView before, IEnumerable<BattleEvent> commandEvents)
        {
            var after = Read(); var events = commandEvents.ToList();
            foreach (var unit in after.Units)
            {
                var old = before.Units.FirstOrDefault(u => u.Id == unit.Id);
                if (old == null) continue;
                if (old.Health?.CurrentHealth != unit.Health?.CurrentHealth) events.Add(new BattleEvent(BattleEventKind.HealthChanged,unit.Id));
                if (!SameStatuses(old.Statuses,unit.Statuses)) events.Add(new BattleEvent(BattleEventKind.StatusChanged,unit.Id));
                if (old.IsEligible && !unit.IsEligible) events.Add(new BattleEvent(BattleEventKind.UnitRemoved,unit.Id));
            }
            if (before.Activation != null && before.Activation.ActivationId != after.Activation?.ActivationId)
                events.Add(new BattleEvent(BattleEventKind.ActivationEnded,before.Activation.UnitId));
            if (before.Round != after.Round) events.Add(new BattleEvent(BattleEventKind.RoundStarted));
            if (after.Activation != null && after.Activation.ActivationId != before.Activation?.ActivationId)
                events.Add(new BattleEvent(BattleEventKind.ActivationStarted,after.Activation.UnitId));
            foreach (var objective in after.Objectives)
            {
                var old = before.Objectives.FirstOrDefault(o => o.Id == objective.Id);
                if (old == null || old.Status != objective.Status || old.Progress != objective.Progress || old.Rescued != objective.Rescued)
                    events.Add(new BattleEvent(BattleEventKind.ObjectiveChanged,objective:objective.Id));
            }
            if (before.Result == null && after.Result != null) events.Add(new BattleEvent(BattleEventKind.BattleEnded));
            return new BattleUpdate(before,after,events);
        }
        private static bool SameStatuses(IReadOnlyList<StatusView> a, IReadOnlyList<StatusView> b)
            => a.Count == b.Count && a.All(x => b.Any(y => x.Id == y.Id && x.Stacks == y.Stacks && x.RemainingOwnerActivations == y.RemainingOwnerActivations));
    }
}
