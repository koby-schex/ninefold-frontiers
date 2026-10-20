using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> LivingVeilImportTests()
    {
        yield return ("Living Veil source imports five lobes with separate transparent bands", LivingVeilSource);
    }
    private static void LivingVeilSource()
    {
        string root = Path.Combine(Path.GetDirectoryName(StriderRoot()), "LivingVeil");
        var materials = GreatStriderSource.ReadMaterials(File.ReadLines(Path.Combine(root, "LivingVeil.mtl")), root);
        var mesh = GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root, "LivingVeil.obj")), materials);
        if (mesh.SourceVertices != 27398 || mesh.Triangles != 54488 || mesh.Parts != 77)
            throw new Exception("Unexpected Living Veil study mesh.");
        if (materials.Count != 14 || materials.Any(m => m.Texture != null))
            throw new Exception("Expected flat study materials.");
        var transparent = materials.Where(m => m.Opacity < 1).Select(m => m.Name).ToHashSet();
        if (mesh.Sections.Count(s => transparent.Contains(s.Material)) != 16)
            throw new Exception("Membrane shells combined or missing.");
        for (int lobe = 0; lobe < 5; lobe++)
            if (mesh.Sections.Count(s => s.Name.Contains("Olyra_Lobe_" + lobe + "_Band_")) != 3)
                throw new Exception("Missing lobe bands.");
        if (mesh.Sections.Sum(s => s.Indices.Count) != 54488 * 3)
            throw new Exception("Triangles lost during import.");
        foreach (var section in mesh.Sections)
            foreach (var n in section.Normals)
                if (Math.Abs(n.Sum(x => x*x)-1) > .001f) throw new Exception("Invalid smooth normals.");
    }
}
