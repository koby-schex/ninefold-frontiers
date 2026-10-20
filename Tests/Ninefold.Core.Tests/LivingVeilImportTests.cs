using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> LivingVeilImportTests()
    {
        yield return ("Living Veil source imports both poses with five continuous UV-mapped lobes", LivingVeilSource);
    }
    private static void LivingVeilSource()
    {
        string root = Path.Combine(Path.GetDirectoryName(StriderRoot()), "LivingVeil");
        var materials = GreatStriderSource.ReadMaterials(File.ReadLines(Path.Combine(root, "LivingVeil.mtl")), root);
        foreach (string stem in new[] { "LivingVeil", "LivingVeilProtective" })
        {
            var mesh = GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root, stem + ".obj")), materials);
            if (mesh.SourceVertices != 33668 || mesh.Triangles != 66948 || mesh.Parts != 97)
                throw new Exception("Unexpected Living Veil study mesh.");
            if (materials.Count != 12 || materials.Count(m => m.Texture != null) != 1)
                throw new Exception("Expected one membrane alpha texture.");
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
            if (mesh.Sections.Sum(s => s.Indices.Count) != 66948 * 3)
                throw new Exception("Triangles lost during import.");
            foreach (var section in mesh.Sections)
                foreach (var n in section.Normals)
                    if (Math.Abs(n.Sum(x => x*x)-1) > .001f) throw new Exception("Invalid smooth normals.");
        }
    }
}
