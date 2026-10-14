using System;
using System.IO;
using System.Text;
using Ninefold.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Ninefold.Editor
{
    /// <summary>Explicit launcher; never resets saves or starts automatically on import.</summary>
    public sealed class PlaytestLauncher : EditorWindow
    {
        private const string ExpectedVersion = "6000.3.21f1";
        private string report = "", status = "";
        private Vector2 scroll;
        private static readonly string[] Styles = { "Home/Home", "Home/Campaign", "Home/Units", "Home/Battle", "Home/Results", PlaytestRevision.ControlsResource };

        [MenuItem("Ninefold/Playtest/Launcher", false, 0)]
        public static void Open() => GetWindow<PlaytestLauncher>("Ninefold Playtest");
        private void OnInspectorUpdate() => Repaint();
        private bool Busy => EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;
        private static bool AssetsReady()
        {
            if (AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Ninefold/Runtime/Presentation/Playtest.uss") == null) return false;
            if (AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Ninefold/Runtime/Presentation/PlaytestTheme.tss") == null) return false;
            foreach (string path in Styles) if (Resources.Load<StyleSheet>(path) == null) return false;
            return true;
        }
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Ninefold: Frontiers", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Loaded source revision", PlaytestRevision.Id);
            EditorGUILayout.LabelField(PlaytestRevision.Features, EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("This identifies your local imported source, not the latest GitHub version. After merging: stop Play, Pull origin in GitHub Desktop, then wait for Unity to finish importing.", MessageType.None);
            EditorGUILayout.HelpBox("Start or resume the abstract integration playtest. Your existing profile and progression are preserved.", MessageType.Info);
            bool version = Application.unityVersion == ExpectedVersion;
            bool pipeline = GraphicsSettings.defaultRenderPipeline != null;
            bool assets = AssetsReady();
            Check("Unity " + ExpectedVersion, version);
            Check("Render pipeline configured", pipeline);
            Check("Playtest UI styles imported", assets);
            EditorGUILayout.LabelField("Integration scene", AssetDatabase.LoadAssetAtPath<SceneAsset>(PlaytestSetup.ScenePath) != null ? "Available" : "Created on first launch");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                EditorGUILayout.HelpBox("Wait for script compilation and asset import to finish. Resolve red Console errors before launching.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Busy || !version))
            {
                if (GUILayout.Button("Configure foundation", GUILayout.MinHeight(32))) Run(FoundationSetup.Configure);
                using (new EditorGUI.DisabledScope(!pipeline || !assets))
                {
                    if (GUILayout.Button("Open / repair integration scene", GUILayout.MinHeight(32)))
                        Run(() => { status = PlaytestSetup.TryCreateOrOpen() ? "Scene ready. Existing references preserved." : "Cancelled. Play mode was not started."; });
                    if (GUILayout.Button("START / RESUME PLAYTEST", GUILayout.MinHeight(42))) Run(() => {
                        if (!PlaytestSetup.TryCreateOrOpen()) { status = "Cancelled. Play mode was not started."; return; }
                        EditorApplication.ExecuteMenuItem("Window/General/Game");
                        EditorApplication.isPlaying = true;
                        status = "Starting. Existing saves resume automatically.";
                    });
                }
            }
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                if (GUILayout.Button("Stop playtest", GUILayout.MinHeight(32))) EditorApplication.isPlaying = false;
            EditorGUILayout.HelpBox("For portrait testing, select 540 × 960 in the Game view's resolution dropdown. The launcher does not change Game view presets or reset saves.", MessageType.None);
            if (GUILayout.Button("Open Console", GUILayout.MinHeight(28))) EditorApplication.ExecuteMenuItem("Window/General/Console");
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);

            GUILayout.Space(12); GUILayout.Label("Diagnostics", EditorStyles.boldLabel);
            if (GUILayout.Button("Capture current report", GUILayout.MinHeight(32))) Run(Capture);
            report = EditorGUILayout.TextArea(report, GUILayout.MinHeight(180));
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(report)))
                if (GUILayout.Button("Copy displayed report", GUILayout.MinHeight(32))) { EditorGUIUtility.systemCopyBuffer = report; status = "Report copied. Paste it into the chat."; }
            EditorGUILayout.HelpBox("Reports include recent error messages while the runtime host is active. Review before sharing. Save contents and screenshots are not collected.", MessageType.None);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Show local save folder", GUILayout.MinHeight(28))) Run(() => {
                    string path = Path.Combine(Application.persistentDataPath, "AbstractPlaytest-v1");
                    if (Directory.Exists(path)) EditorUtility.RevealInFinder(path);
                    else status = "No playtest save folder exists yet. Start a playtest first.";
                });
            EditorGUILayout.EndScrollView();
        }
        private static void Check(string name, bool ok) => EditorGUILayout.LabelField(ok ? "OK · " + name : "NEEDS ATTENTION · " + name, EditorStyles.wordWrappedLabel);
        private void Capture()
        {
            var app = UnityEngine.Object.FindFirstObjectByType<FrontiersPlaytest>();
            if (EditorApplication.isPlaying && app != null) report = app.GetDiagnosticsReport();
            else
            {
                var text = new StringBuilder("NINEFOLD: FRONTIERS — EDITOR PREFLIGHT\n");
                text.AppendLine("Playtest revision: " + PlaytestRevision.Id + " | " + PlaytestRevision.Features);
                text.AppendLine("UTC: " + DateTime.UtcNow.ToString("O"));
                text.AppendLine("Unity: " + Application.unityVersion + " | Expected: " + ExpectedVersion);
                text.AppendLine("Compiling: " + EditorApplication.isCompiling + " | Importing: " + EditorApplication.isUpdating);
                text.AppendLine("Render pipeline configured: " + (GraphicsSettings.defaultRenderPipeline != null));
                text.AppendLine("UI styles imported: " + AssetsReady());
                text.AppendLine("Integration scene available: " + (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlaytestSetup.ScenePath) != null));
                text.AppendLine("Runtime host not active. No profile read or modified. For compile failures, also copy the first red Console error.");
                report = text.ToString();
            }
            status = "Report captured. You can add reproduction steps before copying.";
        }
        private void Run(Action action)
        {
            try { status = ""; action(); }
            catch (Exception e) { status = e.GetType().Name + ": " + e.Message; Debug.LogException(e); }
        }
    }
}
