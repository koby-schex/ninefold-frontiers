using System;
using System.Globalization;
using System.IO;
using System.Text;
using S=Ninefold.Core.Content.FirstEncounterSculpts;

internal static class Program
{
    private static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
    public static int Main(string[] args)
    {
        if(args.Length!=1)throw new ArgumentException("Supply review output directory.");
        Directory.CreateDirectory(args[0]);
        foreach(S.Enemy kind in Enum.GetValues(typeof(S.Enemy)))
        {
            // EXACT tactical rest mesh and source material assignment used
            // in Unity, not a second model built by the visual review tool.
            var m=S.Tactical(kind);
            string path=Path.Combine(args[0],kind+"-RuntimeBody.obj");
            WriteMesh(path,kind+"_RuntimeBody",m);
            Console.WriteLine(kind+" "+m.Vertices.Count+" vertices; "+m.TriangleCount+
                " triangles; "+S.BoneCount(kind)+" anatomical joints.");
        }
        var cloth=Ninefold.Core.Content.FirstEncounterForagerCloth.Tactical();
        WriteMesh(Path.Combine(args[0],"RiftForager-RuntimeCloth.obj"),
            "RiftForager_RuntimeCloth",cloth);
        Console.WriteLine("RiftForager fieldcloth "+cloth.TriangleCount+" triangles.");
        return 0;
    }

    private static void WriteMesh(string path,string label,S.Surface m)
    {
        string[] names={"body","accent","dark"};
        using(var output=new StreamWriter(path,false,new UTF8Encoding(false)))
        {
            output.WriteLine("# Actual runtime rest body / not native Unity screenshot");
            output.WriteLine("# Rift Forager retained runtime gear and mantle are separate from this body");
            output.WriteLine("o "+label);
            foreach(var v in m.Vertices)
                output.WriteLine("v "+F(v[0])+" "+F(v[1])+" "+F(v[2]));
            foreach(var uv in m.UVs)
                output.WriteLine("vt "+F(uv[0])+" "+F(uv[1]));
            foreach(var n in m.Normals)
                output.WriteLine("vn "+F(n[0])+" "+F(n[1])+" "+F(n[2]));
            output.WriteLine("s 1");
            for(int slot=0;slot<3;slot++)
            {
                if(m.Indices[slot].Count==0)continue;
                output.WriteLine("usemtl "+names[slot]);
                var faces=m.Indices[slot];
                for(int i=0;i<faces.Count;i+=3)
                {
                    output.Write("f");
                    for(int k=0;k<3;k++)
                    {
                        int n=faces[i+k]+1;
                        output.Write(" "+n+"/"+n+"/"+n);
                    }
                    output.WriteLine();
                }
            }
        }
    }
}
