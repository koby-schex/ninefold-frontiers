using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Ninefold.Editor
{
    // Shared with executable tests: no Unity API required for parsing/normal generation.
    internal static class GreatStriderSource
    {
        internal sealed class Surface
        {
            public string Name, Texture;
            public float[] Color;
            public float Exponent = -1, Opacity = 1;
        }
        internal sealed class Section
        {
            public string Name, Material;
            public readonly List<float[]> Vertices = new List<float[]>();
            public readonly List<float[]> Normals = new List<float[]>();
            public readonly List<float[]> UVs = new List<float[]>();
            public readonly List<int> Indices = new List<int>();
        }
        internal sealed class Model
        {
            public int SourceVertices, Triangles, Parts;
            public readonly List<Section> Sections = new List<Section>();
        }
        private sealed class Face
        {
            public string Part, Material;
            public int[] V = new int[3], UV = new int[3];
        }
        private static string[] Tokens(string line) => line.Split('#')[0].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        private static float Number(string text)
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidDataException("Invalid finite source number: " + text);
            return value;
        }
        internal static string TexturePath(string root, string relative)
        {
            // Deliberately restricted to this generator's local PNG tiles.
            if (relative == null || !relative.StartsWith("Textures/", StringComparison.Ordinal) ||
                relative.Contains("..") || relative.Contains("\\") || relative.Contains(":") ||
                !relative.EndsWith(".png", StringComparison.Ordinal) || relative.Substring(9).Contains("/"))
                throw new InvalidDataException("Unsupported texture path: " + relative);
            string full = Path.Combine(root, relative);
            if (!File.Exists(full)) throw new FileNotFoundException("Missing source texture", full);
            return full;
        }
        internal static List<Surface> ReadMaterials(IEnumerable<string> lines, string root)
        {
            var result = new List<Surface>(); Surface current = null;
            foreach (string line in lines)
            {
                var t = Tokens(line); if (t.Length == 0) continue;
                if (t[0] == "newmtl")
                {
                    if (t.Length != 2 || result.Any(x => x.Name == t[1])) throw new InvalidDataException("Invalid/duplicate material.");
                    current = new Surface { Name = t[1] }; result.Add(current); continue;
                }
                if (current == null) throw new InvalidDataException("Material property before newmtl.");
                if (t[0] == "Kd")
                {
                    if (t.Length != 4) throw new InvalidDataException("Expected RGB.");
                    current.Color = new[] { Number(t[1]), Number(t[2]), Number(t[3]) };
                }
                else if (t[0] == "Ns" && t.Length == 2) current.Exponent = Number(t[1]);
                else if (t[0] == "d" && t.Length == 2) current.Opacity = Number(t[1]);
                else if (t[0] == "map_Kd")
                {
                    if (t.Length != 2) throw new InvalidDataException("Texture options are not supported.");
                    TexturePath(root, t[1]); current.Texture = t[1];
                }
            }
            if (result.Count == 0) throw new InvalidDataException("Empty material file.");
            foreach (var m in result)
                if (m.Color == null || m.Color.Any(x => x < 0 || x > 1) || m.Exponent < 0 || m.Opacity < 0 || m.Opacity > 1)
                    throw new InvalidDataException("Incomplete/out-of-range material: " + m.Name);
            return result;
        }
        internal static Model ReadMesh(IEnumerable<string> lines, IReadOnlyList<Surface> materials)
        {
            var vertices = new List<float[]>(); var uvs = new List<float[]>(); var faces = new List<Face>();
            var names = new HashSet<string>(); string part = null, material = null;
            var palette = materials.ToDictionary(m => m.Name);
            foreach (var line in lines)
            {
                var t = Tokens(line); if (t.Length == 0) continue;
                if (t[0] == "v" || t[0] == "vt")
                {
                    int count = t[0] == "v" ? 3 : 2;
                    if (t.Length != count + 1) throw new InvalidDataException("Unexpected coordinate size.");
                    var value = t.Skip(1).Select(Number).ToArray();
                    if (count == 3) vertices.Add(value); else uvs.Add(value);
                }
                else if (t[0] == "o")
                {
                    if (t.Length != 2 || !names.Add(t[1])) throw new InvalidDataException("Duplicate/missing part name.");
                    part = t[1];
                }
                else if (t[0] == "usemtl")
                {
                    if (t.Length != 2 || !palette.ContainsKey(t[1])) throw new InvalidDataException("Unknown material.");
                    material = t[1];
                }
                else if (t[0] == "f")
                {
                    if (t.Length != 4 || part == null || material == null) throw new InvalidDataException("Expected named, material-assigned triangle.");
                    var face = new Face { Part = part, Material = material };
                    for (int i = 0; i < 3; i++)
                    {
                        var corner = t[i+1].Split('/');
                        if (corner.Length > 2) throw new InvalidDataException("Expected v or v/vt source indices.");
                        face.V[i] = Index(corner[0], vertices.Count);
                        face.UV[i] = corner.Length == 2 ? Index(corner[1], uvs.Count) : -1;
                        if (palette[material].Texture != null && face.UV[i] < 0) throw new InvalidDataException("Textured triangle missing UV.");
                    }
                    faces.Add(face);
                }
                else if (t[0] != "s" && t[0] != "mtllib") throw new InvalidDataException("Unsupported OBJ statement: " + t[0]);
            }
            if (faces.Count == 0) throw new InvalidDataException("Empty mesh.");
            // Area-weighted normals on ORIGINAL indices, before UV duplication.
            var normals = vertices.Select(v => new float[3]).ToArray();
            foreach (var f in faces)
            {
                var a = vertices[f.V[0]]; var b = vertices[f.V[1]]; var c = vertices[f.V[2]];
                float x = (b[1]-a[1])*(c[2]-a[2])-(b[2]-a[2])*(c[1]-a[1]);
                float y = (b[2]-a[2])*(c[0]-a[0])-(b[0]-a[0])*(c[2]-a[2]);
                float z = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]);
                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y) || float.IsNaN(z) || float.IsInfinity(z) || x*x+y*y+z*z < 1e-16f) throw new InvalidDataException("Degenerate triangle.");
                foreach (int index in f.V) { normals[index][0] += x; normals[index][1] += y; normals[index][2] += z; }
            }
            foreach (var n in normals)
            {
                double length = Math.Sqrt((double)n[0]*n[0]+(double)n[1]*n[1]+(double)n[2]*n[2]);
                if (length == 0) throw new InvalidDataException("Unused vertex or undefined normal.");
                for (int k = 0; k < 3; k++) n[k] = (float)(n[k]/length);
            }
            var model = new Model { SourceVertices = vertices.Count, Triangles = faces.Count, Parts = names.Count };
            // Opaque sections combine by material; transparent closed shells stay separate.
            foreach (var group in faces.GroupBy(f => palette[f.Material].Opacity < 1 ? f.Material+"/"+f.Part : f.Material))
            {
                var section = new Section { Name = group.Key, Material = group.First().Material };
                var remap = new Dictionary<(int, int), int>();
                foreach (var f in group)
                    for (int i = 0; i < 3; i++)
                    {
                        var key = (f.V[i], f.UV[i]);
                        if (!remap.TryGetValue(key, out int index))
                        {
                            index = section.Vertices.Count; remap.Add(key, index);
                            section.Vertices.Add(vertices[key.Item1]); section.Normals.Add(normals[key.Item1]);
                            section.UVs.Add(key.Item2 < 0 ? new float[2] : uvs[key.Item2]);
                        }
                        section.Indices.Add(index);
                    }
                model.Sections.Add(section);
            }
            return model;
        }
        private static int Index(string text, int count)
        {
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int index) || index < 1 || index > count)
                throw new InvalidDataException("Out-of-range source index: " + text);
            return index-1;
        }
    }
}
