using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Core.Combat;
using Ninefold.Core.Missions;

namespace Ninefold.Core.Content
{
    /// <summary>
    /// Deterministic, non-mutating CONTENT AUTHORING diagnostic for prototype
    /// objective access. Samples body-valid ground on a quarter-meter grid;
    /// this is not enemy AI, a runtime movement path, or a battle solvability
    /// proof. Ground costs, combat turns and changing occupancy are not modeled.
    /// </summary>
    public static class OpeningRouteReadiness
    {
        private const decimal Step = .25m;
        private const int MaxCells = 15000;

        public sealed class Finding
        {
            public string MissionId { get; }
            public string ObjectiveId { get; }
            public string Code { get; }
            internal Finding(string mission,string objective,string code)
            { MissionId=mission; ObjectiveId=objective; Code=code; }
        }
        public sealed class Report
        {
            public int MissionsChecked { get; }
            public IReadOnlyList<Finding> Findings { get; }
            public bool Passes => Findings.Count == 0;
            internal Report(int count,IEnumerable<Finding> findings)
            { MissionsChecked=count; Findings=Array.AsReadOnly(findings.ToArray()); }
        }

        /// <summary>
        /// Review a bounded authored set with the first deployed starter's
        /// real collision body and normal-attack range. A future different
        /// unit body requires a separate authoring pass.
        /// </summary>
        public static Report Review(ContentCatalog catalog,IEnumerable<string> missionIds)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var ids=(missionIds ?? throw new ArgumentNullException(nameof(missionIds))).ToArray();
            if (ids.Length == 0 || ids.Length > 100 || ids.Any(string.IsNullOrWhiteSpace) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new ArgumentException("Select 1–100 distinct authored mission IDs.",nameof(missionIds));
            var starter=catalog.Units[catalog.StarterUnits[0]];
            var kit=catalog.Kits[starter.KitId];
            var normal=catalog.Abilities[kit.AbilityIds[(int)AbilitySlot.NormalAttack]].Effect;
            var failures=new List<Finding>();
            foreach (string id in ids)
            {
                if (!catalog.Missions.TryGetValue(id,out var mission))
                    throw new ArgumentException("Unknown mission: "+id,nameof(missionIds));
                failures.AddRange(ReviewMission(mission,starter.Body,normal.Range,catalog.Units));
            }
            return new Report(ids.Length,failures);
        }

        /// <summary>
        /// Exposed for isolated negative-test fixtures. This search uses the
        /// first deployment only, keeping pickup/extraction connected to the
        /// actual designated '$leader' carrier's origin.
        /// </summary>
        public static IReadOnlyList<Finding> ReviewMission(MissionContent mission,
            FieldBody body,decimal normalAttackRange,
            IReadOnlyDictionary<string,UnitContent> unitTemplates)
        {
            if (mission == null || body == null || unitTemplates == null)
                throw new ArgumentNullException(nameof(mission));
            if (normalAttackRange < 0m) throw new ArgumentOutOfRangeException(nameof(normalAttackRange));
            var failures=new List<Finding>();
            var map=mission.Map;
            var origin=mission.Deployment[0];
            // Closed footprints must fit the whole field, not only their center.
            decimal minX=Math.Ceiling((map.Bounds.Min.X+body.HalfWidth)/Step)*Step;
            decimal maxX=Math.Floor((map.Bounds.Max.X-body.HalfWidth)/Step)*Step;
            decimal minZ=Math.Ceiling((map.Bounds.Min.Z+body.HalfDepth)/Step)*Step;
            decimal maxZ=Math.Floor((map.Bounds.Max.Z-body.HalfDepth)/Step)*Step;
            int nx=maxX>=minX?(int)((maxX-minX)/Step)+1:0;
            int nz=maxZ>=minZ?(int)((maxZ-minZ)/Step)+1:0;
            if (nx==0 || nz==0 || (long)nx*nz>MaxCells)
            {
                failures.Add(new Finding(mission.Id,"primary","FIELD_SAMPLE_LIMIT"));
                return failures.AsReadOnly();
            }

            var occupants=mission.Actors.Select(a => {
                if (!unitTemplates.TryGetValue(a.UnitId,out var template))
                    throw new ArgumentException("Missing actor body: "+a.UnitId,nameof(unitTemplates));
                return template.Body.At(a.Position);
            }).ToArray();

            FieldPoint Point(int x,int z) => new FieldPoint(minX+x*Step,origin.Y,minZ+z*Step);
            bool Legal(FieldPoint p)
            {
                var footprint=body.At(p);
                if (!map.Bounds.Contains(footprint)) return false;
                if (map.Obstacles.Any(o=>o.BlocksMovement && o.Bounds.Overlaps(footprint))) return false;
                return !occupants.Any(x=>x.Overlaps(footprint));
            }
            bool Traversable(FieldPoint from,FieldPoint to)
            {
                if (map.Obstacles.Any(o=>o.BlocksMovement &&
                    o.Bounds.Intersects(from,to,out _,out _,body.HalfWidth,body.Height,body.HalfDepth)))
                    return false;
                return !occupants.Any(x=>x.Intersects(from,to,out _,out _,
                    body.HalfWidth,body.Height,body.HalfDepth));
            }

            int startX=(int)((origin.X-minX)/Step), startZ=(int)((origin.Z-minZ)/Step);
            if (startX<0 || startX>=nx || startZ<0 || startZ>=nz ||
                !Point(startX,startZ).Equals(origin) || !Legal(origin))
            {
                failures.Add(new Finding(mission.Id,"primary","DEPLOYMENT_BLOCKED"));
                return failures.AsReadOnly();
            }

            var queue=new Queue<(int X,int Z)>();
            var reached=new bool[nx*nz];
            queue.Enqueue((startX,startZ));
            reached[startX*nz+startZ]=true;
            while(queue.Count>0)
            {
                var pos=queue.Dequeue();
                var from=Point(pos.X,pos.Z);
                foreach(var next in new[] {
                    (X:pos.X+1,Z:pos.Z),(X:pos.X-1,Z:pos.Z),
                    (X:pos.X,Z:pos.Z+1),(X:pos.X,Z:pos.Z-1) })
                {
                    if (next.X<0 || next.X>=nx || next.Z<0 || next.Z>=nz ||
                        reached[next.X*nz+next.Z]) continue;
                    var to=Point(next.X,next.Z);
                    if (!Legal(to) || !Traversable(from,to)) continue;
                    reached[next.X*nz+next.Z]=true;
                    queue.Enqueue(next);
                }
            }

            bool AnyReachable(Func<FieldPoint,bool> condition)
            {
                for(int x=0;x<nx;x++) for(int z=0;z<nz;z++)
                    if (reached[x*nz+z] && condition(Point(x,z))) return true;
                return false;
            }
            bool InRange(FieldPoint p,FieldPoint target,decimal reach)
            {
                var dx=p.X+body.TargetOffset.X-target.X;
                var dy=p.Y+body.TargetOffset.Y-target.Y;
                var dz=p.Z+body.TargetOffset.Z-target.Z;
                if (dx*dx+dy*dy+dz*dz>reach*reach) return false;
                var eye=new FieldPoint(p.X+body.TargetOffset.X,
                    p.Y+body.TargetOffset.Y,p.Z+body.TargetOffset.Z);
                return !map.Obstacles.Any(o=>(o.BlocksMovement || o.BlocksShots) &&
                    o.Bounds.Intersects(eye,target,out _,out _));
            }

            foreach(var obj in mission.Objectives)
            {
                // Protect/Survive are temporal contracts: no fake waypoint or
                // route guarantee is asserted for those objective kinds.
                switch(obj.Kind)
                {
                    case ObjectiveKind.Stabilize:
                    case ObjectiveKind.RescueAndExtract:
                        if (!AnyReachable(p=>InRange(p,obj.Interaction.Point,obj.Interaction.Reach)))
                            failures.Add(new Finding(mission.Id,obj.Id,"INTERACTION_UNREACHABLE"));
                        if (obj.Kind==ObjectiveKind.RescueAndExtract &&
                            !AnyReachable(p=>obj.Zone.Contains(body.At(p))))
                            failures.Add(new Finding(mission.Id,obj.Id,"EXTRACTION_UNREACHABLE"));
                        break;
                    case ObjectiveKind.Secure:
                        if (!AnyReachable(p=>obj.Zone.Contains(body.At(p))))
                            failures.Add(new Finding(mission.Id,obj.Id,"CONTROL_ZONE_UNREACHABLE"));
                        break;
                    case ObjectiveKind.Defeat:
                        foreach(string targetId in obj.Subjects)
                        {
                            var actor=mission.Actors.FirstOrDefault(a=>a.Id==targetId);
                            if (actor==null || !unitTemplates.TryGetValue(actor.UnitId,out var enemy))
                            {
                                failures.Add(new Finding(mission.Id,obj.Id,"TARGET_MISSING"));
                                continue;
                            }
                            var aim=new FieldPoint(actor.Position.X+enemy.Body.TargetOffset.X,
                                actor.Position.Y+enemy.Body.TargetOffset.Y,
                                actor.Position.Z+enemy.Body.TargetOffset.Z);
                            if (!AnyReachable(p=>InRange(p,aim,normalAttackRange)))
                                failures.Add(new Finding(mission.Id,obj.Id,"ATTACK_APPROACH_UNREACHABLE"));
                        }
                        break;
                }
            }
            return failures.AsReadOnly();
        }
    }
}
