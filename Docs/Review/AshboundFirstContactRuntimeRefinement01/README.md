# World Five — Ashbound First-Contact Runtime Refinement 01

**Status:** in-battle *visual study*, not lore-locked production art, not a final mesh, not a skinned rig. Applies to the Rift Forager and Parted Striker in Mission 11; all other actors and the separate playable model workstream are untouched.

## Why this work is different from prior model-study PRs

Earlier Ashbound source studies were static OBJ meshes in `SourceArt/Ashbound` and Editor-only review scenes. PR #171 introduced a real-time runtime fallback, but it still had sparse surface detail and separately animated body pieces. This PR substantially refines the **visible fallback actually instantiated by the Unity battlefield** when an approved prefab is absent; it does not assert that source-model reviews were somehow imported into play.

### Rift Forager

- Natural Avarin temple relief, brow/cheek contour and eye forms, visible paired living crescent environmental sensory mantle with narrow organic filaments.
- Narrower organic upper body, layered cloth and harness, flexible forearm wraps, articulated legs and anatomical feet.
- More legible asymmetric exterior route-scanning hardware: an offset shoulder capture relay, forward hooked **external** route reader, cable-like Wake conduits, a worn field satchel and trailing repair tag.
- Still an ordinary nonplayable Ashbound Avarin. The reading hardware is **not** a firearm, life-force mechanism, magical bone organ, companion, bonded creature, or new canon artifact.

### Parted Striker

- Same recognizable underlying Avarin anatomy and living crescent mantle; visibly heavier, right-dominant asymmetric silhouette.
- Broad layered right salvage shoulder, large close-quarters forearm housing and **blunt** contact ridge, heavier chest overlays, ballast, shin reinforcements and cloth wrapping.
- Same deliberately external engineered unstable **Fracture Wake** hardware; absence of bonded companion is conveyed with empty/abandoned gear, never animal remains or trophies.
- No new class, enemy ability, boss, Apex, equipment reward or lore revelation.

### Live in-game motion and asset cost

- Runtime parent pivots attach eyes/face, natural mantle/filaments, arms/forearm equipment, and legs/foot geometry to the part they actually belong to. Small torso pitch, head scanning, arm counterweight, alternating gait and lifting legs happen on the *visible mesh* without moving the simulation position.
- Quiet idle: low-amplitude breath, face scanning and subtle mantle motion. Hit reactions are triggered exclusively from Core battle event playback. During action playback, idle movement does not override attack poses.
- Surface detail is triangulated using the existing `WorldFiveBattleOrganics.Loft` function, **not** a cube/capsule or 2D overlay. Each part owns an actual curved mesh. Each moving torso/arm/leg merges co-material surfaces into a combined mesh to reduce draw calls. The living face/mantle stays distinct for readable sensory motion.
- Geometry, combined meshes and their source resources are temporary and disposed through the existing battle scene lifecycle. No new physics components, path blockers, AI, save data, material-shader dependencies, content catalog, game stat, targeting, or player model files.

## Canon and production gates

The controlling canon is Master Lore & World-Building Archive v26.0, locked World Five visual showcase and `Docs/Canon/WorldFiveAshboundCanonAddendum01.md`. The addendum establishes Avarin bipedal body, temple-face, **living crescent sensing mantle** (perceives environment/Arcana, not bonds), missing companions, external salvage/Fracture Wake equipment, and harmful Afterecho. **This PR's proposed sculpt forms, palette and utility gear are not new canon.**

Provisional visuals must **not** be shown as franchise release art, approved 3D characters, finished animation, or printable miniatures. Sculpted topology is still procedural, not artist-authored UV/PBR normal maps or a full low/high-poly skin-weighted production mesh. Part pivots are **not a true humanoid skeletal rig**. Real Unity screenshots, final shape review, bone weights/IK and phone performance remain mandatory.

## Native playtest and acceptance in Unity 6000.3.21f1

1. Merge only after GitHub checks are green. Reopen the project and inspect the Unity Console for compilation errors.
2. Choose the World Five **Act One / Encounter Variety** study and enter **Mission 11**. With no explicitly approved battle model mapped, both Ashbound should render as detailed 3D fallback bodies rather than cubes or earlier low-detail procedural forms.
3. Orbit the battlefield scene camera and inspect the Rift Forager's slim silhouette, readable face and paired organic crescent, separate wearable route gear and attached satchel.
4. Inspect the Parted Striker's much broader asymmetric right shoulder and forearm contact ridge. Confirm that its head and living mantle still read clearly. No loose floating weapons, extra limbs, companion creatures, metal body organs or bad scale.
5. Move and attack with both enemies during their turns; verify feet, limb coverings, external gear, face and mantle *move together*. Inspect idle scans/breathing; hit flinch must only happen for a genuine Core ability event.
6. Switch to Mission 12–14 and confirm other four archetypes retain their earlier identity/profile; switch to wildlife tutorial Missions 7/8 and confirm organic animals remain unchanged. Approved model prefabs still take precedence.
7. Verify **Mission 4's gold hold area** and mission help aren't affected; check tap once to inspect, double-tap to attack, move, rescue and saved-state resume.
8. Review 320×568 and 390×844 portrait Game views. Compare direct **before/after Unity render frames** to the locked World Five/Avarin visual master; reject inaccurate mantle, face, material, or proportions even if they look more detailed.
9. Profile GPU frame time/draw calls, triangles and textures on **iPhone 14** before promoting this study to a production prefab. Mesh batching reduces renderer count, but no native performance numbers are claimed.

## Validation

New `BuildTools/Art/validate_ashbound_first_contact_runtime.py` runs in repository structure CI. It checks detailed anatomical identity, differentiated equipment, moving part attachment, material batching, noninterference with approved prefabs, no gameplay physics, and battle-event-bound recoil. Existing Windows and Ubuntu Core tests are unchanged and must stay green.

**Next step after native visual evaluation:** rework geometry against the controlling exact Avarin visual master, then a real weighted skeleton with IK contact and authored motion clips; apply the established quality standard to the other four Ashbound. This PR should not be mistaken for completion of those later gates.
