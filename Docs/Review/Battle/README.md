# Battle interface presentation pass

Extends the approved menu palette to the live UI Toolkit battle host. Battlefield team, targeting and movement colors retain their tactical meanings. Uses existing abstract test content; no new lore, roster, artwork or combat balance.

## Changes

- Compact mission/objective header, prominent current actor and labeled upcoming initiative strip. Enemy upcoming turns have distinct styling and the existing Enemy names.
- Active-unit movement/action budget, readable three-ability controls, exact remaining main cooldown and signature prerequisites from the authored kit. Cooldowns count the owner's turns.
- The generic menu masthead is hidden in battle. Input feedback is shown in the control dock, including rejected commands and animation status.
- Larger two-line unit health badges, numeric health always available without selection, team labels/colors and a distinct active-unit border. Target preview retains the unit name and shows before/after health; the fill remains actual health until committed.
- Health badge placement searches both axes when space is available; size and placement geometry agree. Short landscape panes use smaller panel dimensions. This is best-effort collision avoidance, not a guarantee for arbitrarily crowded armies.
- Range guidance distinguishes geometric range from valid targets/line of sight. Existing range outlines, gold target rings, verified paths, actual stopping markers and partial movement remain driven by core previews.
- Larger confirmation with exact preview effect or movement cost/remaining budget. Movement that stops before the request says so. No viewport resize when opening a preview, preserving second-tap coordinates.
- A fixed-height scrolling dock in portrait and a side dock in short landscape windows. A landscape Home control preserves the escape route when the title row is hidden.

## Browser review

Open Preview.html locally; query states: Idle, Attack, Target, Move, Heal, Enemy, Spent. Use the controls or click the abstract battlefield to switch sample states. The SVG arena is a schematic placeholder. The review navigation and samples do not run combat or save progression.

Checked at 320×568, 390×844, 768×1024 and 844×390 for no horizontal page/control overflow, reachable dock, controls at least 44 physical pixels high and a stable battlefield viewport between input/preview states. Exercised attack → target → cancel; visually inspected portrait and landscape. Runtime health placement is not executed by this browser approximation.

Local metadata/structure, art validation and whitespace checks pass; core regression coverage runs in GitHub CI. Unity is not available here, so presentation compilation, USS import, camera framing, picking and actual phone rendering remain unverified.

## Next Unity playtest

1. Load/reload a battle and rotate/resize. Confirm battlefield framing stays within its surface, safe areas work and Home remains reachable.
2. Check every living unit's health without tapping; move units close together and near edges. Watch damage/healing and verify badges update after commit.
3. Select Attack, inspect range and valid targets, preview/cancel/confirm. Confirm invalid targets and occlusion still reject correctly; inspecting another unit never changes the commanding actor.
4. Preview a distant reachable destination and verify the route ends at the actual affordable endpoint. Confirm by button and by tapping that endpoint again; repeat after changing screen size with a fresh preview.
5. Use Main and verify countdown on owner turns; check signature prerequisite, once-per-battle use and action-spent states. Input remains disabled during animations/enemy actions.
6. Verify stabilization availability and objectives, End turn, battle completion and save/resume. In short landscape, scroll the side dock to confirmation, cancel and Home.
