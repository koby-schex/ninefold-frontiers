# Great Strider — continuous route study 04

This pass joins movement in one persistent world-space timeline. It remains a
provisional animation study under the Master Lore, Production Foundations 1–8
and locked visual masters. No neutral geometry or Unity gameplay changes.

## Review

Download the combined **great-strider-route-…** artifact from **Great Strider art
review**, extract it and open `index.html`. Choose the artifact without `chunk`
in its name. It contains the complete offline viewer, preview and check report.
The separate **great-strider-motion-…** artifact still contains study 03's four
individual clips for comparison.

The 31.16-second route starts idle, accelerates into walking, stops, turns left,
walks along that heading, stops, turns right, walks again and settles into idle.
Each travel section covers two complete four-step cycles. Its middle maintains
forward speed; it does not stop between those cycles. The turns are -18° and +18°.
Total travel is 2.16 m along the route. Distances and timing are study parameters,
not gameplay statistics or locked species locomotion.

The 192-frame preview is about 6 frames/second, intended for pose/transition review,
not final animation smoothness. It plays once and holds the last pose. Pause,
slow motion, scrubbing and Restart are available. Restart is the only deliberate
reset; no resets are inserted between route segments.

A single actual-mesh camera follows translation while keeping a fixed viewing
direction. A fixed-scale top view shows world travel, heading and foot locations.
Gold marks the heading and lifting feet; blue marks planted feet. The ground grid
and route diagram are inspection aids, not battlefield design or game UI. Previous
three-angle clips remain available in the separate motion artifact.

## How the joins work

`route_great_strider.py` accumulates destination position and heading across every
segment. Foot targets stay in world space during stance; their position and yaw
change only during swing. Two-bone IK follows the body without stretching bones.
A shared global time drives breathing, head, sail and cradle/operator motion, so
these channels do not restart when movement changes.

Walking uses smooth acceleration and deceleration with a constant-speed middle.
Each turn is followed by travel along its resulting heading. Both directions use
the same contact-preserving mechanism. Segment endpoints are fully planted.
This is one scripted route, not arbitrary runtime commands or a Unity animation
controller. It applies the previous movement study's rig and foot-placement approach;
it does not blend or concatenate the study 03 raster clips.

## Checks and limits

Validation samples the skeleton at 60 Hz across the route. It checks reachable
joints, fixed bone lengths, support count, stance translation/rotation, final
position and forward travel along the post-turn headings. Every segment boundary
also compares the actual deformed mesh immediately before, at and after the join,
checking displacement and velocity continuity.

Additional mesh samples verify visible contact transforms, pad floor clearance,
finite geometry, fixed body attachments and shared operator/seat rigidity. Foot
lift/plant boundaries are checked for near-zero speed. The packed viewer's actual
JavaScript is tested for its embedded frame count, end hold, replay, scrubbing,
restart and speed control in a minimal DOM; this is not browser visual certification.

Physical balance, full inter-part collisions, final skinning, production export,
terrain adaptation and Unity/device behavior remain unverified. The preliminary
two-bone legs retain fixed additional bends. This pass does not claim those issues
are solved by continuity checks.

## Regenerate on Windows

From the repository root with Python 3.12:

```powershell
py -m pip install -r BuildTools/Art/requirements-review.txt
py BuildTools/Art/route_great_strider.py
```

The result is `SourceArt/GreatStrider/MotionRoute/index.html`. Rendering can take
many minutes. Use `--validate-only` to run checks without rendering. CI renders
four interleaved chunks of the same global timeline in parallel, then orders and
packs all 192 frames. Packing rejects missing, duplicate or mismatched chunks.
These are render-work partitions, not animation boundaries. Artifacts last 30 days;
all generation code and parameters are tracked in the repository.
