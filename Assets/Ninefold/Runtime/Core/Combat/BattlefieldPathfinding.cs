using System;
using System.Collections.Generic;
using System.Linq;

namespace Ninefold.Core.Combat
{
    public sealed partial class BattlefieldController
    {
        /// <summary>
        /// Cheapest route on a bounded visibility graph, not a continuous weighted-path optimum.
        /// Uses current occupancy and the same collision/cost rules as supplied-path commands.
        /// </summary>
        public bool TryFindPath(long activationId, FieldPoint destination, out MovementPreview preview,
            out FieldFailure failure, int maximumNodes = 256)
            => FindPath(activationId,destination,out preview,out failure,maximumNodes,false);

        private bool FindPath(long activationId, FieldPoint destination, out MovementPreview preview,
            out FieldFailure failure, int maximumNodes, bool allowBeyondBudget)
        {
            preview = null; failure = FieldFailure.None;
            if (!Active(activationId)) return Fail(FieldFailure.InactiveTurn, out failure);
            if (maximumNodes < 2 || maximumNodes > 256) return Fail(FieldFailure.InvalidRequest, out failure);
            string id = turns.CurrentActivation.UnitId;
            if (!units.TryGetValue(id, out var actor)) return Fail(FieldFailure.MissingPosition, out failure);
            var start = actor.Position;
            if (start.Equals(destination)) return Fail(FieldFailure.InvalidRequest, out failure);
            if (start.Y != destination.Y) return Fail(FieldFailure.UnsupportedElevation, out failure);
            if (!LegalPosition(id, actor, destination)) return Fail(FieldFailure.IllegalDestination, out failure);
            decimal minimumCost = Math.Ceiling(FieldPoint.Distance(start,destination)*1000000m)/1000000m;
            if (!allowBeyondBudget && minimumCost > turns.CurrentActivation.MovementRemaining)
                return Fail(FieldFailure.InsufficientMovement, out failure);
            // A clear normal-ground straight line cannot be improved by a detour.
            if (ClearSegment(id, actor, start, destination) && SegmentCost(start,destination,actor.Body) == minimumCost)
            {
                preview = new MovementPreview(new[] { destination }, minimumCost); return true;
            }

            var candidates = new HashSet<FieldPoint> { start, destination };
            foreach (var obstacle in Map.Obstacles)
                if (obstacle.BlocksMovement && !AddCorners(obstacle.Bounds)) return Fail(FieldFailure.SearchLimitExceeded, out failure);
            foreach (var unit in units)
                if (unit.Key != id && turns.IsUnitEligible(unit.Key) && !AddCorners(unit.Value.Body.At(unit.Value.Position)))
                    return Fail(FieldFailure.SearchLimitExceeded, out failure);
            foreach (var ground in Map.Ground)
                if (ground.CostPerMeter > 1m && !AddCorners(ground.Bounds)) return Fail(FieldFailure.SearchLimitExceeded, out failure);

            // Stable ordering makes equal-cost choices independent of map/enrollment order.
            var nodes = candidates.OrderBy(n => n.X).ThenBy(n => n.Z).ToArray();
            int startIndex = Array.IndexOf(nodes,start), endIndex = Array.IndexOf(nodes,destination);
            var distance = Enumerable.Repeat(decimal.MaxValue,nodes.Length).ToArray();
            var previous = Enumerable.Repeat(-1,nodes.Length).ToArray();
            var visited = new bool[nodes.Length];
            distance[startIndex] = 0m;
            for (int step = 0; step < nodes.Length; step++)
            {
                int current = -1;
                for (int i=0; i<nodes.Length; i++)
                    if (!visited[i] && distance[i] != decimal.MaxValue && (current < 0 || distance[i] < distance[current])) current = i;
                if (current < 0) return Fail(FieldFailure.PathNotFound, out failure);
                if (current == endIndex)
                {
                    if (!allowBeyondBudget && distance[current] > turns.CurrentActivation.MovementRemaining)
                        return Fail(FieldFailure.InsufficientMovement, out failure);
                    var path = new List<FieldPoint>();
                    for (int at = endIndex; at != startIndex; at = previous[at]) path.Add(nodes[at]);
                    path.Reverse();
                    preview = new MovementPreview(path.ToArray(),distance[current]); return true;
                }
                visited[current] = true;
                for (int next=0; next<nodes.Length; next++)
                {
                    if (visited[next] || !ClearSegment(id,actor,nodes[current],nodes[next])) continue;
                    decimal cost = distance[current] + SegmentCost(nodes[current],nodes[next],actor.Body);
                    if (cost < distance[next]) { distance[next] = cost; previous[next] = current; }
                }
            }
            return Fail(FieldFailure.PathNotFound, out failure);

            bool AddCorners(FieldBox box)
            {
                if (start.Y > box.Max.Y || start.Y+actor.Body.Height < box.Min.Y) return true;
                // Closed collision volumes require a one-coordinate-quantum clearance.
                const decimal clearance = 0.000001m;
                decimal left = box.Min.X-actor.Body.HalfWidth-clearance, right = box.Max.X+actor.Body.HalfWidth+clearance;
                decimal bottom = box.Min.Z-actor.Body.HalfDepth-clearance, top = box.Max.Z+actor.Body.HalfDepth+clearance;
                foreach (decimal x in new[] { left,right })
                    foreach (decimal z in new[] { bottom,top })
                    {
                        if (x < Map.Bounds.Min.X+actor.Body.HalfWidth || x > Map.Bounds.Max.X-actor.Body.HalfWidth
                            || z < Map.Bounds.Min.Z+actor.Body.HalfDepth || z > Map.Bounds.Max.Z-actor.Body.HalfDepth) continue;
                        var point = new FieldPoint(x,start.Y,z);
                        if (LegalPosition(id,actor,point)) candidates.Add(point);
                        if (candidates.Count > maximumNodes) return false;
                    }
                return true;
            }
        }

        /// <summary>Preview the affordable prefix of a legal route. Never bypasses map bounds, occupancy or terrain cost.</summary>
        public bool TryFindReachablePath(long activationId, FieldPoint destination, out MovementPreview preview,
            out FieldFailure failure, int maximumNodes = 256)
        {
            preview = null;
            if (!FindPath(activationId,destination,out var full,out failure,maximumNodes,true)) return false;
            decimal remaining = turns.CurrentActivation.MovementRemaining;
            if (remaining <= 0) return Fail(FieldFailure.InsufficientMovement,out failure);
            if (full.Cost <= remaining) return TryPreviewMovement(activationId,full.Path,out preview,out failure);
            string id = turns.CurrentActivation.UnitId; var actor = units[id]; var from = actor.Position;
            var path = new List<FieldPoint>();
            foreach (var to in full.Path)
            {
                decimal cost = SegmentCost(from,to,actor.Body);
                if (cost <= remaining) { path.Add(to); remaining -= cost; from = to; continue; }
                // Search along this segment using the exact weighted/collision rules. Round to the
                // supported coordinate quantum, then revalidate the final prefix before exposing it.
                decimal lo = 0, hi = 1; var best = from;
                for (int i = 0; i < 60; i++)
                {
                    decimal t = (lo + hi) / 2;
                    var candidate = new FieldPoint(decimal.Round(from.X+(to.X-from.X)*t,6),from.Y,decimal.Round(from.Z+(to.Z-from.Z)*t,6));
                    if (LegalPosition(id,actor,candidate) && ClearSegment(id,actor,from,candidate) && SegmentCost(from,candidate,actor.Body) <= remaining)
                    { lo = t; best = candidate; }
                    else hi = t;
                }
                if (!best.Equals(from)) path.Add(best);
                break;
            }
            if (path.Count == 0) return Fail(FieldFailure.InsufficientMovement,out failure);
            return TryPreviewMovement(activationId,path,out preview,out failure);
        }

        /// <summary>Replans against current state and commits only a fully validated route.</summary>
        public bool TryMoveTo(long activationId, FieldPoint destination, out MovementPreview result,
            out FieldFailure failure, int maximumNodes = 256)
        {
            result = null;
            if (!TryFindPath(activationId,destination,out var route,out failure,maximumNodes)) return false;
            return TryMove(activationId,route.Path,out result,out failure);
        }
    }
}
