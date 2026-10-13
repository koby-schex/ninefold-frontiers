# Playtest launcher and diagnostics

Open **Ninefold > Playtest > Launcher**. Nothing runs automatically on import.

## Launcher

- Checks the pinned Unity version, configured render pipeline and imported UI styles.
- Offers the existing explicit Foundation setup and integration scene open/repair commands.
- Start/resume opens the integration scene, focuses Game view and enters Play mode only after setup succeeds. Cancelling the save-scene prompt returns false and does not start Play mode.
- Start/setup are disabled during compilation, import or Play-mode transitions. Stop and Console remain available as appropriate.
- Game view resolution remains user-selected; 540×960 is the recommended portrait preset.
- Existing profile behavior is preserved. No reset, delete, new-profile or reward-grant command is introduced.
- Show local save folder is available outside Play mode only and reveals an existing folder without creating or editing files.

## Reports

The launcher captures a runtime report when an active host is available, otherwise an editor preflight report. The in-game Settings panel and guarded fatal-error screen show read-only text with refresh/copy controls. Copy copies the displayed snapshot, so users can review what is shared. The launcher's report text can be edited to add reproduction steps.

Runtime fields: UTC capture time, Unity/app version, build GUID (may be empty in Editor), platform, scene, screen/safe area, graphics API/device, sampled FPS, fatal/focus/animation flags, reduced motion, profile initialization/reload state, last rendered flow, owned-unit/receipt counts, mission/squad and available battle/result/preview state.

FPS is a lightweight last-window estimate, not a profiler or device-performance certification. Game state is explicitly labeled as the last rendered snapshot. Capturing never calls Open, Read, Advance, Confirm or any save operation. In-game Settings can include the cached resumable battle even though the battlefield is hidden.

Up to five Error/Exception/Assert messages are captured while the runtime host is enabled, truncated to 800 characters each. Stack traces are excluded and known Unity data/cache directories are replaced. This is not a guarantee that arbitrary third-party error messages contain no personal text: review the displayed report before sharing. No machine name, device identifier, save bytes, screenshots or automatic uploads are collected. Errors before host activation require the Console.

Error subscriptions are removed when the host is disabled. The fatal report uses independent UI callbacks so the gameplay fatal flag does not disable copying. Missing Panel Settings can prevent runtime UI construction; use the editor launcher report in that case.

## Validation

Local repository metadata/structure and existing art validation pass. Reviewed cancellation propagation, busy-state gates, bounded error capture, event subscription cleanup and read-only report construction against the source. Existing core tests run in GitHub CI.

Unity is unavailable in this environment. Editor/presentation compilation, actual launcher rendering, clipboard integration and the following checks remain for the next Unity session; no browser mockup is presented as evidence for native Editor UI.

## Next Unity checks

1. Pull main, let compilation finish, open Launcher and verify preflight indicators.
2. With a modified scene, press Start and cancel the save prompt: Play must remain stopped. Repeat and save: integration scene should launch.
3. Stop/start twice and verify existing progression resumes. Confirm no setup command overwrites non-null references.
4. Capture/copy a launcher report during battle and from Settings; verify mission, turn and screen data. Capture in edit mode and verify it is labeled preflight.
5. Trigger a controlled error in a disposable test scene, check bounded recent messages and that fatal-screen Copy still works. Restore the test change afterward.
6. Inspect after an unsaved preview and after a saved result. Capturing/refreshing must not commit either operation or alter receipt counts.
7. Check report scrolling and copy at narrow portrait sizes; attach a screenshot plus reproduction steps for layout failures.
