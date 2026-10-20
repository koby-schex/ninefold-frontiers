using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ninefold.Editor
{
    /// <summary>Opt-in, editor-only review of the two static source poses. No game/profile access.</summary>
    public sealed class LivingVeilReview : EditorWindow
    {
        private const string Folder = "Assets/Ninefold/ArtReview";
        private static string ScenePath = "";
        private const string Revision = "Canopy study 02 / Unity review 02";
        private static bool Protective = true;
        private static string Stem => Protective ? "LivingVeilProtective" : "LivingVeil";
        private static string Manifest => Protective ? "protective-manifest.json" : "manifest.json";
        private string sourceVersion = "", importedVersion = "";
        private const string Source = "SourceArt/LivingVeil/";
        private float yaw = 145, pitch = 12, distance = 17;
        private string status = "";
        private Vector2 scroll;
        private bool detail;
        private bool showMaterials;
        private static bool Busy => EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        [MenuItem("Ninefold/Art/Living Veil Review")]
        public static void Open() => GetWindow<LivingVeilReview>("Living Veil Review");

        private void OnEnable() => RefreshVersion();
        private void RefreshVersion()
        {
            try { sourceVersion = Fingerprint().Substring(0, 12); }
            catch (Exception e) { sourceVersion = "Unavailable: " + e.Message; }
            var active = SceneManager.GetActiveScene();
            if (active.path.StartsWith(Folder + "/LivingVeil", StringComparison.Ordinal) &&
                Path.GetFileName(active.path) == "LivingVeilReview02.unity")
            {
                ScenePath = active.path;
                Protective = active.GetRootGameObjects().Any(go => go.name.StartsWith("Living Veil — protective — ", StringComparison.Ordinal));
            }
            string report = Path.Combine(Path.GetDirectoryName(SceneManager.GetActiveScene().path) ?? "", "source-version.txt");
            importedVersion = File.Exists(report) ? File.ReadAllText(report) : "No current review scene open";
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Living Veil · Canopy study 02", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Resting and protective static poses with five continuous membrane lobes, an alpha texture and an independent Avarin partner. No animation or production rig. Not final art or canon approval.", MessageType.Info);
            EditorGUILayout.LabelField("Source revision", Revision);
            EditorGUILayout.LabelField("Source fingerprint", sourceVersion);
            EditorGUILayout.HelpBox("Imported scene: " + importedVersion, MessageType.None);
            if (GUILayout.Button("Refresh version status")) RefreshVersion();
            EditorGUILayout.LabelField("Scale", "Meters · Avarin approximately 1.82 m");
            EditorGUILayout.HelpBox("Folded span about 2.6 m; protective span about 5.6 m. Locked lore and visual masters remain authoritative.", MessageType.None);
            using (new EditorGUI.DisabledScope(Busy))
            {
                EditorGUILayout.LabelField("Selected pose", Protective ? "Protective canopy" : "Resting fold");
                if (GUILayout.Button("Show resting fold")) OpenPose(false);
                if (GUILayout.Button("Show protective canopy")) OpenPose(true);
                if (GUILayout.Button("Open / create latest review scene", GUILayout.Height(36)))
                {
                    try { if (OpenScene())
                        {
                            var view = PrepareSceneView();
                            view.LookAt(new Vector3(.3f, 1.7f, 0), Quaternion.Euler(12, 145, 0), 5.5f, false, true);
                            RefreshVersion(); status = "Review scene ready. No Play mode needed.";
                        } }
                    catch (Exception e) { status = e.Message; Debug.LogException(e); }
                }
                if (GUILayout.Button("Build fresh review copy (preserve existing edits)"))
                {
                    try { if (OpenScene(true)) { RefreshVersion(); status = "Fresh review copy created."; } }
                    catch (Exception e) { status = e.Message; Debug.LogException(e); }
                }
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(ScenePath) || SceneManager.GetActiveScene().path != ScenePath))
                {
                    if (GUILayout.Button("Validate imported review assets"))
                    {
                        try { status = ValidateImported(); }
                        catch (Exception e) { status = e.Message; Debug.LogException(e); }
                    }
                    GUILayout.Space(8);
                    EditorGUI.BeginChangeCheck();
                    yaw = EditorGUILayout.Slider("Orbit", yaw, -180, 180);
                    pitch = EditorGUILayout.Slider("Elevation", pitch, -10, 70);
                    distance = EditorGUILayout.Slider("Camera distance", distance, 2, 20);
                    if (EditorGUI.EndChangeCheck()) ApplyView();
                    if (GUILayout.Button("Tactical camera · portrait")) Preset(140, 45, 17, false);
                    if (GUILayout.Button("Full silhouette · portrait")) Preset(145, 12, 17, false);
                    if (GUILayout.Button("Front (+Z)")) Preset(180, 0, 17, false);
                    if (GUILayout.Button("Side")) Preset(90, 0, 17, false);
                    if (GUILayout.Button("Back")) Preset(0, 0, 17, false);
                    if (GUILayout.Button("Avarin / Veil Harness detail")) Preset(180, 0, 5, true);
                    if (GUILayout.Button("Open Game view")) EditorApplication.ExecuteMenuItem("Window/General/Game");
                    if (GUILayout.Button("Open Scene view")) PrepareSceneView().Focus();
                    DrawMaterialInspection();
                }
            }
            EditorGUILayout.HelpBox("Use the Game view at 540 × 960 for portrait framing. Scene view also supports Unity's normal orbit, pan and zoom. Presets update both views. Presets enable scene lighting and hide the grid, gizmos and skybox for inspection. These Scene view display preferences remain adjustable in Unity. Game view is the reference for background and framing. Camera edits are local scene changes; reopening preserves saved edits.", MessageType.None);
            EditorGUILayout.HelpBox("Review five-lobe silhouette, core visibility, partner anatomy, harness and transparent sorting from multiple sides. This scene does not establish mobile performance or battlefield clearance.", MessageType.None);
            if (status.Length > 0) EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private static SceneView PrepareSceneView()
        {
            var view = GetWindow<SceneView>();
            view.sceneLighting = true;
            view.showGrid = false;
            view.drawGizmos = false;
            view.sceneViewState.showSkybox = false;
            return view;
        }

        private void DrawMaterialInspection()
        {
            GUILayout.Space(8);
            showMaterials = EditorGUILayout.Foldout(showMaterials, "Inspect model materials", true);
            if (!showMaterials) return;
            EditorGUILayout.HelpBox("Read-only swatches and imported albedo maps. These first-study surfaces use flat colors; final textures remain pending. Select a material to inspect its full settings. This is an art study, not final canon approval.", MessageType.None);
            var root = SceneManager.GetActiveScene().GetRootGameObjects()
                .FirstOrDefault(go => go.name.StartsWith("Living Veil — ", StringComparison.Ordinal));
            if (root == null) { EditorGUILayout.HelpBox("Review model not found.", MessageType.Warning); return; }
            var materials = root.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null)
                .Distinct().OrderBy(material => material.name, StringComparer.Ordinal);
            foreach (var material in materials)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(material.name, EditorStyles.boldLabel);
                var tint = material.GetColor("_BaseColor");
                var texture = material.GetTexture("_BaseMap");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ColorField("Base tint", tint);
                    EditorGUILayout.ObjectField("Albedo map", texture, typeof(Texture), false);
                }
                if (texture != null)
                {
                    var rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, texture);
                }
                EditorGUILayout.LabelField("Opacity / smoothness", tint.a.ToString("0.00") + " / " + material.GetFloat("_Smoothness").ToString("0.00"));
                if (GUILayout.Button("Select material in Inspector"))
                { Selection.activeObject = material; EditorGUIUtility.PingObject(material); }
                EditorGUILayout.EndVertical();
            }
        }

        private void OpenPose(bool protective)
        {
            bool previous = Protective;
            Protective = protective;
            try
            {
                if (OpenScene()) { RefreshVersion(); Preset(145, 12, 17, false); status = "Static pose opened; no Play mode needed."; }
                else Protective = previous;
            }
            catch (Exception e) { Protective = previous; status = e.Message; Debug.LogException(e); }
        }

        private void Preset(float y, float p, float d, bool close)
        { yaw = y; pitch = p; distance = d; detail = close; ApplyView(); }

        private void ApplyView()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) return;
            var target = detail ? new Vector3(Protective ? .25f : 1.95f, 1.1f, Protective ? .15f : .5f) : new Vector3(.3f, 1.7f, 0);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var camera = Camera.main;
            if (camera != null)
            {
                Undo.RecordObject(camera.transform, "Review camera view");
                camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            var view = PrepareSceneView();
            view.LookAt(target, rotation, distance * .32f, false, true);
            SceneView.RepaintAll();
        }

        private static bool OpenScene(bool fresh = false)
        {
            if (Busy) throw new InvalidOperationException("Stop Play mode and wait for import/compilation first.");
            if (Application.unityVersion != "6000.3.21f1") throw new InvalidOperationException("Use Unity 6000.3.21f1.");
            if (GraphicsSettings.defaultRenderPipeline == null) throw new InvalidOperationException("Configure foundation in the Playtest Launcher first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable. Finish package import first.");
            string fingerprint = Fingerprint();
            string destination = Folder + "/" + Stem + "02_" + fingerprint.Substring(0, 12);
            string expectedScene = destination + "/LivingVeilReview02.unity";
            if (!fresh && File.Exists(expectedScene))
            { ScenePath = expectedScene; EditorSceneManager.OpenScene(ScenePath); return true; }
            // Validate the entire source before creating assets or changing the scene.
            var surfaces = GreatStriderSource.ReadMaterials(File.ReadLines(Source + "LivingVeil.mtl"), Source);
            var data = GreatStriderSource.ReadMesh(File.ReadLines(Source + Stem + ".obj"), surfaces);
            var manifest = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(Source + Manifest));
            if (manifest == null || manifest.status != "canopy study 02; not production ready" ||
                manifest.vertices != data.SourceVertices || manifest.triangles != data.Triangles || manifest.parts != data.Parts ||
                Math.Abs(manifest.height - (Protective ? 3.4147113f : 3.0099094f)) > .0001f || manifest.up != "Y" || manifest.forward != "Z" || manifest.units != "meters" ||
                surfaces.Count != 12 || surfaces.Count(m => m.Texture != null) != 1 || surfaces.Single(m => m.Texture != null).Name != "olyra_membrane")
                throw new InvalidDataException("Unexpected source revision/counts. Update and review the importer before proceeding.");
            var positions = data.Sections.SelectMany(x => x.Vertices).ToArray();
            if (Math.Abs(positions.Min(v => v[1])) > .0001 || Math.Abs(positions.Max(v => v[1])-manifest.height) > .0001)
                throw new InvalidDataException("Unexpected pose bounds; no automatic rescaling.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Ninefold", "ArtReview");
            destination = AssetDatabase.GenerateUniqueAssetPath(destination);
            AssetDatabase.CreateFolder(Folder, Path.GetFileName(destination));
            // A private preview scene keeps temporary prefab objects out of the user's scene.
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject prefab;
            Material ground, marker;
            try
            {
                var materials = new System.Collections.Generic.Dictionary<string, Material>();
                foreach (var surface in surfaces)
                    materials.Add(surface.Name, ImportMaterial(shader, surface, destination));
                ground = Material(shader, "Studio ground", new Color(.20f, .20f, .20f), destination);
                marker = Material(shader, "Scale marker", new Color(.72f, .62f, .39f), destination);
                var root = new GameObject("Living Veil — " + (Protective ? "protective" : "resting") + " — " + Revision + " — " + fingerprint.Substring(0,12));
                SceneManager.MoveGameObjectToScene(root, preview);
                foreach (var section in data.Sections)
                {
                    var mesh = new Mesh { name = section.Name, indexFormat = IndexFormat.UInt32 };
                    mesh.SetVertices(section.Vertices.Select(v => new Vector3(v[0], v[1], v[2])).ToList());
                    mesh.SetNormals(section.Normals.Select(v => new Vector3(v[0], v[1], v[2])).ToList());
                    mesh.SetUVs(0, section.UVs.Select(v => new Vector2(v[0], v[1])).ToList());
                    mesh.SetTriangles(section.Indices, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                    AssetDatabase.CreateAsset(mesh, destination + "/" + section.Name.Replace('/', '_') + ".asset");
                    var part = new GameObject(section.Name); part.transform.SetParent(root.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials[section.Material];
                    if (surfaces.Find(m => m.Name == section.Material).Opacity < 1)
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                prefab = PrefabUtility.SaveAsPrefabAsset(root, destination + "/" + Stem + "Study02.prefab");
                if (prefab == null) throw new IOException("Could not create Living Veil prefab.");
                File.WriteAllText(destination + "/source-version.txt", Revision + "\nSHA-256 " + fingerprint +
                    "\n" + data.SourceVertices + " source vertices / " + data.Triangles + " triangles / " +
                    data.Sections.Count + " render sections / smooth membrane alpha map\nStatic review asset; Unity visual acceptance pending.\n");
                AssetDatabase.ImportAsset(destination + "/source-version.txt");
                AssetDatabase.SaveAssets();
            }
            catch { AssetDatabase.DeleteAsset(destination); throw; }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.30f, .30f, .30f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.reflectionIntensity = 0;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Primitive("Neutral studio ground", PrimitiveType.Cube, new Vector3(0, -.1f, 0), new Vector3(32, .2f, 32), ground);
            Primitive("Avarin baseline — 1.82 m measuring post, NOT anatomy", PrimitiveType.Cube,
                new Vector3(-3.2f, .91f, 0), new Vector3(.12f, 1.82f, .12f), marker);
            Primitive("1.82 m height crossbar", PrimitiveType.Cube,
                new Vector3(-3.2f, 1.81f, 0), new Vector3(.6f, .02f, .12f), marker);
            Light("Studio key", new Vector3(45, -35, 0), Color.white, 1.2f, LightShadows.Soft);
            Light("Studio fill", new Vector3(25, 140, 0), Color.white, .6f, LightShadows.None);
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.fieldOfView = 40; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .12f, .12f);
            var rotation = Quaternion.Euler(12, 145, 0);
            camera.transform.SetPositionAndRotation(new Vector3(.3f, 1.7f, 0) - rotation * Vector3.forward * 17, rotation);
            AssetDatabase.SaveAssets();
            ScenePath = destination + "/LivingVeilReview02.unity";
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save review scene.");
            Selection.activeGameObject = model;
            Debug.Log(ValidateImported());
            return true;
        }

        private static Material Material(Shader shader, string name, Color color, string destination, float smoothness = .28f)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(destination + "/" + name + ".mat"));
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
        private static string ValidateImported()
        {
            var model = SceneManager.GetActiveScene().GetRootGameObjects()
                .SingleOrDefault(go => go.name.StartsWith("Living Veil — ", StringComparison.Ordinal));
            if (model == null) throw new InvalidDataException("Open a generated Living Veil review scene first.");
            int triangles = 0; var textures = new HashSet<Texture>(); int transparent = 0;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh; var renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null || mesh.normals.Length != mesh.vertexCount || mesh.uv.Length != mesh.vertexCount)
                    throw new InvalidDataException("Missing mesh/renderer/normal/UV data: " + filter.name);
                triangles += mesh.triangles.Length / 3;
                var material = renderer.sharedMaterial;
                if (material == null || material.shader.name != "Universal Render Pipeline/Lit")
                    throw new InvalidDataException("Missing URP Lit material: " + filter.name);
                if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                {
                    var texture = material.GetTexture("_BaseMap");
                    // Non-textured Lit materials can expose a built-in white texture.
                    string path = AssetDatabase.GetAssetPath(texture);
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(Folder + "/", StringComparison.Ordinal))
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer == null || !importer.sRGBTexture || !importer.mipmapEnabled ||
                            importer.wrapMode != TextureWrapMode.Clamp || importer.filterMode != FilterMode.Trilinear ||
                            importer.alphaSource != TextureImporterAlphaSource.FromInput || !importer.alphaIsTransparency)
                            throw new InvalidDataException("Unexpected texture import settings: " + path);
                        textures.Add(texture);
                    }
                }
                if (material.name == "olyra_membrane" &&
                    (material.GetTexture("_BaseMap") == null || material.GetColor("_BaseColor").a != 1))
                    throw new InvalidDataException("Membrane alpha texture/multiplier missing.");
                if (material.GetFloat("_Surface") > .5f)
                {
                    transparent++;
                    if (!material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT") || material.GetFloat("_ZWrite") != 0 ||
                        material.renderQueue != (int)RenderQueue.Transparent || material.GetColor("_BaseColor").a <= 0 || (material.GetColor("_BaseColor").a >= 1 && material.GetTexture("_BaseMap") == null))
                        throw new InvalidDataException("Incorrect membrane configuration: " + filter.name);
                }
            }
            if (triangles != 66948 || textures.Count != 1 || transparent != 6)
                throw new InvalidDataException("Unexpected imported geometry/texture/membrane count.");
            return "Imported assets verified: 66,948 triangles, smooth membrane alpha map, " + transparent +
                " separate transparent shells. Visual review and device testing remain required.";
        }

        [Serializable]
        private sealed class SourceManifest
        {
            public string status = "", units = "", up = "", forward = "";
            public int vertices = 0, triangles = 0, parts = 0;
            public float height = 0;
        }
        private static string Fingerprint()
        {
            var files = new[] { "LivingVeil.obj", "LivingVeilProtective.obj", "LivingVeil.mtl", "manifest.json", "protective-manifest.json", "Textures/OlyraMembrane.png" }
                .Select(f => Source + f)
                .Concat(new[] { "Assets/Ninefold/Editor/LivingVeilReview.cs", "Assets/Ninefold/Editor/GreatStriderSource.cs" })
                .OrderBy(f => f, StringComparer.Ordinal);
            using (var hash = SHA256.Create())
            {
                foreach (string file in files)
                {
                    byte[] name = Encoding.UTF8.GetBytes(file.Replace('\\', '/') + "\n");
                    byte[] bytes = File.ReadAllBytes(file);
                    hash.TransformBlock(name, 0, name.Length, name, 0);
                    hash.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
        private static Material ImportMaterial(Shader shader, GreatStriderSource.Surface surface, string destination)
        {
            // MTL Kd is linear; Unity's serialized color property is authored in sRGB.
            var color = new Color(surface.Color[0], surface.Color[1], surface.Color[2]).gamma;
            color.a = surface.Texture != null ? 1 : surface.Opacity;
            float smoothness = 1 - Mathf.Pow(2 / (surface.Exponent + 2), .25f);
            var material = Material(shader, surface.Name, color, destination, smoothness);
            material.SetFloat("_Metallic", 0); // Review approximation, not locked physical parameters.
            if (surface.Texture != null)
            {
                string target = destination + "/" + Path.GetFileName(surface.Texture);
                File.Copy(GreatStriderSource.TexturePath(Source, surface.Texture), target);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(target) as TextureImporter;
                if (importer == null) throw new IOException("Could not import texture: " + target);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true; importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true; importer.isReadable = false;
                importer.SaveAndReimport();
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(target));
                // RGBA map contains tint and continuous alpha. Kd is white; alpha multiplier is one.
            }
            bool transparent = surface.Opacity < 1;
            material.SetFloat("_Surface", transparent ? 1 : 0);
            material.SetFloat("_Blend", 0); material.SetFloat("_AlphaClip", 0);
            material.SetFloat("_BlendModePreserveSpecular", 0);
            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_ZWrite", transparent ? 0 : 1);
            // These are closed shells: back-face culling avoids compositing both walls.
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            material.renderQueue = (int)(transparent ? RenderQueue.Transparent : RenderQueue.Geometry);
            if (transparent) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON"); material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.SetShaderPassEnabled("ShadowCaster", !transparent);
            material.SetShaderPassEnabled("DepthOnly", !transparent);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
