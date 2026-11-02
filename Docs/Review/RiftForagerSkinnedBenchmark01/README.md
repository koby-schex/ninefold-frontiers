# Ninefold: Frontiers — Rift Forager Skinned Benchmark 01

**Purpose:** establish the first genuinely skinned, texturable Ashbound enemy 3D asset pipeline, with visual and performance **rejection gates**. This is a reviewer-facing Unity asset build, not a canon approval, a finished sculpt, or a release-ready mobile battle model.

## Why the approach changes here

PRs #171, #173, #174 and #175 created increasingly complex **procedural runtime fallback geometry** for the six ordinary Ashbound. The player correctly observed that more separate shapes alone do not solve stiff, basic character art. Those meshes use independently rotated parts—not a production character rig—and could still look like stick figures in the actual battle. Adding more detail to the same primitives is not the right next milestone.

We already have **actual source 3D geometry** for a Rift Forager at `SourceArt/Ashbound/RiftForager/`. Its manifest intentionally says **unrigged, unapproved, not mobile-certified**; this PR does NOT rewrite that claim. Instead, it gives us a new review pipeline that creates a weighted Unity skinned mesh, playable skeletal animation states, and real UV-linked base/normal textures **separate from the existing runtime fallback**. This is an objective first step toward a production model, not a claim that art direction or final sculpting has been achieved.

## Actual artifacts created by Unity, not just preview descriptions

Open **Unity 6000.3.21f1**, then choose:

`Ninefold > Art > Build Rift Forager Skinned Benchmark`

Click **Create / open independently staged rigged 3D scene**. The tool reads the real 3D Rift Forager OBJ/MTL/manifest and generates a source-revision-fingerprinted folder:

`Assets/Ninefold/ArtReview/AshboundRiftForagerRig01_<hash>/`

It saves, in that folder:

- **Real `SkinnedMeshRenderer` prefab** with the actual source triangles, calculated normals and tangents, correctly bound bone matrices, and normalized two-bone interpolation from body-part names and original vertex heights.
- **Nineteen named joint transforms** for the body, left/right limbs, face and *natural living crescent environmental sensory mantle*. External field equipment is bound to arms, torso or pelvis, not invented new anatomy.
- Six physical **`.anim` clips**: Idle, Advance, Attack, Hit, Afterecho and Defeat. A Unity **Animator Controller** drives those curves. Attacks have distinct forearm reach, walking counter-swing, torso and head responses; Afterecho has restrained adverse sensory deformation; Defeat settles terminally.
- **Eight URP Lit material assets** with actual imported `_BaseMap` albedo PNGs and tangent-space `_BumpMap` normal PNGs. Source UVs are retained with a fallback projection for unused zero UV corners. Materials have restrained, separate skin, mantle, textile, salvage, hardware, seam, dark and eye surface character. These textures are **algorithmic lookdev studies**, not handcrafted release textures or final canon colors.
- A neutral **Unity review scene** with the skinned model, physical ground, light and orbitable Scene view. The window lets you change model clip, play/pause, scrub pose and reset.

The generated review artifacts are **not committed to Git** automatically and do not get loaded by campaign battles. Review within the Unity Project pane, commit only explicitly approved assets later, and do not stage/publish unapproved ArtReview assets as battle models.

## Canon and protected boundaries

- The approved World Five Ashbound canon addendum and **Master Lore & World-Building Archive v26.0** remain controlling; the locked Avarin visual master is mandatory. Ashbound are nonplayable cult members and retain ordinary **Avarin** anatomy.
- The crescent mantle is living **environmental/Arcana sensing anatomy**, not biological creature-bond hardware or armor. Engineered **Fracture Wake** is an external unstable Arcana capture mechanism; its Afterecho is harmful sensory distortion.
- There is no creature companion, creature trophy, "stolen soul", extra limb, firearms, newly approved faction ability, new named boss or Apex Unit.
- No live UnitId mapping, `BattleUnitVisuals.asset` write, AI, game stat, action input, campaign objective, save revision, enemy combat timing or playable model (including Riftwalker) is changed.
- The experimental predecessor `feature/ashbound-first-contact-skeletal-rigs-167` is **not merged**. This feature is cleanly rebuilt on current `main` for **Rift Forager only**.

## What to test in Unity (must pass before any model promotion)

1. On a clean checkout with the pinned Unity version, ensure the Console has no errors. Create the rig review via the menu above; the tool must generate the skinned prefab, six clips, review controller and base/normal PNGs in `ArtReview`.
2. Select **Idle** then scrub through **Advance, Attack, Hit, Afterecho and Defeat**. Ensure all surfaces deform with joints, left and right limbs have opposing gait, the source face/mantle is continuous, eyes/forearm gear don't float, and the model settles instead of popping back to idle on defeat.
3. Orbit the Scene view at front, left/right side, three-quarter and back. Zoom to mobile battle-camera size. Look for bad weights, bent elbows/knees, visibly thin tubes, clipped equipment, excessive normal-map noise, rigid cloth and unnatural foot movement. **Reject rather than silently approve** any of those failures.
4. Compare source anatomy, temple-face and living sensory mantle to the **locked World Five Avarin visual master**, not just the provisional procedural studies. If it is not anatomically accurate, the actual *source sculpt* must be reworked before any release candidacy.
5. Inspect material assets in Unity: 8 distinct URP materials, each with imported albedo and normal textures, tangents, modest metallicity/smoothness, and no metallic painted skin or randomly colored biological panels.
6. Confirm the gameplay fallback in **World Five Mission 11** still works and looks unchanged in the live game. **This PR does not silently promote this review prefab into battle**. The other five Ashbound models, native wildlife and playable units must be unaffected.
7. Profile the candidate as an actual skinned mesh at **390×844 and 320×568** portrait Game view sizes; record triangles, skinned renderer and bone counts, GPU/CPU frame cost, memory, clipping and blending on the **iPhone 14**. A full six-member squad/mixed-enemy stress scenario must fit the frame budget before promotion. No phone timings are provided by this PR.
8. Only once modeling, rig motion, materials, canon consistency, camera readability, and on-device performance are explicitly approved should we make a **separate** PR to register a production-prefab combat visual for the exact Rift Forager technical UnitId.

## Executable checks

`AshboundSkeletonTests` is wired into the existing Windows/Ubuntu .NET Core suites. It exercises the 19-joint connected hierarchy, original SourceArt vertex-to-bone mapping, normalized weights, finite animation states, gait/attack/mantle differences and continuous clip endpoints. The new `BuildTools/Art/validate_rift_forager_skinned_review.py` verifies actual Unity skinned prefab and clip construction, UV/tangent/normal-map flow, and ArtReview isolation. These are source/runtime-math checks. **Neither compiles Unity-only editor code nor verifies how good the model looks.**

## What comes next

**Do not call this source mesh production quality yet.** The next decision is about visible quality: after examining this genuine rigged scene, fix the **underlying silhouette and topology** in a professional sculpting application (or through an approved production artist workflow), retopologize for deformation, hand-author proper UV/PBR textures, add anatomical secondary movement and reliable foot planting, then compare mobile performance. Use the accepted Forager as a *measured quality benchmark* for the other five enemies, not another mass-generated procedural template.
