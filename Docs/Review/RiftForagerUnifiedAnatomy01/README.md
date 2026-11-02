# Rift Forager — Connected Anatomy Reconstruction 01

**Status:** a REAL changes-to-geometry 3D reconstruction candidate in the isolated Unity ArtReview pipeline. **Not approved canon art, not production-ready sculpt, not a live battle prefab.**

## The user-reported failure and our changed approach

The Rift Forager and other Ashbound units still looked like models assembled from crude spheres, capsules and tubes. Adding extra attachments, increasing subdivisions or applying normal maps to those pieces did not solve the disconnected, childlike character appearance. The prior SourceArt model generator `BuildTools/Art/build_ashbound.mjs` literally creates the biped torso, arms, legs, head, hands and feet as **twelve separate lofts/ovals**, then layers props on top. The PR #177 rigged review and PR #178 camera capture tool operate on those same individual meshes.

This PR **replaces those twelve source anatomical sections**, rather than overlaying more gear or making another slideshow:

- `RiftForagerAnatomyReconstruction.cs` models the body as one smoothly blended three-dimensional anatomical volume: asymmetric shoulder transitions, unified ribs/abdomen/pelvis, connected arms and hands, knees/ankles, planted feet, neck and sculpted temple-face volume.
- An indexed marching-tetrahedra iso-surface creates one continuous set of triangle connections rather than separate primitive surfaces. It generates gradient normals, coherent preliminary UVs, and a restrained two-region material assignment: garment and natural skin.
- Separate original Avarin living **crescent sensory mantle**, natural eyes and facial relief, and existing external Forager field equipment survive: the source parts are retained by their exact names, never reinterpreted as new biology or magical powers.
- `AshboundSkeleton.SkinOrganic` computes normalized four-bone influences across continuous shoulder, hip, head and joint transitions. The previously rigidly delineated biped sections no longer each own a disconnected set of vertices.
- `AshboundRigReview` filters all original disconnected body triangles **and compacts remaining source vertices**, writes a new `RiftForagerContinuousAnatomy01.asset`, creates its own SkinnedMeshRenderer with both garment and skin materials and the same nineteen-joint rig, and preserves six existing animation clips.
- The rebuild script is included in the source-revision fingerprint. Updating its geometry cannot reuse an old disconnected generated prefab.
- The battle-camera quality checker from #178 now **rejects older nonreconstructed candidates** and accepts only a generated rig containing exactly one explicitly named continuous anatomical mesh.

This is a material change to base geometry, not an accessory detailing pass. It affects the **generated review model**, not the combat model currently used in missions.

## Existing model and canon protection

- Old source file `SourceArt/Ashbound/RiftForager/RiftForager.obj`, MTL, manifest and the original all-enemies generator remain intact. The manifest continues to state *provisional, unrigged and not mobile certified*.
- All six **runtime Ashbound** placeholder models in `WorldFiveBattleOrganics.cs` and the battle catalog remain untouched. No new enemy behaviors, hitboxes, physics, damage, saves, lore or companion creatures.
- Only the Rift Forager's **ArtReview** rig generator is changed; Parted Striker/Castigator/Wrecker/Seer/Witness and playable models, especially Riftwalker, remain outside this branch.
- Canon source: **Master Lore & World-Building Archive v26.0**, locked World Five Avarin visual showcase, and approved `Docs/Canon/WorldFiveAshboundCanonAddendum01.md`. The natural mantle senses **environment/Arcana, not bonds**. Fracture Wake remains unstable engineered equipment and Afterecho genuinely harmful.

## How to test once PR checks are green

This requires ordinary Unity art-review interaction, **not Unity AI**.

1. Pull `main` in GitHub Desktop and open the pinned **Unity 6000.3.21f1** project. Confirm the Console has no errors.
2. Select **Ninefold > Art > Build Rift Forager Skinned Benchmark**. Click *Create / open independently staged rigged 3D scene*. The revision fingerprint automatically creates a fresh model in `Assets/Ninefold/ArtReview/AshboundRiftForagerRig01_<hash>`.
3. Select the new **Reconstructed continuous Avarin anatomy** object. Verify it has a SkinnedMeshRenderer with two submeshes, 19 bound bones and that the original torso/arm/leg/head tube surfaces are **absent**, not merely hidden behind new parts.
4. Scrub Idle, Advance, Attack, Hit, Afterecho and Defeat. The head, face, arms, knees and torso should deform across connected surfaces. Reject obvious shoulder stretching, collapsed wrists/ankles, incorrect foot planting, broken mantle/eye attachment, incompatible gear positions or floaty movement.
5. Select **Ninefold > Art > Review Rift Forager at Battle Camera Scale** (also accessible inside the Playtest Launcher). Capture real portrait comparison views and share the **390×844 tactical contact sheet**, **320×568 narrow contact sheet**, and close-ups for scrutiny.
6. Compare **before and after** shots against the exact *locked Avarin* visual master and canon; this procedural meshing improvement does **not** confer lore approval, high-quality sculpture, or hand-painted surface realism.
7. Check Mission 11 still uses the existing stable battle fallback. An ArtReview asset is never allowed to be auto-registered as production.
8. Measure actual character triangle/vertex counts reported by the Unity screenshot tool, test weighted animation at portrait camera scale, and profile eventual battle candidates on **iPhone 14** before shipping or expanding to the five other enemies.

## Automated acceptance checks and remaining art gaps

- Four new real .NET tests: source identity filtering (no mantle/face/gear removal); indexed **closed/manifold topology** (every surface edge is shared by two triangles); finite normalized normals, plausible biped bounds and anatomy; continuous four-bone weights across actual generated vertices.
- Tests run in **Windows and Ubuntu Core CI**, not only via Python source assertions.
- `BuildTools/Art/validate_rift_forager_anatomy_reconstruction.py` verifies genuine source-geometry replacement, compacted sections, skinning, separate material regions, prefab isolation, and untouched gameplay/catalog. Older rig and battle-camera validators still run.
- The result is a **better unified anatomical base**, but its geometry is still algorithmic and remains an intermediate sculpt candidate. There is no claim of human-level muscle/cloth sculpting, production retopology, manual UV unwrap, hand-painted PBR textures, IK or facial animation. These remain high-priority **next passes after viewing the renders**.

**Quality gate:** if the user still sees childish or noncanonical proportions, reject the candidate and modify its actual head/shoulder/limb topology instead of increasing texture intensity or attaching more props. The next step is intentional professional silhouette/sculpt correction, not automatic promotion.
