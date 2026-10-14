# Minimal battle interface — playtest response

This supersedes the always-visible battle dock and health badge presentation in
BattleInterface.md / older playtest checklists. It changes presentation only.
Canon, approved background artwork, battle rules, saves and rewards are unchanged.

## Visible by default

- Small header: Home, objective progress, Info.
- Battlefield with compact numeric health strips for every unit. Ally/enemy color
  and the active-unit gold marker remain. Large identity labels and movement-dot
  carpets are removed; tap ground to preview a route.
- Fixed 224-panel-pixel portrait dock: active unit / movement / action, three
  ability buttons, Move / objective interaction / End turn, one contextual hint.
- A pending action replaces the ability row with effect, Confirm and Cancel.
  It does not change the battlefield viewport or the location of a second tap.
- Info opens a scrollable detail panel over the field: round, upcoming turns,
  inspected unit health/armor, current actor ability requirements/ranges, objective
  readiness and controls. Battlefield taps do not pass through it; orders are
  disabled while it is open. Close or Back to battle restores controls.
- Invalid commands retain the exact rejection explanation in the dock.
- Landscape uses a 200-panel-pixel side dock.

Buttons use a shared raised-edge treatment, rounded corners and explicit pressed,
selected, disabled and focus states. Primary actions retain the warm gold fill.

## Saved battle policy (explicitly retained)

During an unfinished battle, Home offers Resume and Settings. Campaigns, Units and
Quick Battle remain disabled. No browsing or progression gates were changed. The
Home explanation now calls out the restriction. Complete and claim the battle to
return to the full menu. There is no abandon/reset command in this change.

## Validation

Repository metadata/art validators and whitespace checks run locally. Core CI runs
on Windows and Linux. Neither compiles Unity Presentation/Editor assemblies.

A temporary browser approximation using the repository styles was checked at
320×568, 390×844, 768×1024 and 844×390, in idle and pending-preview states.
No horizontal overflow; action targets at least 45 physical pixels in the smallest
portrait; identical battlefield bounds before/after preview. The field occupied
about 67% of the 320×568 screen height and 73% at 390×844. Browser output is not a
Unity render and does not verify picking, fonts, camera, safe areas or UI Toolkit.

## Next Unity check

1. Resume saved battle; verify menu gates remain and Home explanation is visible.
2. Confirm health strips identify current HP for every unit without selection.
3. Tap ground; Confirm/Cancel and second-tap confirmation work without camera jump.
4. Select Attack: range and valid-target rings appear; target preview shows effect.
5. Try an obstructed/out-of-range target and confirm a helpful rejection appears.
6. Open Info; scroll, read requirements, and close it. No click moves a unit underneath.
7. Use an objective, main/signature, and End turn; watch enemy progression.
8. Finish, collect rewards and verify Campaigns/Units/Quick Battle unlock again.
9. Check the portrait dock on a small window and the landscape side dock; scroll
   remains available for long translated/error text or future multi-objective missions.

Unity import and interactive checks remain unverified until run in the Editor.
