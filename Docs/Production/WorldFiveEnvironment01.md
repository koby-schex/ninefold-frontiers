# World Five Environment Production 01 — W5-ENV-01

## Goal and project boundaries

Ninefold: Frontiers is a **portrait-first tactics game**. The World Five (Avarin/Continuance) campaign is planned as the **launch tutorial campaign**, with many distinct short encounters and future campaigns for other worlds. The current `fixture-opening` and `fixture-finale` arenas are still abstract engineering content, not the finished commercial story or finalized named sites.

This pass builds the first **reusable World Five-inspired environment kit** on top of the PT-058 distinct authored map layouts and per-encounter scene studies. It introduces no lore-incompatible monuments, invented historical sites, fictional flora or false canon labels. All generated geology is an art study; approval against the locked World Five visual master remains mandatory before public release.

## Implemented

### Reusable fracture-mineral kit

`WorldFiveSceneryKit.cs` (and tracked Unity meta):

- Defines independent `W5-ENV-01` visual revision.
- Matches only the two existing **World Five-inspired** environment studies: `W5 study · pale survey corridor` and `W5 study · deep mineral fracture`. Every other scene returns `false` and retains its current non-World Five materials.
- Produces one small (256², RGBA32 with mipmaps) deterministic **violet-gray strata texture** at renderer initialization. Grain and mineral lines vary smoothly without random seed drift or external source photos. No global random state, download, content schema or Unity Inspector assignment is required.
- Exposes an authored style per study with separate facet counts, mineral palette and restrained reflective seam tint. Opening and finale are intentionally different.

### Noninteractive geometry and materials

`BattlefieldScenery.cs` uses the World Five style to:

- Apply the distinct surface texture to already existing *presentation-owned* terrain, cover crowns and mineral slabs only when the current scene is World Five-inspired; restore the original neutral study texture for other maps.
- Add batched faceted geology, distant fracture shelves, and subtle mineral seam faces **beyond the real authored far and side movement bounds**. These are cosmetic environment pieces; **no colliders, pathfinding cells, tactical targets, script components, gameplay obstacles, fake holes or navigable ledges**.
- Use different cluster densities (8 vs 15) between the opening and deeper mission studies, alongside the separate existing SceneDirection palettes, lighting and horizon variation.
- Create only three additional scenery mesh objects for these features per World Five field, with batching for the constituent facets. Each mesh is bounded to a few thousand vertices, not thousands of individual draw objects.
- Keep all mesh/material/texture resource ownership under `BattlefieldScenery` and explicitly destroy generated meshes, materials and World Five texture on disposal. Rebuilds do not accumulate geometry in the scene.
- Preserve the existing authored map boundaries and transparent battlefield UI overlays; no gameplay/scene-camera parameters are modified.

### Not included (intentional)

- Production 3D environment meshes, authored architectural features, foliage animations, character model materials, photogrammetry, shader pipelines, particle systems or finalized World Five geography.
- Actual 100+ mission launch campaign, named tactical units/abilities, story/tutorial cinematics, multi-world art selection, adaptive 3D camera or World Collision playable battles.
- Any mapping from abstract fixture IDs to canon unit models. Unit model assets/rigging/animations/preview renderers remain owned by the **other Work chat**.

## Why this PR is isolated

`ui/mobile-native-shell-and-battle-hud-01` (PR #73, PT-060) is an independent mobile app / battle HUD pass. This World Five environment branch starts from `main` and intentionally does **not** touch the central UI host, `PlaytestBattlefield.cs`, PlaytestRevision, PlaytestLauncher or any stylesheet.

The changes are restricted to:
- New `Assets/Ninefold/Runtime/Presentation/WorldFiveSceneryKit.cs` and its .meta.
- `Assets/Ninefold/Runtime/Presentation/BattlefieldScenery.cs`.
- This production note.

No file overlaps PR #73 and no file is inside the Work chat's model-generator/model-art directories. The source indicator remains **PT-060 after PR #73 merges**, and this independent subpass is identified by **W5-ENV-01**; it is not a second UI source milestone that could overwrite the other chat's revision.

## Required Unity performance and visual QA

1. Merge PR #73 first, then this PR. Stop Play, pull updated main into Unity 6000.3.21f1 and let it finish compilation. Verify no pink materials, texture import failures, duplicate actors or missing script references.
2. Resume any previously saved battle before attempting a new one; verify layout boundaries, obstacles, positions, turn order and objectives remain unchanged. All changes here are renderer-only and should leave replayable checkpoint bytes untouched.
3. Enter the World Five opening study. Observe more textured violet-gray mineral pavement, subtle outer reflections and a restrained faceted distant silhouette. Ensure objective and units remain much clearer than the scenery; scenery must NOT obstruct the battlefield camera.
4. Enter the extended World Five finale study. Compare darker/richer fracture colors and higher density of out-of-bounds formations against opening. They should not read as copies of the same location.
5. Start the neutral Quick Battle and faction-B fixture. Verify the World Five texture and faceted decorations are **absent** from these arenas.
6. Check the full authored arena at 540×960, 390×844 and 320×568 phone-sized Game tab frames. Make sure no outside geological piece appears to be a walkable path, obstructed unit, or fake cover and the battle radar remains correct.
7. Inspect Unity Profiler on PC and iPhone when available: draw calls, material instances, meshes, geometry counts, texture memory, and steady frame rate during movement/restart. A 256² mipmapped texture is inexpensive but actual performance is **not yet measured**. Investigate renderer settings/shaders if opaque accents appear excessive.
8. Re-enter an encounter several times, return Home, change scenes, reopen the app. Verify no duplicate scenery or rising leaked memory and `Dispose` destroys the owned World Five texture.
9. Take paired screenshots of opening versus finale with the same camera scale and capture the neutral fixture for comparison. Review against locked World Five master visuals before declaring any generated geology final canon.

## Future

World Five environment **production pass 02** should move beyond procedural facets to approved modular terrain meshes, native World Five ecology/architecture as defined in the master lore, richer atmospheric effects and real 3D material authoring. Mission-level environment identities should be authored per canonical region; no generic geometry should be indiscriminately reused across all nine worlds.

The client should NEVER display Small/Medium/Large or let players choose map size. World Collision remains an explicitly authored separate experience when larger squad capacities, AI, camera, performance and lore-ready war environments exist.
