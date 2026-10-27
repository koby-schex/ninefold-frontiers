# The Ninefold: Frontiers — Ashbound First Contact Motion 01

**Review-only actual geometry motion** for the first post-tutorial enemies, the Rift Forager and Parted Striker. This changes no combat rules, actor positions, missions, catalogs, enemy ability damage, saves, or playable creature model files.

## Why this work exists

The first six Ashbound studies are meaningful separate OBJ meshes, but they were static. Two of them (Forager and Striker) appear at the first Ashbound contact in **Mission 11**. Their sense of motion and weight matters, and an Avarin face/mantle must remain recognizable through movement. This PR adds a **native Unity Editor motion viewport driven by the true source mesh vertices**, not a concept still, floating screenshot, rigid one-piece sprite or a claimed finished animation rig.

## Unity review

1. Open the project using pinned **Unity 6000.3.21f1** with the existing URP setup.
2. Choose **Ninefold > Art > Ashbound First Contact Motion Review**.
3. Select **Rift Forager** or **Parted Striker**; click **Load genuine source mesh**. All source is loaded directly from `SourceArt/Ashbound/<model>/` into private transient memory.
4. Select one of six clips: **Idle, Advance, Attack, Hit, Afterecho, Defeat**. Use **Play/Pause**, precise scrub slider, reset, orbit/front/side/tactical cameras and zoom.
5. Observe six pose studies on actual, independently-deformed source vertices: opposed biped leg movement; planted/raised sole contact; restrained shoulder/arm balance; different attack reach; responsive natural sensory mantle and hazardous strain; non-glamorized defeat.
6. Close the window to discard the in-memory meshes. It does **not** write to or overwrite current Unity scene, source OBJ, prefab, Unity Animator controllers, user saves or `Assets/Ninefold/ArtReview`.

## Real implementation, strict limits

- `Assets/Ninefold/Editor/AshboundMotion.cs` contains Unity-free deterministic motion math. It reads source-part names and original rest-coordinate vertices, and evaluates the position independently each frame. It never feeds a deformed frame back as the next rest pose. It is tested on Windows and Linux Core runners.
- `Assets/Ninefold/Editor/AshboundMotionReview.cs` is a separate Unity `PreviewRenderUtility` window. It clones the source geometry in memory, continually updates mesh vertices/normals/bounds, and renders the actual posed mesh. It does not instantiate an actual battle unit or drive turns.
- **This is not skinning or a real production skeletal rig.** Motion is scripted per-body-region geometry deformation and does not have authored bone weights, skeletal constraints, IK foot locking in a moving world-space root, secondary-cloth simulation, finished combat animation events or rig export. The walk is a stationary in-place study with deliberate opposed leg contact.
- **Clip names are editor review names**, not gameplay registration. Combat Animator events from earlier PR #159 remain separate and do not magically gain clips from this PR.
- Placeholder material swatches and inherited study geometry remain unapproved. Existing lore-locked Avarin anatomy and crescent sensory mantle must be compared with the reference master; Fracture Wake is engineered unstable Arcana capture, with harmful Afterecho. No dead companion/trophies, creature summons, bonus Apex units or new cult lore.

## Regression coverage

Five tests under `Tests/Ninefold.Core.Tests/AshboundMotionTests.cs` execute the actual motion class in the Core test runner, verifying:
- six supported motion states, finite outputs, invalid input failure and positive durations;
- alternating locomotion leg movement and a maintained nominal contact-floor envelope;
- distinct attack reach, impact recoil, sensory strain and nontrivial final defeat pose;
- exact rest-pose time zero, cyclic endpoints for nonterminal motion, continuous sampled movement and nonaccumulation;
- real Rift Forager and Parted Striker OBJ meshes still contain both feet, both arms, a natural living crescent mantle and temple-face, with preserved source-part assignment for vertex-level deformation.

These tests do **not** substitute for Unity's native compilation/visual review or phone-device benchmarking. CI checks the source motion math; Unity Editor code still requires a pinned Unity compile.

## Next practical production tasks

1. Compare each native review animation at portrait battle camera scale to the locked World Five visual master, note anatomical intersections and revise shapes/weighting.
2. Build an **actual** reusable Avarin bone/skin-weight rig, add proper foot planting, natural torso/temple/mantle deformation, and author asset-backed animator clips.
3. Polish bespoke materials, textures and details for all six; create efficient LODs.
4. On explicit approval, stage proper self-contained battle prefabs and map exact World Five enemy UnitIds. Verify attack timing with true Core hit events. Profile on iPhone 14 before declaring anything production-ready.

This pull request is the **first motion-quality step** after finishing six static source studies. It does not falsely claim that all six are lifelike finished enemies already.
