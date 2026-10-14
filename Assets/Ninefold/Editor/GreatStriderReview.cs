using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ninefold.Editor
{
    /// <summary>Opt-in, editor-only review of the unchanged source mesh. No game/profile access.</summary>
    public sealed class GreatStriderReview : EditorWindow
    {
        private const string Folder = "Assets/Ninefold/ArtReview";
        private const string ScenePath = Folder + "/GreatStriderReview03.unity";
        private const string Source = "SourceArt/GreatStrider/";
        private float yaw = 145, pitch = 12, distance = 19;
        private string status = "";
        private Vector2 scroll;
        private bool detail;
        private static bool Busy => EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        [MenuItem("Ninefold/Art/Great Strider Review")]
        public static void Open() => GetWindow<GreatStriderReview>("Great Strider Review");

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Great Strider · Form study 03", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Early model study — not final art or a canon revision. Source geometry and provisional palette; no rig, animation, textures or final membrane shading.", MessageType.Info);
            EditorGUILayout.LabelField("Scale", "Ilyth 6.0 m · Avarin reference 1.82 m");
            EditorGUILayout.HelpBox("The upright scale marker is a measuring aid, not an Avarin model. Locked lore and visual masters remain authoritative.", MessageType.None);
            using (new EditorGUI.DisabledScope(Busy))
            {
                if (GUILayout.Button("Open / create review scene", GUILayout.Height(36)))
                {
                    try { if (OpenScene())
                        {
                            var view = GetWindow<SceneView>(); view.sceneLighting = true;
                            view.LookAt(new Vector3(0, 3, 0), Quaternion.Euler(12, 145, 0), 6, false, true);
                            status = "Review scene ready. No Play mode needed.";
                        } }
                    catch (Exception e) { status = e.Message; Debug.LogException(e); }
                }
                using (new EditorGUI.DisabledScope(SceneManager.GetActiveScene().path != ScenePath))
                {
                    GUILayout.Space(8);
                    EditorGUI.BeginChangeCheck();
                    yaw = EditorGUILayout.Slider("Orbit", yaw, -180, 180);
                    pitch = EditorGUILayout.Slider("Elevation", pitch, -10, 70);
                    distance = EditorGUILayout.Slider("Camera distance", distance, 3, 35);
                    if (EditorGUI.EndChangeCheck()) ApplyView();
                    if (GUILayout.Button("Full silhouette · portrait")) Preset(145, 12, 19, false);
                    if (GUILayout.Button("Front (+Z)")) Preset(180, 0, 19, false);
                    if (GUILayout.Button("Side")) Preset(90, 0, 19, false);
                    if (GUILayout.Button("Back")) Preset(0, 0, 19, false);
                    if (GUILayout.Button("Cradle / operator detail")) Preset(145, 5, 6, true);
                    if (GUILayout.Button("Open Game view")) EditorApplication.ExecuteMenuItem("Window/General/Game");
                    if (GUILayout.Button("Open Scene view")) GetWindow<SceneView>().Focus();
                }
            }
            EditorGUILayout.HelpBox("Use the Game view at 540 × 960 for portrait framing. Scene view also supports Unity's normal orbit, pan and zoom. Presets update both views. Scene lighting must be enabled to inspect the studio lights. Camera edits are local scene changes; reopening preserves saved edits.", MessageType.None);
            EditorGUILayout.HelpBox("Review silhouette, foot contacts, cradle clearance, operator readability and material separation. This scene does not establish mobile performance or battlefield clearance.", MessageType.None);
            if (status.Length > 0) EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void Preset(float y, float p, float d, bool close)
        { yaw = y; pitch = p; distance = d; detail = close; ApplyView(); }

        private void ApplyView()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) return;
            var target = detail ? new Vector3(0, 3.7f, .3f) : new Vector3(0, 3, 0);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var camera = Camera.main;
            if (camera != null)
            {
                Undo.RecordObject(camera.transform, "Review camera view");
                camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            var view = GetWindow<SceneView>();
            view.sceneLighting = true;
            view.LookAt(target, rotation, distance * .32f, false, true);
            SceneView.RepaintAll();
        }

        private static bool OpenScene()
        {
            if (Busy) throw new InvalidOperationException("Stop Play mode and wait for import/compilation first.");
            if (Application.unityVersion != "6000.3.21f1") throw new InvalidOperationException("Use Unity 6000.3.21f1.");
            if (GraphicsSettings.defaultRenderPipeline == null) throw new InvalidOperationException("Configure foundation in the Playtest Launcher first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return true; }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable. Finish package import first.");
            // Parse and validate before changing the open scene or creating assets.
            var colors = ReadPalette(out var finishes);
            var mesh = ReadMesh(new List<string>(colors.Keys));
            // Unique destination avoids overwriting partial output from a failed previous attempt.
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Ninefold", "ArtReview");
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Folder + "/GreatStriderStudy03.asset");
            AssetDatabase.CreateAsset(mesh, meshPath);
            var materials = new List<Material>();
            foreach (var pair in colors) materials.Add(Material(shader, pair.Key, pair.Value, finishes[pair.Key]));
            var ground = Material(shader, "Studio ground", new Color(.20f, .19f, .23f));
            var marker = Material(shader, "Scale marker", new Color(.72f, .62f, .39f));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.30f, .29f, .34f);
            var model = new GameObject("Great Strider — form study 03 — 6 m");
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            Primitive("Neutral studio ground", PrimitiveType.Cube, new Vector3(0, -.1f, 0), new Vector3(32, .2f, 32), ground);
            Primitive("Avarin baseline — 1.82 m measuring post, NOT anatomy", PrimitiveType.Cube,
                new Vector3(-2.6f, .91f, 0), new Vector3(.12f, 1.82f, .12f), marker);
            Primitive("1.82 m height crossbar", PrimitiveType.Cube,
                new Vector3(-2.6f, 1.81f, 0), new Vector3(.6f, .02f, .12f), marker);
            Light("Studio key", new Vector3(45, -35, 0), new Color(1, .91f, .82f), 1.5f, LightShadows.Soft);
            Light("Studio fill", new Vector3(25, 140, 0), new Color(.68f, .72f, 1), .6f, LightShadows.None);
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.fieldOfView = 40; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .085f, .12f);
            var rotation = Quaternion.Euler(12, 145, 0);
            camera.transform.SetPositionAndRotation(new Vector3(0, 3, 0) - rotation * Vector3.forward * 19, rotation);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save review scene.");
            Selection.activeGameObject = model;
            return true;
        }

        private static Material Material(Shader shader, string name, Color color, float smoothness = .28f)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + name + ".mat"));
            return material;
        }
        private static void Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            DestroyImmediate(go.GetComponent<Collider>());
        }
        private static void Light(string name, Vector3 rotation, Color color, float intensity, LightShadows shadows)
        {
            var light = new GameObject(name).AddComponent<UnityEngine.Light>();
            light.type = LightType.Directional; light.color = color; light.intensity = intensity; light.shadows = shadows;
            light.transform.rotation = Quaternion.Euler(rotation);
        }
        private static string[] Tokens(string line) => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        private static float Number(string value)
        {
            float result = float.Parse(value, CultureInfo.InvariantCulture);
            if (float.IsNaN(result) || float.IsInfinity(result)) throw new InvalidDataException("Non-finite source coordinate.");
            return result;
        }
        private static Dictionary<string, Color> ReadPalette(out Dictionary<string, float> finishes)
        {
            var colors = new Dictionary<string, Color>(); string name = null;
            finishes = new Dictionary<string, float>();
            foreach (string line in File.ReadLines(Source + "GreatStrider.mtl"))
            {
                var t = Tokens(line); if (t.Length == 0) continue;
                if (t[0] == "newmtl") name = t[1];
                if (t[0] == "Kd") colors.Add(name, new Color(Number(t[1]), Number(t[2]), Number(t[3])));
                if (t[0] == "Ns") finishes.Add(name, 1 - Mathf.Pow(2 / (Mathf.Max(0, Number(t[1])) + 2), .25f));
            }
            if (colors.Count != 12) throw new InvalidDataException("Expected study 03's 12 materials; review the importer for a new source revision.");
            foreach (string key in colors.Keys)
                if (!finishes.ContainsKey(key)) throw new InvalidDataException("Missing material finish: " + key);
            return colors;
        }
        // Narrow importer for the repository's validated triangle-only OBJ, not arbitrary OBJ files.
        // Keep coordinates/scale and outward triangle order; no Unity model-import axis conversion.
        private static Mesh ReadMesh(List<string> materials)
        {
            var vertices = new List<Vector3>(); var faces = new List<int>[materials.Count];
            for (int i = 0; i < faces.Length; i++) faces[i] = new List<int>();
            int current = -1, triangles = 0;
            foreach (string line in File.ReadLines(Source + "GreatStrider.obj"))
            {
                var t = Tokens(line); if (t.Length == 0) continue;
                if (t[0] == "v") vertices.Add(new Vector3(Number(t[1]), Number(t[2]), Number(t[3])));
                else if (t[0] == "usemtl") current = materials.IndexOf(t[1]);
                else if (t[0] == "f")
                {
                    if (t.Length != 4 || current < 0) throw new InvalidDataException("Expected triangles with a known material.");
                    for (int i = 1; i <= 3; i++)
                    {
                        int index = int.Parse(t[i], CultureInfo.InvariantCulture) - 1;
                        if (index < 0 || index >= vertices.Count) throw new InvalidDataException("Invalid OBJ index.");
                        faces[current].Add(index);
                    }
                    triangles++;
                }
            }
            if (vertices.Count != 32928 || triangles != 65148) throw new InvalidDataException("Expected study 03 mesh counts; review the importer for a new source revision.");
            float low = float.MaxValue, high = float.MinValue;
            foreach (var v in vertices) { low = Mathf.Min(low, v.y); high = Mathf.Max(high, v.y); }
            if (Mathf.Abs(low) > .01f || Mathf.Abs(high - 6) > .01f) throw new InvalidDataException("Expected grounded six-meter study; no automatic rescaling applied.");
            var mesh = new Mesh { name = "Great Strider form study 03", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.subMeshCount = faces.Length;
            for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
