using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Ninefold.Editor
{
    /// <summary>
    /// Makes an isolated, unapproved battle candidate from an explicitly selected
    /// Great Strider or Living Veil review prefab. Never edits ArtReview,
    /// gameplay IDs, the runtime catalog, or the model team's source files.
    /// </summary>
    internal static class BattleModelCandidateStager
    {
        private const string ReviewRoot = "Assets/Ninefold/ArtReview/";
        private const string ArtRoot = "Assets/Ninefold/ArtProduction";
        private const string CandidatesRoot = ArtRoot + "/BattleCandidates";
        private const string Command = "Ninefold/Art/Stage Selected Review Model as Battle Candidate";

        [MenuItem(Command, true)]
        private static bool CanStage()
        {
            GameObject prefab;
            string path;
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling &&
                   TrySelectedSource(out prefab, out path);
        }

        [MenuItem(Command)]
        private static void Stage()
        {
            GameObject source;
            string sourcePath;
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                !TrySelectedSource(out source, out sourcePath))
            {
                EditorUtility.DisplayDialog("Battle model candidate",
                    "Select a GreatStriderStudy, LivingVeilStudy or LivingVeilProtectiveStudy prefab under ArtReview in the Project window. Stop Play mode first.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Stage model for battle review",
                "This copies the selected review model's meshes, materials and textures into an isolated candidate. " +
                "It does NOT approve the model, alter its source, assign a gameplay UnitId, or make it appear in missions. " +
                "Continue?", "Stage unapproved candidate", "Cancel"))
                return;

            string folder = null;
            Scene preview = default(Scene);
            try
            {
                ValidateSource(source);
                EnsureFolder("Assets/Ninefold", "ArtProduction");
                EnsureFolder(ArtRoot, "BattleCandidates");
                string stem = Path.GetFileNameWithoutExtension(sourcePath);
                folder = NewCandidateFolder(stem);
                preview = EditorSceneManager.NewPreviewScene();
                var root = PrefabUtility.InstantiatePrefab(source, preview) as GameObject;
                if (root == null) throw new InvalidDataException("Cannot instantiate selected source prefab.");
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);

                var meshes = new Dictionary<Mesh, Mesh>();
                var textures = new Dictionary<Texture, Texture>();
                var materials = new Dictionary<Material, Material>();
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null)
                        filter.sharedMesh = CopyMesh(filter.sharedMesh, folder, meshes);
                foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (skin.sharedMesh != null)
                        skin.sharedMesh = CopyMesh(skin.sharedMesh, folder, meshes);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var originals = renderer.sharedMaterials;
                    var copied = new Material[originals.Length];
                    for (int i = 0; i < originals.Length; i++)
                    {
                        if (originals[i] == null)
                            throw new InvalidDataException("A renderer has an empty material slot.");
                        copied[i] = CopyMaterial(originals[i], folder, materials, textures);
                    }
                    renderer.sharedMaterials = copied;
                }

                root.name = stem + " - unapproved battle candidate";
                var path = folder + "/" + stem + "-Candidate.prefab";
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new IOException("Unity failed to save the candidate prefab.");
                AssetDatabase.SaveAssets();
                string[] transients = AssetDatabase.GetDependencies(path, true)
                    .Where(p => p.StartsWith(ReviewRoot, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (transients.Length != 0)
                    throw new InvalidDataException("Candidate still depends on ArtReview: " +
                        string.Join(", ", transients.Take(5).ToArray()));
                if (saved.GetComponentsInChildren<Renderer>(true)
                    .All(r => !r.enabled))
                    throw new InvalidDataException("Saved candidate has no enabled renderer.");

                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
                Debug.Log("NINEFOLD BATTLE CANDIDATE (NOT APPROVED): " + path +
                    " | copied " + meshes.Count + " meshes, " + materials.Count +
                    " materials, " + textures.Count +
                    " transient textures. Run the readiness audit, confirm canon and " +
                    "mobile performance, then manually map an exact gameplay UnitId.");
                EditorUtility.DisplayDialog("Unapproved candidate staged",
                    "An isolated prefab has been selected in the Project window. " +
                    "Review it, run the battle model audit and test on device before " +
                    "manually enabling an exact UnitId catalog entry. No gameplay registration was changed.", "OK");
                folder = null; // Only remove our fresh output on failure.
            }
            catch (Exception exception)
            {
                Debug.LogError("Battle model staging failed: " + exception);
                if (folder != null && AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.DeleteAsset(folder);
                EditorUtility.DisplayDialog("Battle model staging failed",
                    exception.Message + "\nNo gameplay registration was changed.", "OK");
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static bool TrySelectedSource(out GameObject prefab, out string path)
        {
            prefab = Selection.activeObject as GameObject;
            path = prefab == null ? null : AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path) || !path.StartsWith(ReviewRoot,
                    StringComparison.OrdinalIgnoreCase) ||
                !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                return false;
            var stem = Path.GetFileNameWithoutExtension(path);
            return stem.StartsWith("GreatStriderStudy", StringComparison.Ordinal) ||
                   stem.StartsWith("LivingVeilStudy", StringComparison.Ordinal) ||
                   stem.StartsWith("LivingVeilProtectiveStudy", StringComparison.Ordinal);
        }

        private static void ValidateSource(GameObject prefab)
        {
            int renderers = 0;
            foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) != 0)
                    throw new InvalidDataException("Review prefab has missing scripts.");
                if (node.GetComponents<MonoBehaviour>().Length != 0)
                    throw new InvalidDataException("Review prefab contains active script components.");
                if (node.GetComponents<Camera>().Length != 0 ||
                    node.GetComponents<Light>().Length != 0 ||
                    node.GetComponents<Collider>().Length != 0 ||
                    node.GetComponents<Rigidbody>().Length != 0)
                    throw new InvalidDataException("Review prefab contains camera, light or physics components.");
                foreach (var renderer in node.GetComponents<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    renderers++;
                    if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh == null)
                        throw new InvalidDataException("Skinned renderer has no mesh.");
                    if (renderer is MeshRenderer)
                    {
                        var filter = node.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null)
                            throw new InvalidDataException("Mesh renderer has no mesh.");
                    }
                    if (renderer.sharedMaterials.Any(m => m == null))
                        throw new InvalidDataException("Renderer has an empty material slot.");
                }
            }
            if (renderers == 0)
                throw new InvalidDataException("Review prefab has no enabled model renderers.");
        }

        private static Mesh CopyMesh(Mesh original, string folder, Dictionary<Mesh, Mesh> cache)
        {
            Mesh copied;
            if (cache.TryGetValue(original, out copied)) return copied;
            copied = Object.Instantiate(original);
            copied.name = original.name;
            AssetDatabase.CreateAsset(copied, folder + "/mesh-" + (cache.Count + 1).ToString("D3") + ".asset");
            cache.Add(original, copied);
            return copied;
        }

        private static Material CopyMaterial(Material original, string folder,
            Dictionary<Material, Material> cache, Dictionary<Texture, Texture> textures)
        {
            Material copied;
            if (cache.TryGetValue(original, out copied)) return copied;
            copied = new Material(original) { name = original.name };
            foreach (string property in original.GetTexturePropertyNames())
            {
                Texture texture = original.GetTexture(property);
                if (texture != null)
                    copied.SetTexture(property, StableTexture(texture, folder, textures));
            }
            AssetDatabase.CreateAsset(copied, folder + "/material-" +
                (cache.Count + 1).ToString("D3") + ".mat");
            cache.Add(original, copied);
            return copied;
        }

        private static Texture StableTexture(Texture original, string folder,
            Dictionary<Texture, Texture> cache)
        {
            Texture copied;
            if (cache.TryGetValue(original, out copied)) return copied;
            string source = AssetDatabase.GetAssetPath(original);
            if (string.IsNullOrEmpty(source))
                throw new InvalidDataException("A material uses a transient in-memory texture: " + original.name);
            if (!source.StartsWith(ReviewRoot, StringComparison.OrdinalIgnoreCase))
            {
                cache.Add(original, original);
                return original; // Stable project, package or built-in resource.
            }
            string extension = Path.GetExtension(source);
            string path = folder + "/texture-" + (cache.Count + 1).ToString("D3") + extension;
            if (!AssetDatabase.CopyAsset(source, path))
                throw new IOException("Cannot copy review texture " + source);
            copied = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (copied == null) throw new IOException("Copied texture did not import: " + path);
            cache.Add(original, copied);
            return copied;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name) &&
                string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name)))
                throw new IOException("Could not create " + parent + "/" + name);
        }

        private static string NewCandidateFolder(string stem)
        {
            for (int i = 1; i <= 999; i++)
            {
                string name = stem + "-Candidate" + i.ToString("D3");
                string path = CandidatesRoot + "/" + name;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(CandidatesRoot, name)))
                    throw new IOException("Could not create " + path);
                return path;
            }
            throw new IOException("Candidate revision limit reached for " + stem);
        }
    }
}
