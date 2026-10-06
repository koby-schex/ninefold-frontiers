using System;
using System.IO;
using System.Linq;
using Ninefold.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Ninefold.Editor
{
    /// <summary>Opt-in scene generation. Existing scenes/assets are opened/reused, never overwritten.</summary>
    public static class PlaytestSetup
    {
        public const string ScenePath = "Assets/Ninefold/Scenes/IntegrationPlaytest.unity";
        private const string PanelPath = "Assets/Ninefold/Settings/PlaytestPanel.asset";
        private const string MaterialPath = "Assets/Ninefold/Settings/PlaytestFixture.mat";

        [MenuItem("Ninefold/Playtest/Create or Open Integration Scene")]
        public static void CreateOrOpen()
        {
            if (Application.unityVersion != "6000.3.21f1") throw new InvalidOperationException("Use Unity 6000.3.21f1 for this project.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before creating/opening the scene.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (existing == null && File.Exists(ScenePath)) throw new InvalidOperationException("Unexpected existing file at " + ScenePath);
            if (GraphicsSettings.defaultRenderPipeline == null)
                throw new InvalidOperationException("First run Ninefold > Setup > Configure Foundation, then run this command again.");
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Ninefold/Runtime/Presentation/Playtest.uss");
            if (style == null) throw new InvalidOperationException("Playtest stylesheet did not import. Check the Console.");
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Ninefold/Runtime/Presentation/PlaytestTheme.tss");
            if (theme == null) throw new InvalidOperationException("Playtest theme did not import. Check the Console.");
            var panel = Asset<PanelSettings>(PanelPath, () => {
                var p = ScriptableObject.CreateInstance<PanelSettings>();
                p.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                p.referenceResolution = new Vector2Int(540, 960);
                p.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                p.match = 0; // Keep touch targets sized by the portrait width.
                p.themeStyleSheet = theme;
                return p;
            });
            var material = Asset<Material>(MaterialPath, () => {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable. Finish package import first.");
                var m = new Material(shader); m.SetFloat("_Smoothness", .3f); return m;
            });
            if (existing != null)
            {
                var opened = EditorSceneManager.OpenScene(ScenePath);
                bool changed = false;
                foreach (var existingApp in opened.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FrontiersPlaytest>(true)))
                {
                    var doc = existingApp.GetComponent<UIDocument>();
                    if (doc.panelSettings == null) { AssignPanel(doc,panel); changed = true; }
                }
                if (changed && !EditorSceneManager.SaveScene(opened)) throw new InvalidOperationException("Could not save the repaired panel reference.");
                Debug.Log(changed ? "Repaired missing PlaytestPanel reference and saved the scene." : "Integration scene opened; existing references preserved.");
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Default scene did not create a main camera.");
            // The battlefield camera occupies only the touch surface. Clear the rest of the
            // screen explicitly rather than relying on retained pixels behind transparent UI.
            var background = new GameObject("UI background camera").AddComponent<Camera>();
            background.depth = camera.depth - 10; background.cullingMask = 0;
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = new Color(.035f, .063f, .094f);
            var host = new GameObject("Frontiers — abstract integration playtest");
            var document = host.AddComponent<UIDocument>();
            var app = host.AddComponent<FrontiersPlaytest>();
            app.BattlefieldCamera = camera; app.FixtureMaterial = material; app.ScreenStyles = style;
            // Assign after adding all components, then serialize explicitly before saving the scene.
            AssignPanel(document,panel); EditorUtility.SetDirty(app);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save the integration scene.");
            AssetDatabase.SaveAssets(); Selection.activeGameObject = host;
            Debug.Log("Integration playtest created. Open the Game tab, choose a 540 × 960 portrait resolution, and press Play. This is non-canon fixture content; see Docs/UnityPlaytest.md.");
        }

        private static void AssignPanel(UIDocument document, PanelSettings panel)
        {
            document.panelSettings = panel;
            var serialized = new SerializedObject(document);
            var property = serialized.FindProperty("m_PanelSettings");
            if (property == null) throw new InvalidOperationException("UIDocument panel serialization changed; inspect the pinned Unity version.");
            property.objectReferenceValue = panel; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(document);
        }

        private static T Asset<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Unexpected asset type at " + path);
            var asset = create(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
    }
}
