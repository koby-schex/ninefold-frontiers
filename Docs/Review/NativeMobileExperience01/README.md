# Native Mobile Experience 01 — PT-060

## Why this exists

Unity playtest feedback after PT-059: Home looked better, but Campaigns/Units appeared as **small popup panels over the Home environment**, navigation Home/Back controls were inconsistent or failed on nested Unit/Squad/Briefing pages, and the battle HUD used too much of the phone's screen. The target is a portrait-first game app, not a desktop application or mobile website.

## Implemented

### Dedicated full-screen pages
- The sanctuary background is visible ONLY on Home. All other pages render as independent, fully opaque, full-width phone destinations with a fixed native-style top bar. Campaigns and mission paths have their own backdrop tones; Unit Collection is separate; Settings/Squad use neutral game interfaces.
- A coherent back button is available on Unit Detail, Mission List, Briefing and Squad; a reliable **HOME** button sits at the top of every nonbattle menu. Four persistent main tabs now remain reachable on Briefing and Squad as well, so no user can become trapped on a nested page.
- Tapping the Units bottom tab goes to the **roster**, rather than unexpectedly reopening an inspected unit; Unit Detail's contextual back returns to the roster. Pending unit offers are cancelled safely by existing navigation.
- Fixed briefing/squad deployment actions remain separate from scrollable body content and sit above the bottom tabs.
- Every ScrollView uses touch/finger scrolling, with scroll bars explicitly hidden at the UI Toolkit control level. The final stylesheet hides Unity's internal vertical/horizontal scroller visuals on all screens; the actual swipe gesture remains intact, including on the Playtest desktop Game tab. No visible scroll bars in release UI.

### Battle: phone-first controls and less text
- The top bar contains only a compact back-to-Home button, short real objective and **END** (end this unit's turn) control. There is no permanent Info button; Unit Details are available by tapping PASSIVE on the ability strip.
- The battlefield takes most of the portrait screen. The bottom dock now contains the current actor name, remaining movement and whether the primary action is available plus **ATTACK / ABILITY / ULT / PASSIVE**. They are scoped to the unit whose turn it is. There is no permanent Move, Stabilize, Info, Confirm, Cancel or End Turn second row.
- Tapping unobstructed ground once starts and **commits** legal movement via the same battle core `TapDestination` preview → versioned `Confirm` save path. The core still clips requested distant movement to available movement, respects actor collision/boundaries/slow terrain and persists changes.
- Tapping an enemy when no ability is selected selects the normal attack; that tap previews the enemy/health result. A second tap on the enemy (or contextual Confirm) commits the attack. **No automatic spending of a once-per-battle ultimate.** Abilities/ultimates remain explicit selections requiring a chosen target and confirmation; displayed targets, health prediction and hard no-miss rule remain untouched.
- Tapping an authored objective marker while the action is available directly performs the interaction through the same existing `TapObjective` + versioned `Confirm` transaction; otherwise the marker remains a visible cue and can't bypass range/action limits.
- While a special action has a pending preview, the lower strip switches to **preview / CONFIRM / CANCEL**, then returns to ability icons after resolution.
- The selected unit's **remaining movement radius** is shown as a clipped geometric ring when no ability is selected. If an ability is selected, the existing ability range ring appears instead. The movement ring represents a geometric maximum, **not a guaranteed navigable area through cover**, and never changes the core pathfinding/attack rules.
- A short error message appears only when an input is invalid. The old permanent instructional paragraph has been removed from battle; detailed help remains available in Settings.

## Safety / compatibility

- **No changes to core battles, mission objectives, saved checkpoints, shards, one-Apex-per-squad rules, unit kits or 3D models**. Inputs are routed through existing core previews, identifiers, confirmations and save machinery. Touch-to-move intentionally reduces the old two-tap accidental-motion protection; narrow phone tests should verify the finger drift threshold and avoid overlapping UI.
- **Do not modify** Work chat's generated model sources, geometry, materials, animation, prefabs, rendering adapters or canonical asset mappings.
- Home's approved sanctuary image and the Work chat's World Five models remain untouched.
- Current fixture abilities retain prototype labels; canonical ability names/descriptions will be handled as a separate content production pass.
- World Collision remains a specially authored large-scale encounter, not a player-controlled map-size selector.

## Unity smoke test (mandatory before final approval)

1. Pull merged main with Unity closed/stopped; allow Unity 6.3 LTS import. Check launcher shows **PT-060**, and all USS files import without warning.
2. At 540×960 portrait with Game tab Fit scale, switch Home → Campaigns → Units → Settings repeatedly. Every destination should be **full screen**, with no sanctuary backdrop, inset popup/modal illusion, clipped body or visible scrollbar.
3. Inspect a Unit Detail, tap top **HOME**, return Units tab. Check Units opens the roster. Test Unit Detail back and nested Campaign → Missions → Briefing → Squad back. Confirm all routes work and no pending upgrade gets committed by navigation.
4. Confirm Briefing/Squad fixed deployment buttons and four tabs remain visible on 390×844 and 320×568, and touch-scrolling works without a scrollbar.
5. Resume a saved battle; confirm *identical* saved positions, initiative and resources. Tap ground once: verify legal movement only, animated normally, auto-partial destination if outside range, persists on reopen, and never crosses blocked cover/boundaries.
6. Tap a valid enemy: check an attack preview appears. Tap again to attack; examine health preview before and after, selected target ring, no miss chance, and identical damage/stamina/action rules.
7. Select ABILITY and ULT: confirm they **do not fire on selection**, selected targeting is still accurate, and double-tap/Confirm commits only once. Cancel should never expend charges.
8. Select PASSIVE, inspect unit ability details, close and return to battle; make sure combat continues normally. Test END turn top-right with and without active previews.
9. Approach a stabilization objective: tap its visible goal marker when eligible. Confirm correct reward, no accidental movement/interaction when out of reach, and no lost saved checkpoint.
10. Check the movement radius: circle follows the current unit, shrinks with movement, disappears during enemy turn, switches to range visualization for selected abilities, and never implies a path through cover.
11. Test narrow/short Game views, landscape fallback, touch target sizes, empty/null activation transitions, scroll bar removal on every screen, collision with radar inset, status/health strip visibility, complete Results and campaign flow.
12. Record Home, Campaign, Unit Detail, Briefing/Squad and 540×960 Battle screenshots. This code has not been compiled/visually assessed in Unity by the assistant.

## Next separate phase

**World Five Environment Production 01** will remain an independent PR, enhancing presentation-owned modular scenery and lighting while leaving gameplay collision and the model-development chat alone. The commercial World Five tutorial campaign, named characters/abilities and multi-world art are larger upcoming content projects.
