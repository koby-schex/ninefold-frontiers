# Great Strider material study 08

This is an offline source-asset refinement, not final game art or a Unity
integration. The Master Lore Archive v26.0, retained v25.3, Production
Foundations 1–8 and locked World Five visual master remain authoritative.

## Direction and canon

The retained **Great Strider Surface, Color, and Harness Integration** section
specifies muted pearl-gray / pale ash tissue, a lightweight smooth torso,
darker lower joints and contacts, pale silver-white grown reinforcement, and
smoky translucent sensory sails. The Avarin remains smoky plum/charcoal.
The existing cradle, Waycaster, face, mantle and ribbons keep their geometry.

This pass introduces no new lore or markings. Fine color variation, finish
values, fabric weave scale and projection are provisional art implementation
choices, subject to visual review. The locked palette and biological versus
engineered distinction control these choices, not incidental reference lighting.

## Deliverables

- Eight deterministic 64×64 RGB PNG albedo tiles, with base color baked in and
  sRGB encoding: living tissue, limb tissue, contact tissue, mineral, Avarin skin,
  cloth, frame and ceramic. Variation is restrained (2.5–11% in linear color).
- Actual OBJ texture coordinates and MTL `map_Kd` paths. Textured materials use
  white Kd to avoid applying the baked tint twice; remaining materials retain
  their existing flat Kd. Membrane opacity is unchanged.
- A material catalog (`SourceArt/GreatStrider/materials.json`) records linear
  base colors, sRGB map paths, opacity and the review renderer's finish values.
  These are Blinn-style study parameters, not a calibrated Unity PBR preset.
- Rest-pose dominant-axis box projection on 142 parts, with 45,827 UV coordinates.
  Projection changes are provisional UV seams. Geometry remains 184 parts,
  40,646 vertices, 80,556 triangles, six meters tall, exactly matching PR #53.
- The software reviewer samples equivalent linear texture values using bilinear
  and trilinear mip filtering. Small details fade into their average at a
  distance. UVs stay attached to mesh vertices through the motion studies.

The mineral palette is now neutral silver-white instead of slightly yellowed;
cloth is darker and more matte than exposed skin. Grown mineral has softer
highlights than the engineered ceramic. No chips, cracks, damage, scars,
ornaments, emissive marks or displacement have been added.

## Review and validation

```sh
python -m pip install -r BuildTools/Art/requirements-review.txt
python BuildTools/Art/build_great_strider.py
python BuildTools/Art/validate_great_strider.py
python BuildTools/Art/review_strider_materials.py
```

The generator remains standard-library-only. Review requires NumPy/Pillow and
full Git history. Comparison baseline is PR #53's fixed merge commit
`906f8a15572466d7ec4560da77ff8bf09aed67ce`.

The GitHub Actions artifact `great-strider-materials-<sha>` contains:

- `comparison.png`: whole-unit, skin/mineral close-up and labeled rider cutaway,
  with identical geometry, lighting and camera framing in both columns.
- `mobile.png`: matched 390-pixel portrait views from the proposed tactical camera.
- `checks.json`: exact geometry/material assignment preservation, opacity,
  UV index integrity, PNG reproducibility and decoded pixels, distant-filter
  convergence, and UV retention in three sampled motion poses.

All existing geometry, connection, motion and route checks continue to run.
The old surface comparison remains a pinned historical review. New material
checks and previews inspect current output.

## Integration and quality limits

Material study 08 originally shipped as an offline asset pass. Unity review 09
now supplies the separate importer update for independent OBJ vertex/UV indices,
smooth normals across seams, texture color space and transparent-surface settings.
See [the Unity review guide](GreatStriderUnityReview.md). The generated static
review prefab is not a battlefield replacement, and native Unity appearance and
device performance remain unverified until the next review.

These are low-cost source tiles, not a production unwrap, baked normal maps,
sculpted microdetail or an optimized texture atlas. Box-projection seams can
remain visible in close-up. The offline renderer is illustrative, with a fixed
studio light and simplified transparency; no device performance or production
lighting claim is made. Unity import, gameplay-camera inspection and LOD work
remain required before final asset acceptance.
