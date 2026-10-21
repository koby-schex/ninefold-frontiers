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

The motion pass starts from merged PRs #67 (Olyra canopy) and #68 (UI Visual
Interface Foundation 02). The preview contract is now verified in
`Assets/Ninefold/Runtime/Presentation/FrontiersVisualPrimitives.cs`:
`IFrontiersPreviewSource.GetUnitPreview(string unitId)` and
`GetCampaignPreview(string campaignId)` return a `Texture`. A separate provider
registers through `FrontiersPreviewRegistry.Source` and owns/cleans up its camera,
RenderTexture and model instances. UI reads it on the next normal screen refresh.
No provider registration, ID mapping or UI change is implemented by this motion PR.

## Asset inventory

These are editor-generated review prefabs, not stable runtime registrations.
Fingerprint suffixes change when source changes; fresh copies may add a numeric
suffix. Do not hardcode these generated paths in Collection UI or gameplay.

| Model | Source | Generated review prefab |
| --- | --- | --- |
| Great Strider / Ilyth | `SourceArt/GreatStrider/GreatStrider.obj` | `Assets/Ninefold/ArtReview/GreatStrider11_<fingerprint>/GreatStriderStudy11.prefab` |
| Living Veil / Olyra — resting | `SourceArt/LivingVeil/LivingVeil.obj` | `Assets/Ninefold/ArtReview/LivingVeil05_<fingerprint>/LivingVeilStudy05.prefab` |
| Living Veil / Olyra — protective | `SourceArt/LivingVeil/LivingVeilProtective.obj` | `Assets/Ninefold/ArtReview/LivingVeilProtective05_<fingerprint>/LivingVeilProtectiveStudy05.prefab` |

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
  (2.831, 3.432, 2.633) m. Use bounds-based framing rather than humanoid assumptions.
- Use a dedicated preview layer/camera and a preview-owned RenderTexture with
  explicit lifetime cleanup when the final adapter is agreed. No Camera.main or
  active gameplay scene dependency should enter that adapter.
- Preserve source material assets. Preview-specific tint/selection effects should
  use per-instance overrides rather than mutate shared materials.
- Transparent membranes require alpha blending, ZWrite off, back-face culling,
  input alpha texture, and native overlap review. Do not flatten them into opaque
  sections. Avoid transparency-shadow assumptions; review membrane shadows are off.
- Olyra has an editor-only procedural motion study, but no runtime Animator,
  production rig, AnimationClip assets or gameplay colliders. Support a static
  model fallback and do not wait for animation events from this asset.
- Current models are detail studies, not certified mobile budgets. LODs, draw-call
  and memory profiling, production packaging and native review precede integration.

The editor review tools currently use their own scenes and cameras. The future
runtime adapter must use the reusable UI interface, not invoke editor tools or
copy their scene-management code. No changes to that interface are made here.

Surface study 04 preserves all existing model identifiers and topology counts.
It is based on main through PR #80 and changes only model generation, art review,
source assets and this documentation. No preview provider, UI or gameplay
registration is added. The source membrane texture is now 512 × 512 RGBA.

Partner study 05 updates the standing Avarin only. The pair now has 122 parts,
44,848 source vertices and 89,208 triangles per pose. Existing art IDs, source
pose keys and GUIDs remain unchanged. No runtime prefab registration is implied.
New review assets use revision-05 folders; previous review copies are retained.
This pass starts from main through merged PR #93, keeping the other chat's
rescue/extraction gameplay intact. Production LODs and device profiling remain
necessary before battle integration.


## Living Veil surface study 06

Partner materials now include six 128 px albedo/normal/metallic-smoothness map sets.
See [LivingVeilMaterialsStudy.md](LivingVeilMaterialsStudy.md) for import settings,
validation limits and current generated prefab paths. Current review outputs are
`LivingVeil06_<fingerprint>/LivingVeilStudy06.prefab` and
`LivingVeilProtective06_<fingerprint>/LivingVeilProtectiveStudy06.prefab` under
`Assets/Ninefold/ArtReview/`. The scene name is `LivingVeilReview06.unity`.
Previous 05 paths above are historical. Source ID, geometry and preview contract
are unchanged. No runtime provider or authoritative gameplay-ID mapping is added.
Native material acceptance and mobile budgets remain outstanding.
