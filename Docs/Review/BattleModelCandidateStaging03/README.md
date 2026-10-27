# Frontiers — first playable World Five model: isolated candidate handoff 03

## Why this milestone exists

PR #158 enables opt-in exact gameplay-UnitId battle prefabs; PR #159 wires optional combat Animator signals and a read-only audit. The currently tracked Great Strider and Living Veil work is sophisticated **review** art, not yet final, rigged or iPhone-certified. The fingerprint-named Unity imports live under `Assets/Ninefold/ArtReview/`, which the battle model audit correctly rejects as transient. Simply referencing a review prefab in the gameplay catalog would create fragile dependencies and imply approval that has not happened.

This step creates a **safe, reproducible, editor-driven staging bridge**. It does not declare either organism canon-final or make an unreviewed unit playable by default.

## Unity operation (explicit, no guessing)

1. In Unity 6.3.21f1, run **Ninefold > Art > Great Strider Review** or **Ninefold > Art > Living Veil Review**, create/open the latest art review scene, and validate its imported assets. Keep this model-development team's source/review assets untouched.
2. In the **Project** window, select the appropriate generated **prefab asset** under `Assets/Ninefold/ArtReview/`: `GreatStriderStudy*.prefab`, `LivingVeilStudy*.prefab`, or `LivingVeilProtectiveStudy*.prefab`. Do not select the scene, measuring post, or a GameObject scene instance.
3. Run **Ninefold > Art > Stage Selected Review Model as Battle Candidate** and explicitly confirm.
4. Unity creates a unique candidate under `Assets/Ninefold/ArtProduction/BattleCandidates/<study>-Candidate###/`: a standalone prefab with copies of its imported meshes, materials and review-hosted textures. Source prefab links are unpacked. The tool refuses missing scripts, MonoBehaviours, scene lighting/cameras/physics, or residual references to `ArtReview`. It selects the output candidate and logs copied counts; failures remove only the newly created candidate folder.
5. This candidate is **not** assigned a gameplay UnitId and is **not** approved. It remains outside `Resources/`, but may be directly referenced by the existing `BattleUnitVisuals.asset` catalog when the art and gameplay teams explicitly agree upon an actual UnitId. Never infer a gameplay ID from `world05.living-veil.olyra`, an OBJ filename or the model display name.
6. Run **Ninefold > Art > Create or Open Playable Battle Visual Catalog**, add the exact roster gameplay UnitId and candidate prefab, keep **Approved For Battle** disabled until the art/QA gates below pass, then run **Ninefold > Art > Audit Playable Battle Models**. If and only if formally accepted, enable the flag and start a mission featuring that exact unit to verify it renders in-game. Disable the flag to revert immediately to placeholders.

### Candidate limitations — no hidden claims

- Staging snapshots the current **static** review meshes and materials, preserving current transparent Olyra shells. It does **not** create an animation rig, controller, combat clips, detailed creature motion, optimized LODs or final art. PR #159's Animator bridge will work only once the model-production pipeline delivers a real animation-ready prefab.
- Meshes and textures are copied for **independence, not optimization**. Review texture settings may remain uncompressed, and current models have substantial triangle counts. Do not label a copied candidate mobile-ready merely because the dependency audit passes.
- The model-team pipeline retains ownership of Great Strider and Living Veil geometry, UVs, source textures, rigs and visual approval. The generated candidate's GUIDs are separate; source GUIDs and existing scene links are preserved.
- Subsequent staging always creates a new candidate revision and never overwrites a previous reviewed prefab. Staging does not edit save data, the HUD, movement rules, camera, existing campaigns, unit stats or the developer's current catalog mappings.
- Nothing is checked in to `ArtProduction/` until the created prefab plus its `.meta` and dependencies have been manually reviewed. Do not commit generated `ArtReview/` artifacts.

## Required verification before accepting a gameplay mapping

| Gate | Acceptance |
| --- | --- |
| Unity compile | Editor compiles in pinned Unity 6.3.21f1, menu enabled only for correct Project prefab |
| Source safety | Source meshes, import settings, textures, `.meta` GUIDs, review scenes and model scripts unchanged |
| Dependency isolation | `AssetDatabase.GetDependencies(candidate, true)` has **zero** paths under `ArtReview/` |
| Visual | Avarin partner, silhouette, grounding, model orientation and Living Veil's five-lobed canopy reviewed at tactical distance |
| Battle test | Exact UnitId mapping only; test spawn, selection, double-tap attack, movement, damage, defeat, reload and encounter cleanup |
| Fallback | Unmapped and non-approved units keep placeholders; Mission 3 and 9 rescue/extract remain playable; previous saves unchanged |
| Motion | Verify `Moving`/`Attack`/`Hit`/`Heal`/`Defeat` only where a real runtime Animator controller and clips exist |
| iPhone 14 | Profile actual frame times, memory, triangle/draw-call cost, transparency overlap and visual clarity in portrait |

**CI limit:** repository checks cover Core and structure, not Unity Editor compilation, actual candidate generation or mobile runtime. This is a handoff to the next genuinely playable model milestone, **not** a claim that the first final 3D unit has already been delivered.
