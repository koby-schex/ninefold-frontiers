# Great Strider deformation study 05

This offline study refines the shared mesh deformation used by the walking,
sequence and continuous-route reviews. It does not install a Unity rig or change
locked anatomy, the neutral OBJ, materials, unit scale, route, or gait timing.
The Master Lore archive, Production Foundations 1–8 and locked visual masters
remain authoritative. These motion weights are provisional implementation work.

## Changes

The knee and ankle now blend normalized quaternions around their shared joint
pivots instead of interpolating transformed vertex positions. This preserves each
vertex's distance to that pivot and avoids the radial contraction of the old
blend. It does not guarantee tissue volume or prevent every fold/intersection.
Hip roots blend from torso orientation to upper-leg orientation over 0.60 m.
The planted contact geometry remains rigid.

The short neck blends rotation angles rather than positions. Secondary sail
pitch fades in from zero near the attachment root, while inheriting head motion.
Cradle, operator and equipment retain their previous transforms.

## Review without Unity or Blender

Run from the repository root with Python and the pinned art requirements:

```sh
python -m pip install -r BuildTools/Art/requirements-review.txt
python BuildTools/Art/inspect_strider_deformation.py
python BuildTools/Art/move_great_strider.py --validate-only
python BuildTools/Art/route_great_strider.py --validate-only
```

The inspection requires Git history containing the pinned PR #50 merge
`dcb5edc7fed199ff4a0a47e72b744484af51ba99`. It loads the old skinning code
without altering the working tree. Both columns use identical geometry, route
state, lighting and cameras. The deepest knee bend found in 241 route samples
selects the inspection pose; this is a sampled maximum, not an analytic bound.

Outputs in `SourceArt/GreatStrider/DeformationReview/`:

- `comparison.png`: before/after knee, ankle, hip, neck/sail and rider views.
  The first two isolate the inspected limb. The rider view hides limbs and head
  to expose the cradle; it is a diagnostic cutaway, not a gameplay rendering.
- `checks.json`: neutral identity, joint radius, anchored hip roots, unchanged
  rider/cradle/equipment and sampled actual-knee radius errors.

GitHub Actions publishes these as `great-strider-deformation-<sha>` and also
regenerates all four motion clips and the continuous route using the new blend.
Download and unzip the artifact to view the PNG; no application setup is needed.

## Assessment and remaining work

The knee change is intentionally subtle at the existing slow-crawl amplitude.
The tests exercise larger synthetic rotations as well as the actual route.
Neutral identity, exact blend endpoints and near-180-degree quaternion
conversion are checked independently of the motion snapshots. Route checks still
cover foot planting, continuity, bone lengths and operator/seat rigidity.

The model remains a procedural form study. Surface faceting and authored part
seams remain visible; this change does not resolve those through remeshing.
Rider/seat/equipment transforms are unchanged across 33 route samples, preserving
their prior relationship. Cutaways support visual review but do not certify full
collision clearance. Intentional seated/contact overlaps, torso clearance,
physical balance, production topology and final skinning still require review.
