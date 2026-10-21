# World Five Opening Ten — Unity Integration 01 (PT-066)

## Purpose

Connect the **ten-mission V4 content catalog** approved for internal development in PR #86 to the existing Unity Home / Campaigns / Missions flow, now that PR #85's touch-first battle HUD has merged. This is an actual playable prototype integration, **not a launch-ready campaign** or declaration of locked story/canon.

### Immediately visible behavior

- Home displays **TRY WORLD FIVE MISSIONS 001–010**.
- With no active saved battle or unclaimed results in the *current* profile, a tap opens the **World Five Campaign Path** directly, showing **all ten cards**. Mission 001 is playable; 002–010 are locked behind sequential mission completion and reward claiming.
- Each of Missions **007–010** has its own provisional in-game name, concise objective explanation, unique authored arena, and correct existing objective rules:
  - 007 **Clear the Approach** — Defeat two hostile test proxies, teaching inspection and validated normal attack.
  - 008 **The Divided Route** — Secure a contested zone for two nonconsecutive rounds on a wider but bounded field.
  - 009 **Keep the Bond** — Use the first deployed unit to rescue a friendly proxy, then return to a separate extraction zone while one test threat pressures the team.
  - 010 **A Line That Holds** — Stabilize the primary point twice, with optional threat defeat and two-round survival. The primary waypoint remains the central objective.
- A friendly pickup proxy overlaps the rescue marker in **003 and 009**. Both missions now prioritize tapping the rescue marker when interaction is valid rather than accidentally inspecting the underlying character. After pickup, the existing PT-063 carrier extraction goal and radar guide the player to the exit.
- The existing **PT-065 battle HUD, double-tap normal attacks, inspectable units, active HP dock, passive info control, and unified bottom navigation** remain unchanged.
- **No new map size picker** is exposed. Fields are varied through actual mission definition geometry; World Collision battles remain separate events only and do not appear in these ten introductory missions.

## V4 saves — important

The World Five Home switch now opens `WorldFiveOpeningTenPrototype.Create()/Open()` using:

- Directory: `Application.persistentDataPath/WorldFiveOpeningTen-v4`
- Profile ID: `w5-opening-ten-profile-v4`
- Content revision: `w5-opening-ten-layout-04`

This **does not import, overwrite, delete or migrate** saves from the original `AbstractPlaytest-v1` playtest or World Five V1/V2/V3 folders (`WorldFiveSlice-v1`, `WorldFiveSlice-v2`, `WorldFiveSlice-v3`). **V4 starts with its own campaign progress**. If the user previously cleared the first six V3 missions, those victories still exist on disk but will not be shown as completed in V4. Restoring or importing older progression should only happen in a separately reviewed migration design, not ad hoc during this integration.

Switching between original and V4 is explicitly blocked by a **current saved battle** (offers existing keep/end-battle confirmation) or an **unclaimed result** (asks user to claim result); there is never an automatic forfeit, checkpoint replacement, or silent save deletion. If the V4 catalog cannot open, the existing rollback behavior restores the prior mode and surfaces an error.

The Home / Campaigns / Units / Settings screens remain equal-level game tabs. The World Five prototype is still entered through Home and then displayed in the standard mission path, rather than a new modal popup.

## Locked lore and art boundaries

World Five is **The Fractured Meridian** and the prototype launch lens is a small Continuance field team investigating a historically dependable route whose destination has become inconsistent. Final narrative details, character appearance, adversary identity and ability descriptions still require review against the latest Ninefold Master Lore Archive and the locked visual masters.

All `fixture-npc` opponents and starter `fixture-a` units remain explicit **noncanon engineering stand-ins**. No named enemy faction, protected story mystery, canonical unit kit, Work-produced 3D model, prefab or physical miniature is added in this PR. The existing `study-w5-survey` and `study-w5-fracture` scene treatments are visual studies, **not** ten different finished World Five biome assets.

## Code scope / conflict prevention

Only:
- `Assets/Ninefold/Runtime/Presentation/FrontiersPlaytest.cs` — Home entry, V4 catalog open, mission names/descriptions, rescue click-through for #009.
- `Assets/Ninefold/Runtime/Presentation/PlaytestRevision.cs` — PT-066 launcher identifier.
- `Assets/Ninefold/Runtime/Core/Content/WorldFiveOpeningTenPrototype.cs` — clarify now-live prototype status comments only. **No gameplay data/definitions changed from PR #86.**
- This review document.

No other Work-chat model generator, 3D rendering, rigging, animations, prefab, render target, Unity scene, style resource, save serializer, movement/attack/turn rule, combat AI or mission content file is touched.

## How to verify in Unity

After merging, **stop Play**, pull latest main in GitHub Desktop, let Unity import, open `Ninefold > Playtest > Launcher` and confirm **PT-066**. Before any gameplay test, verify the Console is free of compilation errors and that the PT-065 HUD import remains intact.

1. With the original test profile in **Selection** phase, tap **TRY WORLD FIVE MISSIONS 001–010** on Home. The ten mission cards should appear immediately; the path should show **0/10 completed** on a fresh V4 profile. Mission 001 is unlocked, all others locked. Do not confuse this with old V3 progress; it is still stored separately.
2. Open Mission 001, choose a legal squad, and confirm the existing tap-to-move, double-tap enemy attack, single-tap inspection, active HP HUD, objective marker and End Turn controls still work.
3. Progress sequentially through 002–006 to unlock 007. Confirm the prior carrier rescue 003, zone control 004, Protect 005 and timed Survive 006 function; these authored definitions are unchanged from V3.
4. **007:** Two hostile test proxies require appropriate actions to defeat. Verify no premature victory, that taps once inspect and double-taps perform exactly one valid normal attack, and that obstacle/path rules remain authoritative.
5. **008:** Reach the marked capture zone and hold for two (not necessarily consecutive) completed rounds. Enemy presence may contest. Verify the HUD does not invent a second goal.
6. **009:** Recover the friendly proxy with the first deployed squad member, under enemy pressure. Verify the pickup marker gets tap priority over inspecting the friendly proxy, that the objective switches to extraction **without ending the battle**, and that reaching the exit with the living carrier completes the mission.
7. **010:** Stabilize the gold objective twice. Extra defeat/survival goals are **optional** and should not obscure the primary one. Finish mission, claim rewards and verify campaign completion progression.
8. Stop and reopen Play mode during any V4 battle, including **after pickup in 009**. Reenter V4 from original Home; checkpoint must resume with the right map, round, objectives, HP, carry state and no duplicate rewards.
9. With a saved **original** battle, tap the V4 entry: the app must offer a clear keep/end confirmation, never auto-forfeit. Claim outstanding results before switching. Returning to original mode should restore its old progress.
10. Compare Home/Campaigns/Units/Settings nav heights and battle HUD clarity at **540×960**, **390×844**, and **320×568**. Review text wrapping, objective visibility, tap hit targets, and no visible scrollbars. Send screenshots and any red Console messages to the UI development chat.

## Tests and release gate

PR #86's existing `WorldFiveTenMissionTests` already validate the V4 content construction, first-six fidelity, separate V4 profile and checkpoint resume, all ten gates, six objective types and distinct geometric layouts. PT-066 integrates its Unity entry; GitHub's Windows/Linux .NET tests and repository checks can show the backend remains healthy, **but cannot execute Unity UI Toolkit code** or establish that click/touch behavior works on devices.

**This is a prototype milestone, not a commercially releasable ten-mission campaign.** The 100+-mission launch target, canonical World Five models, canon-approved story, real ability names/descriptions, varied environments, performance, tutorial pacing and accessibility still require substantial production.
