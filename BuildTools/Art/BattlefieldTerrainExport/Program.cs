using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Ninefold.Core.Combat;
using Ninefold.Core.Content;

internal static class Program
{
    private static float[] V(Vector3 p)=>new[]{p.X,p.Y,p.Z};
    private static float[] F(FieldPoint p)=>new[]{(float)p.X,(float)p.Y,(float)p.Z};
    private static object Box(FieldBox b)=>new {min=F(b.Min),max=F(b.Max)};
    public static int Main(string[] args)
    {
        if(args.Length!=1)throw new ArgumentException("Output directory required.");
        Directory.CreateDirectory(args[0]);
        var catalog=WorldFiveConvergence.Create();
        var bodies=catalog.Units.Values.Select(u=>u.Body).ToArray();
        var manifest=new List<object>();
        void Export(MissionContent mission,int seed,string label)
        {
            string attempt=new Guid(seed,0,0,new byte[8]).ToString("N");
            var map=BattlefieldVariation.Resolve(mission,attempt,bodies);
            var scene=BattlefieldArtPlan.Build(map,mission.Id,mission.EnvironmentKey,attempt,mission.ProceduralCover);
            var data=new {mission=mission.Id,label,mood=scene.Mood.ToString(),seed=scene.Seed,
                bounds=Box(map.Bounds),cover=map.Obstacles.Select(o=>Box(o.Bounds)),
                deployment=mission.Deployment.Select(F),hostiles=mission.Actors.Select(a=>F(a.Position)),
                sky=V(scene.Sky),fill=V(scene.Fill),palette=scene.Palette.Select(V),
                meshes=scene.Meshes.Where(m=>m.Vertices.Count>0).Select(m=>new {
                    material=(int)m.Material,slot=m.Material.ToString(),vertices=m.Vertices.Select(V)})};
            string file=label+".json";
            File.WriteAllText(Path.Combine(args[0],file),JsonSerializer.Serialize(data));
            manifest.Add(new {file,scene.Triangles,drawBatches=scene.Meshes.Count(m=>m.Vertices.Count>0),
                label,mood=scene.Mood.ToString(),missionId=mission.Id});
            Console.WriteLine(label+" / "+scene.Mood+" / "+scene.Triangles+" real triangles");
        }
        foreach(var id in WorldFiveOpeningTenPrototype.MissionIds
            .Concat(new[]{WorldFiveStorySequence.FirstCampaignMissionId}).Concat(WorldFiveEncounterVarietyV2.NewMissionIds))
            Export(catalog.Missions[id],17,id);
        foreach(var chapter in WorldFiveRouteRecovery.All.Concat(WorldFiveEscalation.All).Concat(WorldFiveConvergence.All))Export(chapter.Mission,17,chapter.Mission.Id);
        for(int i=1;i<=4;i++)Export(catalog.Missions["fixture-mixed"],i,"quick-"+i);
        File.WriteAllText(Path.Combine(args[0],"manifest.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));
        return 0;
    }
}
