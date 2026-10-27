using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninefold.Editor;

internal static partial class Program
{
    private static IEnumerable<(string Name, Action Run)> StriderImportTests()
    {
        yield return ("Strider real OBJ/MTL imports with UV seams and separate transparent shells", StriderRealSource);
        yield return ("Strider UV splits retain original smooth normals", StriderSeamNormals);
        yield return ("Strider importer rejects malformed geometry and unsafe texture paths", StriderRejectInvalid);
        yield return ("World Five new roster sources import with canon limb counts", WorldFiveRosterSources);
    }
    private static void WorldFiveRosterSources()
    {
        string sourceArt=Directory.GetParent(StriderRoot()).FullName;
        foreach (string unit in new[] { "Burdenbeast", "Riftwalker" })
        {
            string root=Path.Combine(sourceArt,unit);
            var materials=GreatStriderSource.ReadMaterials(File.ReadLines(Path.Combine(root,unit+".mtl")),root);
            var mesh=GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root,unit+".obj")),materials);
            var names=mesh.Sections.SelectMany(s=>s.SourceParts).Distinct().ToArray();
            string prefix=unit=="Burdenbeast"?"Tervan_":"Vaskel_";
            int limbs=unit=="Burdenbeast"?8:2;
            if (names.Count(n=>n.StartsWith(prefix+"Limb_",StringComparison.Ordinal))!=limbs ||
                names.Count(n=>n.StartsWith(prefix+"Contact_",StringComparison.Ordinal))!=limbs)
                throw new Exception("Incorrect roster anatomy: "+unit);
            if (names.Count(n=>n.StartsWith("Avarin_Arm_",StringComparison.Ordinal))!=2 ||
                names.Count(n=>n.StartsWith("Avarin_Leg_",StringComparison.Ordinal))!=2 ||
                names.Count(n=>n.StartsWith("Avarin_SensoryRibbon_",StringComparison.Ordinal))!=6)
                throw new Exception("Missing independent Avarin partner: "+unit);
            foreach (var section in mesh.Sections)
                if (section.Vertices.Count!=section.Normals.Count || section.SourceParts.Count!=section.Vertices.Count)
                    throw new Exception("Incomplete new roster mesh streams: "+unit);
        }
    }
    private static string StriderRoot()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root != null)
        {
            string path = Path.Combine(root.FullName, "SourceArt", "GreatStrider");
            if (Directory.Exists(path)) return path;
            root = root.Parent;
        }
        throw new Exception("Cannot find Great Strider test asset.");
    }
    private static void StriderRealSource()
    {
        string root = StriderRoot();
        var materials = GreatStriderSource.ReadMaterials(File.ReadLines(Path.Combine(root, "GreatStrider.mtl")), root);
        var mesh = GreatStriderSource.ReadMesh(File.ReadLines(Path.Combine(root, "GreatStrider.obj")), materials);
        if (mesh.SourceVertices != 40646 || mesh.Triangles != 80556 || mesh.Parts != 184) throw new Exception("Unexpected study 11 mesh.");
        if (materials.Count != 12 || materials.Count(m => m.Texture != null) != 8) throw new Exception("Missing source materials/textures.");
        if (Math.Abs(materials.Single(m => m.Name == "sail_membrane").Opacity - .68f) > .00001) throw new Exception("Opacity lost.");
        foreach (var m in materials.Where(m => m.Texture != null))
        {
            if (m.Color.Any(c => c != 1)) throw new Exception("Textured tint would be applied twice.");
            foreach (string suffix in new[] { "_Normal.png", "_Finish.png" })
                if (!File.Exists(Path.Combine(root, "Textures", m.Name + suffix)))
                    throw new Exception("Missing detailed Strider surface map: " + m.Name + suffix);
        }
        if (mesh.Sections.Count(s => s.Material == "sail_membrane") < 3) throw new Exception("Transparent shells combined.");
        foreach (var s in mesh.Sections)
        {
            if (s.Vertices.Count != s.UVs.Count || s.Vertices.Count != s.Normals.Count) throw new Exception("Incomplete mesh streams.");
            if (s.Indices.Any(i => i < 0 || i >= s.Vertices.Count)) throw new Exception("Invalid remapped index.");
            foreach (var n in s.Normals)
                if (Math.Abs(Math.Sqrt(n.Sum(x => x*x))-1) > .00001) throw new Exception("Non-unit normal.");
        }
        if (mesh.Sections.Sum(s => s.Indices.Count) != 80556*3) throw new Exception("Triangles lost.");
    }
    private static List<GreatStriderSource.Surface> StriderPalette() => new List<GreatStriderSource.Surface>
    { new GreatStriderSource.Surface { Name = "test", Color = new[] { 1f, 1f, 1f }, Exponent = 24, Texture = "Textures/test.png" } };
    private static string[] StriderFixture() => new[]
    {
        "o sample", "usemtl test", "v 0 0 0", "v 1 0 0", "v 0 1 0", "v 0 0 1",
        "vt 0 0", "vt 1 0", "vt 0 1", "vt .5 .5", "f 1/1 2/2 3/3", "f 1/4 4/2 2/1"
    };
    private static void StriderSeamNormals()
    {
        var mesh = GreatStriderSource.ReadMesh(StriderFixture(), StriderPalette());
        var section = mesh.Sections.Single();
        var copies = Enumerable.Range(0, section.Vertices.Count).Where(i => section.Vertices[i].All(x => x == 0)).ToArray();
        if (copies.Length != 2 || section.UVs[copies[0]].SequenceEqual(section.UVs[copies[1]])) throw new Exception("UV seam was not split.");
        if (!section.Normals[copies[0]].SequenceEqual(section.Normals[copies[1]])) throw new Exception("Shading seam introduced.");
        var n = section.Normals[copies[0]];
        if (Math.Abs(n[1] - .70710678) > .00001 || Math.Abs(n[2] - .70710678) > .00001) throw new Exception("Incorrect area-weighted normal.");
    }
    private static void StriderRejectInvalid()
    {
        void Reject(Action action)
        {
            try { action(); } catch (InvalidDataException) { return; }
            throw new Exception("Malformed input accepted.");
        }
        Reject(() => GreatStriderSource.ReadMesh(StriderFixture().Select(x => x == "v 0 0 0" ? "v NaN 0 0" : x), StriderPalette()));
        Reject(() => GreatStriderSource.ReadMesh(StriderFixture().Select(x => x == "f 1/1 2/2 3/3" ? "f 1/999 2/2 3/3" : x), StriderPalette()));
        Reject(() => GreatStriderSource.ReadMesh(StriderFixture().Select(x => x == "f 1/1 2/2 3/3" ? "f 1 2 3" : x), StriderPalette()));
        Reject(() => GreatStriderSource.ReadMesh(StriderFixture().Select(x => x == "usemtl test" ? "usemtl absent" : x), StriderPalette()));
        Reject(() => GreatStriderSource.ReadMesh(StriderFixture().Select(x => x == "f 1/1 2/2 3/3" ? "f 1/1 1/1 3/3" : x), StriderPalette()));
        Reject(() => GreatStriderSource.TexturePath(StriderRoot(), "Textures/../../private.png"));
        Reject(() => GreatStriderSource.ReadMaterials(new[] { "newmtl x", "Kd 1 1 1", "Ns 2", "d 2" }, StriderRoot()));
    }
}
