# Great Strider connection and rider study 07

This pass refines the existing game source study. It is not a new anatomy master,
final sculpt, print model or Unity rig. The Master Lore Archive v26.0 (including
its retained v25.3 content), Production Foundations 1–8 and locked World Five
visual master remain controlling canon.

## Canon grounding

The v25.3 sections **World 5 Playable Species — Head and Face Design** and
**Great Strider Surface, Color, and Harness Integration**, retained by v26.0,
control this pass: a calm, expressive Avarin face, reduced nose, organic mouth,
temple-to-cheek/jaw mineral bands, forehead seam, a lightweight Ilyth torso,
grown stress reinforcement and a suspended, partially enclosing traversal cradle.

The existing mantle and six ribbons remain unchanged. The hand contour is a
provisional grouped form; it does not establish a canonical digit count. No
new lore, weapons, creature appendages, costume items or canon revisions are
introduced by this implementation.

## Mesh changes

- Taper the short neck's root and tip into the torso and compact head.
- Narrow each limb's first root ring inside the torso, retaining its center,
  original bone anchors and lower-leg geometry. The mineral covering follows it.
- Bring fitted mineral sheath edges closer to the underlying skin: edge offset
  decreases from 18 to 4 mm in authoring space, with a thinner closed shell.
  Raised central relief and soft joint gaps remain.
- Replace the rider's overlapping face piece with relief in the head surface.
  Add subtle cheek/nose shaping, contour-following eyelids and flatter mineral
  bands that extend from temple through cheek toward jaw. Preserve the eyes,
  organic mouth and narrow forehead seam.
- Shape the grouped hands over the console rim and introduce shallow cloth
  relief that fades out at the loft ends. Seated body anchors, seat, footrests
  and equipment remain in place.

The source now has 184 parts, 40,646 vertices and 80,556 triangles. Height is
still 6 m. The full XYZ envelope matches PR #52 exactly. Seventy protected
parts retain identical vertices and topology, including the torso, compact
creature head, sails, contact pads, cradle and survey equipment.

## Review without Unity

```sh
python -m pip install -r BuildTools/Art/requirements-review.txt
python BuildTools/Art/build_great_strider.py
python BuildTools/Art/validate_great_strider.py
python BuildTools/Art/review_strider_connections.py
python BuildTools/Art/route_great_strider.py --validate-only
```

Git history must contain PR #52's merge
`b58714df3995b63455434ebf51fa01e93f27d0db`. The comparison imports that generator
without changing the working tree and renders both meshes with identical
lighting and cameras. Images come from the actual exported geometry.

The `great-strider-connections-<sha>` GitHub Actions artifact contains:

- `comparison.png`: neck/hips, isolated forelimb reinforcement, isolated Avarin
  face, and a rider/cradle cutaway. Isolation is explicitly labeled.
- `mobile.png`: before/after at 390-pixel portrait width.
- `checks.json`: protected geometry, unchanged envelope, neutral containment,
  and containment across 17 sampled route poses (3,264 cap-ring vertices).

All 32 vertices of each of the six inspected end rings must be inside the host
surface. This prevents exposed flat caps; it does not prove tangent continuity
or a fully welded biological mesh. Existing geometric and route checks also
verify closed outward-wound parts, eye sightlines, grounded feet, bone lengths,
stance stability and operator/seat rigidity.

The older surface-study comparison is now pinned at both PR #51 and PR #52,
so its original unchanged-centerline tests remain meaningful as a historical
review. Current geometry receives this new connection review and all existing
motion/route tests; historical checks do not replace current validation.

## Remaining quality gap

Separate intersecting parts and shading transitions still exist. End-cap
containment does not certify full inter-part collision clearance, skin welding
or production deformation. The face, grouped hands and clothing remain stylized
procedural studies, requiring further sculpt/topology and texture refinement.
No Unity import, mobile performance, UV, LOD or final art acceptance is claimed.
