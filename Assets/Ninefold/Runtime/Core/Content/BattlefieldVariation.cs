using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ninefold.Core.Combat;

namespace Ninefold.Core.Content
{
    /// <summary>Frozen v1 deterministic, bounded cover variation for fresh attempts.
    /// Existing snapshots contain their resolved geometry and are never regenerated.</summary>
    public static class BattlefieldVariation
    {
        public const string Revision="battlefield-cover-variation-01";
        public static uint Seed(string value)
        {
            unchecked { uint h=2166136261; foreach(char c in value??""){h^=c;h*=16777619;}return h; }
        }
        public static uint Mix(uint seed,uint index)
        {
            unchecked {uint n=seed+0x9e3779b9u*(index+1);n=(n^(n>>16))*0x85ebca6bu;
                n=(n^(n>>13))*0xc2b2ae35u;return n^(n>>16);}
        }
        public static bool IsLaterOptional(string id) => id!=null &&
            id.StartsWith("w5-story-",StringComparison.Ordinal)&&id.Length==12&&
            int.TryParse(id.Substring(9),NumberStyles.None,CultureInfo.InvariantCulture,out int n)&&n>=15&&n<=999;

        public static BattlefieldMap Resolve(MissionContent mission,string attempt,IEnumerable<FieldBody> bodies)
        {
            if(mission==null)throw new ArgumentNullException(nameof(mission));
            var map=mission.Map;
            if(!mission.ProceduralCover||!Guid.TryParseExact(attempt,"N",out _)||map.Obstacles.Count<2)
                return map;
            var all=bodies?.ToArray()??Array.Empty<FieldBody>();
            if(all.Length==0)return map;
            decimal hw=all.Max(b=>b.HalfWidth),hd=all.Max(b=>b.HalfDepth),height=all.Max(b=>b.Height);
            // Preserve the central authored cover and the total obstacle count;
            // only try the final, secondary obstacle. All flags/cost stay intact.
            var source=map.Obstacles[map.Obstacles.Count-1];
            if(!source.BlocksMovement||source.Bounds.Min.Y!=map.Bounds.Min.Y)return map;
            var retained=map.Obstacles.Take(map.Obstacles.Count-1).ToArray();
            var placements=mission.Deployment.Concat(mission.Actors.Select(a=>a.Position)).ToArray();
            uint seed=Seed(Revision+"/"+mission.Id+"/"+attempt);
            var offsets=new[]{-.75m,-.50m,-.25m,.25m,.50m,.75m};
            for(uint i=0;i<12;i++)
            {
                decimal dx=offsets[Mix(seed,i*2)%6];
                decimal dz=offsets[Mix(seed,i*2+1)%6];
                var a=source.Bounds.Min;var b=source.Bounds.Max;
                var box=new FieldBox(new FieldPoint(a.X+dx,a.Y,a.Z+dz),new FieldPoint(b.X+dx,b.Y,b.Z+dz));
                // Expanded rectangle proof: every blocking box is interior,
                // spaced apart and clear of every spawn for the largest body.
                // Therefore a path around every obstacle remains available.
                bool Interior(FieldBox p)=>p.Min.X>map.Bounds.Min.X+2*hw+.15m &&
                    p.Max.X<map.Bounds.Max.X-2*hw-.15m &&
                    p.Min.Z>map.Bounds.Min.Z+2*hd+.15m && p.Max.Z<map.Bounds.Max.Z-2*hd-.15m;
                bool Separated(FieldBox p,FieldBox q)=>p.Max.X+2*hw+.15m<q.Min.X ||
                    q.Max.X+2*hw+.15m<p.Min.X || p.Max.Z+2*hd+.15m<q.Min.Z || q.Max.Z+2*hd+.15m<p.Min.Z;
                if(!Interior(box)||retained.Any(o=>o.BlocksMovement&&(!Interior(o.Bounds)||!Separated(o.Bounds,box))))continue;
                var blocking=retained.Where(o=>o.BlocksMovement).ToArray();
                if(blocking.SelectMany((p,j)=>blocking.Skip(j+1).Select(q=>!Separated(p.Bounds,q.Bounds))).Any(v=>v))continue;
                if(placements.Any(p=>box.Overlaps(new FieldBox(
                    new FieldPoint(p.X-hw-.30m,p.Y,p.Z-hd-.30m),
                    new FieldPoint(p.X+hw+.30m,p.Y+height,p.Z+hd+.30m)))))continue;
                if(map.Ground.Any(g=>g.Bounds.Overlaps(box)))continue;
                if(retained.Any(o=>o.Bounds.Overlaps(box)))continue;
                // Preserve any other retained geometry exactly. No new terrain,
                // enemy, objective, cover count, bounds or movement costs.
                var moved=new FieldObstacle(box,source.BlocksMovement,source.BlocksShots,source.CoverReduction);
                return new BattlefieldMap(map.Bounds,retained.Concat(new[]{moved}),map.Ground);
            }
            return map; // bounded safe fallback; never invalidate a start.
        }
    }
}
