# Great Strider — editable form study 03

An incremental refinement of the existing six-meter study, **not a finished game
model**. The locked Master Lore, Production Foundations 1–8 and World Five visual
master remain controlling. Koby's approval of Reference Study 02 as a visual
direction and six-meter study envelope does not make this mesh final canon.

## This pass

- Narrower upper limb tissue; tapered grown coverings with shallow longitudinal
  relief, replacing blunt sheet ends. Darker underlying limb tissue separates the
  pale mineral areas, soft joints and contact pads.
- Curved paired sensory sails with finer branching channels; no added wings,
  horns, weapon barrels or new abilities.
- Rounded heel contacts, fitted cradle side panels and seams, a console rim,
  operator shoulder protection and clothing hems.
- Distinct provisional material finishes for tissue, cloth, ceramic and frame.
- Outward winding for closed mirrored parts. Eight inward-facing parts in study 02
  were concealed by the software renderer's two-sided shading; the generator now
  corrects signed orientation and the validator rejects inward/zero-volume parts.

The cradle attachments, clothing construction, exact surface relief and finishes
remain implementation proposals. No biological or equipment canon is added here.
The model is still substantially simpler than the reference. The operator's face
remains obscured from some frontal angles, and inter-part intersections, harness
clearance and deformation still need work. Materials remain opaque approximations;
final translucent membranes, textures and skin shading are not implemented.

## Source and reproduction

- `GreatStrider.obj`: editable named parts; meters, +Y up, +Z forward.
- `GreatStrider.mtl`: diffuse palette plus per-material specular/finish study.
- `manifest.json`: measured counts and limits, not approved mobile budgets.
- `python BuildTools/Art/build_great_strider.py`: reproduce OBJ, MTL, manifest and
  local review data. Standard Python library only.
- `python BuildTools/Art/validate_great_strider.py`: reproducibility, indexed finite
  nondegenerate triangles, closed consistently outward-wound parts, six-meter
  envelope and defining part identities.
- `python BuildTools/Art/render_great_strider.py`: actual-mesh review sheet, low-front
  cradle detail and 390 px portrait study; NumPy and Pillow required.
- `python BuildTools/Art/compare_great_strider.py`: study 02 beside study 03 under
  the same camera/light. Baseline pinned to merged PR44, not a moving branch.
  Optional `--reference PATH` produces a local-only approved-direction comparison.

The GitHub **Great Strider art review** workflow uploads these renders as an
artifact retained for 30 days. They can be regenerated from tracked sources.
They are software renders, not Unity captures or generated concept illustrations.
The private locked reference is never committed or uploaded by this workflow.

## Unity and next stages

Use **Ninefold > Art > Great Strider Review**, or the Playtest Launcher shortcut.
Study 03 generates a distinct `GreatStriderReview03.unity` scene so previously
saved study 02 scenes are preserved. See [review instructions](../../Docs/Production/GreatStriderUnityReview.md).
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
