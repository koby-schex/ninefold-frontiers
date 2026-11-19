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
            if(!S.IsAvarin(kind))
                WriteMesh(Path.Combine(args[0],kind+"-RuntimeEyes.obj"),
                    kind+"_RuntimeEyes",S.EyeShells(kind));
            Console.WriteLine(kind+" "+m.Vertices.Count+" vertices; "+m.TriangleCount+
                " triangles; "+S.BoneCount(kind)+" anatomical joints.");
        }
        var cloth=Ninefold.Core.Content.FirstEncounterForagerCloth.Tactical();
        WriteMesh(Path.Combine(args[0],"RiftForager-RuntimeCloth.obj"),
            "RiftForager_RuntimeCloth",cloth);
        Console.WriteLine("RiftForager fieldcloth "+cloth.TriangleCount+" triangles.");
        string roster=Path.Combine(args[0],"FullAshboundRoster");
        Directory.CreateDirectory(roster);
        for(int k=0;k<6;k++)WriteComplete(roster,k);
        return 0;
    }

    private static void WriteComplete(string directory,int role)
    {
        var kind=S.Ashbound(role);
        string path=Path.Combine(directory,kind+"-CompleteRuntime.obj");
        using(var writer=new StreamWriter(path,false,new UTF8Encoding(false)))
        {
            writer.WriteLine("# The exact assembled live rest geometry. Not a Unity pose or final art approval.");
            writer.WriteLine("# Identical body, cloth and wearable source used in WorldFiveBattleOrganics.");
            writer.WriteLine("mtllib AshboundRuntime.mtl");
            int offset=0;
            void Add(string label,S.Surface geometry,string[] slots)
            {
                writer.WriteLine("o "+label.Replace(" ","_"));
                foreach(var v in geometry.Vertices)
                    writer.WriteLine("v "+F(v[0])+" "+F(v[1])+" "+F(v[2]));
                foreach(var uv in geometry.UVs)
                    writer.WriteLine("vt "+F(uv[0])+" "+F(uv[1]));
                foreach(var n in geometry.Normals)
                    writer.WriteLine("vn "+F(n[0])+" "+F(n[1])+" "+F(n[2]));
                for(int slot=0;slot<3;slot++)
                {
                    var faces=geometry.Indices[slot];if(faces.Count==0)continue;
                    writer.WriteLine("usemtl "+slots[slot]);
                    for(int i=0;i<faces.Count;i+=3)
                    {
                        writer.Write("f");
                        for(int j=0;j<3;j++)
                        {
                            int index=faces[i+j]+offset+1;
                            writer.Write(" "+index+"/"+index+"/"+index);
                        }
                        writer.WriteLine();
                    }
                }
                offset+=geometry.Vertices.Count;
            }
            Add(kind+"_Body",S.Tactical(kind),new[]{"skin","fabric","dark"});
            Add(kind+"_Cloth",Ninefold.Core.Content.FirstEncounterForagerCloth.Tactical(role),
                new[]{"skin","fabric","dark"});
            var parts=Ninefold.Core.Content.AshboundWearablePlan.Build(role);
            foreach(var piece in parts)
            {
                string material=piece.Role.ToString().ToLowerInvariant();
                Add(piece.Name,Ninefold.Core.Content.AshboundWearablePlan.Geometry(piece),
                    new[]{material,material,material});
            }
            Console.WriteLine(kind+" full rest asset: "+offset+" vertices with "+parts.Count+" exact wearables.");
        }
        File.WriteAllText(Path.Combine(directory,"AshboundRuntime.mtl"),
            "newmtl skin\nKd 0.43 0.48 0.50\nnewmtl mantle\nKd 0.32 0.42 0.46\n"+
            "newmtl fabric\nKd 0.23 0.24 0.28\nnewmtl gear\nKd 0.52 0.45 0.42\n"+
            "newmtl wake\nKd 0.66 0.48 0.39\nnewmtl dark\nKd 0.13 0.19 0.23\n");
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
