using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)> AshboundSkeletonTests()
    {
        yield return ("Ashbound skin rig contains valid connected nineteen-joint Avarin hierarchy", SkeletonTopology);
        yield return ("Ashbound two-joint weights are continuous finite and normalized", SkeletonWeights);
        yield return ("Ashbound first-contact actual OBJ anatomy binds to stable rig joints", SkeletonOriginalMeshes);
        yield return ("Ashbound six skinned clips are finite and return to rest when nonterminal", SkeletonClipBoundaries);
        yield return ("Ashbound skinned clips move gait attack mantle strain and defeat independently", SkeletonMotionDetail);
    }

    private static void SkeletonTopology()
    {
        Equal(19,AshboundSkeleton.Count);
        var names=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<AshboundSkeleton.Count;i++)
        {
            var j=AshboundSkeleton.Bones[i];
            if(!names.Add(j.Name)||string.IsNullOrWhiteSpace(j.Name))
                throw new Exception("Duplicate skeleton joint name.");
            if(j.Parent>=i || j.Parent< -1 ||
               (i==0 && j.Parent!=-1)||(i!=0 && j.Parent<0))
                throw new Exception("Disconnected or forward-pointing bone hierarchy.");
            if(float.IsNaN(j.X)||float.IsNaN(j.Y)||float.IsNaN(j.Z))
                throw new Exception("Invalid binding transform.");
        }
        Equal("LivingCrescentMantle",AshboundSkeleton.Bones[6].Name);
        Equal("LeftAnkle",AshboundSkeleton.Bones[15].Name);
        Equal("RightAnkle",AshboundSkeleton.Bones[18].Name);
    }
    private static void ValidateWeight(string part,float[] coordinate)
    {
        var w=AshboundSkeleton.Skin(part,coordinate);
        if(w.First<0||w.First>=AshboundSkeleton.Count ||
           w.Second<0||w.Second>=AshboundSkeleton.Count)
            throw new Exception("Skin assigns nonexistent bone: "+part);
        if(float.IsNaN(w.Primary)||float.IsNaN(w.Secondary) ||
           w.Primary<0 || w.Secondary<0 || w.Primary>1 || w.Secondary>1 ||
           Math.Abs(w.Primary+w.Secondary-1)>1e-6)
            throw new Exception("Skin weight is not normalized: "+part);
        if(w.First!=w.Second &&
            AshboundSkeleton.Bones[w.First].Parent!=w.Second &&
            AshboundSkeleton.Bones[w.Second].Parent!=w.First)
            throw new Exception("Skin interpolates non-neighboring bones: "+part);
    }
    private static void SkeletonWeights()
    {
        foreach(var name in new[]{
            "Avarin_Torso","Avarin_Pelvis","Avarin_Neck","Avarin_TempleFace",
            "Avarin_LivingCrescentMantle","Avarin_CrescentRim",
            "Avarin_Leg_L","Avarin_Leg_R","Avarin_Foot_L","Avarin_Foot_R",
            "Avarin_Arm_L","Avarin_Arm_R","Avarin_Hand_L","Avarin_Hand_R",
            "Forager_Probe","Forager_SensorEnd",
            "Striker_RightForearmPlate","Striker_ImpactNode",
            "Wake_EngineeredCaptureHousing","Salvage_WaistHousing"
        })
        {
            for(int k=0;k<=100;k++)
                ValidateWeight(name,new[]{.11f,.001f+k*.018f,.04f});
        }
        Equal(15,AshboundSkeleton.Skin("Avarin_Foot_L",new[]{-.15f,.06f,.1f}).First);
        Equal(18,AshboundSkeleton.Skin("Avarin_Foot_R",new[]{.15f,.06f,.1f}).First);
        Equal(6,AshboundSkeleton.Skin("Avarin_LivingCrescentMantle",
            new[]{.21f,1.81f,-.12f}).Second);
        Throws<ArgumentException>(()=>AshboundSkeleton.Skin("",new[]{0f,0f,0f}));
        Throws<ArgumentException>(()=>AshboundSkeleton.Skin("Avarin_Foot_L",
            new[]{0f,float.PositiveInfinity,0f}));
    }
    private static void SkeletonOriginalMeshes()
    {
        string strider=StriderRoot();
        string project=Directory.GetParent(Directory.GetParent(strider)!.FullName)!.FullName;
        foreach(string name in new[]{"RiftForager","PartedStriker"})
        {
            string folder=Path.Combine(project,"SourceArt","Ashbound",name);
            var mat=GreatStriderSource.ReadMaterials(
                File.ReadLines(Path.Combine(folder,name+".mtl")),folder);
            var source=GreatStriderSource.ReadMesh(
                File.ReadLines(Path.Combine(folder,name+".obj")),mat);
            int checkedVertices=0;
            var witnessed=new HashSet<int>();
            foreach(var section in source.Sections)
            {
                Equal(section.Vertices.Count,section.SourceParts.Count);
                for(int i=0;i<section.Vertices.Count;i++)
                {
                    var w=AshboundSkeleton.Skin(section.SourceParts[i],section.Vertices[i]);
                    ValidateWeight(section.SourceParts[i],section.Vertices[i]);
                    witnessed.Add(w.First);witnessed.Add(w.Second);
                    checkedVertices++;
                }
            }
            if(checkedVertices<18000||!witnessed.Contains(6)||
                !witnessed.Contains(15)||!witnessed.Contains(18)||
                !witnessed.Contains(5))
                throw new Exception("Actual mesh failed bind coverage: "+name);
            Console.WriteLine("INFO "+name+" skin-mapped vertices: "+checkedVertices);
        }
    }
    private static void SkeletonClipBoundaries()
    {
        foreach(bool striker in new[]{false,true})
        foreach(var clip in (AshboundMotion.Clip[])Enum.GetValues(typeof(AshboundMotion.Clip)))
        {
            var duration=AshboundMotion.Duration(clip,striker);
            if(duration<.5 || duration>4)throw new Exception("Invalid clip length.");
            for(int bone=0;bone<AshboundSkeleton.Count;bone++)
            {
                var start=AshboundSkeleton.Sample(bone,clip,0,striker);
                if(Math.Abs(start.X)+Math.Abs(start.Y)+Math.Abs(start.Z)+Math.Abs(start.PositionY)>1e-5)
                    throw new Exception("Incorrect bind pose first frame");
                if(clip!=AshboundMotion.Clip.Defeat)
                {
                    var last=AshboundSkeleton.Sample(bone,clip,1,striker);
                    if(Math.Abs(last.X)+Math.Abs(last.Y)+Math.Abs(last.Z)+Math.Abs(last.PositionY)>1e-5)
                        throw new Exception("Nonterminal bone does not return to rest.");
                }
                for(int i=0;i<=100;i++)
                {
                    var pose=AshboundSkeleton.Sample(bone,clip,i/100.0,striker);
                    if(float.IsNaN(pose.X)||float.IsInfinity(pose.X)||
                       float.IsNaN(pose.Y)||float.IsInfinity(pose.Y)||
                       float.IsNaN(pose.Z)||float.IsInfinity(pose.Z)||
                       float.IsNaN(pose.PositionY)||float.IsInfinity(pose.PositionY))
                        throw new Exception("Nonfinite skeletal keyframe");
                }
            }
        }
        Throws<ArgumentOutOfRangeException>(()=>AshboundSkeleton.Sample(
            0,AshboundMotion.Clip.Idle,double.NaN,false));
        Throws<ArgumentOutOfRangeException>(()=>AshboundSkeleton.Sample(
            -1,AshboundMotion.Clip.Idle,0,false));
    }
    private static void SkeletonMotionDetail()
    {
        foreach(bool striker in new[]{false,true})
        {
            var left=AshboundSkeleton.Sample(13,AshboundMotion.Clip.Advance,.25,striker);
            var right=AshboundSkeleton.Sample(16,AshboundMotion.Clip.Advance,.25,striker);
            if(left.X*right.X>=0||Math.Abs(left.X)<8||Math.Abs(right.X)<8)
                throw new Exception("Legs do not alternate realistically in rig.");
            var rightAttack=AshboundSkeleton.Sample(11,AshboundMotion.Clip.Attack,.5,striker);
            if(Math.Abs(rightAttack.X)<25)
                throw new Exception("Rig lacks differentiated forearm attack.");
            var feedback=AshboundSkeleton.Sample(6,AshboundMotion.Clip.Afterecho,.45,striker);
            if(Math.Abs(feedback.X)<1 || Math.Abs(feedback.Z)<1)
                throw new Exception("Sensory mantle lacks Afterecho deformation.");
            var defeated=AshboundSkeleton.Sample(1,AshboundMotion.Clip.Defeat,1,striker);
            if(defeated.PositionY>-.15f)
                throw new Exception("Defeat fails to lower center of gravity.");
        }
    }
}
