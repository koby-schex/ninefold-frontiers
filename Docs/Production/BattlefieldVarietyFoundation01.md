# Battlefield Variety Foundation 01 — PT-058

## Product decision / north star

The **World Five campaign** is intended as the *first playable, tutorial-serving campaign at launch*, not the entire game or the sole source of battles. Future worlds each get their own canonical campaign and may eventually contain multiple factions, playable units, mission types, and world-dependent modes. A long campaign must not make players fight on an indistinguishable or repeatedly recycled map.

The player **never selects a battlefield size**. There is no public Small/Medium/Large tier, difficulty selection based on terrain size or map-size label. Level designers select footprints internally according to objective, spectacle and pacing. Only **World Collision** can be explicitly identified to players as a special large-scale war encounter, triggered in deliberately authored situations and game modes rather than a generic map-size picker.

World Collision's eventual fantasy is a battle involving **more than five player units**, at supported mobile performance and clear tactical readability. The engine's **current** maximum is eight deployment slots; raising it needs separate simulation/UI/performance validation. Do not interpret this PR as shipping a playable World Collision or unlocking a new mission.

## What this PR delivers now

### Truly different playable prototype layouts

Current `PlaytestContentPackage` already had four mission-specific maps but with similar ground art and clustered objective mechanics. The new **`fixture-layouts-v3`** catalog now introduces different legal rectangles, side routes, cover arrangements and meaningful goal separation. Only the original engineering fixture missions are changed; they are not yet the authored commercial 100+ World Five story missions.

| Fixture battle | Internal authoring use, not player-facing | Navigational/visual distinction |
| --- | --- | --- |
| `fixture-opening` — World Five-inspired first campaign study | compact | 11×11 field, offset cover, three separated entry points, one reachable but distant objective; tutorial shows where to go |
| `fixture-finale` — World Five-inspired continuation study | expanded (not enormous) | 17×14 field, three distinct cover shapes, an optional center slow route and deeper objective; longer positional route |
| `fixture-next` — unrelated faction fixture | intermediate | 14×13 arena with asymmetric obstructions and limited slow ground; deliberately **not** assigned World Five canon art |
| `fixture-mixed` — quick combat mode fixture | intermediate | 14×11 combat arena, two separated obstacles, opposing actor and no invented stabilized-objective destination |

These footprints and names are **internal only**, not campaign menu choices. The existing real-world camera continues to frame each bounded arena; no terrain extends the navigable area by implication. The small scale is still suitable for portrait tactics; the extended fixture is *not* a World Collision event. Content is deterministic, not random, so replaying one mission does not silently relocate a checkpoint or rewrite its objective.

### Objective clarity

- The current Stabilize objectives were previously at the initial deployment position, allowing an immediate interaction without moving. Each V3 stabilization mission places the primary interaction at an **authored approach destination**, with a readable objective marker and accessible route.
- A modest visible objective guide post and a contrasting cap accompany existing gold objective surfaces and the existing on-screen objective icon; these are **noncanonical wayfinding props** and have no collider or false game mechanics.
- The **Mission Briefing tactical scout**, updated in PR #70, reads the new authoritative geometry and objective/deployment positions automatically. No extra UI coordinate fiction or hidden size control is needed.
- Mission success rules, action economy, movement legality, range calculation, terrain slow cost, rewards, campaign prerequisites, and unit kits remain unchanged.

### Presentation-specific environment variations

New `BattlefieldSceneDirection` selects a style by **authored EnvironmentKey** on `MissionContent`, not by selecting a map size or guessing a canonical world location. V3 fixtures use `study-w5-survey`, `study-w5-fracture`, and neutral fixture studies for the remaining missions.

- The opening and continuation use distinct **World Five-inspired**, canon-compatible mineral/ceramic, dusky mauve, ash-silver and restrained vegetation treatments drawn from the existing production art brief. Separate sky colors, exterior geological silhouettes, horizon/plant densities and fill-light tints make the two arenas meaningfully different.
- These are **environmental studies**, not the named Nareth Sanctuary or any canonical new landmarks. The exact mission sites, final materials, 3D environment assets and lighting remain pending visual/canon approval.
- All procedural outer-world scenery remains outside the movement bounds or below walking height, without colliders. No effect on line of sight, attack legality, pathfinding or saved battle location.
- Surface mesh subdivision remains bounded by the existing cap; the number of decorative rock shapes varies within existing batch meshes. Shader/performance and texture content remain subject to Unity/mobile review.
- Future worlds and factions can author their own `EnvironmentKey` and dedicated material/scenery palette; this PR does **not** paste World Five art onto World One–Four or Six–Nine.

### Durable save migration

**Critical:** Changing a saved map's bounds/objective positions under the same content revision would invalidate durable battle replay. This PR intentionally keeps the previous layout **V2** factory frozen via `CreateLayoutV2()` and increments the manifest to `fixture-layouts-v3`.

The Unity Playtest opens separate battle save directories in priority order:

1. `Layouts-v3` — new layout revision
2. `Layouts-v2` — previous playable layout; if any unclaimed fight is active, load **that original catalog and geometry**
3. Original abstract v1 legacy slot — same prior preservation policy
4. New V3 profile/encounter after no earlier battle is pending

An old attempt remains on its original layout through victory/defeat and claim. Once fully claimed, it gives way to the next revision without changing earned profile, resources, unlocked units, or campaign claims. Old battle files are **not deleted or overwritten**. Corrupt newer checkpoints still fail closed rather than being misidentified as older versions. The existing user prompt to finish the saved battle is preserved. Tests cover V2 → V3 continuity and a fresh V3 save while old bytes remain.

### World Collision authoring gate

The new `EncounterKind` distinguishes `Standard` from `WorldCollision`:

- Standard encounters cannot secretly acquire more than five deployment slots.
- A declared World Collision must author **6–8** player deployment positions and require at least six units.
- Existing one-Apex-per-squad, ownership, faction gates, body-placement clearance and core progression rules remain enforced.
- Only explicitly authored World Collision missions may show the **WORLD COLLISION** designation in mission list/briefing. No generic small/medium/large tier is exposed anywhere.
- The unit/encounter system is exercised in a test-only 8-slot map with a six-unit valid mixed squad, plus invalid double-Apex and unauthorized standard-size cases. **No World Collision mission is added to the production or playtest selection menus by this PR.**

## What remains to build

The existing fixture campaign is still **two sample missions**, not the required full World Five campaign of at least 100 distinct encounters. Those need approved story design, many unique map/layout definitions, proper canonical unit names and abilities, World Five-specific environment models, progression pacing, cutscenes and gameplay modes. The real 3D unit asset pipeline is under separate Work-chat ownership and is not connected by this PR.

The scalable production direction is:
- Create a **World Five environment kit** (modular canonical landscape, architectural materials, distinctive terrain motifs, environmental FX), then author many distinct short missions using it without reusing exact obstacle/deployment/objective positions.
- Maintain author-side internal footprint/mission-role tags for selection and QA only. No public size chooser.
- Keep objectives legible in three places: mission path/briefing scout, visible in-game objective marker, and context-sensitive in-battle HUD. Large war encounters need additional camera/group-command UX review.
- Ship other worlds and modes through the same content/scene-key pipeline when their canon art and mechanics are approved, not by treating the World Five tutorial as a universal template.
- Expand World Collision into an actual game mode or special story mission only after enough ownable units, squads >5, camera readability, initiative timing, model footprint, tactical AI, phone memory and mission rewards are approved.

## Unity validation after merge

1. Stop Play mode, pull `main`, wait for imports. Launcher milestone must read **PT-058**.
2. **Before abandoning any existing V2 battle:** resume it. It should use exactly its previous positions and terrain until the attempt is claimed, and progress/rewards must remain intact.
3. Complete/claim the old encounter, reopen, then start V3 `fixture-opening`. Check a visibly different compact battlefield, a reachable but nontrivial objective approach and a clear gold guide marker. Your squad must not spawn on the objective.
4. Reach and stabilize the marked objective. Confirm movement cost, partial movement, action use and checkpoint recovery work as previously.
5. Progress to `fixture-finale`. Observe a visibly **wider and deeper** route layout, different cover geometry and more violet/dark mineral environment treatment with a visible path to the objective.
6. Use Mission Briefing's diagram to identify deployment points, objectives, cover and slow ground. Compare each to the actual 3D arena; nothing may falsely suggest walking outside legal bounds.
7. Try mixed Quick Battle (combat objective) and follow-up faction fixture. They should not imply canon World Five geography.
8. At 320×568, 390×844 and 540×960 portrait, confirm camera keeps all legal squares, objective cues and health overlays visible. On extended arenas, check that distant units aren't too tiny and objective markers don't overlap enemies/health bars.
9. Confirm no new pink materials, shadowing errors, scenery clipping, animation/picking regressions or performance spikes. Capture a clean screenshot of each revised map; static CI is not visual verification.

## Merge boundaries with the Work chat

Touches the fixture content/versioned battle-loading contracts, test code, Unity terrain rendering/scenery, mission-list World Collision conditional label and source milestone. Does **not** touch Work-owned unit model source, OBJ, model-review tools, prefabs, materials, animations or lore archives. Check any new Work PR against these changed files before merge. Preserve Unity metadata GUIDs.
