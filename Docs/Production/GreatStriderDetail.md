# Great Strider — detail study 11

This pass refines the actual source model after the first successful Unity review.
The user's approval is of its structural direction, not final art or a canon revision.
Unity review 11 imports this source into a separate versioned folder using the
neutral lighting and material inspection introduced in review 10.

## Source authority

Reviewed the v26.0 reconciliation layer (which retains unchanged v25.3 canon),
v25.3's Great Strider body/head/surface/cradle and Waycaster sections, and the
locked World Five Avarin / Continuance showcase. Master Lore, Production
Foundations 1–8 and locked visual masters remain controlling.

The creature must retain four long articulated legs, a narrow torso, a compact
wedge-soft head on a short neck, paired biological sensory sails, pale tissue
and pale grown stress reinforcement. The Avarin must remain darker and partly
enclosed within a suspended traversal cradle. The Waycaster remains a survey
system. This pass adds no equipment function, markings, species anatomy or lore.

## Changes and limits

- Slightly fuller leg-root transitions on existing rings, tapering to zero before
  the lower limb. The grown sheath is regenerated from the same underlying surface.
- Low-amplitude neck relief with unchanged attachment caps and centerline.
- Deeper cloth folds with zero displacement at the loft ends; wrist/calf fits remain.
- Shallow inset relief inside existing cradle side panels; panel boundaries remain.
- Softer tissue and cloth highlights, and a narrower mineral highlight, using the
  existing MTL finish controls. Palette, eight albedo maps and membrane opacity
  remain unchanged. Smoothness conversion is approximate, not physical calibration.

Topology stays at 184 parts, 40,646 vertices and 80,556 triangles. All part names,
materials and triangle indices remain; no extra draw sections or geometry budget
are introduced. Overall bounds, all four contact groups, head, sensory sails,
operator face/mantle, seat and Waycaster geometry are preserved. Source changes
are bounded to less than 2.5 cm per vertex at six-meter scale.

These are modest surface refinements. They do not weld the separate meshes or
make this a finished sculpt. Final topology, unwrap, rig, animation, LODs,
intersection cleanup, device performance and art/canon acceptance remain pending.

## Verification and review

`python BuildTools/Art/review_strider_detail.py` builds the pinned before source
from merged PR 56 and compares the current source under matched software lighting.
It asserts unchanged topology, palette, opacity and overall bounds; restricts
which parts may change; bounds displacement; and checks that root caps remain
inside their host bodies. It writes `DetailReview/checks.json` and `comparison.png`.
`--check-only` skips rendering. The comparison includes an explicitly labeled
cradle/operator cutaway and a small tactical view. It is not a Unity screenshot.

Existing geometry checks cover outward winding, closed individual parts,
reproducible exports, clear forward eye rays, grounded contact groups and the
open calf region. Existing route and motion checks still run on current geometry.
Historical connection/material comparisons are pinned to the releases they
originally documented; the new detail comparison checks current geometry.

Native check: open **Ninefold > Art > Great Strider Review**, click **Open / create
latest review scene**, and confirm **Detail study 11 / Unity review 11**. Compare
full silhouette, cradle detail and side views. Earlier generated scenes remain.
Native Editor compilation, actual URP highlights and visibility at the final
battle camera require Unity review; automated source checks cannot certify them.
