# World Five Route Reliability 01 — Content Quality Gate

## Why now
The user has merged PT-066 (ten-mission World Five prototype) but wants several more development merges before another Unity playtest. Structural catalog validation already confirms map bounds, actor placements, mission prerequisites and six objective types, but it does **not** prove that an actual player-sized body can travel from deployment to an objective.

This PR adds a **non-mutating authoring audit** plus regression tests. It does not rebalance enemy damage, lengthen the campaign, change the PT-065 combat HUD, touch saved profiles, alter canon, or modify the other chat's 3D model work. No new checkpoint revision is needed.

## What the audit does
`OpeningRouteReadiness.Review(catalog, missionIds)` uses the first starter's **actual `FieldBody` collision footprint**, the mission's **first deployment slot**, the **existing obstacles and fixed NPC placements**, and the starter's authored normal attack range to flood-fill walkable points on a **0.25 world-meter cardinal grid**. Each step validates a whole collision footprint and an obstacle/actor-swept segment. It then checks:

- **Stabilize:** at least one reachable legal position lies within actual interaction range **and** has an unblocked interaction line to the marker.
- **RescueAndExtract:** the pickup has a legal interaction approach; a **separate body-fitting extraction location** lies in the same connected reachable component. This specifically guards the first-deployed designated carrier's return path.
- **Secure:** the full friendly unit footprint can enter the holding zone.
- **Defeat:** the player can approach each authored enemy enough for the fixture's normal attack reach with no static obstructing wall.
- **Protect / Survive:** these are temporal goals. The audit refuses to invent a location requirement and does not claim their success is automatic.
- **Optional goals:** spatial optional objectives are checked too (for example, the mission 010 threat).
- Impossible/blocked first deployment and excessive grid extents produce actionable diagnostic codes rather than changing the runtime battle.

Finding codes: `DEPLOYMENT_BLOCKED`, `FIELD_SAMPLE_LIMIT`, `INTERACTION_UNREACHABLE`, `EXTRACTION_UNREACHABLE`, `CONTROL_ZONE_UNREACHABLE`, `ATTACK_APPROACH_UNREACHABLE`, `TARGET_MISSING`.

The report is **read-only**: it never moves units, calls `StartNextRound`, mutates health/AI/turn order, changes a content revision, or accesses saved profile directories.

### Exact scope / limitations
This is **conservative static navigation sampling**, **not** a proof of combat victory, complete dynamic pathfinding or a replacement for the actual gameplay engine. The audit assumes the first deployment's starter body and initial fixed NPC occupancy. It does not estimate risk from enemy behavior, initiative, number of turns, damaged characters, difficult-ground time cost, pathfinding node-limit changes, moving opponents, battle interaction UI states or optional AI behavior.

A mission could fail the sampled diagnostic despite having a valid narrow non-grid route, or pass and still be tactically difficult. That is why any failed audit needs a human geometry review, not an automatic simplification of world geography or combat mechanics.

## Tests
New `OpeningRouteReadinessTests` registered into the existing .NET suite:
1. All ten World Five V4 prototype missions (primary and optional goals) have safe sampled routes from the designated deployment.
2. Deliberately impossible stabilization behind a field-spanning wall is detected.
3. A pickup that is reachable but an extraction point behind the same wall is rejected as an exit failure.
4. Duplicate, unknown and empty mission IDs are refused explicitly.
5. The audit leaves the campaign's original mission IDs, spawn coordinates, save revision and V4 profile identifier unchanged.

This test suite must pass on **Windows and Linux** before the PR is merge-ready. Repository-structure checks should also pass. A green result guarantees the audit logic's specified static checks only, not Unity Editor compilation or an on-device playtest.

## Coordination
Allowed modified files:
- `Assets/Ninefold/Runtime/Core/Content/OpeningRouteReadiness.cs` + its Unity `.meta`
- `Tests/Ninefold.Core.Tests/OpeningRouteReadinessTests.cs`
- `Tests/Ninefold.Core.Tests/Program.cs` (runner registration only)
- This documentation

No existing World Five mission geometry, enemy AI, objectives, UIs, statuses, materials, animations, meshes, prefabs, scenes, save serializers or Work-owned 3D generation assets are touched.

## Next
After this quality gate, the next additive milestone can target **encounter pacing and threat-pressure checks** (enemy turns, difficulty/round budgets, optional objective clarity), and then the user can test multiple merged changes together in Unity. The commercial World Five campaign still requires 100+ properly authored missions and canon-approved characters/art.
