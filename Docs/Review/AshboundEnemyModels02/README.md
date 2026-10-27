# World Five — Ashbound enemy 3D models, Batch 02

**Status: provisional geometry and material review; no canon/production approval.**
Adds the next two of six locked anonymous, nonplayable Ashbound archetypes. Does not affect the playable 3D-model workstream, mission stats/AI, UI, saved campaigns, or the battle visual catalog.

## New source studies

### Seam Castigator — `ashbound-seam-castigator`
- Lean, recognizable Avarin biped with visible temple-face and a **living crescent sensory mantle**.
- Ranged disruption equipment language: offset shoulder shawl, rear capture spool, open external field lattice at the right forearm, trailing conduit wiring, and balancing counterweight. The field lattice is **not a rifle** and produces no gameplay effect from art alone.
- Different tactical silhouette and gear density from the light Rift Forager and close-range Parted Striker. Details remain unapproved physical proposals.

### Anchor Wrecker — `ashbound-anchor-wrecker`
- Broad, weight-bearing Avarin salvage protection: large uneven shoulders, layered load ribs, reinforced leg bracing and compact ballast cassettes.
- A blunt contact pad and forearm bracing read as **route-anchor disruption tools** in a slower, close-pressure silhouette. Not a firearm, robotic replacement for the Avarin, or a new species.
- Engineered unstable hardware is depicted as equipment rather than life-force extraction.

### Shared constraints
Both remain anatomically Avarin, approximately 1.82m from ground to natural head/mantle top; an adult baseline review dimension, **not** a final individual character measurement. The Parting is neither celebrated nor shown; no dead bonded animals, companion units, trophy remains, biological bond receptors, supernatural soul devices or hidden route-crisis revelations are introduced. Ashbound's approved definition, **Fracture Wake** mechanism and harmful **Afterecho** remain unchanged under `Docs/Canon/WorldFiveAshboundCanonAddendum01.md`, Master Lore v26.0, and the locked visual showcase.

The provisional palette, salvaged panel construction, externally housed conduits, and field gear are merely modeling proposals. Full locked-showcase comparison is required before artistic acceptance.

## Reproducible source assets

- `SourceArt/Ashbound/SeamCastigator/SeamCastigator.obj` / `.mtl` / `manifest.json`.
- `SourceArt/Ashbound/AnchorWrecker/AnchorWrecker.obj` / `.mtl` / `manifest.json`.
- Both are deterministic outputs of `node BuildTools/Art/build_ashbound.mjs`, which still regenerates the existing Rift Forager/Parted Striker from their unchanged authored profiles.
- `node BuildTools/Art/validate_ashbound.mjs` checks all four for actual nondegenerate source triangles, index validity, bounds, individual mesh identities, material references and manifest agreement. Passing is **not** a mobile or canon certification.
- `python BuildTools/Art/render_ashbound.py` generates four real-mesh views each (front/three-quarter/right/back). GitHub Action **Ashbound model source checks** publishes all four PNG review sheets as a single `ashbound-enemy-mesh-study-02` artifact for 30 days.

## Unity Editor: direct 3D visual inspection

In Unity **6000.3.21f1**, open **Ninefold > Art > Ashbound Enemy Review** and choose either of the two new enemy models. This creates a fingerprint-stable `Assets/Ninefold/ArtReview/Ashbound<model>01_<fingerprint>/` prefab and a neutral-lit scene, the same workflow as the two earlier enemies. Inspect face/temple details, mantle visibility, material contrast, native height, equipment intersections and visual readability in portrait. Do not map these throwaway review prefabs into battle. The actual source and existing scenes are never edited.

## Current limitations and next gates

- Source is **static**; no production sculpt, UV maps, skinned deforming rig, idle/travel/attack/hit/defeat animations, LODs or final surface polish.
- Counts are high for an unoptimized device asset; the actual iPhone 14 GPU, transparency/draw-call budget, shader cost, texture memory and clipping are **not** tested.
- The completed design direction must first be compared with the controlling Avarin visual master and revised, **then** rigged, animated, and separately staged as approved runtime prefabs using the #158–161 battle handoff.
- No gameplay mapping is made even though exact technical NPC IDs already exist. These models remain ordinary NPC archetypes, not player unlocks, bosses or Apex Units.
- Next after this batch: the **Afterecho Seer** and **Camp Witness** source studies, followed by a coordinated visual-polish and animation pass across all six.

## Merge checks

1. Original Rift Forager/Parted Striker generated files remain byte-for-byte identical.
2. All four files have valid named geometry, material definitions, coherent scale and authentic front/side/back silhouettes.
3. Reproducibility, geometric validator, repository Core and existing art-review checks pass in CI.
4. No file changes under World Five playable 3D model ownership, game rules, existing mission profiles, or approved lore archive.
5. Unity editor compilation and native visual checks remain mandatory **manual** reviews; no CI test claims to substitute for Unity execution.

An approved **enemy archetype name** is not equivalent to a locked **model design**. This PR intentionally keeps them separate.
