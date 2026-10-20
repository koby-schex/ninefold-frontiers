# Living Veil / Olyra — resting form study 01

## Authority and scope

Master Lore v26.0 is the reconciliation layer; unchanged v25.3 anatomy and the
locked World Five Avarin / Continuance visual master remain authoritative.
Reviewed v25.3's Living Veil/Olyra and Avarin anatomy, equipment and bond sections,
v26.0 section 26.6, and the World Five master. Production Foundations 1–8 and
all locked masters remain controlling. This model is an implementation proposal,
not a new visual master, canon lock or finished production asset.

## Canon-to-model mapping

| Canon | First-study treatment |
| --- | --- |
| Dense core with five broad overlapping translucent lobes | Dark charcoal-plum core; five asymmetrical lobes folded downward around it |
| No conventional head, wings or flight | Small smooth forward sensory crest and tiny visual organs; no head/wing appendages |
| Pearl-gray membranes, clearer edges, organic pale ribbing | Three opacity bands per lobe, with pale curved ribs; no permanent rainbow glow |
| Independent consensual partner | Avarin standing alongside; no saddle, reins, control collar or mounted pose |
| Avarin two arms/two legs; living crescent mantle and 4–6 ribbons | Two arms/two legs, six ribbons, established face/mantle geometry reused from the Strider source study |
| Veil Harness with clips, monitors, suspension lines and regulator | Low-profile shoulder straps, clips, monitor/regulator and stowed line housings |
| 4–6 m deployed membrane span | Deferred to a deployed pose; the folded study is about 2.6 m across, not a substitute scale target |

The three tiny visual organs, lobe curvature, harness housing placement, exact
opacity values and standing pose are provisional implementation choices. Final
visual acceptance requires review against the master. The software render uses
nearest-shell transparency; layered overlap appearance can differ in Unity.

## Source and output

- `BuildTools/Art/build_living_veil.py`: deterministic editable model generator.
- `SourceArt/LivingVeil/LivingVeil.obj` and `.mtl`: actual exported geometry/materials.
- `SourceArt/LivingVeil/manifest.json`: revision, bounds, counts and limitations.
- `BuildTools/Art/review_living_veil.py`: reproducibility/geometry checks and optional renders.
- `Assets/Ninefold/Editor/LivingVeilReview.cs`: opt-in native Unity review importer.

77 named parts, 27,398 source vertices, 54,488 triangles, 14 flat materials.
Meters, Y up, +Z forward. The grounded Avarin is approximately 1.82 m tall;
Olyra's resting presentation floats above ground and tops out near 3.01 m.
Its presentation altitude is not a fixed biological height or gameplay rule.
No game roster, campaign, abilities, profile, progression or battlefield is changed.

The generator reuses the existing Mesh primitives and Avarin face/mantle study.
Future shared-source changes must regenerate and verify both units. Export
reproducibility gates detect stale checked-in Olyra assets. The importer reuses
the tested OBJ/MTL parser; translucent bands stay separate for native sorting.

## Unity review

1. Pull the merged PR; open Unity 6000.3.21f1 and let compilation finish.
2. Open **Ninefold > Art > Living Veil Review**.
3. Click **Open / create latest review scene**. Configure the URP foundation
   through the existing Playtest Launcher if prompted. No Play mode is needed.
4. Confirm **Resting form study 01 / Unity review 01**; inspect front, side,
   back, tactical and Avarin/Veil Harness detail views. Use portrait Game view.
5. Expand material inspection or run **Validate imported review assets**.

Generated assets live under `Assets/Ninefold/ArtReview/LivingVeil01_<fingerprint>/`.
Reopening preserves saved edits; **Build fresh review copy** creates a unique
folder. The save-scene prompt can be canceled. Great Strider scenes remain intact.
Scene display controls hide grid/gizmos/skybox; Unity's normal controls restore them.

## Validation and acceptance limits

`python BuildTools/Art/review_living_veil.py` checks deterministic exports, closed
and outward-wound individual parts, nondegenerate triangles, finite vertices,
five three-band lobes, two arms/two legs, six ribbons, partner height/grounding,
and a positive gap between creature and partner bounds in this resting pose.
The C# parser test imports the real asset, checks counts, normals and all 16
separate transparent sections (15 lobe bands plus Avarin mantle).

`python BuildTools/Art/review_living_veil.py --render` produces three source views
and checks.json in the ignored local Review folder. GitHub's Living Veil art
review workflow publishes them as a downloadable artifact. These are actual
source renders, not generated concept images or native Unity screenshots.

Native Unity compilation and visual checks remain pending: check band sorting
from all views, core visibility, portrait framing and partner readability.
The three opacity bands are an interim approximation, not a seamless alpha map.
Closed individual parts do not certify inter-part intersections or deformation.
No final textures, production topology, UV unwrap, rig, animation, LODs or device
performance claims. Protective wrapping, deployed canopy and suspended traversal
poses will be developed after review of this resting form.
