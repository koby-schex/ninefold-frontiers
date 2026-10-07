# Great Strider — editable form study 02

This is a refined early 3D mesh, not a finished game model. It preserves the
approved 6.0 m study envelope, four-limb stance, short neck, paired sensory sails,
cradle, seated Avarin and Waycaster console. The render is made from this mesh.
It is substantially simpler than the approved visual study and must not replace
the locked master or be described as production-quality art.

## Files and reproduction

- `GreatStrider.obj`: named editable mesh parts; meters, +Y up, +Z forward.
- `GreatStrider.mtl`: provisional flat material palette; keep beside the OBJ.
- `manifest.json`: measured counts, intended scale and limitations.
- [Mesh generator](../../BuildTools/Art/build_great_strider.py): run from repository
  root with `python BuildTools/Art/build_great_strider.py`. Standard library only.
- [Review renderer](../../BuildTools/Art/render_great_strider.py): requires NumPy
  and Pillow. Run after the generator with `python BuildTools/Art/render_great_strider.py`.
  Generated review JSON and PNG are local outputs, not needed to import the OBJ.
- [Comparison renderer](../../BuildTools/Art/compare_great_strider.py): renders the
  merged first study beside the current mesh under the same camera and lighting.
  Run `python BuildTools/Art/compare_great_strider.py` from a full repository clone.
  The optional `--reference PATH` adds a local approved-art comparison; it does not
  upload or commit that image.

## Changes in this pass

Limbs have more tapered profiles and surface-conforming mineral coverings, with
soft joint gaps and visible flexor structures. Loft frames now rotate continuously
instead of abruptly changing axis along a curve. Paired sails have curved profiles
and ribs. The operator has one crescent mantle, six sensory ribbons, face structure,
fitted protection and split clothing panels. Cradle attachment pads, braces,
retracted survey cartridges and line reels develop the equipment fit.

These are implementation studies within the approved direction, not new biological
or equipment canon. The result remains much simpler than the approved illustration.
It does not close the high-quality visual gate. Counts in `manifest.json` are
measured output, not approved mobile budgets; optimization remains outstanding.

## Review images

The **Great Strider art review** GitHub Actions run uploads the three-view sheet,
operator/cradle close-up, 390 px portrait study and before/after comparison as one
downloadable artifact. Artifacts are retained for 30 days and can be regenerated
from tracked source. The private Project reference is not part of that artifact.
The optional local reference comparison makes the remaining quality gap explicit.

The portrait image uses a proposed camera, not the Unity battle camera. It reveals
limited readability of smaller operator/equipment details. The mesh still needs
sculpting and silhouette review; these images are evidence of work in progress,
not a claim of reference parity. Basic review shading is opaque; final translucent
membrane materials, shadows and lighting are not implemented.

## Review status and authority

Koby approved Reference Study 02 as visual direction on 7 October 2026, including
the proposed 6.0 m Ilyth height. This does not revise locked canon. The controlling
sources remain Archive v26.0 with retained v25.3, PF1–8 and the locked World Five
showcase. See [the art brief](../../Docs/Production/FirstMissionArtBrief.md).

The mesh uses smooth lofts for primary anatomy and closed membrane surfaces. It
does not reproduce the reference's surface detail, ornament, clothing or material
finish. The palette is an implementation study, not new color canon. The Avarin
mantle, limb contours, sensory sails and operator access require further visual
reconciliation. Part overlap is intentional for editable construction and has not
been cleared for deformation. Closed parts do not establish printability or a
single watertight assembled creature. The renderer's soft floor shadow is a studio
presentation aid, not a physically simulated shadow or ground-contact test.

## Import and continuation

Import the OBJ and adjacent MTL into Blender with Y up and Z forward; confirm a
six-meter overall envelope after import. Keep named parts separate for sculpting
and rig preparation. Save a native Blender master after that import. No `.blend`
master is claimed here because Blender was unavailable in the build environment.
The mesh stays outside Unity's Assets folder and does not replace any playtest unit
or change gameplay. Unity import/material conversion remains a later verified
step; basic OBJ materials are not authored URP shaders.

Remaining work: detailed sculpt, reference-matched silhouette refinement,
retopology, UVs, textures/materials, rig/weights, paired movement and survey
animation, LODs, collision/selection anchors, Unity prefab integration and device
measurement. No animation, performance or final art gate has passed.

## Validation

Run `python BuildTools/Art/validate_great_strider.py` to verify reproducible output,
finite indexed triangles, nonzero triangle areas, closed consistently wound named
parts, the six-meter envelope, four limb objects, two sensory-sail objects and
material references. This is geometric integrity, not an anatomy or visual-quality
certification. Actual-mesh review images were inspected for framing and overlaps.
