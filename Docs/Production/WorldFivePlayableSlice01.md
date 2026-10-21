# World Five Playable Campaign 01 — Missions 001–003

**PT-061 / W5-SLICE-01 · Opt-in engineering prototype · NOT locked canon**

## Scope

This is the **first playable three-mission slice** of the proposed World Five opening arc, grounded in the Master Lore Archive v26.0. It is not yet the commercial World Five campaign. The real Avarin/Continuance roster, canonical unit appearance, ability names/definitions, production enemy identities and full narrative require their own approval and Work-chat model integration.

The world has an established opening lens: a small Continuance route-preservation team near Year 0 investigates a historically reliable route arriving at the wrong place. The game's immediate story concerns route stabilization and nearby rescue rather than revealing the underlying anomaly.

### How to access

From the Unity playtest Home screen, tap **TRY WORLD FIVE MISSIONS 001–003**. This enters a separate campaign prototype and saves progress under `WorldFiveSlice-v1` rather than writing to `AbstractPlaytest-v1`. Choose Campaigns, then the World Five opening prototype. When no battle is active, use **RETURN TO ORIGINAL PLAYTEST** on Home to restore the previous fixture campaign and its original progress.

Switching is intentionally disabled while a battle or unclaimed result is active. Finish/claim the encounter before switching. The original fixture campaign, results, Quick Battle, unit collection and achievements have not been replaced or migrated. The prototype's separate unit collection is independent too.

### Actual authored encounters

| Mission | Working draft title | World Five story candidate | Existing core objective | Author-only footprint / terrain |
| --- | --- | --- | --- | --- |
| 001 | The Wrong Arrival | Investigate a mismatched route destination | Stabilize at a marked interaction point, requiring movement | compact 11×11 approach, two separated cover blocks |
| 002 | Hold the Passage | Keep a second connection passable | Stabilize along a longer route | 15×13 asymmetrical corridors with central difficult ground |
| 003 | Return the Stranded | Reach a stranded nearby traveler at a recovery zone | Rescue & Extract | 16×13 arena, route cover, different slow-ground position, distinct recovery marker |

The player **never sees or chooses** a map-size scale. Each map retains the same legal core movement bounds, initiative/action economy, squad limit of three fixture units, one-Apex maximum and existing attack/ability rules. The level sequence is **001 → 002 → 003**, unlocked on victory/claim. All three start with the existing abstract `fixture-a` starter units (no new canonical Avarin model identities are asserted). Rewards use isolated fixture resources only.

The mission titles and scenarios remain **canon candidates**, not approved public lore.

### Important rescue prototype limitation

The engine currently supports Rescue & Extract but does **not** yet support issuing escort orders to an independently spawned friendly NPC. To keep the first slice playable, mission 003's provisional friendly `fixture-npc` is already inside the marked recovery zone. The player must reach the proxy and activate its rescue interaction, demonstrating the **reach + rescue + completion** core flow without claiming to offer a full mobile escort system.

A genuine two-phase rescue—retrieve a stranded character, then guide that character into a separate extraction zone—requires additional friendly-NPC follow/escort behavior and tests before mission 003 becomes commercially acceptable. This shortcut must **not** be used to misrepresent a finished rescue mission.

### World identity and quality

These encounters currently use `study-w5-survey` and `study-w5-fracture`, the approved *direction* of the existing World Five-inspired art studies, not finished lore-master environment models. Their authored obstacles and objectives are distinct. A future art pass will supply lore-reviewed world geography/architecture and additional scene variants. No other world's environment art is reused as World Five.

The mission content is explicitly marked `AbstractFixture` and therefore fails the release-stage `CampaignProductionAudit`, as it should. The opening-ten draft does not become canon or a live catalog by this PR.

### Save and model safety

- **No changes** to Work-owned 3D models, prefabs, Blender scripts, render cameras, textures, animation or unit-asset mapping.
- New content has a distinct `ContentManifest`, `WorldFivePlayableSlice.ProfileId`, battle save directory and profile save directory. Existing original playtest saves remain in place. Reopening Unity restores original playtest by default; opt in again from Home to resume the separate World Five prototype.
- A game started using a previous prototype catalog is not silently changed. Any future update that changes this slice's authored map/goal geometry requires a new revision/slot strategy, just as PT-058's V2→V3 fixtures did.
- No World Collision mission is introduced. Larger squads require separate camera, simulation and encounter work.

### Post-merge Unity validation

1. Pull current main after stopping Unity Play mode. Launcher should show **PT-061**. Verify the new Home entry and that the prior saved encounter is intact.
2. With the original campaign at selection, tap **TRY WORLD FIVE MISSIONS 001–003** and open Campaigns. All three draft mission titles appear on their own campaign path, 002/003 locked.
3. Mission 001: select 1–3 owned abstract standards, move to the gold marker and stabilize. Confirm the primary objective is not reachable from spawn.
4. Claim result, check 002 unlocks. Complete 002 using an alternate route around difficult ground. Verify mission reward count and checkpoint recovery.
5. Complete 003 by approaching the provisional rescue actor at the gold recovery marker; tap the marker/actor to perform rescue. Verify victory, claim, and third mission completion.
6. While in an active battle or unclaimed result, the mode toggle is disabled. Test app pause/relaunch during each encounter, including during a pending action.
7. After clearing all three, claim campaign completion rewards. Tap Home and **RETURN TO ORIGINAL PLAYTEST**. Old original progression/resources/saved mission should be unchanged. Re-enter the World Five prototype and confirm its progress is preserved.
8. Review on 540×960 / 390×844 / 320×568 with no scrollbars, no popup menus, safe area alignment, visible objectives, usable tap-to-move and touch-target sizes. If a friendly rescue actor masks the objective marker, the latest tap prioritization explicitly targets the rescue marker.
9. Photograph the first three battlefields, campaign path and rescue interaction for the next visual and mission-design review.
10. CI/.NET validates core construction and isolated saves; Unity Editor rendering and iPhone behavior still need user playtesting.

## Next production work

After review, replace the temporary 003 recovery shortcut with true companion extraction mechanics; refine contextual tutorial guidance for 001–003 without clutter; integrate approved World Five unit identities, actual 3D models and meaningful abilities. Expand the next playable batch (004–006) only after validating the first three and conserving scene/save revisions.
