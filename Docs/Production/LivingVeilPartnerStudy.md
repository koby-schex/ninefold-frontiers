# Living Veil — Avarin partner study 05

## Scope and authority

Standing partner refinement against the current v26 reconciliation, retained
v25.3 Avarin mantle/face/bonding and Olyra Veil Harness passages, and the actual
locked World Five showcase. Production Foundations 1–8 remain controlling.
This is an implementation study, not a new canon lock or finished character.

| Controlling feature | Implementation |
| --- | --- |
| Two arms, two legs and calm layered temple-face | More shaped sleeves, legs and grouped hands; small jaw refinement beneath the existing expressive eyes and temple bands |
| Crescent living mantle, 4–6 short sensory ribbons | One curved continuous mantle, six flat tapered ribbons and six restrained reinforcing veins |
| Layered Continuance clothing | Two split front panels, two curved side/back panels, collar and cuffs with differentiated existing materials |
| Light safety/coordination harness | Clip loops/hinges, stowed suspension-line coils and monitor bezel; existing regulator retained |
| Independent, consensual Olyra partnership | Partner remains planted during the existing canopy motion; no control leash or forced-bond equipment |

Individual digit count is not established by the grouped hand contour. Exact
garment cuts, folds, fitting shapes and anatomy relief remain art proposals.
The forehead seam and other surfaces receive no new permanent emission.

## Implementation and compatibility

`BuildTools/Art/refine_avar_partner.py` refines only the standing Living Veil
partner. The Great Strider's shared geometry source and seated Avarin are untouched.
It runs before grounding and protective-pose placement, so both source poses
retain matching topology. Olyra source geometry and all motion curves are unchanged.
The motion fixture is revalidated and importer count guards updated for the new partner.

Each pair now contains 122 named parts, 44,848 vertices and 89,208 triangles.
The increased detail is for review; this is not a mobile draw-call or triangle
budget approval. Production LODs, rigging and device profiling remain future work.
Existing IDs and `.meta` GUIDs are preserved. Review-05 output paths generate new
assets and retain earlier review copies. No UI, gameplay or preview provider is
changed; this work starts from main through PR #93.

## Review

Open **Ninefold > Art > Living Veil Review**, confirm **Partner study 05 / Unity
review 05**, then open either pose. Use **Avarin / Veil Harness detail**, the new
**Avarin face / temple detail**, side/back and tactical presets. Motion remains
available through **Living Veil Motion Review**. No Blender sculpting is required.

`python BuildTools/Art/review_living_veil.py --render` produces the normal pose
comparison, `LivingVeil-Detail05.png` and `Avarin-Partner05.png`. The latter shows
front, side, back and face views of actual geometry. CI publishes them with the
motion GIFs. Software rendering uses nearest-shell transparency and is not a
substitute for native URP review.

Validation covers reproducible sources, closed outward individual parts,
nondegenerate triangles, grounding, two arms/two legs, six ribbons, paired drapes,
clip loops and coils, positive garment ground clearance and the split front hem.
Motion checks cover matching poses, fixed partner and sampled Olyra clearance.
They do not certify every inter-part intersection, skin deformation, transparency
sorting or native Unity performance. Inspect garment attachment, cuffs, harness
connections and mantle layering in Unity before visual acceptance.
