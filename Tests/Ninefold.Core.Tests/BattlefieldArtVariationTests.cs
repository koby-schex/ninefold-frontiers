using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;
using Ninefold.Core.Missions;
using Ninefold.Core.Persistence;
using A=Ninefold.Core.Content.BattlefieldArtPlan;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> BattlefieldArtVariationTests()
    {
        yield return ("Quick Battle generates bounded distinct secondary cover and keeps main layout, spawns and rules",BattlefieldOptionalVariants);
        yield return ("All fourteen story maps stay exact; optional cover is explicit and restricted to later defeat-only missions",BattlefieldStoryProtection);
        yield return ("Fresh generated map persists through the actual mission flow checkpoint and reopen",BattlefieldSavedVariation);
        yield return ("All fourteen story terrains and varied quick arenas have complete flat floor, exact cover and safe exterior relief",BattlefieldArtMeshGeometry);
        yield return ("Art moods, deterministic seeds and terrain mesh resource budgets remain distinct and bounded",BattlefieldArtMoodSeeds);
    }
    private static string MapSignature(BattlefieldMap map)
        =>String.Join("|",map.Obstacles.Select(o=>o.Bounds.Min.X+","+o.Bounds.Min.Z+","+o.Bounds.Max.X+","+o.Bounds.Max.Z));
    private static FieldBody[] ArtBodies(ContentCatalog c)=>c.Units.Values.Select(u=>u.Body).ToArray();
    private static string ArtAttempt(int n)=>new Guid(n,0,0,new byte[8]).ToString("N");
    private static bool InBox(Vector3 p,FieldBox b,float eps=.0001f)=>
        p.X>=(float)b.Min.X-eps&&p.X<=(float)b.Max.X+eps&&p.Y>=(float)b.Min.Y-eps&&p.Y<=(float)b.Max.Y+eps&&
        p.Z>=(float)b.Min.Z-eps&&p.Z<=(float)b.Max.Z+eps;
    private static bool InFootprint(Vector3 p,FieldBox b,float eps=.0001f)=>
        p.X>=(float)b.Min.X-eps&&p.X<=(float)b.Max.X+eps&&p.Z>=(float)b.Min.Z-eps&&p.Z<=(float)b.Max.Z+eps;
    private static void BattlefieldOptionalVariants()
    {
        var c=PlaytestContentPackage.Create();var m=c.Missions["fixture-mixed"];var bodies=ArtBodies(c);
        Equal(true,m.ProceduralCover);
        var variants=new HashSet<string>();
        for(int i=1;i<=128;i++)
        {
            string seed=ArtAttempt(i);
            var map=BattlefieldVariation.Resolve(m,seed,bodies);
            Equal(MapSignature(map),MapSignature(BattlefieldVariation.Resolve(m,seed,bodies)));
            Equal(m.Map.Bounds,map.Bounds);Equal(m.Map.Obstacles.Count,map.Obstacles.Count);
            Equal(m.Map.Obstacles[0],map.Obstacles[0]);Equal(m.Map.Ground.Count,map.Ground.Count);
            var old=m.Map.Obstacles.Last();var changed=map.Obstacles.Last();
            Equal(old.BlocksShots,changed.BlocksShots);Equal(old.BlocksMovement,changed.BlocksMovement);
            Equal(old.CoverReduction,changed.CoverReduction);
            Equal(old.Bounds.Max.X-old.Bounds.Min.X,changed.Bounds.Max.X-changed.Bounds.Min.X);
            Equal(old.Bounds.Max.Z-old.Bounds.Min.Z,changed.Bounds.Max.Z-changed.Bounds.Min.Z);
            if(Math.Abs(changed.Bounds.Min.X-old.Bounds.Min.X)>.75m||Math.Abs(changed.Bounds.Min.Z-old.Bounds.Min.Z)>.75m)
                throw new Exception("Secondary cover exceeded shift budget.");
            foreach(var p in m.Deployment.Concat(m.Actors.Select(a=>a.Position)))
                if(p.X>changed.Bounds.Min.X-.55m&&p.X<changed.Bounds.Max.X+.55m&&p.Z>changed.Bounds.Min.Z-.55m&&p.Z<changed.Bounds.Max.Z+.55m)
                    throw new Exception("Cover too close to a spawn");
            variants.Add(MapSignature(map));
        }
        if(variants.Count<12)throw new Exception("Procedural cover only has "+variants.Count+" resolved layouts.");
        Equal(m.Map,BattlefieldVariation.Resolve(m,"catalog-validation",bodies));
        Equal(m.Map,BattlefieldVariation.Resolve(m,ArtAttempt(1),new[]{new FieldBody(5,5,2,new FieldPoint(0,0,0),new FieldPoint(0,0,0))}));
        Console.WriteLine("INFO Quick Battle: "+variants.Count+" distinct layouts / 128 stable seeds.");
    }
    private static void BattlefieldStoryProtection()
    {
        var c=WorldFiveTutorialWildlifeV1.Create();
        foreach(var m in c.Missions.Values.Where(m=>m.IsCampaign))
        {
            Equal(false,m.ProceduralCover);
            Equal(m.Map,BattlefieldVariation.Resolve(m,ArtAttempt(74),ArtBodies(c)));
        }
        Equal(false,PlaytestContentPackage.CreateLayoutV2().Missions["fixture-mixed"].ProceduralCover);
        var quick=c.Missions["fixture-mixed"];
        Equal(true,quick.ProceduralCover);
        Equal(false,BattlefieldVariation.IsLaterOptional("w5-story-014"));
        Equal(true,BattlefieldVariation.IsLaterOptional("w5-story-015"));
        Throws<ArgumentException>(()=>new MissionContent("w5-story-014","fixture-a",true,1,quick.Map,quick.Deployment,
            quick.Actors,quick.Objectives[0],proceduralCover:true));
        Throws<ArgumentException>(()=>new MissionContent("w5-story-015","fixture-a",true,1,quick.Map,quick.Deployment,
            quick.Actors,ObjectiveDefinition.Survive("primary",2),proceduralCover:true));
        var future=new MissionContent("w5-story-015","fixture-a",true,1,quick.Map,quick.Deployment,
            quick.Actors,quick.Objectives[0],proceduralCover:true);
        Equal(true,future.ProceduralCover);
    }
    private static void BattlefieldSavedVariation()
    {
        var c=PlaytestContentPackage.Create();var files=new FakeSaveFiles();var progress=new ProgressFiles();
        var flow=c.CreateFlow(files,progress,"terrain-variation-test");
        flow.CreateProfile(c.StarterUnits);
        flow.Start("fixture-mixed",c.StarterUnits);
        var b=flow.ReadBattle();var map=b.Battlefield.Map;
        var expected=BattlefieldVariation.Resolve(c.Missions["fixture-mixed"],b.Mission.Definition.AttemptId,ArtBodies(c));
        Equal(MapSignature(expected),MapSignature(map));
        var resumed=c.CreateFlow(files,progress,"terrain-variation-test");resumed.Open();
        var again=resumed.ReadBattle();
        Equal(b.Mission.Definition.AttemptId,again.Mission.Definition.AttemptId);
        Equal(MapSignature(map),MapSignature(again.Battlefield.Map));
        foreach(var id in c.StarterUnits)Equal(b.Battlefield.GetPosition(id),again.Battlefield.GetPosition(id));
        // Save restoration reads real geometry even when the original map template differs.
        Equal(MapSignature(map),MapSignature(BattleSave.Restore(BattleSave.Capture(b,c.Manifest.SaveRevision),
            c.Manifest.SaveRevision).Battlefield.Map));
    }
    private static void CheckArtGeometry(BattlefieldMap map,A.Scene scene)
    {
        double floorArea=0;
        foreach(var mesh in scene.Meshes)
        {
            Equal(0,mesh.Vertices.Count%3);
            for(int i=0;i<mesh.Vertices.Count;i+=3)
            {
                var a=mesh.Vertices[i];var b=mesh.Vertices[i+1];var c=mesh.Vertices[i+2];
                var normal=Vector3.Cross(b-a,c-a);
                if(!float.IsFinite(normal.LengthSquared())||normal.LengthSquared()<1e-12f)
                    throw new Exception("Invalid terrain triangle "+mesh.Material);
                foreach(var p in new[]{a,b,c})
                {
                    switch(mesh.Material)
                    {
                        case A.Surface.Floor:case A.Surface.FloorShade:case A.Surface.FloorLight:
                        case A.Surface.Threads:case A.Surface.Rim:
                            if(!InFootprint(p,map.Bounds)||Math.Abs(p.Y)>.020f)
                                throw new Exception("Walkable floor left the legal plane");
                            break;
                        case A.Surface.Slow:case A.Surface.SlowEdge:
                            if(!map.Ground.Any(g=>InFootprint(p,g.Bounds))||p.Y>.02f)
                                throw new Exception("Slow-ground art lies outside its actual rule area");
                            break;
                        case A.Surface.Cover:case A.Surface.CoverBand:case A.Surface.CoverCrown:
                            if(!map.Obstacles.Any(o=>InBox(p,o.Bounds)))
                                throw new Exception("Cover geometry extends beyond the actual blocker");
                            break;
                        default:
                            if(p.Y>.001f&&InFootprint(p,map.Bounds))
                                throw new Exception("Landscape obstructs legal combat space "+mesh.Material);
                            break;
                    }
                }
                if((int)mesh.Material<=2)
                {
                    if(normal.Y<=0)throw new Exception("Floor triangle flipped");
                    floorArea+=normal.Y*.5;
                }
            }
        }
        double area=(double)((map.Bounds.Max.X-map.Bounds.Min.X)*(map.Bounds.Max.Z-map.Bounds.Min.Z));
        if(Math.Abs(floorArea-area)>.003)throw new Exception("Floor coverage mismatch "+floorArea+" / "+area);
        if(scene.Triangles<800||scene.Triangles>10000||scene.Meshes.Count(m=>m.Vertices.Count>0)>16)
            throw new Exception("Terrain geometry budget violated: "+scene.Triangles);
    }
    private static void BattlefieldArtMeshGeometry()
    {
        var c=WorldFiveTutorialWildlifeV1.Create();
        foreach(var m in c.Missions.Values)
        {
            var map=BattlefieldVariation.Resolve(m,ArtAttempt(18),ArtBodies(c));
            CheckArtGeometry(map,A.Build(map,m.Id,m.EnvironmentKey,ArtAttempt(18),m.ProceduralCover));
        }
        Console.WriteLine("INFO all campaign fields and optional arena: exact flat coverage and surface bounds verified.");
    }
    private static void BattlefieldArtMoodSeeds()
    {
        var c=WorldFiveTutorialWildlifeV1.Create();
        string Signature(A.Scene s)=>String.Join("|",s.Meshes.Select(m=>m.Vertices.Count==0?"":m.Vertices[m.Vertices.Count/2].ToString()));
        var moods=new HashSet<A.Mood>();
        foreach(var m in c.Missions.Values.Where(m=>m.IsCampaign))
        {
            var a=A.Build(m.Map,m.Id,m.EnvironmentKey,ArtAttempt(1),false);
            var b=A.Build(m.Map,m.Id,m.EnvironmentKey,ArtAttempt(2),false);
            Equal(a.Seed,b.Seed);Equal(Signature(a),Signature(b));moods.Add(a.Mood);
        }
        Equal(5,moods.Count);
        var quick=c.Missions["fixture-mixed"];
        var one=A.Build(quick.Map,quick.Id,quick.EnvironmentKey,ArtAttempt(1),true);
        var two=A.Build(quick.Map,quick.Id,quick.EnvironmentKey,ArtAttempt(2),true);
        Equal(A.Mood.Neutral,one.Mood);
        Equal(false,one.Seed==two.Seed);Equal(false,Signature(one)==Signature(two));
    }
}
