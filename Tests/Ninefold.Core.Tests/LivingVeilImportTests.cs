using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> LivingVeilImportTests()
    {
        yield return ("Olyra motion matches reference and keeps partner grounded", LivingVeilMotionContracts);
        yield return ("Living Veil source imports both poses with five continuous UV-mapped lobes", LivingVeilSource);
    }
    private static void LivingVeilSource()
    {
        string root = Path.Combine(Path.GetDirectoryName(StriderRoot()), "LivingVeil");
        var materials = GreatStriderSource.ReadMaterials(File.ReadLines(Path.Combine(root, "LivingVeil.mtl")), root);
        foreach (string stem in new[] { "LivingVeil", "LivingVeilProtective" })
        {
            var mesh = GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root, stem + ".obj")), materials);
            if (mesh.SourceVertices != 47136 || mesh.Triangles != 93768 || mesh.Parts != 126)
                throw new Exception("Unexpected Living Veil study mesh.");
            if (materials.Count != 12 || materials.Count(m => m.Texture != null) != 7)
                throw new Exception("Expected one Olyra alpha texture and six partner albedo maps.");
            foreach (var surface in materials.Where(m => m.Texture != null && m.Name != "olyra_membrane"))
                foreach (string suffix in new[] { "_Normal.png", "_Finish.png" })
                    if (!File.Exists(Path.Combine(root, "Textures", surface.Name + suffix)))
                        throw new Exception("Missing partner detail map: " + surface.Name);
            var transparent = materials.Where(m => m.Opacity < 1).Select(m => m.Name).ToHashSet();
            if (mesh.Sections.Count(s => transparent.Contains(s.Material)) != 6)
                throw new Exception("Membrane shells combined or missing.");
            for (int lobe = 0; lobe < 5; lobe++)
                if (mesh.Sections.Count(s => s.Name.Contains("Olyra_Lobe_" + lobe)) != 1)
                    throw new Exception("Missing continuous lobe.");
            foreach (var lobe in mesh.Sections.Where(s => s.Material == "olyra_membrane"))
                if (lobe.UVs.Any(uv => uv.Any(v => v < 0 || v > 1)) ||
                    lobe.UVs.Min(uv => uv[1]) != 0 || lobe.UVs.Max(uv => uv[1]) != 1)
                    throw new Exception("Missing continuous membrane UV domain.");
            if (mesh.Sections.Sum(s => s.Indices.Count) != 93768 * 3)
                throw new Exception("Triangles lost during import.");
            foreach (var section in mesh.Sections)
                foreach (var n in section.Normals)
                    if (Math.Abs(n.Sum(x => x*x)-1) > .001f) throw new Exception("Invalid smooth normals.");
        }
    }
    private static void LivingVeilMotionContracts()
    {
        string root=Path.Combine(Path.GetDirectoryName(StriderRoot()), "LivingVeil");
        using var reference=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"motion-study.json")));
        var probes=reference.RootElement.GetProperty("probes").EnumerateArray().ToArray();
        foreach (var sample in reference.RootElement.GetProperty("samples").EnumerateArray())
        {
            var clip=Enum.Parse<LivingVeilMotion.Clip>(sample.GetProperty("clip").GetString());
            var frame=LivingVeilMotion.Sample(clip,sample.GetProperty("time").GetDouble());
            var expected=sample.GetProperty("frame").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            var actual=new[] {frame.Blend,frame.Lift,frame.Shift,frame.Wave};
            for (int i=0;i<4;i++) if (Math.Abs(expected[i]-actual[i])>1e-10) throw new Exception("Motion curve drift.");
            var points=sample.GetProperty("vertices").EnumerateArray().ToArray();
            for (int i=0;i<probes.Length;i++)
            {
                string name=probes[i].GetProperty("part").GetString();
                var a=probes[i].GetProperty("rest").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
                var b=probes[i].GetProperty("shelter").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
                LivingVeilMotion.Vertex(name.StartsWith("Olyra_"),LivingVeilMotion.IsMembrane(name),a,b,frame,out float x,out float y,out float z);
                var wanted=points[i].EnumerateArray().Select(q=>q.GetDouble()).ToArray();
                if (Math.Abs(x-wanted[0])>2e-5 || Math.Abs(y-wanted[1])>2e-5 || Math.Abs(z-wanted[2])>2e-5)
                    throw new Exception("Motion vertex drift: "+name);
            }
        }
        // Regression: drift must overlap lift/unfold rather than wait for a late sideways stage.
        foreach (double t in new[] { .1, .25, .5, .75, .9 })
        {
            var glide = LivingVeilMotion.Sample(LivingVeilMotion.Clip.Deploy,t);
            if (glide.Shift <= 0 || glide.Shift >= 1 || glide.Lift < 0 || glide.Lift > 1.01)
                throw new Exception("Staged or excessive deployment movement returned.");
        }
        if (LivingVeilMotion.Duration(LivingVeilMotion.Clip.Retract) <= LivingVeilMotion.Duration(LivingVeilMotion.Clip.Deploy))
            throw new Exception("Retraction should settle more slowly than deployment.");
        bool rejected=false;
        try { LivingVeilMotion.Sample(LivingVeilMotion.Clip.Deploy,double.NaN); }
        catch (ArgumentOutOfRangeException) { rejected=true; }
        if (!rejected) throw new Exception("NaN time accepted.");
        var materials=GreatStriderSource.ReadMaterials(File.ReadLines(Path.Combine(root,"LivingVeil.mtl")),root);
        var rest=GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root,"LivingVeil.obj")),materials);
        var shelter=GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root,"LivingVeilProtective.obj")),materials);
        for (int i=0;i<rest.Sections.Count;i++)
        {
            var a=rest.Sections[i];var b=shelter.Sections[i];
            if (a.Name!=b.Name || !a.Indices.SequenceEqual(b.Indices) || !a.SourceParts.SequenceEqual(b.SourceParts) ||
                a.SourceParts.Count!=a.Vertices.Count || a.Vertices.Count!=b.Vertices.Count ||
                !a.UVs.Zip(b.UVs, (u,v) => u.SequenceEqual(v)).All(equal => equal))
                throw new Exception("Incompatible motion topology.");
            for (int j=0;j<a.Vertices.Count;j++)
            {
                string name=a.SourceParts[j];bool organism=name.StartsWith("Olyra_");
                var frame=LivingVeilMotion.Sample(LivingVeilMotion.Clip.Deploy,1);
                LivingVeilMotion.Vertex(organism,LivingVeilMotion.IsMembrane(name),a.Vertices[j],b.Vertices[j],frame,out float x,out float y,out float z);
                var v=organism?b.Vertices[j]:a.Vertices[j];
                if (Math.Abs(x-v[0]-(organism?1.7:0))>2e-5 || Math.Abs(y-v[1])>2e-5 || Math.Abs(z-v[2]-(organism ? .35 : 0))>2e-5)
                    throw new Exception("End pose or fixed partner mismatch.");
            }
        }
    }

}
