using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> AshboundMotionTests()
    {
        yield return ("Ashbound first-contact motion uses six deterministic authored review states", AshboundMotionClips);
        yield return ("Ashbound walk steps alternate while soles remain above baseline", AshboundMotionGait);
        yield return ("Ashbound attack, hit, Afterecho and defeat have distinct readable poses", AshboundMotionPoses);
        yield return ("Ashbound preview math preserves rest sources and cycle endpoints", AshboundMotionRestAndContinuity);
        yield return ("Ashbound real source meshes contain required movable Avarin anatomy", AshboundMotionMeshes);
    }

    private static float[] Point(float x, float y, float z) => new[] { x, y, z };

    private static float[] Pose(string part, float[] rest,
        AshboundMotion.Clip clip, double t, bool striker=false)
    {
        var frame=AshboundMotion.Sample(clip,t);
        AshboundMotion.Vertex(part,rest,frame,striker,
            out float x,out float y,out float z);
        foreach(float value in new[] {x,y,z})
            if(float.IsNaN(value)||float.IsInfinity(value))
                throw new Exception("Nonfinite animation coordinate in "+part);
        return new[] { x,y,z };
    }

    private static double Distance(float[] a,float[] b) =>
        Math.Sqrt(a.Zip(b,(x,y)=>(x-y)*(x-y)).Sum());

    private static void AshboundMotionClips()
    {
        var clips=(AshboundMotion.Clip[])Enum.GetValues(typeof(AshboundMotion.Clip));
        Equal(6,clips.Length);
        foreach(var clip in clips)
        {
            foreach(bool striker in new[]{false,true})
                if(AshboundMotion.Duration(clip,striker)<=.50)
                    throw new Exception("Unusable motion duration");
            foreach(double t in new[]{0.0,.01,.14,.33,.50,.71,.96,1.0})
            {
                var pose=Pose("Avarin_LivingCrescentMantle",
                    Point(.12f,1.72f,-.13f),clip,t);
                if(pose.Length!=3) throw new Exception("Bad pose size");
            }
        }
        Throws<ArgumentOutOfRangeException>(()=>AshboundMotion.Sample(
            AshboundMotion.Clip.Idle,double.NaN));
        Throws<ArgumentOutOfRangeException>(()=>AshboundMotion.Sample(
            AshboundMotion.Clip.Idle,double.PositiveInfinity));
        Throws<ArgumentException>(()=>AshboundMotion.Vertex(
            "Avarin_Foot_L",new[]{float.NaN,0f,0f},
            AshboundMotion.Sample(AshboundMotion.Clip.Idle,0),
            false,out _,out _,out _));
    }

    private static void AshboundMotionGait()
    {
        foreach(bool striker in new[]{false,true})
        {
            float x=.154f;
            float lz=.091f+(striker ? .17f : .07f)*-.26f;
            float rz=.091f+(striker ? .17f : .07f);
            var left=Point(-x,.063f,lz);
            var right=Point(x,.063f,rz);
            var a=Pose("Avarin_Foot_L",left,AshboundMotion.Clip.Advance,.25,striker);
            var b=Pose("Avarin_Foot_R",right,AshboundMotion.Clip.Advance,.25,striker);
            if(Distance(a,left)<.035 || Distance(b,right)<.035)
                throw new Exception("Walk should deform both legs, not rigidly bob the body.");
            for(int i=0;i<=100;i++)
            {
                double t=i/100.0;
                var lf=Pose("Avarin_Foot_L",left,AshboundMotion.Clip.Advance,t,striker);
                var rf=Pose("Avarin_Foot_R",right,AshboundMotion.Clip.Advance,t,striker);
                if(lf[1]<.060 || rf[1]<.060)
                    throw new Exception("A walk sole sank beneath its source contact plane.");
                if(lf[1]>.137 || rf[1]>.137)
                    throw new Exception("A walk swing exceeds the limited foot-lift study envelope.");
            }
        }
    }

    private static void AshboundMotionPoses()
    {
        foreach(bool striker in new[]{false,true})
        {
            var right=Point(.32f,1.02f,.13f);
            var head=Point(.047f,1.68f,.11f);
            var attacking=Pose("Avarin_Arm_R",right,AshboundMotion.Clip.Attack,.5,striker);
            var hit=Pose("Avarin_Torso",Point(0,1.28f,.1f),AshboundMotion.Clip.Hit,.5,striker);
            var neutral=Point(0,1.28f,.1f);
            var after=Pose("Avarin_LivingCrescentMantle",Point(.12f,1.72f,-.14f),
                AshboundMotion.Clip.Afterecho,.42,striker);
            var afterRest=Point(.12f,1.72f,-.14f);
            var defeated=Pose("Avarin_TempleFace",head,AshboundMotion.Clip.Defeat,1,striker);
            if(Distance(attacking,right)<.06)throw new Exception("Attack must have purposeful reach.");
            if(Distance(hit,neutral)<.025)throw new Exception("Hit recoil is not visible.");
            if(Distance(after,afterRest)<.003)throw new Exception("Afterecho has no visible sensory strain.");
            if(defeated[1]>=head[1]-.02)
                throw new Exception("Defeat pose does not compress and settle the body.");
        }
    }

    private static void AshboundMotionRestAndContinuity()
    {
        foreach(bool striker in new[]{false,true})
        {
            var samples=new[] {
                ("Avarin_Foot_L",Point(-.154f,.063f,.069f)),
                ("Avarin_Arm_R",Point(.28f,1.15f,.04f)),
                ("Avarin_TempleFace",Point(.046f,1.68f,.10f)),
                ("Avarin_LivingCrescentMantle",Point(.20f,1.71f,-.13f)),
                ("Striker_RightForearmPlate",Point(.321f,1.06f,.13f)),
                ("Forager_Probe",Point(.320f,1.05f,.37f))
            };
            foreach(var item in samples)
            {
                foreach(var clip in (AshboundMotion.Clip[])Enum.GetValues(typeof(AshboundMotion.Clip)))
                {
                    var atStart=Pose(item.Item1,item.Item2,clip,0,striker);
                    if(Distance(atStart,item.Item2)>1e-6)
                        throw new Exception(clip+" starts with a displaced "+item.Item1);
                    if(clip!=AshboundMotion.Clip.Defeat)
                    {
                        var atEnd=Pose(item.Item1,item.Item2,clip,1,striker);
                        if(Distance(atEnd,item.Item2)>1e-6)
                            throw new Exception(clip+" cannot blend back to "+item.Item1);
                    }
                    float[] previous=atStart;
                    for(int t=1;t<=100;t++)
                    {
                        var next=Pose(item.Item1,item.Item2,clip,t/100.0,striker);
                        if(Distance(previous,next)>.11)
                            throw new Exception("Discontinuous motion "+clip+" on "+item.Item1);
                        previous=next;
                    }
                }
            }
        }
    }

    private static void AshboundMotionMeshes()
    {
        foreach(string name in new[]{"RiftForager","PartedStriker"})
        {
            string root=StriderRoot().Replace(
                Path.Combine("SourceArt","GreatStrider"),Path.Combine("SourceArt","Ashbound",name));
            var materials=GreatStriderSource.ReadMaterials(
                File.ReadLines(Path.Combine(root,name+".mtl")),root);
            var data=GreatStriderSource.ReadMesh(
                File.ReadLines(Path.Combine(root,name+".obj")),materials);
            if(data.SourceVertices<18000 || data.Triangles<36000)
                throw new Exception("Missing source geometry for "+name);
            var parts=data.Sections.SelectMany(s=>s.SourceParts).ToHashSet();
            foreach(string required in new[]{
                "Avarin_LivingCrescentMantle","Avarin_TempleFace",
                "Avarin_Foot_L","Avarin_Foot_R","Avarin_Arm_L","Avarin_Arm_R"
            })
                if(!parts.Contains(required))
                    throw new Exception("Motion preview anatomy disappeared: "+required);
            foreach(var section in data.Sections)
                if(section.SourceParts.Count!=section.Vertices.Count)
                    throw new Exception("Per-source-part binding lost in imported "+name);
        }
    }
}
