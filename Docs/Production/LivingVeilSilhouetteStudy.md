# Living Veil — Avarin silhouette study 07

## Scope

Standing-partner refinement against the retained v26/v25.3 reconciliation,
Production Foundations 1–8 and the locked World Five visual master. Exact garment
cuts and surface curvature remain art proposals. This is not final visual approval.

The shoulder layers now cover the sleeve roots; collar and neck contours blend
more closely. Sleeves have restrained elbow folds. Existing front and outer drapes
have revised width, flare and hem curvature, with two new rear layers attached at
the tunic. A slight jaw taper moves the facial features with the skin. Mantle
curvature is softened while retaining its crescent, reinforcement and six short
ribbons. No new anatomy, symbols or emission are added.

Olyra geometry, motion curves, harness fittings and the 19 texture files remain
unchanged. All changes are local to the Living Veil standing partner; the Great
Strider's seated partner is unaffected. Both poses retain matching topology and
UV correspondence. Four new closed garment panels bring each pair to 126 parts,
47,136 source vertices and 93,768 triangles. This is not a mobile budget approval.

## Review and integration

Open **Ninefold > Art > Living Veil Review** and confirm **Silhouette study 07 /
Unity review 07**. Generated prefab paths are now:

- `Assets/Ninefold/ArtReview/LivingVeil07_<fingerprint>/LivingVeilStudy07.prefab`
- `Assets/Ninefold/ArtReview/LivingVeilProtective07_<fingerprint>/LivingVeilProtectiveStudy07.prefab`

The scene in each directory is `LivingVeilReview07.unity`. Earlier review assets
remain available. Model ID `world05.living-veil.olyra` and existing GUIDs are retained.

This branch includes main through PR #114. No UI, gameplay or shared presentation
files are modified. The newly merged interactive preview contract is acknowledged;
no provider, production rig or gameplay-ID mapping is implemented in this pass.

## Validation and limits

Run `python BuildTools/Art/review_living_veil.py --render` for reproducibility,
closed/outward geometry, grounding, garment counts and hem clearance, surface maps,
pose-stable UVs, and current front/side/back/face and tactical renders. The output
includes `Avarin-Partner07.png`, `LivingVeil-Detail07.png`, and material comparisons
using the current geometry. Local before/after silhouette renders were also
compared at identical framing and lighting.

Run `python BuildTools/Art/review_living_veil_motion.py` for the existing 187-frame
motion checks and sampled partner clearance. The unchanged motion fixture remains
valid. Source import tests have updated geometry counts; native Unity import and
rendering remain pending. These checks do not certify all cloth-body intersections,
production skinning, transparency sorting or mobile performance. Inspect shoulder
edges, collar layering, rear-panel attachment and mantle overlap in Unity before
visual acceptance. The asset remains visibly simplified relative to the locked master.
