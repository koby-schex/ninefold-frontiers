# Model development and UI preview handoff

## Coordination ownership

The Work conversation owns 3D geometry, model materials/textures, animation,
prefabs and their associated art rendering/review tools. The UI conversation owns
Home, campaigns/missions, Collection, battle HUD, shared styles and navigation.

Both work from latest `main` on separate feature branches and PRs. Never develop
on `main`, force-push, or overwrite/remove the other conversation's work. Preserve
Unity metadata GUIDs. Check latest merged changes before PR creation and again
before recommending merge. Run repository and available automated checks.

Model PRs avoid `FrontiersPlaytest.cs`, shared UI styles and presentation scripts.
Explain any proposed overlap in preview interfaces or shared gameplay architecture
to Koby before implementing it. Canon and locked visual masters remain controlling.

The canopy pass incorporates `main` through PR #66. The UI preview-interface PR
was described as planned #67, but did not exist when checked. GitHub then assigned
#67 to this Olyra model pass. Identify the eventual UI PR by its actual title and
branch, not that planned number. No assumptions about its API are implemented.
Read its contract before runtime integration and document the agreed mapping here
without renaming gameplay IDs.

## Asset inventory

These are editor-generated review prefabs, not stable runtime registrations.
Fingerprint suffixes change when source changes; fresh copies may add a numeric
suffix. Do not hardcode these generated paths in Collection UI or gameplay.

| Model | Source | Generated review prefab |
| --- | --- | --- |
| Great Strider / Ilyth | `SourceArt/GreatStrider/GreatStrider.obj` | `Assets/Ninefold/ArtReview/GreatStrider11_<fingerprint>/GreatStriderStudy11.prefab` |
| Living Veil / Olyra — resting | `SourceArt/LivingVeil/LivingVeil.obj` | `Assets/Ninefold/ArtReview/LivingVeil02_<fingerprint>/LivingVeilStudy02.prefab` |
| Living Veil / Olyra — protective | `SourceArt/LivingVeil/LivingVeilProtective.obj` | `Assets/Ninefold/ArtReview/LivingVeilProtective02_<fingerprint>/LivingVeilProtectiveStudy02.prefab` |

Olyra's source manifest model identifier is `world05.living-veil.olyra`; pose keys
are `resting` and `protective`. This is an art identifier, NOT a gameplay UnitId.
A future mapping must use the existing roster and the UI preview interface's agreed API. Stable
production prefab paths/GUIDs and the Great Strider art ID will be assigned during
that integration. Keep source identifiers separate from display names and revisions.

## Rendering requirements to accommodate

- Current review assets use URP Lit and meters/Y-up/+Z-forward with scale 1.
- Prefab root is the model origin; scene cameras, lights, measuring posts and
  studio ground are separate review objects and must not enter a unit preview.
- Olyra includes both organism and Avarin partner as one presentation asset.
- Fit the complete Renderer bounds, including translucent lobes, mantle and
  partner. Olyra protective bounds are approximately (-2.732, 0, -2.730) to
  (2.831, 3.415, 2.633) m. Use bounds-based framing rather than humanoid assumptions.
- Use a dedicated preview layer/camera and a preview-owned RenderTexture with
  explicit lifetime cleanup when the final adapter is agreed. No Camera.main or
  active gameplay scene dependency should enter that adapter.
- Preserve source material assets. Preview-specific tint/selection effects should
  use per-instance overrides rather than mutate shared materials.
- Transparent membranes require alpha blending, ZWrite off, back-face culling,
  input alpha texture, and native overlap review. Do not flatten them into opaque
  sections. Avoid transparency-shadow assumptions; review membrane shadows are off.
- Olyra has no Animator, rig, clips or gameplay colliders yet. Support a static
  model fallback and do not wait for animation events from this asset.
- Current models are detail studies, not certified mobile budgets. LODs, draw-call
  and memory profiling, production packaging and native review precede integration.

The editor review tools currently use their own scenes and cameras. The future
runtime adapter must use the reusable UI interface, not invoke editor tools or
copy their scene-management code. No changes to that interface are made here.
