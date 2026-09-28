using System;
using System.Linq;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattlefieldController
    {
        internal bool IsInside(string id, FieldBox zone) => zone.Contains(units[id].Body.At(units[id].Position));
        internal bool OverlapsZone(string id, FieldBox zone) => zone.Overlaps(units[id].Body.At(units[id].Position));
        internal InteractionFailure CheckInteraction(string id, FieldPoint point, decimal reach)
        {
            if (!units.TryGetValue(id,out var actor)) return InteractionFailure.IneligibleActor;
            // Authored target offset is the provisional interaction reference, not a universal anatomical reach claim.
            var origin = FieldPoint.Add(actor.Position,actor.Body.TargetOffset);
            if (FieldPoint.Distance(origin,point) > reach) return InteractionFailure.OutOfReach;
            if (Map.Obstacles.Any(o => (o.BlocksMovement || o.BlocksShots) && o.Bounds.Intersects(origin,point,out _,out _)))
                return InteractionFailure.Obstructed;
            return InteractionFailure.None;
        }
    }
}
