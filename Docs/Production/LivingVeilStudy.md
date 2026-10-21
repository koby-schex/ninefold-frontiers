# Living Veil / Olyra — partner study 05

## Authority and scope

Master Lore v26.0 is the reconciliation layer; unchanged v25.3 anatomy and the
locked World Five Avarin / Continuance visual master remain authoritative.
This pass checked v26.0 section 26.6, v25.3 Living Veil anatomy/equipment and the
actual World Five master image. Production Foundations 1–8 remain controlling.
This is a model implementation proposal, not a canon revision or finished asset.

## Canon-to-model mapping

| Canon | Study treatment |
| --- | --- |
| Dense core and five overlapping translucent lobes | Same five lobes and dark core in resting and protective static poses |
| Folded rest; asymmetric floating canopy | Folded span about 2.6 m; protective span 5.56 m with curved sheltering sides |
| No conventional head or flight wings | Small smooth forward crest and tiny visual organs; no additional head/wing appendages |
| Smoky pearl membranes, clearer edges and organic pale ribbing | One continuous UV-mapped shell per lobe; smooth RGBA alpha gradient; curved main ribs and finer branches |
| Consensual independent partner and deliberate protection | Avarin stands alongside at rest, beneath the shelter in the protective pose; no control harness or force-field geometry |
| Light Veil Harness | Clips, monitor/regulator, stowed emergency lines; no suspended traversal pose yet |
| Two arms/two legs, crescent mantle and 4–6 sensory ribbons | Existing Avarin study anatomy retained with six ribbons |

Exact curvature, vein layout, alpha values, three tiny visual organs and harness
housing placement are provisional. The protective pose is a broad shelter pose;
a tighter body-wrapping variation, locomotion and gliding remain future work.
No permanent spectral glow is added. The silhouette is still a study requiring
visual acceptance against the master, especially membrane layering and core detail.

## Source and outputs

- Generator: `BuildTools/Art/build_living_veil.py`
- Resting source: `SourceArt/LivingVeil/LivingVeil.obj`, `manifest.json`
- Protective source: `SourceArt/LivingVeil/LivingVeilProtective.obj`, `protective-manifest.json`
- Shared material source: `LivingVeil.mtl`, `Textures/OlyraMembrane.png`
- Validation and six-view comparison: `BuildTools/Art/review_living_veil.py`
- Unity review tool: `Assets/Ninefold/Editor/LivingVeilReview.cs`

Each pose has 122 named parts, 44,848 source vertices, 89,208 triangles and 12
materials. Five lobe shells plus the Avarin mantle remain separate transparent
render sections. Meters, Y up, +Z forward; Avarin height approximately 1.82 m.
The two source poses have matching topology and remain static assets. Motion study
03 uses them in a separate transient editor viewport; see LivingVeilMotionStudy.md.
Protective pose top is about 3.43 m; this review altitude is not a gameplay rule.

The 512 × 512 RGBA texture contains the membrane tint and continuous alpha.
MTL `d=0.55` marks the surface as transparent for the existing parser; the Unity
review importer deliberately uses a base-color alpha multiplier of 1 for the
mapped membrane. Other OBJ importers may multiply `d` again and look darker.
Texture alpha is read from input; clamp, mipmaps and trilinear filtering are enabled.
The map now includes low-contrast cloudy tissue and fibre variation, with the existing clear-edge alpha envelope retained. Core, cloth and harness surfaces have continuous geometric relief; production texture work remains pending.

## Unity review

1. Pull the merged changes and open Unity 6000.3.21f1; finish package import.
2. Open **Ninefold > Art > Living Veil Review**.
3. Choose **Show resting fold** or **Show protective canopy**. Each pose has a
   separate versioned scene/prefab. Unsaved scene changes receive the usual prompt.
4. Confirm **Partner study 05 / Unity review 05**. Use front, side, back, tactical
   and partner-detail presets. The two full views use the same camera distance.
5. Run **Validate imported review assets** and inspect membrane overlap, core
   visibility and Avarin readability. Use a 540 × 960 portrait Game view.

No Play mode is required. Configure the existing URP foundation through the
Playtest Launcher if needed. Existing review scenes and edits are preserved;
**Build fresh review copy** generates a new unique folder. No model is installed
in the live battlefield, Collection screen or a player profile by this tool.

See `ModelPreviewHandoff.md` for paths and integration requirements.

## Validation and limits

`python BuildTools/Art/review_living_veil.py --render` validates both source poses
and publishes `SourceArt/LivingVeil/Review/LivingVeil-Review.png` and checks.json.
Checks cover finite/nondegenerate geometry, closed outward-wound individual parts,
five continuous lobes, membrane UV domain, continuous alpha samples, partner
anatomy/grounding, deterministic OBJ/MTL/PNG/manifests, resting separation and a
conservative triangle-versus-expanded-partner-box exclusion in the protective pose.
These checks do not certify lobe-to-lobe self-intersection or deformation quality.

The C# test imports both actual OBJ files through the shared parser and verifies
geometry counts, normals, UV coverage and all six transparent sections. The
software renderer's opt-in interpolated alpha path has a numeric blend regression
check; existing constant-alpha rendering is unchanged. Software comparison views
use the nearest transparent shell, not all overlapping layers or refraction.
Native Unity compilation, transparency sorting and visual acceptance remain
pending. No mobile performance, final rig, animation, LOD or production-ready claim.

## Motion study 03 addition

**Ninefold > Art > Living Veil Motion Review** opens the separate motion viewport.
The static review window also links to it. Motion leaves source materials/meshes
and open scenes intact. Protective strand rings are cyclically reindexed (same
surface geometry and counts) to avoid cross-section collapse during blending.
This is source correspondence repair, not new anatomy. Source export validation
continues to cover both poses. No production rig or runtime integration is added.

## Surface study 04

Rechecked the current v26 reconciliation, retained v25.3 Olyra anatomy/equipment,
and the actual locked World Five image. The same five lobes, darker core, small
sensory crest and separate consensual Avarin partnership remain controlling.
No new anatomy, abilities, permanent glow or canon revisions are introduced.

Changes: softly scalloped lobe surfaces, tapered organic ribs/veins, continuous
core shaping, restrained 512 px membrane tint variation, cloth folds/draped tunic
hem and shouldered harness fittings. Timings and motion curves are unchanged.
The part/vertex/triangle counts are unchanged; both poses still match for motion.
Fine surface relief is an implementation proposal pending visual acceptance.

The static Unity window adds **Core / membrane-root detail** alongside the partner
and tactical presets. Fresh imports use review-04 folders and preserve older
review scenes, prefabs and GUIDs. Motion review automatically reads the new source.
CI publishes `LivingVeil-Detail04.png` with core, partner and tactical views in
addition to the same-camera resting/protective sheet and four motion GIFs.

Software previews read the actual authored PNG, decode its tint to linear space
and filter it through mipmaps. They approximate tint as a scalar and alpha at
mesh vertices; they still show only the nearest transparent shell. Native URP
layer sorting, close-up appearance and mobile performance remain unverified.
Use native Unity review before accepting this as finished art.

## Partner study 05

See `LivingVeilPartnerStudy.md` for the standing Avarin refinement, anatomy and
harness mapping, review presets and validation limits. The prior surface-04
section describes the preceding pass; current import paths use revision 05.
Olyra geometry and motion curves are unchanged by partner 05.


## Current surface revision — 06

The geometry remains partner study 05; current generated Unity assets use surface
study 06. See [LivingVeilMaterialsStudy.md](LivingVeilMaterialsStudy.md) for the
six partner texture sets, revised prefab paths and actual-model comparison sheet.
Earlier revision paths in this document are historical.
