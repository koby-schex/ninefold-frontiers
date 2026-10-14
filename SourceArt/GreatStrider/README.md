# Great Strider — editable form study 04

An incremental refinement of the existing six-meter study, **not a finished game
model**. The locked Master Lore, Production Foundations 1–8 and World Five visual
master remain controlling. Koby's approval of Reference Study 02 as a visual
direction and six-meter study envelope does not make this mesh final canon.

## This pass

- Refined short-neck profile and deeper shoulder-root joins; smaller soft-joint
  bulges retain the same four-limb stance and six-meter envelope.
- Lowered the intact operator and survey equipment 22 cm in authoring space
  (approximately 22 cm after normalization), without shrinking the Avarin. Fixed
  body attachment pads remain in place; load-frame extensions follow the new seat.
- Replaced the floor that crossed the calves with a rear seat shelf and two
  supported footrests, leaving an open leg well. Existing face details now sit on
  the visible facial surface instead of being buried inside it.
- Grounded all four contact-pad groups, retaining closed editable geometry.
- Added front, side, back and oblique underside inspection renders, plus a Unity
  Scene-view cradle-underside preset. Comparison baseline is merged study 03.

All exact harness construction and surface transitions remain implementation
proposals beneath the locked masters. This is still an unfinished, comparatively
simple mesh. Operator visibility is improved in the neutral front view, but
other angles can remain obscured. Full body/equipment intersections, movement
clearance, weight distribution and deformation are not certified. The geometry
remains separate overlapping source parts rather than a welded animation mesh.

## Source and reproduction

- `GreatStrider.obj`: editable named parts; meters, +Y up, +Z forward.
- `GreatStrider.mtl`: diffuse palette plus per-material specular/finish study.
- `manifest.json`: measured counts and limits, not approved mobile budgets.
- `python BuildTools/Art/build_great_strider.py`: reproduce OBJ, MTL, manifest and
  local review data. Standard Python library only.
- `python BuildTools/Art/validate_great_strider.py`: reproducibility, indexed finite
  nondegenerate triangles, closed consistently outward-wound parts, six-meter
  envelope and defining part identities. Also checks forward eye rays against the
  Ilyth, grounded contact groups and the open calf region. These limited neutral
  pose checks do not establish full clearance.
- `python BuildTools/Art/render_great_strider.py`: actual-mesh review sheet, low-front
  cradle detail, four-view inspection sheet and 390 px portrait study; NumPy and Pillow required.
- `python BuildTools/Art/compare_great_strider.py`: study 03 beside study 04 under
  the same camera/light. Baseline pinned to merged PR45, not a moving branch.
  Optional `--reference PATH` produces a local-only approved-direction comparison.

The GitHub **Great Strider art review** workflow uploads these renders as an
artifact retained for 30 days. They can be regenerated from tracked sources.
They are software renders, not Unity captures or generated concept illustrations.
The private locked reference is never committed or uploaded by this workflow.

## Unity and next stages

Use **Ninefold > Art > Great Strider Review**, or the Playtest Launcher shortcut.
Study 04 generates a distinct `GreatStriderReview04.unity` scene so previously
saved study 03 scenes are preserved. See [review instructions](../../Docs/Production/GreatStriderUnityReview.md).
The importer checks updated source counts and converts MTL finish values into
approximate URP smoothness; no pipeline settings or battle units change.

The OBJ remains the editable source, with separate named objects. The Unity review
uses a combined inspection mesh, not a sculpt/rig master. No native Blender master,
UV unwrap, weights, rig, animations, LODs, optimized prefab or device measurements
are delivered. This is neither printability certification nor a final-art gate.

Sources rechecked 14 October 2026: v26.0 §26.0/26.6; retained v25.3 Great Strider
body, sensory, surface, cradle and Waycaster entries; locked World Five showcase
inspected as pixels. The written compact-head/short-neck and suspended-cradle
constraints remain controlling; incidental showcase details do not authorize a
cavalry saddle or civic building on this unit. See [the art brief](../../Docs/Production/FirstMissionArtBrief.md).
