# Battlefield Objective Guidance 01 — PT-059

## Purpose

After Battlefield Variety 01 (PT-058), the authored fixture maps now differ in shape, cover, approach and atmosphere, and **the player's primary objective must never become harder to understand merely because the map is larger**. The earlier battle HUD had a specific defect: it presented every non-Stabilize goal as "Defeat enemies", which would be inaccurate for Protect, Rescue/Extract, Secure and Survive encounters.

This follow-up is deliberately **presentation-only**. It does not alter map sizes, mission availability, saved geometry, unit or ability behavior, hit/line-of-sight rules, campaign content, faction canon or the Work chat's model pipeline.

## In-game behavior

### Correct goal header

Replace the overloaded, wordy battle-objective sentence with a compact, high-contrast goal panel. Display the **first authored objective** as the primary task with its actual ObjectiveKind and current progress, and show an understated BONUS completed/total indicator for at most two optional objectives.

The supported labels are:

| ObjectiveKind | On-screen type |
| --- | --- |
| Defeat | ELIMINATE |
| Protect | PROTECT |
| RescueAndExtract | RESCUE / EXTRACT |
| Secure | SECURE |
| Stabilize | STABILIZE |
| Survive | SURVIVE |

Completed/failed objectives show DONE/FAILED. These are not marketing strings or inferred game outcomes; they are read directly from `BattleView.Objectives`. No unit-specific ability names are changed in this PR.

### Read-only tactical radar

Add an understated portrait inset map to the top-right of the battlefield view, generated from the current **authoritative BattleView.Map**. This uses **no secondary camera, render texture or 3D model import**. It displays:

- Authored cover blocks and costly/slow terrain from the real map.
- Living player-squad units as cyan circles; hostiles as red/orange squares; neutral actors as pale circles; the current actor gets an outlined ring.
- The **currently active** objective destination as a gold ring if its definition provides a position: Stabilize interaction, Secure zone, Rescue interaction/exit, or a named target actor for Defeat/Protect. Survive has no fake target point and says HOLD POSITION.
- When there is no active objective, a COMPLETE or FAILED state rather than a misleading waypoint.

Radar coordinates are computed from the same map bounds as the playable field and briefing scout; they automatically adapt to internal footprint changes and do **not** display dimensions or tiers. The radar does **not** calculate paths or draw straight lines through blocking cover: it tells the player *where*, not *how*. Map size never appears as a player control. World Collision remains its own reserved encounter designation, and no new mode is introduced here.

**Input safety:** The inset and all of its children are `PickingMode.Ignore`, and the battlefield surface explicitly excludes the inset rectangle on pointer down/up. Touching a radar marker is not a movement or attack order. All battle inputs still run through the existing `BattleInteraction` preview/confirm and persistence flow.

### Accurate briefing copy for V3 maps

Update the temporary fixture mission descriptions to reflect PT-058's actual terrain geometry and objective separation. Old V2 and v1 recovered encounters retain the earlier descriptions. These are engineering/playtest names only: **not** the final World Five campaign narrative, canon unit kit names or region titles.

## Future world/campaign framework

World Five remains the **first intended launch/tutorial campaign**, not the game's only playable mode. Its finished campaign will require a production narrative, many unique authored maps, terrain kits with actual canon-compatible 3D visual identity, and the approved Great Strider/Living Veil/Burdenbeast unit models from the separate Work chat.

Other worlds must supply their own environments through the authored EnvironmentKey/production art process. World Collision remains a distinctly presented high-stakes war encounter, reserved for suitable missions/modes and subject to separate performance, AI, camera and larger-squad evaluation. **No player-facing small/medium/large map size, selector or hidden difficulty hint should be introduced**.

## Files / coordination

- New `Assets/Ninefold/Runtime/Presentation/BattleObjectiveGuidance.cs` and stable Unity meta: self-contained presentation-only HUD/radar builder.
- New `Assets/Ninefold/Resources/Home/BattleObjectiveGuidance01.uss` and stable Unity meta: battle-scoped portrait styling, responsive for small/short preview windows.
- `FrontiersPlaytest.cs`: calls the builder, excludes the radar from picking, corrects temporary V3 mission text, and loads the stylesheet last.
- `PlaytestLauncher.cs`: validates the stylesheet during import.
- `PlaytestRevision.cs`: PT-059.
- This review note.

**Do not modify** the Work chat's `SourceArt/`, `BuildTools/Art/`, model review prefab/material/animation files or 3D integration code. Do not conflate tactical token fixtures with canon unit models. Retain the existing optional `IFrontiersPreviewSource` integration contract.

## Required Unity review

1. Merge only after the actual repository checks pass; pull latest main, stop Play, allow Unity 6000.3 LTS to import; launcher must show **PT-059** with no missing stylesheet warning.
2. Open a saved PT-058 battle and **resume without movement/state change**. Radar should show the same legal terrain, surviving actors and objective; no new checkpoints should be written by UI reading.
3. In the World Five-inspired compact and deeper finale arenas, inspect radar vs. the briefing's FIELD SCOUT preview. Obstacles, slow-ground patches and destination must agree exactly; no invented paths over cover.
4. Verify a stabilization objective header, its 0/1 to DONE transition, objective status ring and visibility on the 3D field. Enter Quick Battle and confirm **ELIMINATE** rather than the previous generic label, with the living enemy marked.
5. Tap the radar repeatedly while a unit can move or a target preview is active. There must be **no movement, attack, target selection, saved-state mutation, or confirmation** from a radar tap. Move normally on the actual battlefield outside the inset.
6. View the inset with a unit or its health strip beneath it. Check visual overlap and whether the inset needs to be smaller or move elsewhere; no data should be unreadable.
7. Test 540×960 at fitted Game-view Scale, 390×844, 320×568 and a short landscape preview. Header, Info/Home controls, all action buttons and health/target previews must remain visible and reachable.
8. If testing Protect/Rescue/Secure/Survive via existing core fixtures, confirm correct primary label, no fabricated waypoint when an objective is nonspatial, and completion/failure states.
9. Reopen Unity after battle updates. Observe no duplicated radar elements, shrinking battlefield, lost selection, memory leaks, build errors, clipping or palette mismatch.

## Honest limitations

Automated GitHub tests exercise the independent .NET gameplay/core and source checks; they **do not** run Unity Editor or iPhone rendering. The inset map is a schematic, not a high-quality environment model or canonical cartographic artwork. Future objectives such as multiple simultaneous rescue candidates, moving targets, timed reinforcements and World Collision's larger field may need expanded icon/camera presentation, which should be reviewed before production release.
