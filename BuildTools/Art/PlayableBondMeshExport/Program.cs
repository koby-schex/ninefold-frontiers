using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using R=Ninefold.Core.Content.PlayableBondRig;
using S=Ninefold.Core.Content.PlayableBondSource;

internal static class Program
{
    private static string F(float x)=>x.ToString("R",CultureInfo.InvariantCulture);
    private static float[] Coordinates(Vector3 a)=>new[]{a.X,a.Y,a.Z};
    // Unity Quaternion.Euler applies local Z, then X, then Y. This
    // independent inspection transforms the exact shared rig math in the
    // same order; it does not replace native Unity deformation review.
    private static Quaternion Rotation(Vector3 e)
    {
        const float rad=MathF.PI/180f;
        return Quaternion.CreateFromAxisAngle(Vector3.UnitY,e.Y*rad)*
            Quaternion.CreateFromAxisAngle(Vector3.UnitX,e.X*rad)*
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ,e.Z*rad);
    }
    private static Vector3 Move(Vector3 v,Vector3 pivot,R.BonePose pose,Quaternion q)
        =>Vector3.Transform(v-pivot,q)+pivot+pose.Translation;
    private static void Write(string dir,string label,R.Bond kind,S.Model model,S.Model shelter,
        R.Plan plan,float fit,Vector3 center,float bottom,bool active,bool protect)
    {
        float cycle=active?.94f:0f;
        float motion=active?1f:0f;
        var poses=Enumerable.Range(0,plan.Pivots.Length).Select(i=>
            R.Pose(plan,i,cycle,motion,0,0,0,0)).ToArray();
        var rot=poses.Select(p=>Rotation(p.Euler)).ToArray();
        using(var output=new StreamWriter(Path.Combine(dir,label+".obj"),false,new UTF8Encoding(false)))
        {
            output.WriteLine("# Exact packed runtime source, posed by shared core rig. Not a Unity screenshot.");
            int offset=0;
            foreach(var section in model.Sections)
            {
                output.WriteLine("o "+section.Name.Replace("/","_"));
                var normals=new Vector3[section.Vertices.Count];
                for(int i=0;i<section.Vertices.Count;i++)
                {
                    string part=section.SourceParts[i];
                    Vector3 original=R.V(section.Vertices[i]),v=original,n=R.V(section.Normals[i]);
                    if(protect&&shelter!=null&&R.IsOlyra(part))
                    {
                        int source=section.SourceIndices[i];
                        v=R.ProtectiveVertex(part,original,R.V(shelter.RawVertices[source]));
                        n=R.V(shelter.RawNormals[source]);
                    }
                    var w=R.Skin(plan,part,original);
                    v=Move(v,plan.Pivots[w.A],poses[w.A],rot[w.A])*w.W+
                        Move(v,plan.Pivots[w.B],poses[w.B],rot[w.B])*(1-w.W);
                    n=Vector3.Normalize(Vector3.Transform(n,rot[w.A])*w.W+
                        Vector3.Transform(n,rot[w.B])*(1-w.W));
                    v=(v-new Vector3(center.X,bottom,center.Z))*fit;
                    output.WriteLine("v "+F(v.X)+" "+F(v.Y)+" "+F(v.Z));
                    normals[i]=n;
                }
                foreach(var uv in section.UVs)output.WriteLine("vt "+F(uv[0])+" "+F(uv[1]));
                foreach(var n in normals)output.WriteLine("vn "+F(n.X)+" "+F(n.Y)+" "+F(n.Z));
                output.WriteLine("usemtl "+section.Material);
                for(int i=0;i<section.Indices.Count;i+=3)
                {
                    output.Write("f");
                    for(int c=0;c<3;c++)
                    {
                        int index=section.Indices[i+c]+offset+1;
                        output.Write(" "+index+"/"+index+"/"+index);
                    }
                    output.WriteLine();
                }
                offset+=section.Vertices.Count;
            }
        }
    }

    public static int Main(string[] args)
    {
        if(args.Length!=2)throw new ArgumentException("Supply packed source directory and output directory.");
        string source=args[0],dir=args[1];Directory.CreateDirectory(dir);
        var reports=new List<object>();
        foreach(R.Bond kind in Enum.GetValues(typeof(R.Bond)))
        {
            string name=kind.ToString(),folder=Path.Combine(source,name);
            var surfaces=S.ReadMaterials(File.ReadLines(Path.Combine(folder,name+".mtl.bytes")),
                p=>File.Exists(Path.Combine(folder,p+".bytes")));
            var model=S.ReadMesh(File.ReadLines(Path.Combine(folder,name+".obj.bytes")),surfaces);
            var plan=R.Build(kind,model);
            Vector3 min=plan.Min,max=plan.Max;
            S.Model shelter=null;
            if(kind==R.Bond.LivingVeil)
            {
                shelter=S.ReadMesh(File.ReadLines(Path.Combine(folder,"LivingVeilProtective.obj.bytes")),surfaces);
                S.ValidateCorrespondence(model,shelter);
                foreach(var section in model.Sections)
                for(int i=0;i<section.Vertices.Count;i++)
                {
                    var p=R.ProtectiveVertex(section.SourceParts[i],R.V(section.Vertices[i]),
                        R.V(shelter.RawVertices[section.SourceIndices[i]]));
                    min=Vector3.Min(min,p);max=Vector3.Max(max,p);
                }
            }
            float fit=R.Fit(min,max,1.45f,1.42f);
            Vector3 center=(min+max)*.5f;
            Write(dir,name+"-rest",kind,model,shelter,plan,fit,center,min.Y,false,false);
            Write(dir,name+"-motion",kind,model,shelter,plan,fit,center,min.Y,true,false);
            if(shelter!=null)Write(dir,name+"-protective",kind,model,shelter,plan,fit,center,min.Y,false,true);
            reports.Add(new {name,model.SourceVertices,model.Triangles,model.Parts,
                sections=model.Sections.Count,bones=R.BoneCount(kind),companionLimbs=R.Limbs(kind),
                normalizedSize=Coordinates((max-min)*fit),fittingScale=fit});
            Console.WriteLine(name+": "+model.Triangles+" triangles / "+model.Sections.Count+" material sections.");
        }
        File.WriteAllText(Path.Combine(dir,"playable-bond-metrics.json"),
            JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));
        return 0;
    }
}
