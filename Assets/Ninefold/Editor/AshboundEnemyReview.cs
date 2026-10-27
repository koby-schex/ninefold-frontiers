using System;
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
    /// <summary>
    /// Read-only canonical-boundary review of four anonymous Ashbound art candidates.
    /// Imported assets are generated in ArtReview, NEVER registered for battle.
    /// No source assets or gameplay IDs are modified.
    /// </summary>
    public sealed class AshboundEnemyReview : EditorWindow
    {
        private const string SourceRoot = "SourceArt/Ashbound/";
        private const string ReviewRoot = "Assets/Ninefold/ArtReview";
        private string message = "";
        private static readonly string[] Models = { "RiftForager", "PartedStriker", "SeamCastigator", "AnchorWrecker" };

        [MenuItem("Ninefold/Art/Ashbound Enemy Review")]
        public static void Open() => GetWindow<AshboundEnemyReview>("Ashbound Review");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("WORLD FIVE / ASHBOUND", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Four nonplayable Ashbound studies: Rift Forager, Parted Striker, " +
                "Seam Castigator and Anchor Wrecker. " +
                "Study 01, not final canon, not rigged, and NOT approved for mobile battles. " +
                "Their living crescent mantles are sensory anatomy, NOT bonding organs. " +
                "All attached hardware is provisional engineered equipment.",
                MessageType.Info);
            EditorGUILayout.HelpBox(
                "Use the model team's locked Avarin visual master to review the temple-face, " +
                "mantle curvature, anatomy and equipment. No bonded companion, soul device, " +
                "animal trophy or combat prefab is provided here.",
                MessageType.None);

            foreach (string model in Models)
            {
                if (GUILayout.Button("Open " + SplitName(model) + " review scene", GUILayout.Height(34)))
                {
                    try { Build(model); message = "Opened " + SplitName(model) + " study."; }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                        Debug.LogError("Ashbound model review failed: " + ex);
                    }
                }
            }
            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Review gate", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scene/prefab under ArtReview is disposable. Do NOT enable it in " +
                "BattleUnitVisuals.asset. Match exact gameplay IDs only after art, animation, " +
                "mobile and canon review. Unity never changes the source OBJ, MTL or manifest.",
                MessageType.Warning);
        }

        private static string SplitName(string n)
        {
            switch (n)
            {
                case "RiftForager": return "Rift Forager";
                case "PartedStriker": return "Parted Striker";
                case "SeamCastigator": return "Seam Castigator";
                case "AnchorWrecker": return "Anchor Wrecker";
                default: throw new ArgumentException("Unknown Ashbound study.", nameof(n));
            }
        }

        private static void Build(string model)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play mode and finish Unity compilation first.");
            if (Application.unityVersion != "6000.3.21f1")
                throw new InvalidOperationException("Pinned review editor version is Unity 6000.3.21f1.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!Models.Contains(model)) throw new ArgumentException("Unknown study.");

            string root = SourceRoot + model + "/";
            string obj = root + model + ".obj";
            string mtl = root + model + ".mtl";
            string manifestFile = root + "manifest.json";
            if (!File.Exists(obj) || !File.Exists(mtl) || !File.Exists(manifestFile))
                throw new FileNotFoundException("Missing tracked Ashbound source file. Fetch the complete PR first.");

            var manifest = JsonUtility.FromJson<StudyManifest>(File.ReadAllText(manifestFile));
            var materials = GreatStriderSource.ReadMaterials(File.ReadLines(mtl), root);
            var geometry = GreatStriderSource.ReadMesh(File.ReadLines(obj), materials);
            if (manifest == null || manifest.model != model || manifest.units != "meters" ||
                manifest.up != "Y" || manifest.forward != "Z" ||
                !manifest.bipedal_avarin || !manifest.living_crescent_mantle ||
                manifest.bonded_creature_count != 0 || manifest.rigged ||
                geometry.SourceVertices != manifest.vertices ||
                geometry.Triangles != manifest.triangles ||
                geometry.Parts != manifest.parts)
                throw new InvalidDataException("Study manifest is inconsistent with source geometry.");
            if (materials.Count != 8 ||
                geometry.Sections.Sum(s => s.Indices.Count) != manifest.triangles * 3)
                throw new InvalidDataException("Incomplete imported Ashbound mesh/material streams.");
            if (geometry.Sections.Any(s => s.Vertices.Any(v =>
                v[1] < -.005f || v[1] > 1.95f)))
                throw new InvalidDataException("Out-of-envelope Avarin source geometry.");

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader unavailable; configure Unity graphics first.");

            string fingerprint;
            using (var sha = SHA256.Create())
            {
                var bytes = File.ReadAllBytes(obj).Concat(File.ReadAllBytes(mtl))
                    .Concat(File.ReadAllBytes(manifestFile)).ToArray();
                fingerprint = BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", "").Substring(0, 12).ToLowerInvariant();
            }
            if (!AssetDatabase.IsValidFolder(ReviewRoot))
                AssetDatabase.CreateFolder("Assets/Ninefold", "ArtReview");
            string destination = ReviewRoot + "/Ashbound" + model + "01_" + fingerprint;
            string scenePath = destination + "/" + model + "Review01.unity";
            if (File.Exists(scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
                return;
            }

            // Review snapshots are never reused across incompatible fingerprints.
            if (!AssetDatabase.IsValidFolder(destination))
                AssetDatabase.CreateFolder(ReviewRoot, "Ashbound" + model + "01_" + fingerprint);
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject prefab;
            try
            {
                var palette = materials.ToDictionary(
                    s => s.Name,
                    s => CreateMaterial(shader, s.Name,
                        new Color(s.Color[0], s.Color[1], s.Color[2], s.Opacity),
                        destination), StringComparer.Ordinal);

                var rootObject = new GameObject("Ashbound " + SplitName(model) + " / noncanonical 3D study 01");
                SceneManager.MoveGameObjectToScene(rootObject, preview);
                int sectionIndex = 0;
                foreach (var section in geometry.Sections)
                {
                    var mesh = new Mesh
                    {
                        name = section.Name,
                        indexFormat = IndexFormat.UInt32
                    };
                    mesh.SetVertices(section.Vertices.Select(v =>
                        new Vector3(v[0], v[1], v[2])).ToList());
                    mesh.SetNormals(section.Normals.Select(v =>
                        new Vector3(v[0], v[1], v[2])).ToList());
                    mesh.SetUVs(0, section.UVs.Select(v =>
                        new Vector2(v[0], v[1])).ToList());
                    mesh.SetTriangles(section.Indices, 0);
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(
                        destination + "/section-" + (sectionIndex++).ToString("D2") + ".asset"));
                    var child = new GameObject(section.Name);
                    child.transform.SetParent(rootObject.transform, false);
                    child.AddComponent<MeshFilter>().sharedMesh = mesh;
                    child.AddComponent<MeshRenderer>().sharedMaterial = palette[section.Material];
                }
                prefab = PrefabUtility.SaveAsPrefabAsset(rootObject,
                    destination + "/" + model + "Study01.prefab");
                if (prefab == null) throw new IOException("Could not write review-only prefab.");
                AssetDatabase.SaveAssets();
            }
            catch
            {
                AssetDatabase.DeleteAsset(destination);
                throw;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.31f, .32f, .37f);
            RenderSettings.skybox = null;
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "Neutral ground - studio only, not a creature or battle prop";
            ground.transform.position = new Vector3(0, -.075f, 0);
            ground.transform.localScale = new Vector3(1.05f, .075f, 1.05f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial(shader,
                "Review neutral ground", new Color(.15f, .18f, .21f), destination);
            DestroyImmediate(ground.GetComponent<Collider>());
            var light = new GameObject("Neutral review key").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.17f;
            light.transform.rotation = Quaternion.Euler(43f, -39f, 0f);
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(2.9f, 2.18f, 3.2f);
            camera.transform.LookAt(new Vector3(0, .95f, 0));
            camera.fieldOfView = 36f; camera.nearClipPlane = .05f;
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new IOException("Could not save Ashbound review scene.");
            Selection.activeGameObject = modelInstance;
            Debug.Log("Ashbound review scene " + scenePath +
                " / " + manifest.triangles + " triangles, " + manifest.parts +
                " named model parts. Requires locked-canon and device review.");
        }

        private static Material CreateMaterial(Shader shader, string name,
            Color color, string folder)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material,
                AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".mat"));
            return material;
        }

        [Serializable]
        private sealed class StudyManifest
        {
            public string model, units, up, forward;
            public int vertices, triangles, parts, bonded_creature_count;
            public bool bipedal_avarin, living_crescent_mantle, rigged;
        }
    }
}
