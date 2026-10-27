# World Five — Ashbound Enemy Model Batch 03: Afterecho Seer and Camp Witness

**Status:** provisional 3D geometry study, not canon-approved final appearance, not rigged, not imported into live battles.

This batch completes the initial *source mesh coverage* for all six canon-approved, anonymous ordinary Ashbound NPC archetypes; it **does not** claim six production-ready characters.

## Two newly authored model studies

**Afterecho Seer** — `ashbound-afterecho-seer`

- Retains ordinary Avarin upright anatomy, temple-face, eyes and natural crescent sensory mantle.
- Narrow, lightly guarded silhouette with asymmetric hanging gear, separate broken external reflector petals, a rear unstable capture cassette and fine stabilization conduits.
- Visual direction conveys distorted environmental readings and hazardous Afterecho consequences through equipment readouts. **No floating soul halo, spell staff, creature companion, new body organs, firearm or protected story revelation.**
- Gameplay roster profile already exists (fragile long-range pressure); this PR does not change stats or add an ability implementation.

**Camp Witness** — `ashbound-camp-witness`

- Recognizably Avarin, relatively rectangular support-oriented silhouette, two different shoulder coverings, patched field coat, utility inventory satchel and folded camp signal plate.
- Ordinary logistical/sensing hardware reads as support gear rather than offensive artillery, ritual glorification or command rank.
- No named character, boss, special enemy, Apex Unit, special relationship to a bonded creature or biological bond sensor is introduced.

Both models use about 1.82 m adult Avarin reference scale, meter units, +Y up and +Z forward. Exact proportions, salvage layering, material palette, equipment mechanics and wear are visual **proposals**, not approved world canon. Controlling sources: Master Lore & World-Building Archive v26.0, locked World Five visual master and `Docs/Canon/WorldFiveAshboundCanonAddendum01.md`.

## New source assets / reproducible generation

- `SourceArt/Ashbound/AfterechoSeer/{AfterechoSeer.obj, AfterechoSeer.mtl, manifest.json}`
- `SourceArt/Ashbound/CampWitness/{CampWitness.obj, CampWitness.mtl, manifest.json}`
- `BuildTools/Art/ashbound_final_pair_geometry.mjs`: isolated authored geometry for Seer/Witness. Existing four models keep their original individual meshes.
- `BuildTools/Art/build_ashbound.mjs`: regenerates all six, including the first four, using the same underlying Avarin study mesh utilities.
- `BuildTools/Art/validate_ashbound.mjs`: checks six real OBJ geometries for finite 3D coordinates, legal indices, nondegenerate triangles, independently named parts, material integrity, scale, required Avarin elements, model-specific silhouette features and declared nonplayable companion count of zero.
- `BuildTools/Art/render_ashbound.py`: creates six review sheets, each rendering the **actual model geometry** from front / three-quarter / right / back. In CI these appear in the **Ashbound model source checks** workflow artifact `ashbound-enemy-mesh-study-03`.

## Unity Editor review

Open Unity **6000.3.21f1** and select `Ninefold > Art > Ashbound Enemy Review`. Two new buttons allow the Seer and Witness scene reviews. For each, the tool reads the OBJ and MTL, validates manifest/height, imports a fingerprint-isolated temporary prefab, and opens a neutral-lit 3D review scene. All objects remain under `Assets/Ninefold/ArtReview` and should not be promoted directly to a gameplay catalog.

**Visual checks:** distinctive at portrait mobile camera distance; anatomical mantle and face not obscured by hardware; no intersections, hovering attached panels, mirrored equipment mistakes, UV/normal defects, false creature anatomy or effects that appear to originate from the mantle. Compare the shapes directly to the locked World Five showcase before approval.

## Art production priority after this PR

1. **Review and revise real shape fidelity on all six.** Fix visible anatomy, face, mantle, weighting, silhouette conflicts and mechanical fit. Currently these are procedurally surfaced studies rather than polished hand-sculpted characters.
2. **Rig and animate.** Establish Avarin anatomy deformation, mantle flex, idle perception, purposeful locomotion, normal attack/main ability, hit reaction and defeat. Separately show Afterecho strain; do not treat it as an invulnerable power-up.
3. **Upgrade surfaces.** Authored material variants, physically coherent wear, high-quality textures, proper UVs/normal maps, smooth deformation and restrained arcane visual cues. Retain distinct identities and natural Avarin anatomy.
4. **Optimize and playtest.** LODs, appropriate skinned mesh/draw-call budgets, runtime animation controller transitions, portrait readability, iPhone 14 frame/memory/thermal tests. The current ~39k–60k source triangles per NPC are **unmeasured**, not mobile-approved.
5. **One-to-one battle handoff.** Only after explicit visual/canon approval use the dedicated model candidate staging and runtime visual catalog. Existing anonymous combat IDs are reference identifiers, *not* permission to auto-map a source study to an active mission.

No source file in this PR modifies the World Five playable Great Strider, Living Veil, Burdenbeast, Riftwalker, any player progression, existing Ashbound enemy combat kits, campaign missions, saves, AI or lore archive.

## CI and remaining unverified gates

- CI rebuilds all six source meshes and checks the tracked OBJ/MTL/manifest reproducibly (no silent modifications to the earlier four).
- Structural geometry verifier and real-source render workflow must pass.
- Repository Core, Great Strider and Living Veil automated checks must pass independently.
- Unity compilation, real visual master comparison, production animation rig and actual mobile performance **cannot be certified by GitHub's automated mesh-source workflow alone**. These remain explicit review gates.

**The next development step is model quality and movement, not more provisional NPC names or automatic playable-unit registration.**
