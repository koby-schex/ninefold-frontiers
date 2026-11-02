# Rift Forager — Automated Geometry Evidence 01

**Status:** real static model comparison images generated automatically by GitHub Actions. This does **not** assert that the model looks professional, matches canon, plays correctly in Unity, or is approved for the live game.

## Why this PR exists

PRs #179–180 rewrote the Rift Forager's original twelve disjoint cylindrical/oval body parts into one connected anatomical field, then improved the natural shape and corrected UV seams. CI checked mathematical topology and skinning continuity but never produced an image of **that reconstructed version**. The earlier Ashbound review workflow only rendered the original, unchanged static OBJ, so the review images lagged behind actual C# model development.

To break the cycle of adding procedural details without seeing the result, this PR makes the current **exact C# anatomy** visible in the GitHub workflow, with no Unity AI interaction or code writing by the user.

## What the CI job actually does

The dedicated job is named **Rift Forager connected anatomy comparison**, in the existing **Ashbound model source checks** workflow.

1. Compiles `Assets/Ninefold/Editor/RiftForagerAnatomyReconstruction.cs` directly in a tiny independent **.NET 8** export project. This is the same source generator executed by Unity's `AshboundRigReview`: no rewritten Python geometry approximation.
2. Calls the real `Generate()` and `MakeTextureReady()`, exporting its skinned-rest **connected-body geometry, UVs, normals and material assignments** to an OBJ file. The exporter does *not* bake animation or skinning, so never call the file a rigged gameplay prefab.
3. Reads the original checked-in `SourceArt/Ashbound/RiftForager/RiftForager.obj`. It **removes** the twelve separately assembled torso/neck/head/arm/leg/foot source parts and **preserves** all original face details, living crescent sensory mantle, eyes, garment and nonbiological salvaged field equipment. The new single-piece body is inserted as a static mesh at the same real-world coordinates.
4. Saves a combined `RiftForager-AfterComposite-REVIEW.obj` plus the raw `RiftForager-ConnectedBody-REVIEW.obj`, and the original MTL swatches for optional external inspection.
5. Creates **genuine geometry-derived raster comparisons**: the original source asset and reconstructed composite rendered with **identical** camera, ambient material swatches, neutral background and scale, in front/three-quarter/side/back views.
6. Uploads PNG comparisons, OBJ files and a **`comparison-metrics.json`** geometry report as a GitHub Actions downloadable artifact; these are NOT committed to the game's runtime catalog.

## Output

Find the pull request's latest GitHub Actions run named **Ashbound model source checks**. Open the job **Rift Forager connected anatomy comparison**, then scroll to the run's *Artifacts* section. Download:

**`rift-forager-connected-anatomy-before-after`**

The ZIP contains:

| File | Purpose |
|---|---|
| `RiftForager-BeforeAfter-Tactical-390x844.png` | Side-by-side original vs connected anatomy at 58° down/orthographic half-height 4, matching the game's static camera projection and portrait dimensions |
| `RiftForager-BeforeAfter-Close-390x844.png` | Side-by-side closer 18° comparison exposing anatomical proportions and detail problems |
| `RiftForager-BeforeAfter-Narrow-320x568.png` | Two-angle narrow portrait static silhouette assessment |
| `RiftForager-ConnectedBody-REVIEW.obj` | Exact newly generated skinned-rest body geometry with UVs/normals; unanimated |
| `RiftForager-AfterComposite-REVIEW.obj` | One-piece body and unchanged source accessory faces in a single static OBJ |
| `RiftForager.mtl` | Existing eight provisional source color swatches, not approved final PBR materials |
| `comparison-metrics.json` | Triangle and vertex counts before/after, displaced body-part names, preserved accessory count, source/reference caveats |

All sheets show **BEFORE** and **AFTER** directly beside each other for each angle. They come from real geometry, not an AI concept illustration or invented model.

## Honest limits and next decisions

- **These are NOT Unity renders or playtest screenshots.** They use a CPU triangle painter with approximate source material colors, no proper URP rendering, no actual bone animation/skin deformation, no normal-map relief, no collision, no lighting parity with World Five, and no proof of phone performance. The 58° perspective and orthographic framing match the static camera parameters for **shape comparison only**.
- The original and new model are deliberately on the same scale; the reviewer should compare temple face, head/body proportions, shoulder and hip transitions, joint size, planted feet and the natural crescent mantle, then reject any shape that still looks amateurish.
- The controller for all changes remains **Master Lore & World-Building Archive v26.0**, the locked World Five Avarin visual master, and approved `Docs/Canon/WorldFiveAshboundCanonAddendum01.md`. This PR does not invent biology, companion creatures, roles, powers, bosses or new lore.
- **Neither source file nor live gameplay files are changed.** No `BattleModelVisualCatalog`, approved prefab registration, physics/AI, story, stat values, attacks, HUD, mission logic, Riftwalker, or the other five Ashbound models are modified. Unity AI is never accessed.
- A new image cannot certify quality. The goal is to obtain measurable visible evidence **before doing any more sculpt work**, so the next sculpt PR can fix specific features, rather than adding speculative accessories.

## Checks

The normal Windows and Ubuntu Core tests continue as before, including manifold topology/UV/skin tests. The repository structure job now checks the generator-to-review workflow. The dedicated job then verifies both OBJ assets, three correctly sized nonidentical PNG sheets, accessory preservation, triangle counts, and explicit review-only caveats.

**After this PR:** use the automatic geometry previews to identify the major silhouette and anatomy deficiencies of the Rift Forager. Once the static proportions are accepted, use the existing Unity review builder to verify deformation and materials and perform iPhone 14 performance tests before considering *any* explicit battle-model promotion.
