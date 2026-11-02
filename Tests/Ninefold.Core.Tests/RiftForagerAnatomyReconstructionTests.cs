using System;
using System.Collections.Generic;
using System.Linq;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name,Action Run)>
        RiftForagerAnatomyReconstructionTests()
    {
        yield return ("Organic reconstruction removes only old disconnected Avarin torso and limbs",RebuildIdentity);
        yield return ("Reconstructed Avarin is one closed indexed surface with distinct face and feet",RebuildTopology);
        yield return ("Reconstructed Rift Forager body has finite smooth normals and bounded dimensions",RebuildSurface);
        yield return ("Reconstructed organic skinning has four normalized anatomical influences",RebuildWeights);
    }

    private static RiftForagerAnatomyReconstruction.Surface body;
    private static RiftForagerAnatomyReconstruction.Surface Body() =>
        body??=RiftForagerAnatomyReconstruction.Generate();

    private static void RebuildIdentity()
    {
        foreach(string p in new[]{"Avarin_Torso","Avarin_Pelvis","Avarin_Neck",
            "Avarin_TempleFace","Avarin_Leg_L","Avarin_Leg_R",
            "Avarin_Foot_L","Avarin_Foot_R","Avarin_Arm_L",
            "Avarin_Arm_R","Avarin_Hand_L","Avarin_Hand_R"})
            if(!RiftForagerAnatomyReconstruction.ReplacesOldBodyPart(p))
                throw new Exception("Did not remove original shape primitive: "+p);
        foreach(string p in new[]{"Avarin_LivingCrescentMantle",
            "Avarin_CrescentRim","Avarin_Eye_L","Avarin_Eye_R",
            "Avarin_TempleRelief_L","Avarin_TempleRelief_R",
            "Avarin_MantleSensingRibbon_L","Avarin_ForeheadRidge",
            "Forager_Probe","Wake_EngineeredCaptureHousing",
            "Salvage_SplitFieldApron","fixture-npc","","Ashbound_Riftwalker"})
            if(RiftForagerAnatomyReconstruction.ReplacesOldBodyPart(p))
                throw new Exception("Wrongly erased canonical/provisional source feature: "+p);
    }

    private static void RebuildTopology()
    {
        var m=Body();
        if(m.Vertices.Count<4000 || m.TriangleCount<8000 ||
           m.TriangleCount>160000)
            throw new Exception("Continuous anatomy is empty or too costly to review.");
        if(m.SkinIndices.Count==0 || m.GarmentIndices.Count==0)
            throw new Exception("Face and garments lost distinct materials.");
        var uses=new Dictionary<ulong,int>();
        void Count(int a,int b)
        {
            uint low=(uint)Math.Min(a,b),high=(uint)Math.Max(a,b);
            ulong key=((ulong)low<<32)|high;
            uses[key]=uses.TryGetValue(key,out var v)?v+1:1;
        }
        foreach(var tris in new[]{m.GarmentIndices,m.SkinIndices})
        for(int i=0;i<tris.Count;i+=3)
        {
            int a=tris[i],b=tris[i+1],c=tris[i+2];
            if(a==b||b==c||c==a)throw new Exception("Degenerate indexed face.");
            Count(a,b);Count(b,c);Count(c,a);
        }
        if(uses.Count<9000)
            throw new Exception("The source is still primitive-scale complexity.");
        var missing=uses.Count(x=>x.Value!=2);
        if(missing>0)
            throw new Exception("Seamless anatomy has "+missing+
                " boundary/nonmanifold edges out of "+uses.Count);
    }

    private static void RebuildSurface()
    {
        var m=Body();
        Equal(m.Vertices.Count,m.Normals.Count);
        Equal(m.Vertices.Count,m.UVs.Count);
        float minY=100,maxY=-100,minX=100,maxX=-100;
        bool head=false,feet=false,shoulder=false;
        for(int i=0;i<m.Vertices.Count;i++)
        {
            var p=m.Vertices[i];var n=m.Normals[i];var uv=m.UVs[i];
            if(p.Length!=3||n.Length!=3||uv.Length!=2 ||
                p.Any(v=>float.IsNaN(v)||float.IsInfinity(v)) ||
                n.Any(v=>float.IsNaN(v)||float.IsInfinity(v)) ||
                uv.Any(v=>float.IsNaN(v)||float.IsInfinity(v)))
                throw new Exception("Invalid generated rest geometry/UV/normal.");
            float size=n[0]*n[0]+n[1]*n[1]+n[2]*n[2];
            if(Math.Abs(size-1)>2e-3)
                throw new Exception("Incorrect gradient surface normal.");
            minY=Math.Min(minY,p[1]);maxY=Math.Max(maxY,p[1]);
            minX=Math.Min(minX,p[0]);maxX=Math.Max(maxX,p[0]);
            if(p[1]>1.73f)head=true;
            if(p[1]<.08f)feet=true;
            if(Math.Abs(p[0])>.25f && p[1]>1.1f)shoulder=true;
        }
        if(minY<-.016f || minY>.055f ||
           maxY<1.75f || maxY>1.89f ||
           minX>-.27f || maxX<.27f ||
           !head || !feet || !shoulder)
            throw new Exception("Organic biped anatomy exceeds sensible profile: "+
                minX+","+maxX+" / "+minY+","+maxY);
        for(int side=-1;side<=1;side+=2)
        {
            float x=side*.155f;
            if(RiftForagerAnatomyReconstruction.Field(x,.07f,.12f)>=0)
                throw new Exception("Foot is missing from connected surface.");
        }
        if(RiftForagerAnatomyReconstruction.Field(0,1.65f,.01f)>=0 ||
           RiftForagerAnatomyReconstruction.Field(0,1.18f,0)>=0)
            throw new Exception("Face or torso interior missing.");
        Throws<ArgumentOutOfRangeException>(()=>
            RiftForagerAnatomyReconstruction.Generate(.12f));
    }

    private static void RebuildWeights()
    {
        var m=Body();
        bool face=false,legs=false,arms=false;
        foreach(var p in m.Vertices)
        {
            var w=AshboundSkeleton.SkinOrganic(p);
            var bs=new[]{w.A,w.B,w.C,w.D};
            var fs=new[]{w.WA,w.WB,w.WC,w.WD};
            if(bs.Any(i=>i<0||i>=AshboundSkeleton.Count) ||
               fs.Any(v=>float.IsNaN(v)||float.IsInfinity(v) || v<0 || v>1) ||
               Math.Abs(w.Total-1)>1e-5)
                throw new Exception("Continuous organic rest mesh is not fully skinnable.");
            if(p[1]>1.72f && (bs.Contains(5)||bs.Contains(4)))face=true;
            if(p[1]<.38f && (bs.Contains(14)||bs.Contains(17)||
                bs.Contains(15)||bs.Contains(18)))legs=true;
            if(Math.Abs(p[0])>.265f && p[1]>.98f &&
                (bs.Contains(7)||bs.Contains(10)||bs.Contains(8)||bs.Contains(11)))
                arms=true;
        }
        if(!face||!legs||!arms)
            throw new Exception("Skin weight regions lack anatomical deformation.");
        Throws<ArgumentException>(()=>
            AshboundSkeleton.SkinOrganic(new[]{0f,float.NaN,0f}));
    }
}
