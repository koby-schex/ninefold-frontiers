# Frontiers — Battle model motion & mobile-readiness pass 02

## What this adds

### 1. Optional combat-model animation handoff

The playable-battle model integration from PR #158 now supports optional Animator reactions based on authoritative combat events:

| Animator parameter | Type | When signalled |
| --- | --- | --- |
| `Moving` | bool | Start and end of a committed movement animation |
| `Attack` | trigger | Source of a committed ability-use event |
| `Hit` | trigger | Target of an actual damaging or status-inflicting event |
| `Heal` | trigger | Target of an actual health-increasing event |
| `Defeat` | trigger | Target defeated by that real event |

The bridge **does not author gameplay outcomes**, adjust tactical coordinates, move hitboxes, or wait for clips to finish. Damage, turns, range checks, the saved battle and reward claims continue to come from Core. Runtime Animator root motion remains disabled. Parameter presence and type are checked before signalling, so static meshes, missing clips and partially developed rigs remain supported. The bridge never adds scripts to prefabs. Reduced-motion/zero-duration playbacks skip these optional animation cues, and a suspended/exited battle resets the Moving state.

**Scope:** This is a contract and event driver—not a newly built Great Strider, Olyra, or other unit rig. The parallel model team must supply actual runtime-ready prefabs and authored clips before physical motion appears in playable missions. No art IDs are guessed or mapped to abstract starter gameplay IDs.

### 2. In-Unity model-readiness audit

Run **Ninefold > Art > Audit Playable Battle Models** with a local catalog created using the existing **Create or Open Playable Battle Visual Catalog** command.

The audit is read-only and reports: exact gameplay UnitId duplicates, null/review-only prefab references, dependencies on transient `ArtReview` assets, missing/active MonoBehaviour scripts, absent visible renderers, missing shader/materials, triangle/material counts, large textures, embedded cameras/lights/physics, and optional Animator parameters. The entire per-model report appears in the Console.

**Performance flags are review guidance, not fabricated approval**: high triangle or material counts require iPhone GPU profiling. An error-free audit does not certify lore accuracy, successful runtime animation, framerate or mobile memory limits. The user must explicitly approve and map the correct model into the catalog.

### 3. Manual QA gate

1. Confirm no model catalog is present: Missions 1–11, navigation, the Mission 3/9 carry-extraction interactions, and Core saves work with placeholder visuals.
2. Register a deliberately selected non-scripted test prefab with its exact gameplay UnitId. Audit and review the Console output before checking `Approved For Battle`.
3. Check idle/Moving/Attack/Hit/Heal/Defeat animation parameters when the Animator provides them; missing parameters must not spam errors, freeze, or alter turns.
4. Check the actual on-screen attack moment versus authored animation timing, particularly when an attack and movement occur together.
5. Toggle reduced motion and exit a battle in mid-animation; no unit should remain stuck walking.
6. Re-run battles after losing a unit: no old model, animator, or scene object should survive a new encounter.
7. Profile actual iPhone 14 GPU/frame time, draw calls, texture memory and translucent model surfaces. Do not approve an ArtReview prefab simply because it previews correctly.

No model source files, prefabs, Unity scene assets or existing catalog mappings are modified in this PR. The Work modeling branch retains ownership of asset design/rigging.

## Important CI limitation

Repository CI covers netstandard Core logic and structural checks only; it does **not** compile the Unity editor/runtime presentation assemblies. A Unity 6.3.21f1 compile and on-device visual playtest are needed before treating the new graphics pipeline as production-certified.
