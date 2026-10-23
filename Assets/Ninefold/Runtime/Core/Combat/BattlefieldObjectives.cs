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
            // Ground objectives use planar approach range, NOT the unit's
            // elevated attack/target offset. Otherwise an adjacent carrier can
            // be permanently unable to rescue a ground-level civilian because
            // vertical body height consumes the full pickup reach.
            // Keep the elevated origin for the actual obstruction ray below.
            var origin = FieldPoint.Add(actor.Position,actor.Body.TargetOffset);
            decimal dx=actor.Position.X-point.X, dz=actor.Position.Z-point.Z;
            if (dx*dx+dz*dz > reach*reach) return InteractionFailure.OutOfReach;
            if (Map.Obstacles.Any(o => (o.BlocksMovement || o.BlocksShots) && o.Bounds.Intersects(origin,point,out _,out _)))
                return InteractionFailure.Obstructed;
            return InteractionFailure.None;
        }
    }
}
