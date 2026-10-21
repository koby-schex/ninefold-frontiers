# Enemy Encounter & Difficulty Quality 01 — World Five

## Purpose
Following merged PR #88, add a reproducible **authoring pressure baseline** for all ten prototype missions and a focused lethal-enemy-turn regression. This is deliberately **observational**, not an automatic difficulty adjustment or proof that the enemy AI is balanced.

## What CI now records
The dependency-free .NET core tests emit one PRESSURE line per World Five mission: hostile actor count, deployment squad capacity, hostile-to-slot ratio, friendly NPC count, primary objective kind and optional objective count. The tests also pin distinct encounter shapes for the late-world objectives: two-target clear (007), contested secure (008), protected rescue NPC under threat (009), and multi-goal stabilization finale (010).

Additional combat regression ensures a lethal aggressive enemy turn ends the mission with **exactly one enemy activation**, disallows repeat execution and applies the correct result.

## Why this is useful
These metrics make future encounter edits reviewable: changing enemy count, rescue pressure, objective mix, or available squad slots will show up in the quality output. The current tests enforce the explicitly authored structure of 007–010 without requiring a particular difficulty rating.

## Limits and coordination
- No claims of automated win rates, strategic fairness, successful dynamic pathfinding, or balance quality.
- No altered gameplay AI, enemy damage/health, encounter definitions, UI, save data, profile identifiers, or World Five canon.
- No changes to Work-owned meshes, materials, textures, animation, models, prefabs, or scenes.
- No existing mission geometry or PR #88 route checks touched.
- Before changing actual balancing parameters, collect real multi-round simulation observations and Unity playtest results.

## Test scope
- `Tests/Ninefold.Core.Tests/EnemyEncounterQualityTests.cs`
- `Tests/Ninefold.Core.Tests/Program.cs` (test registration only)
- This review document

Run the standard Windows and Linux core test workflow for the PR. This work is an incremental diagnostic foundation, **not** a completed encounter-balancing pass.
