using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ninefold.Editor
{
    // Explicit setup only: opening the project never overwrites settings or scenes.
    public static class FoundationSetup
    {
        private const string EditorVersion = "6000.3.21f1";
        private const string RendererPath = "Assets/Ninefold/Settings/FrontiersRenderer.asset";
        private const string PipelinePath = "Assets/Ninefold/Settings/FrontiersPipeline.asset";
        private const string ScenePath = "Assets/Ninefold/Scenes/Bootstrap.unity";

        [MenuItem("Ninefold/Setup/Configure Foundation")]
        public static void Configure()
        {
            if (Application.unityVersion != EditorVersion)
                throw new InvalidOperationException($"Open this project with Unity {EditorVersion}.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                EnsureVacant(RendererPath);
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                EnsureVacant(PipelinePath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Remove quality-specific pipeline overrides so all levels use the default.
            int previousQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = null;
            }
            QualitySettings.SetQualityLevel(previousQuality, false);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.productName = "Ninefold: Frontiers";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EnsureVacant(ScenePath);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save the Bootstrap scene.");
            }
            // Preserve any later scene configuration; seed only an empty list.
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Ninefold foundation configured. Review and commit generated settings, assets and package lock. No gameplay is implemented.");
        }

        private static void EnsureVacant(string path)
        {
            if (System.IO.File.Exists(path))
                throw new InvalidOperationException($"Existing asset at {path} has an unexpected type. Resolve it before setup.");
        }
    }
}
