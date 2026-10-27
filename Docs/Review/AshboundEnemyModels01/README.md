# The Ninefold — World Five Ashbound enemy 3D studies 01

**Status:** editable 3D source candidate; **NOT approved final models**, not a canon addition, not registered for gameplay. This pass deliberately shifts the Chat workstream to the Ashbound enemy *models*, while the World Five bonded playable models remain with the parallel art workstream.

## Delivered now

- `SourceArt/Ashbound/RiftForager/RiftForager.obj` and `.mtl`: a bipedal Avarin Rift Forager study with living crescent mantle and visible temple-face; travel-worn asymmetric field gear, offset route satchel, lightweight sensor probe and unstable external Fracture Wake equipment. Role: quick ordinary pursuit/scavenger; first seen in Mission 11.
- `SourceArt/Ashbound/PartedStriker/PartedStriker.obj` and `.mtl`: separate bipedal Avarin Parted Striker study with recognizable species anatomy, heavier uneven salvage protection, a reinforced close-range forearm plate, and externally visible unstable Wake-stabilizer modules. Role: aggressive close-range pressure; first seen in Mission 11.
- Independently editable named mesh parts, provisional distinct material swatches, consistent meters / +Y up / +Z forward, and machine-readable model manifests. Both generated sources have about 39k triangles each (these are **unprofiled** review counts, not validated mobile budgets).
- Deterministic regeneration using `node BuildTools/Art/build_ashbound.mjs`. The dedicated GitHub workflow compares regenerated geometry with the tracked models.
- **Ninefold > Art > Ashbound Enemy Review** in the Unity Editor: build/open fingerprint-isolated nonplayable review scenes under `Assets/Ninefold/ArtReview/Ashbound*/` with the actual authored OBJ surfaces. Neutral materials/lighting and a ground stage make silhouette assessment possible without creating gameplay registrations.

## Locked continuity safeguards

Source: `Docs/Canon/WorldFiveAshboundCanonAddendum01.md` (approved 2026-10-22), reconciled with Master Lore v26.0 and the locked World Five visual master at final review.

**Do not alter:** Ashbound are hostile splinter Avarin, nonplayable and without bonded creatures; Avarin are upright bipeds with living crescent sensory mantles and temple-face features. The mantle senses environment/Arcana, never partner bonds. The Parting is betrayal, not a triumph; no creature corpses/trophies, soul-powered devices or new physiology are depicted. Fracture Wake is engineered unstable Arcana capture and the Afterecho has damaging sensory consequences.

**Proposed and subject to visual approval:** exact salvage shapes, material swatches, route probe, shoulder protection and external circuitry. No horned monster, conventional fantasy witch, helmet replacing the mantle, or firearm was designed. No additional named character, enemy Apex Unit, boss or playable character is introduced. Neither model modifies the six already canon-approved enemy archetype names.

## Unity review workflow

1. Open pinned Unity `6000.3.21f1` and verify URP foundation import.
2. Choose **Ninefold > Art > Ashbound Enemy Review** and select Rift Forager or Parted Striker.
3. Review the generated scene and actual 3D meshes. Confirm proportional Avarin anatomy, living mantle, temple-face visibility, gear asymmetry, appropriate non-glamorized ritual aftermath and distinct silhouette at portrait tactical camera distance.
4. Inspect `SourceArt/Ashbound/<model>/manifest.json` for mesh counts, orientation and current limitations.
5. Review against the **locked World Five visual master** before authorizing any production material, rigging, animation or runtime-presentation handoff.

## Not yet delivered

- These are source meshes, **not** accepted production sculpts. No authored anatomy rig, combat clips, real organic motion, UV texture maps, LODs, equipment deformation or native device certification exists yet. Do not claim these two units are visually final or fully animated.
- Model-independent combat kits and encounter actors already exist in `WorldFiveActOnePrototype`, `WorldFiveCombatMechanicsV1` and `WorldFiveEncounterVarietyV2`. We did not change mission balance, enemy AI, save profiles, unlocks or canon status.
- This is **not** a battle prefab handoff. Unity-generated `ArtReview` assets are deliberately excluded by the Battle Model Readiness Audit and must not be auto-registered. Exact gameplay IDs are stored in the manifests as references only, not authorizations to map the model.

## Next enemy-art sequence

After reviewing Forager/Striker, sculpt and rig both toward the approved visual reference; test moving, striking, hit response and Afterecho signals against #159's optional Animator bridge. Then tackle the four other ordinary Ashbound in sequence: **Seam Castigator**, **Anchor Wrecker**, **Afterecho Seer**, **Camp Witness**. Each needs an independently recognizable silhouette, equipment language and battle identity. Do not add a unique boss without approval.

## Acceptance criteria

1. GitHub source regeneration and repository CI pass.
2. Unity 6000.3.21f1 scene generation completes with no compile errors; re-opening an existing fingerprint scene does not overwrite user changes.
3. The two enemy silhouettes are distinct and still anatomically Avarin (including mantles and temple-faces).
4. One-to-one source asset ownership; zero creature companion meshes, zero gameplay catalog mutations.
5. Device measurements and full 360-degree/canon reference review **before** production promotion.

This first art pass is a reviewable **starting mesh**, not a declaration that its appearance already meets the locked visual master.
