# Great Strider Unity review — PT-044

This is an opt-in inspection scene for the existing **form study 02**, not a new
model, final art, battlefield replacement or canon revision. It follows the source
register and scale constraints in [FirstMissionArtBrief.md](FirstMissionArtBrief.md)
and the approved six-meter study described in the source-art README. No new lore
or appearance is approved by this tooling PR. The locked visual master still
controls refinement; the current model has not reached that quality gate.

## Open it later

1. Stop Play, pull the merged changes and wait for Unity 6000.3.21f1 to import.
2. Open **Ninefold > Playtest > Launcher**, then **Great Strider visual review**.
   **Ninefold > Art > Great Strider Review** opens the same window.
3. Click **Open / create review scene**. If the foundation is not configured,
   configure it through the existing launcher first. The normal save-scene prompt
   protects unsaved work; Cancel aborts opening.
4. Open Game view and choose **540 × 960**. No Play mode is required. Use the review
   window's orbit, elevation, distance and view presets; they update the scene
   camera and Scene view. Unity's native Scene view navigation works independently.
5. To return to battles, use the launcher's **Open / repair integration scene** or
   **START / RESUME PLAYTEST**. Existing battle/profile saves remain intact.

The generated scene is `Assets/Ninefold/ArtReview/GreatStriderReview.unity`.
Existing scenes are reopened without regeneration. Saved camera/material edits
are preserved; presets change the camera and mark the scene dirty. The window
starts Scene view at a standard inspection angle when opening the scene.
No automatic Play, build-scene registration, package installation or pipeline
setting changes occur. Generated mesh/material assets live alongside the scene;
source and reproducible setup code are in GitHub. These local generated assets
may be committed after a successful Unity review, including their metadata.

## What to inspect

- Full six-meter silhouette, all four feet, paired sensory sails and central cradle.
- Seated operator, sensory mantle and Waycaster survey-equipment readability.
- Front, side, back and close-up views against the locked master, especially hidden
  anatomy and proposed attachments that still need reconciliation.
- The **1.82 m measuring post**, clearly a scale aid rather than an Avarin model.
- Material separation and ground contact under a neutral studio key and fill.
  The key requests soft shadows; actual shadow quality depends on the configured
  URP/quality settings. Scene lighting must be enabled in Scene view.

The gray studio floor, lights and marker are presentation tools, not World Five
locations or canon props. No automatic rotation or animation suggests a rig exists.
The review UI is an Editor utility, not proposed player-facing game UI.

## Import scope and limits

The small importer accepts the generator's positive-index, triangle-only OBJ and
its diffuse MTL palette. It preserves coordinates, topology and meter scale,
recalculates normals, and groups geometry into 11 material submeshes. Source named
parts remain in the OBJ; the combined review mesh is not the sculpt/rig master.
It checks finite coordinates, index bounds, material assignment, the grounded
0–6 m envelope and expected study counts (26,298 vertices / 52,044 triangles).
A later source revision must intentionally update these checks; no silent scaling.

Materials are opaque URP Lit palette approximations. No UVs, textures, translucent
membrane shading, skin weights, animation, LODs or optimized gameplay prefab are
claimed. It is not added to the battle roster. Multiple editable source parts
intersect; neither deformation nor inter-part clearance has passed review.

## Validation

Repository metadata and reproducible source-geometry checks run locally and in CI.
Core rules run on Linux and Windows in CI. They do **not** compile the Editor
assembly or render the scene. Unity is unavailable in the implementation environment.
Native import, shader appearance, shadow behavior, camera framing and device
performance therefore remain unverified until the next Unity review.

At that review, confirm no red Console errors, open/reopen and Cancel behavior,
all camera presets in portrait, outward surfaces, complete silhouette, grounded
feet, material assignments and the measuring post. Return to the integration scene
and confirm the existing battle resumes. Capture front/side/back and cradle detail
for the next model refinement pass; this is not a final-art acceptance checklist.
