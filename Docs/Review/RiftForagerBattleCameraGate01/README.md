# Rift Forager — Real Battle-Camera Quality Gate 01

**Status:** reviewer-side automated Unity screenshot capture and measurements; the Rift Forager remains unapproved. This is not a new procedural enemy geometry pass or an automatic model promotion.

## Problem this solves

PR #177 built the first actual skinned source-mesh Rift Forager review candidate with normal-mapped URP materials and skeletal animation. However, an attractive close-up review is *not* evidence that an enemy reads well in a vertical mobile tactical game. The **real battlefield camera** in `PlaytestBattlefield.Layout` is orthographic, tilted **58° downward**, with a minimum vertical orthographic half-size of **4 world units**, and it often zooms farther out when objectives/terrain need framing.

The user reported character models look like simple drawings. This new tool gives us comparable **actual rendered PNG evidence from the real skinned prefab** before deciding whether to spend more time rigging or to substantially rebuild its source topology.

## Unity instructions

1. Update local `main` and open the project in **Unity 6000.3.21f1**. Ensure no Console compilation errors.
2. If not already created after PR #177, use `Ninefold > Art > Build Rift Forager Skinned Benchmark`, then click **Create / open independently staged rigged 3D scene**. That creates a fingerprinted source-mesh prefab under `Assets/Ninefold/ArtReview/AshboundRiftForagerRig01_<hash>/RiftForagerRigStudy01.prefab`.
3. Use `Ninefold > Art > Review Rift Forager at Battle Camera Scale` or the corresponding Art review button in the Playtest Launcher. If there are multiple generated rigs, explicitly choose the most recent desired rig in **Rig review prefab**; it cannot accept an unrelated, approved gameplay, or unsafely scripted prefab.
4. Click **CAPTURE PORTRAIT ART REVIEW**. A disposable preview scene is used; the current Unity scene and saves are not touched. The tool uses the actual skinned mesh, source bones and textures, real Animator clip samples, and neutral lighting and floor.
5. Click **Reveal exported PNGs and measurements**. The output is a timestamped folder under your **local** Unity project `Library/NinefoldReviews/RiftForager/`, which remains outside version control. Open `review-report.txt` for exact renderer, vertex, triangle, 19-bone and eight-material counts, source prefab path, camera settings and file list.
6. Review and share the screenshots, especially `tactical-390x844-idle-front.png`, `tactical-390x844-attack-three-quarter.png`, `tactical-390x844-afterecho-side.png`, and `detail-390x844-idle-front.png`.

## What is actually captured

- Twelve **390×844** tactical frames: **Idle**, **Attack** and **Afterecho** at front, three-quarter, side and rear views, using the live battlefield's **58° tilt** and conservative minimum `orthographicSize=4` (the live camera may show the actor even smaller).
- Four **320×568** tactical frames: Idle and Attack at front and three-quarter, with the same battlefield camera projection.
- Four **390×844** close-up frames: Idle and Attack at front and three-quarter, from 18° with orthographicSize 1.26 to inspect face, matte materials, equipment and skin weights. These are **not** gameplay screenshots.
- One **390×844** flat-normal-map control frame: the exact same Idle pose and URP material assets but a zero `_BumpScale`, to assess how much tangent-space normal relief contributes and whether the visual issue is topology rather than surface maps.
- Total: **21 real Unity PNG frames plus a measurement report**. No AI-generated image can substitute for them.

**Important caveat:** all captures use a neutral stage and the currently built skinned *review candidate*, rather than a live played Mission 11. That isolates geometry/animation for visual comparison. Even the tactical 58° angles do not fully reproduce the mission's map zoom, clutter, environment lighting, or phone thermal conditions. These must be reviewed separately before registering a model in any battle.

## Strict acceptance / rejection criteria

| Gate | What to inspect | Reject when |
|---|---|---|
| Avarin species anatomy | Temple-face, bipedal body, living natural crescent sensory mantle against the **locked World Five art master** and Master Lore & World-Building Archive v26.0 | Looks like disconnected primitives, has a fabricated extra sensory organ, or deviates from the approved silhouette |
| Distinct Rift Forager profile | Slim scavenger posture, attached external route-scanning equipment and empty companion gear | Looks generic, misreads as a playable faction, spawns a bonded creature, or introduces inappropriate weapons |
| Articulation | Attack/Afterecho joints, head/mantle, ankle/foot orientation, forearm and shoulder gear | Floating equipment, mesh tears, hard bending, inverted normals, or foot sliding |
| Tactical readability | Front, side, rear at 390×844 and narrow 320×568, **58°** camera | Shape/role cannot be identified without reading its HUD name |
| Surface character | Albedo/normal comparison, skin vs textile/salvage, natural matte mantle | Color maps merely conceal weak anatomy; plastic shine, overly noisy or flat materials |
| Performance | iPhone 14 GPU and CPU profiling after later model acceptance | Unacceptable triangles, skinned draw calls, texture memory, framerate, heat, or loading overhead |

Passing the structural checks only proves the **capture tool exists**, not that any image looks good. Do not approve based on file generation alone.

## Integration safety

- The review tool **does not** register the candidate in `BattleUnitVisuals.asset` or modify any `FrontiersBattleVisualCatalog`, runtime entity, combat action, mission rule, saved-state format, unit IDs, Core tests, playable-unit art or Riftwalker.
- It uses `EditorSceneManager.NewPreviewScene`, and always closes that scene and restores render targets after capture. It does not replace the user's current open scene.
- All exports are to the local Unity `Library` directory, not `Assets`, so no unapproved image or generated prefab is silently committed.
- All captured images are from **actual Unity rendering** of the skinned asset, not concept artwork; keep the distinction clear when comparing them with official locked showcase illustrations.
- No new biological powers, weapons, bosses, creatures or enemy skills are inferred. Current Ashbound canon remains authoritative.

## What comes next

**Review actual exported PNGs first.** If they confirm the character still lacks believable anatomy or shape language, prioritize a true source-sculpt correction with better topology, weight painting, UV/PBR and natural movement in the single Rift Forager model, before applying the method to the other five. If the result is genuinely good and survives actual Mission 11/mobile profiling, propose an explicit separate, reviewable battle-model promotion—never automatic approval.
