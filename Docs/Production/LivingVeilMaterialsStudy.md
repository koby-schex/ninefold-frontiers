# Living Veil — partner surface study 06

## Purpose and canon

This pass gives the standing Avarin partner separate skin, fabric, living mantle,
frame and ceramic surface treatments. It follows partner study 05 and the retained
v26/v25.3 canon reconciliation, Production Foundations 1–8 and the locked World Five
visual master. The master was visually rechecked for this pass. Existing palette,
anatomy, geometry, six ribbons, safety equipment and Olyra partnership are retained.
Exact texture and finish parameters are provisional art choices, not canon locks.
No emission, new species markings, new costume symbols or damage history is added.

## Surface assets

`BuildTools/Art/living_veil_materials.py` generates six deterministic map sets in
`SourceArt/LivingVeil/Textures/`. Each material name below has `.png` (sRGB albedo),
`_Normal.png` (tangent normal) and `_Finish.png` (linear R metallic / A smoothness).
All maps are 128×128. The existing 512×512 Olyra RGBA map remains byte-identical.

| Material | Intent | Average smoothness | Metallic |
| --- | --- | --- | --- |
| avar_skin | Quiet organic variation | .32 | 0 |
| cloth | Fine weave and restrained broad fabric variation | .16 | 0 |
| soft_charcoal | Matte dark layers, cuffs, lines and contact surfaces | .22 | 0 |
| sail_membrane | Subtle curved tissue variation | .47 | 0 |
| continuance_frame | Restrained satin fittings | .55 | .25 |
| ceramic | Smoother light housings | .62 | 0 |

Albedo contains no baked lighting. Normal maps add small surface relief, not new
geometry folds. Existing modeled folds and seams remain. Fine weave is mip-filtered
at distance. The low-contrast surface variation does not imply specific wear events.
Partner UVs use rest-space dominant-axis projection and travel unchanged between
poses. This is a provisional mapping, not a production unwrap; inspect projection
seams and tangent transitions in Unity before acceptance.

## Unity and preview handoff

Open **Ninefold > Art > Living Veil Review** and confirm **Surface study 06 / Unity
review 06**. Newly generated assets use:

- `Assets/Ninefold/ArtReview/LivingVeil06_<fingerprint>/LivingVeilStudy06.prefab`
- `Assets/Ninefold/ArtReview/LivingVeilProtective06_<fingerprint>/LivingVeilProtectiveStudy06.prefab`
- `LivingVeilReview06.unity` in the respective generated directory.

Older generated assets remain available. Source fingerprints include all texture
files so a changed detail map cannot reuse an outdated cached review asset. Normal
maps use NormalMap import, finish maps use linear color, and partner maps repeat.
Olyra's map remains clamped with authored alpha. The RGB Avarin mantle map retains
its .68 material opacity; finish alpha controls smoothness, never transparency.
URP Lit materials enable their normal and metallic-gloss keywords; meshes already
recalculate tangents. Diagnostics verify map settings and report 89,208 triangles.

Model ID `world05.living-veil.olyra` and existing GUIDs are retained. There are no
UI, gameplay, preview registry or shared Great Strider source changes. Gameplay-ID
mapping and runtime preview provider integration remain separate coordination work.

## Validation and limits

Run `python BuildTools/Art/review_living_veil.py --render` for reproducibility,
geometry, PNG channels/ranges, unit normals, packed finishes and pose-stable UV
checks plus actual-model review sheets. `Avarin-Materials06-Comparison.png` uses the
same geometry, camera and light for flat study-05 surfaces and study-06 albedo with
average finish. `Avarin-Partner06.png` shows front, side, back and face. Existing
full-pair and tactical views are regenerated too. CI uploads these as review artifacts.

The software renderer reads the exported albedo PNGs, but does not render tangent
normal maps, per-pixel finish or URP layered transparency. Its comparison is subtle
and cannot certify the Unity result. Native import/render, production unwrap,
rigging, LODs, texture compression and device profiling remain pending. The model
remains a visibly simplified study, far from final locked-master visual quality.

Geometry stays at 122 parts, 44,848 source vertices and 89,208 triangles per pose.
UV seams can increase imported vertex counts. Eighteen extra texture maps are a
review setup, not an approved mobile memory/material budget. Repository validation,
source import tests and existing 187-frame motion checks remain required.
