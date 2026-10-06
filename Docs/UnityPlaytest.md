# First Unity playtest — Windows

This is the first **abstract integration scene** for Ninefold: Frontiers. It connects
the existing saved game systems to a 3D battlefield and portrait UI. Test faction A/B,
capsules, cubes, colors, mission names, two-step test campaign, stats and animation
motions are **engineering fixtures, not lore, production balance or final art**.
The current Master Lore & World-Building Archive, Production Foundations 1–8 and
locked visual masters remain authoritative and unchanged.

## 1. Get the merged code

Merge the integration PR after its checks pass. If you already have a local clone,
use GitHub Desktop to select `ninefold-frontiers`, switch to `main`, then **Fetch
origin / Pull origin**. Preserve any local edits; do not discard them to update.

If you have not cloned it, use **File > Clone repository** in GitHub Desktop, choose
the repository (or its URL), and choose a local Windows folder. Sign into the GitHub
account that has access if prompted. The folder to open in Unity is the one containing
`Assets`, `Packages`, and `ProjectSettings`. Do not create a new Unity template project
inside that folder. Git LFS is used for future art; a GitHub Desktop clone is preferred
over repeatedly downloading ZIP copies.

## 2. Open the pinned editor

In Unity Hub, add/open the repository folder as an existing project and select
**Unity 6.3 LTS, 6000.3.21f1**. Let the package restore and script compilation finish.
The first import requires internet for packages; the playtest itself uses no network.
No iOS module, Mac or phone is needed for this Windows Editor test.

Open **Window > General > Console**. If there are red compilation errors, stop here
and send the first error's full text. Do not change Unity versions or install random
packages to work around it. The `Ninefold` menu appears only after scripts compile.

## 3. Generate the scene explicitly

1. Run **Ninefold > Setup > Configure Foundation**. Save an existing modified scene
   if Unity asks. This sets up URP and the portrait foundation; Bootstrap is empty.
2. Run **Ninefold > Playtest > Create or Open Integration Scene**.
3. The active scene should be `IntegrationPlaytest`. Its hierarchy contains a camera,
   light, and `Frontiers — abstract integration playtest`.
4. Open the **Game** tab. In its resolution/aspect dropdown, add a **Fixed Resolution**
   of **540 × 960**, named `Portrait playtest`, then select it. Fit/scale the preview
   to your editor window. Maximize the Game tab if needed.
5. Press Unity's **Play** button at the top. You should see mission selection.

The scene setup command reopens an existing integration scene instead of replacing it.
It does not run automatically or add the test scene to the player build list.
If mouse/touch UI is unresponsive, check **Edit > Project Settings > Player > Other
Settings > Configuration > Active Input Handling**. This fixture uses Unity's built-in
UI Toolkit event handling with **Input Manager (Old)**; **Both** also supports it if
the Input System package was independently installed. Do not install it just for this test.
If Unity asks for an editor restart after an input-setting change, allow the restart.

## 4. First five-minute check

### Stabilization and saving

1. Select **01 · Stabilization test**. Three owned test units are selected automatically.
   You can deselect/reselect units before deploying; invalid squads disable Deploy.
2. Choose **Deploy**. You should see cyan capsules with numeric health bars, a grid,
   and a gold active-unit marker. Every active unit has a health bar without selection.
3. Click empty ground near the active unit. A route and movement cost should preview.
   **Cancel / move** must not spend movement. Preview again and use **Confirm Movement**,
   or tap the same destination marker again. The unit should move once.
4. Choose **Reload / resume**. Position and remaining movement should be preserved.
   An unconfirmed preview should not survive a reload.
5. Choose **Stabilize**, then **Confirm Interaction**. If you moved outside its reach,
   move closer on a later turn; the test objective is at the original A1 deployment.
6. On Victory, choose **Claim & save rewards**, then **Return to missions**. The claim
   becomes unavailable after use. Replaying this mission uses its smaller replay reward.

### Combat and enemy turns

1. Select **02 · Combat test**, keep the three units and deploy.
2. Confirm that cyan allies and the red enemy all have visible health bars.
3. Tap **Attack**, then the enemy. The preview shows its health before and after.
   Confirm, or tap that enemy again. Its health should change once, with no miss roll.
4. **End turn** to use the next unit. To see the enemy act, pass your remaining player
   turns without defeating it. Enemy turns and round boundaries advance automatically;
   player turns have no timer.
5. Try **Heal** on a damaged ally. A full-health target is rejected without spending
   the action. Signature starts locked in this fixture and becomes ready after that
   unit has used a normal attack; it still requires an available action on a later turn.
6. While the battle is active, stop Play mode and press Play again. The current saved
   turn, health and movement should resume. Finish combat and claim rewards.

### Shared progression

From mission selection, open **Unit collection & upgrades**. First-clear rewards can
unlock A4 and advance A1. Preview an upgrade, cancel it, then preview and confirm it;
only confirmation should spend fragments. Try the free health/armor trade-off and
check the same stats in squad preparation. Complete **03 · Starter completion test**
after the opening test, claim its mission reward, then claim the separate starter
campaign completion reward. A5 and B1/B2/B3 should become owned; the faction B test
becomes available. This shortened fixture tests one-time reward behavior, not the
approved production campaign length.

## Controls and limitations

- Mouse clicks in the Editor use the same pointer path as touch taps. The battlefield
  handles pointer-up only; UI buttons and scroll gestures do not also issue ground commands.
- A second tap on the same preview confirms it; it is a deliberate two-tap flow with
  no required double-click speed. Movement has half-unit destination assistance in this fixture.
- The active initiative unit receives commands; inspecting another unit does not take
  control of it. Attack/heal/signature buttons always refer to the active unit.
- Actions commit to disk **before** basic movement/attack feedback plays. During that
  feedback, gameplay input is locked. **Motion: reduced** skips the interpolation.
- The camera automatically frames the current living units. Manual camera gestures,
  production terrain/verticality, cinematic animations, audio, final accessibility and
  device performance tuning are future work. Passive is an explicit placeholder here.
- The bottom control area scrolls if needed, keeping its height stable when a preview
  opens. Safe-area offsets are applied; an ordinary Game view does not validate a notch.

## Saves and recovery

Test saves live in `Application.persistentDataPath/AbstractPlaytest-v1`, separate from
future production profiles. On Windows this is under `%USERPROFILE%\AppData\LocalLow`
with Unity's configured company/product folders. All writes use the existing two-slot
battle/profile stores. No save-on-quit is required; accepted commands already checkpoint.
Resuming after an app pause reloads saved state and discards transient previews/animations.

Errors stop playtest input and show the save path; they never silently reset a profile.
Send the first Console error. For a deliberate fresh test, **stop Play mode first** and
rename only the `AbstractPlaytest-v1` folder to keep a backup, then press Play again.
Do not run two Editor/player instances against this single-writer save folder.

## Report and capture the first import

Record the editor version, Windows version, portrait resolution, first Console error
if any, and which checklist step failed. A screenshot of both Game view and Console
is helpful. A clean first run should cover previews/cancel, enemy turns, health bars,
results, one-time claims, upgrades, and stopping/restarting Play mode.

Unity will generate project settings, a package lock, URP assets, panel/material assets,
and the two scenes with metadata. After successful validation, review those files in
a follow-up PR so the next clone can open the scene directly. Do not commit Library,
Temp, Logs, credentials or local saves. Preserve generated `.meta` files with assets.

**Validation boundary:** repository and core tests do not compile or run these Unity
presentation scripts. The first actual Unity import, UI rendering, picking, animation,
safe-area and persistent-path checks must be recorded on Windows. iPhone installation,
signing, real touch/performance and offline device tests remain separate gates.

API references used for this integration:
- https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Runtime-Panel-Settings.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.PointerUpEvent.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.RuntimePanelUtils.ScreenToPanel.html
