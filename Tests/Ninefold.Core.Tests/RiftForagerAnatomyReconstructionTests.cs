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
        yield return ("Rift Forager procedural rest sculpt has smooth anatomical landmark definition",RebuildAnatomicalPlanes);
        yield return ("Higher-density Avarin jaw sculpt remains localized and retains facial detail clearance",RebuildJawSculptDensity);
        yield return ("Rift Forager rebuilt face exposes existing source eyes, mouth and brow",RebuildSourceFeatureClearance);
        yield return ("Rift Forager UV output splits wrap without losing faces or deformation data",RebuildUvSeams);
        yield return ("Rift Forager four-bone skinning remains spatially continuous across sculpted joints",RebuildWeightContinuity);
    }

    private static RiftForagerAnatomyReconstruction.Surface reconstructedAnatomy;
    private static RiftForagerAnatomyReconstruction.Surface OrganicAnatomy() =>
        reconstructedAnatomy??=RiftForagerAnatomyReconstruction.Generate();

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
        var m=OrganicAnatomy();
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
        var m=OrganicAnatomy();
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
        var m=OrganicAnatomy();
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

    private static void RebuildAnatomicalPlanes()
    {
        // Real local volume must be present in the same SDF, not in
        // separately rendered tubes, plates or invented creature organs.
        foreach(float side in new[]{-1f,1f})
        {
            float f=side;
            foreach(var center in new[]{
                new[]{f*.063f,1.636f,.058f},   // cheek and temple transition
                new[]{f*.116f,1.365f,.063f},   // collar shoulder tissue
                new[]{f*.131f,1.294f,-.106f}, // scapula behind torso
                new[]{f*.255f,1.045f,.075f},  // lower forearm connection
                new[]{f*.135f,.485f,.079f},   // knee tissue
                new[]{f*.159f,.061f,.212f}    // forward anatomical foot
            })
                if(RiftForagerAnatomyReconstruction.Field(
                    center[0],center[1],center[2])>=0)
                    throw new Exception(
                        "Required connected natural anatomical landmark is missing.");
        }
    }

    private static void RebuildJawSculptDensity()
    {
        // This is a PHYSICAL surface-detail pass, not a normal-map or
        // cosmetic material trick. Smaller marching cells refine the actual
        // connected body triangles used by the skinned Unity review asset.
        if(RiftForagerAnatomyReconstruction.DefaultStep>.024f)
            throw new Exception("Facial/shoulder sculpt resolution regressed to coarse facets.");
        var surface=OrganicAnatomy();
        if(surface.TriangleCount<32000 || surface.TriangleCount>90000)
            throw new Exception("Unexpected face-resolution triangle budget: "+surface.TriangleCount);

        float inset=RiftForagerAnatomyReconstruction.JawContour(.100f,1.613f,.040f);
        float symmetric=RiftForagerAnatomyReconstruction.JawContour(-.100f,1.613f,.040f);
        float eye=RiftForagerAnatomyReconstruction.JawContour(.044f,1.680f,.110f);
        float mouth=RiftForagerAnatomyReconstruction.JawContour(0f,1.592f,.109f);
        float torso=RiftForagerAnatomyReconstruction.JawContour(.100f,1.20f,.040f);
        if(inset<.0085f || inset>.010f || Math.Abs(inset-symmetric)>1e-7f ||
           eye>.001f || mouth>.0001f || torso>.00001f)
            throw new Exception("Lower face contour became asymmetric or moved eye, mouth or torso.");
    }

    private static void RebuildSourceFeatureClearance()
    {
        // Actual old OBJ feature locations from build_ashbound.mjs.
        // Positive Field is *outside* the new continuous skin; features
        // need visible clearance, but should not float far from the face.
        float left=RiftForagerAnatomyReconstruction.Field(-.044f,1.68f,.110f);
        float right=RiftForagerAnatomyReconstruction.Field(.044f,1.68f,.110f);
        if(left<.004f || left>.027f || right<.004f || right>.027f ||
           Math.Abs(left-right)>.006f)
            throw new Exception("Reconstructed skull hides or detaches original paired eye geometry.");
        float mouth=RiftForagerAnatomyReconstruction.Field(0,1.592f,.109f);
        if(mouth<.002f || mouth>.025f)
            throw new Exception("Reconstructed jaw hides or detaches original source mouth.");
        float brow=RiftForagerAnatomyReconstruction.Field(0,1.738f,.092f);
        if(brow<.0005f || brow>.025f)
            throw new Exception("Reconstructed forehead buries the original living brow ridge.");
        float sash=RiftForagerAnatomyReconstruction.Field(-.060f,1.23f,.115f);
        if(sash<.0005f || sash>.030f)
            throw new Exception("Reconstructed chest buries or detaches the original diagonal field braid.");
        if(RiftForagerAnatomyReconstruction.Field(0,1.65f,.01f)>=0)
            throw new Exception("A feature-clearance carve has accidentally hollowed out the skull.");
    }

    private static void RebuildUvSeams()
    {
        var raw=OrganicAnatomy();
        var corrected=RiftForagerAnatomyReconstruction.MakeTextureReady(raw);
        Equal(raw.TriangleCount,corrected.TriangleCount);
        Equal(corrected.Vertices.Count,corrected.Normals.Count);
        Equal(corrected.Vertices.Count,corrected.UVs.Count);
        if(corrected.Vertices.Count<=raw.Vertices.Count)
            throw new Exception("Back-of-body texture wrap was not split.");
        foreach(var list in new[]{corrected.SkinIndices,corrected.GarmentIndices})
        for(int i=0;i<list.Count;i+=3)
        {
            int a=list[i],b=list[i+1],c=list[i+2];
            if(a<0||a>=corrected.Vertices.Count||
               b<0||b>=corrected.Vertices.Count||
               c<0||c>=corrected.Vertices.Count)
                throw new Exception("UV seam has an out-of-range triangle.");
            float u0=corrected.UVs[a][0],u1=corrected.UVs[b][0],
                u2=corrected.UVs[c][0];
            float span=Math.Max(u0,Math.Max(u1,u2))-
                Math.Min(u0,Math.Min(u1,u2));
            if(span>.50001f)
                throw new Exception("A texture-facing triangle still crosses "+
                    "the U=0/1 wrap by "+span);
        }

        // Every render-only seam duplication must be in exactly the same
        // rest position and normal as some original raw vertex, preserving
        // weighted deformation across the seam.
        var keys=new HashSet<string>(raw.Vertices.Select((point,i)=>
            string.Join(",",point.Select(v=>v.ToString("R",
                System.Globalization.CultureInfo.InvariantCulture)))+"|"+
            string.Join(",",raw.Normals[i].Select(v=>v.ToString("R",
                System.Globalization.CultureInfo.InvariantCulture)))));
        for(int i=raw.Vertices.Count;i<corrected.Vertices.Count;i++)
        {
            var k=string.Join(",",corrected.Vertices[i].Select(v=>
                v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+
                "|"+string.Join(",",corrected.Normals[i].Select(v=>
                v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            if(!keys.Contains(k))
                throw new Exception("Seam duplication moved actual anatomy/normal.");
        }
        Throws<ArgumentNullException>(()=>
            RiftForagerAnatomyReconstruction.MakeTextureReady(null!));
    }

    private static void RebuildWeightContinuity()
    {
        float[] Vector(float[] point)
        {
            var w=AshboundSkeleton.SkinOrganic(point);
            var values=new float[AshboundSkeleton.Count];
            values[w.A]+=w.WA;values[w.B]+=w.WB;
            values[w.C]+=w.WC;values[w.D]+=w.WD;
            return values;
        }
        foreach(float side in new[]{-1f,1f})
        foreach(var center in new[]{
            new[]{side*.165f,1.39f,-.021f}, // shoulder blend
            new[]{side*.250f,1.125f,.048f},// elbow region
            new[]{side*.108f,.86f,-.02f},  // hip
            new[]{side*.135f,.49f,.025f},  // knee
            new[]{side*.060f,1.64f,.03f}  // temple/face
        })
        {
            var a=Vector(center);
            var b=Vector(new[]{center[0]+side*.001f,
                center[1]+.001f,center[2]});
            float difference=0;
            for(int i=0;i<a.Length;i++)
                difference+=Math.Abs(a[i]-b[i]);
            if(difference>.115f)
                throw new Exception("Skin weight discontinuity near anatomical joint: "+
                    difference);
        }
    }
}
