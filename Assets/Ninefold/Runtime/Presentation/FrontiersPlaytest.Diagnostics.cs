using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Ninefold.Presentation
{
    public sealed partial class FrontiersPlaytest
    {
        private readonly Queue<string> diagnosticErrors = new Queue<string>();
        private float diagnosticSeconds, diagnosticFps;
        private int diagnosticFrames;

        private void OnEnable() => Application.logMessageReceived += RecordDiagnostic;
        private void OnDisable() => Application.logMessageReceived -= RecordDiagnostic;
        private void RecordDiagnostic(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (diagnosticErrors.Count == 5) diagnosticErrors.Dequeue();
            diagnosticErrors.Enqueue(type + ": " + RedactDiagnostic(condition));
        }
        private static string RedactDiagnostic(string value)
        {
            string text = value ?? "";
            foreach (var path in new[] { Application.persistentDataPath, Application.dataPath, Application.temporaryCachePath })
                if (!string.IsNullOrEmpty(path)) text = text.Replace(path, "<local path>");
            return text.Length > 800 ? text.Substring(0, 800) + "…" : text;
        }
        private void SampleDiagnostics()
        {
            if (suspended) { diagnosticSeconds = 0; diagnosticFrames = 0; return; }
            diagnosticSeconds += Time.unscaledDeltaTime; diagnosticFrames++;
            if (diagnosticSeconds >= 1)
            {
                diagnosticFps = diagnosticFrames / diagnosticSeconds;
                diagnosticSeconds = 0; diagnosticFrames = 0;
            }
        }

        // Reads cached presentation snapshots only: never reloads, advances or writes a save.
        public string GetDiagnosticsReport()
        {
            var report = new StringBuilder();
            report.AppendLine("NINEFOLD: FRONTIERS — PLAYTEST REPORT v1");
            report.AppendLine("Playtest revision: " + PlaytestRevision.Id + " | " + PlaytestRevision.Features);
            report.AppendLine("Shared controls loaded: " + sharedControlsLoaded);
            report.AppendLine("Battlefield scenery: " + (field == null ? "host not initialized" : field.SceneryStatus));
            report.AppendLine("Fixture shader: " + (FixtureMaterial == null || FixtureMaterial.shader == null ? "missing" : FixtureMaterial.shader.name));
            report.AppendLine("UTC: " + DateTime.UtcNow.ToString("O"));
            report.AppendLine("Content: abstract engineering fixture (not production canon)");
            report.AppendLine("Unity: " + Application.unityVersion + " | Expected: 6000.3.21f1");
            report.AppendLine("App version: " + Application.version + " | Build GUID: " + Application.buildGUID);
            report.AppendLine("Platform: " + Application.platform + " | Editor: " + Application.isEditor);
            report.AppendLine("Scene: " + SceneManager.GetActiveScene().name);
            report.AppendLine("Screen: " + Screen.width + " x " + Screen.height + " | Safe area: " + Screen.safeArea);
            if (root != null && root.panel != null && safe != null)
                report.AppendLine("UI panel (UI units): " + root.resolvedStyle.width.ToString("0.#") + " x " +
                    root.resolvedStyle.height.ToString("0.#") + " | safe viewport: " +
                    safe.resolvedStyle.width.ToString("0.#") + " x " + safe.resolvedStyle.height.ToString("0.#"));
            report.AppendLine("Editor Game-view Scale zoom is not an in-game UI size setting.");
            report.AppendLine("Graphics: " + SystemInfo.graphicsDeviceType + " | " + SystemInfo.graphicsDeviceName);
            report.AppendLine("Observed FPS (~1 second): " + (diagnosticFps > 0 ? diagnosticFps.ToString("0.0") : "not sampled"));
            report.AppendLine("Fatal: " + fatal + " | Suspended: " + suspended + " | Animating: " + (playback != null));
            report.AppendLine("Reduced motion: " + reducedMotion + " | Page: " + page);
            report.AppendLine("Profile: AbstractPlaytest-v1 | Initialized: " + (menu != null));
            report.AppendLine("Reload required: " + (menu != null && menu.NeedsReload));
            report.AppendLine("State below is the last rendered snapshot, not a new save read.");
            report.AppendLine("Flow: " + (menuView == null ? "not rendered" : menuView.Phase.ToString()));
            if (menuView != null)
            {
                report.AppendLine("Recovered save: " + menuView.Recovered);
                report.AppendLine("Owned units: " + menuView.Progress.OwnedUnits.Count + " | Battle receipts: " + menuView.Progress.Claims.Count);
                report.AppendLine("Prepared mission: " + (menuView.SelectedMissionId ?? "none"));
                report.AppendLine("Squad: " + string.Join(", ", menuView.Squad));
                if (menuView.Results != null)
                    report.AppendLine("Result: " + menuView.Results.Result.Outcome + " | Saved: " + menuView.Results.Claimed);
            }
            var battle = battleView?.Battle ?? menuView?.Resume;
            if (battle != null)
            {
                report.AppendLine("Mission: " + battle.MissionId + " | Attempt: " + battle.AttemptId);
                report.AppendLine("Battle phase: " + battle.Phase + " | Round: " + battle.Round);
                report.AppendLine("Actor: " + (battle.Activation?.UnitId ?? "none") + " | Movement: " + battle.Activation?.MovementRemaining + " | Action: " + battle.Activation?.PrimaryActionAvailable);
                report.AppendLine("Selected unit: " + (battleView?.SelectedUnitId ?? "none") + " | Ability: " + battleView?.SelectedAbility);
                report.AppendLine("Pending preview: " + (battleView?.Pending == null ? "none" : battleView.Pending.Kind.ToString()));
            }
            report.AppendLine("Recent errors (up to 5, messages only; review before sharing):");
            if (diagnosticErrors.Count == 0) report.AppendLine("None captured since this host was enabled.");
            foreach (string error in diagnosticErrors) report.AppendLine(error);
            report.AppendLine("Reproduction steps: [describe what you clicked]");
            report.AppendLine("Expected / actual: [describe the problem; attach a screenshot if useful]");
            return report.ToString();
        }

        private void BuildDiagnostics(VisualElement parent)
        {
            var panel = Card(parent);
            Text(panel, "Playtest diagnostics", "section");
            Text(panel, "Copy this report when reporting a problem. It includes recent error messages; review it before sharing. No save files are included.", "detail");
            var report = new TextField { multiline = true, isReadOnly = true, value = GetDiagnosticsReport() };
            report.style.height = 200; report.style.fontSize = 16; panel.Add(report);
            var status = Text(panel, "Snapshot captured. Refresh to update.", "hint");
            // Deliberately bypass gameplay guards so diagnostics remain usable after a fatal error.
            var refresh = new Button(() => { report.value = GetDiagnosticsReport(); status.text = "Snapshot refreshed."; }) { text = "Refresh report" };
            var copy = new Button(() => { GUIUtility.systemCopyBuffer = report.value; status.text = "Report copied. Paste it into the chat."; }) { text = "Copy report" };
            refresh.AddToClassList("game-button"); copy.AddToClassList("game-button");
            refresh.style.minHeight = copy.style.minHeight = 54;
            panel.Add(refresh); panel.Add(copy);
        }
    }
}
