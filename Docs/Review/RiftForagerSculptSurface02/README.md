# Rift Forager — Connected Sculpt Surface 02 / UV Quality

**Status:** proposed changes to the actual *generated* skinned Rift Forager ArtReview mesh. NOT approved character art or a live-battle replacement.

## The problem

The first reconstruction (PR #179) physically replaced detached body tubes with one sealed continuous body. That solved a genuine structural issue, but the resulting anatomical shape still needed more intentional planes at important joints and facial features. Additionally, all geometry vertices retained a single cylindrical U coordinate; triangles across the back-of-body 0/1 wrap interpolated across almost an entire texture map, creating conspicuous unnatural material stretching and incorrect tangent-space normal relief.

A **continuous mesh alone is not a finished sculpt**. This pass improves the *underlying body surface* and corrects the render UVs without creating a new pile of tubes.

## Actual updates

**Integrated anatomy — same connected surface:** Added broad, smooth low-frequency anatomical transitions at the left/right cheeks/temples, shoulders/clavicular region, upper back/scapula, forearms, knees, and forward feet. All modifications are integrated into `RiftForagerAnatomyReconstruction.Field` **before** iso-surface extraction: there are no new accessory GameObjects, separate appendages, hero-style new organs, weapons, or lore additions. Lower-amplitude sculptural tension better follows waist, upper back and neck, rather than using arbitrary high-frequency noise. Original living crescent mantle, eyes, temple relief and external field salvage are still separate exact-named source elements.

**Material continuity:** `MakeTextureReady` duplicates render vertices only where the original cylindrical UVs cross U=0/1 and adds +1 to the repeated U coordinate. Triangle positions, normals, skinning and material assignment are unchanged. The original `Generate()` remains a **watertight indexed manifold**; the seam-ready renderer uses deliberately coincident UV seam vertices, as normal production render UV unwrapping requires. Its tangents are recalculated in Unity. Both garment and skin materials remain on the unified body, and all original eight PBR study materials remain in use.

**Real build integration:** The `AshboundRigReview` build now creates the new sculpt and UV-seam-ready render mesh, binds every vertex with the existing four-influence anatomical skinning and six animation clips, and uses the existing source fingerprint to generate a fresh `ArtReview` prefab. The unfinished model remains outside Mission 11 and the approved visual catalog.

## What to do after merge

1. Pull latest `main` and open the project with **Unity 6000.3.21f1** (Unity AI interaction is not needed).
2. Choose **Ninefold > Art > Build Rift Forager Skinned Benchmark** to generate a *new fingerprinted* ArtReview version. Confirm no compile errors and that the candidate has one **Reconstructed continuous Avarin anatomy** SkinnedMeshRenderer alongside its existing face/gear/mantle details.
3. Open **Ninefold > Art > Review Rift Forager at Battle Camera Scale** (also available in the Playtest Launcher), produce true **390×844** tactical and **320×568** narrow contact sheets, and compare with screenshots from prior iterations. Inspect the forehead/cheeks, shoulder/collar transition, forearm and knee bends, foot profile, and back texture seam specifically.
4. Test six clips for actual deformation tears or surface normals; verify that the natural paired living crescent mantle stays anatomical and environmental, not a mechanical or bond sensing device.
5. Compare against the **locked World Five Avarin visual showcase**, Master Lore & World-Building Archive v26.0, and approved Ashbound addendum. Reject the candidate if proportions are inaccurate or it still resembles simplistic generic sculpt art. These changes are **not** a claim of canon approval.
6. For any eventual mobile promotion, profile actual draw calls, triangle counts, UV artifacts, skinning cost and FPS/memory on **iPhone 14**. The current PR contains **no device profiling claims**.

## Validation

- Existing manifold, finite-position/normal, anatomy bounding box and skinning tests continue to run. 
- Added actual C# generator tests verifying the integrated anatomical landmark centers, UV seam splits without new triangle loss, no cross-wrap triangle interpolation, identical duplicate rest positions/normals, and local continuity of normalized four-bone joint weights.
- `BuildTools/Art/validate_rift_forager_sculpt_surface.py` verifies these are geometric surface edits and actual ArtReview pipeline changes with unchanged gameplay catalog and source manifest.

## What still remains

Even if the generated model is visibly better, **it is still a procedural sculpt study**. A fully believable, franchise-grade Avarin requires intentional head/body proportions approved against canonical character references, manual art-direction sculpt iteration, hand-authored UV/PBR painting, deformable clothing, proper skinning and contacts/foot IK, LODs, and real mobile gameplay lighting tests. **Do not apply this build automatically to the other five Ashbound.** Their original role and anatomy must be validated independently.

Next priority is a real side-by-side rendered comparison, then targeted redesign of the weakest proportions or a switch to a dedicated professional sculpt/retopology asset, not additional primitive decoration.
