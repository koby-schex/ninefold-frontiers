# World Five silhouette rebuild 03

The user rejected the visual fidelity of the previous Riftwalker and Burdenbeast
studies. This revision prioritizes their forms before additional animation.
Neither the previous studies nor these replacement contours are approved visual
canon. v26.0, retained v25.3 anatomy, Production Foundations 1–8 and the locked
World Five showcase remain controlling. No canon changes are proposed.

## Actual geometry changes

Vaskel's former broad frontal arch is replaced by a compact sagittal crescent
with an offset waist, unequal planted limbs, tapered hocks, ankle transitions
and a flattened raised counterweight. The pale sensory crescent follows
the outer body with layered margins; tiny pits, secondary eyes and separate
non-emissive channels remain. The opposing counterweight never becomes a third
ground contact. Exact unseen contours and tissue folds remain proposals.

Tervan's low torso and short sensory hood are reshaped with broader muscular limb
roots and tapered lower joints. Eight circular pads remain grounded. Back ridges
extend across the load-bearing roots. The civic platform gains load spreaders,
sills, a deck, fitted compartments, inset panels, structural piers and roof rails.
The exact civic arrangement is proposed; modules and the stowed Convergence
housing remain equipment separate from the creature. Independent Avarin partners
retain their previous geometry and position.

## Review before more motion

Generate a fresh scene with the existing Burdenbeast or Riftwalker Review menu.
New paths are `Assets/Ninefold/ArtReview/<Unit>03_<fingerprint>/`, containing
`<Unit>Study03.prefab` and `<Unit>Review03.unity`. These are **static** reviews:
the study-02 Animator is not attached to revised anatomy. Previously generated
scenes and their animations remain intact. Walking work stays paused until the
silhouette is reviewed. Shared UI, enemy sources and battle catalogs are unchanged.

Run `build_worldfive_roster.py`, `review_worldfive_roster.py --render` and
`review_worldfive_rebuild.py` under `BuildTools/Art/`. CI's roster artifact includes
front, side, three-quarter and tactical views plus a matched old/new comparison.
The comparison uses flat materials and the same framing to reveal actual form
changes rather than texture or lighting changes.

Geometry checks verify outward closed parts, scale, limb counts, fixed ground
contacts, separate partners and reproducible source files. C# import checks remain
active. Those checks do not establish likeness, full self-intersection clearance,
production rig readiness or mobile performance. Native Unity compilation and
visual inspection remain required. This is a substantial silhouette revision,
with further sculpting, anatomy transitions, civic detail and materials still
needed before final master fidelity.
