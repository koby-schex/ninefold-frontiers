# World Five — paired color and surface study 01

This pass refines Great Strider (Ilyth and Avarin partner) and Living Veil
(Olyra and Avarin partner) against the locked World Five visual palette:
pearl-gray tissue, silver-white grown reinforcement, smoky plum skin,
charcoal fabric and translucent pale canopy. It introduces no new markings,
symbols or canon. The simplified geometry remains a study, not final master fidelity.

Great Strider uses eight 128-pixel albedo tiles with finer tissue, mineral and
fabric variation. Living Veil adds core albedo, tangent-normal and finish maps;
seven detailed surfaces plus the canopy produce 22 PNGs total. Core UVs are
identical in both poses. Canopy alpha, all vertex positions and assignments,
and the accepted Living Veil motion code/fixture are unchanged.

## Unity review and integration handoff

Use **Ninefold > Art > Great Strider Review** (Color study 12) and
**Ninefold > Art > Living Veil Review** (Color study 09). Generate/open the
current review assets; an old scene will retain its old materials.

Generated paths beneath `Assets/Ninefold/ArtReview/`:

| Source identifier | Generated prefab | Scene |
| --- | --- | --- |
| GreatStrider | `GreatStrider12_<fingerprint>/GreatStriderStudy12.prefab` | `GreatStriderReview12.unity` |
| LivingVeil | `LivingVeil09_<fingerprint>/LivingVeilStudy09.prefab` | `LivingVeilReview09.unity` |
| LivingVeilProtective | `LivingVeilProtective09_<fingerprint>/LivingVeilProtectiveStudy09.prefab` | `LivingVeilReview09.unity` |

Each scene is inside its corresponding fingerprint folder. Previous generated
assets and GUIDs remain intact. These are art source identifiers, not authoritative
gameplay unit IDs. A future preview provider must map gameplay IDs, frame each
model's bounds, use URP Lit and preserve normal/finish and canopy alpha settings.
No UI, preview API or runtime gameplay changes are made here.

## Verification and limits

`review_worldfive_color.py --render` verifies geometry against the PR 118 baseline,
unchanged canopy alpha and motion files, and material luminance separation. Its
comparison applies the previous and revised palettes to current detail maps using
matched cameras and lighting; it is not an exact screenshot of the older release.
CI uploads the sheet and checks as `world-five-color-<sha>`.

The model validators also check reproducible exports, geometry and UVs; repository
checks guard Unity metadata. Software renders were inspected at whole-unit and
close-up scale. Native Unity shading, layered transparency, perceived detail at
phone scale and device performance still require review. No production LOD,
rigging, final visual approval or mobile performance certification is implied.
