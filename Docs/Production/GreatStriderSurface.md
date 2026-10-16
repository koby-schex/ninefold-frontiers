# Great Strider surface study 06

This is an offline mesh/material refinement, governed by the Master Lore Archive,
Production Foundations 1–8 and locked World Five visual master. It introduces no
new anatomy, equipment, lore or gameplay. The existing procedural model remains
an unfinished form study; this pass is not final sculpt quality.

## Changes

- Increase radial resolution on broad creature forms to 32 sides and selected
  rider/cradle forms to 20. Preserve authored centerlines, control radii and ends.
- Increase sampling of fitted grown-mineral sheaths; read their source loft's
  actual side count instead of assuming 16, preserving their fitted placement.
- Separate matte tissue/cloth, grown mineral, ceramic and manufactured frame
  through restrained specular finishes. Colors are unchanged.
- Export sail-membrane opacity of 0.68 in MTL and review data. This material also
  serves the Avarin crescent mantle, which receives the same provisional finish.
- Carry opacity through walking, sequence and route review data.

The result has 181 named parts, 40,236 vertices and 79,748 triangles (previously
33,188 vertices and 65,652 triangles). Height remains 6 m. This is a source study,
not an approved mobile triangle budget; LODs and device profiling remain pending.

## Review

```sh
python -m pip install -r BuildTools/Art/requirements-review.txt
python BuildTools/Art/build_great_strider.py
python BuildTools/Art/validate_great_strider.py
python BuildTools/Art/test_review_transparency.py
python BuildTools/Art/review_strider_surface.py
```

This historical surface comparison is pinned to the PR #51 and PR #52 releases,
so later anatomy refinements do not invalidate its unchanged-centerline checks.
The comparison requires Git history containing the PR #51 merge
`28a1b51cdf87ad7305447f4bbe1169118b4dd573`. It generates actual mesh views under
identical cameras, lighting and framing. The baseline has its original opaque
material data. No concept image substitutes for a rendered model.

GitHub Actions publishes `great-strider-surface-<sha>` containing:

- `comparison.png`: whole model, head/neck/sails, and rider/cradle cutaway.
  The cutaway hides the creature's legs/head to expose the suspended assembly.
- `mobile.png`: side-by-side 390-pixel portrait camera studies.
- `checks.json`: source counts, silhouette envelope, unchanged part/material
  assignments, loft endpoint/center checks, and membrane opacity.

The existing motion and route artifacts are regenerated too. Download and unzip
artifacts to inspect the PNGs or open their standalone motion viewers. Unity and
Blender are not required for this review.

## Verification and limitations

Geometry validation checks reproducible exports, closed outward-facing parts,
finite nondegenerate triangles, grounded contact pads, forward eye rays and an
open calf region. Surface checks retain exact loft endpoints and centerlines.
The previously authored nonlinear stretch of the cradle load frames causes a
small change in sampled vertex centroids when radial resolution increases;
this is separately reported and limited to 0.1 mm rather than treated as an
exact centerline measurement. No frame profile or attachment was redesigned.

Transparency checks cover blending, opaque occlusion, part ordering, zero
opacity and avoiding double opacity through a closed thin shell. The software
renderer composites only the nearest translucent surface over opaque geometry;
it does not model multiple translucent layers, refraction or physical scattering.
MTL opacity does not configure Unity render queues or shaders automatically.

Visible seams between separate authored parts remain. This pass does not weld,
remesh or conceal them, and does not certify full collision clearance, production
rigging, texture/UV quality or device performance. The next production stages
still require those tasks and visual approval.
