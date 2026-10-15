# Great Strider — offline motion study 03

This pass adds a small movement library to the existing actual-mesh motion study.
It is provisional animation development under the Master Lore, Production
Foundations 1–8 and locked visual masters, not a canon revision or a final gait.
The neutral study 04 geometry and Unity gameplay remain unchanged.

## Review without Unity

Open the PR's **Great Strider art review** workflow. Download the combined
`great-strider-motion-…` artifact (not an individual `great-strider-sequence-…`
artifact), extract the ZIP and open `index.html` in a browser.

Choose Idle, Start/walk/settle, Stepping turn or Continuous walk. All use synchronized
three-quarter, front and side views. Pause, scrub, change playback speed or Restart.
Idle and continuous walk loop. Start/stop and turn play once and hold the final pose;
Restart intentionally resets the clip and is not an animated reverse movement.
Individual GIFs are included, but the HTML viewer is the authoritative playback
review because some GIF applications impose their own looping behavior.

| Clip | Duration | Behavior |
| --- | --- | --- |
| Idle | 6 s | Four planted feet; 4 mm breathing amplitude, restrained head/sail response. |
| Start, walk and settle | 8 s | One deliberate four-step sequence; advances 0.36 m and settles with all feet down. |
| Stepping turn | 8 s | Four deliberate placements to a new 18° heading; each planted foot retains world position and orientation. |
| Continuous walk | 6 s | Study 02 crawl retained for comparison. |

These durations, distances and angles are review parameters, not combat statistics.
Start/stop is a complete short travel clip, not yet a runtime transition into/out of
the looping walk at arbitrary animation phases. The viewer switches clips with a
reset; it does not claim continuous runtime blending. The turning example is a
small right turn; mirrored and larger turns remain future work.

The camera tracks forward travel while keeping its viewing direction during turns.
Ground lines are a review aid, not a battlefield. The finite ground patch may reveal
edge resets during loop playback; foot planting is checked in world coordinates.

## Implementation

`sequence_great_strider.py` schedules one foot swing at a time. It interpolates the
foot's target position and yaw only during swing, solves two-bone IK against the
moving body, and uses the shared deformation code from `move_great_strider.py`.
Body heading does not rotate planted contacts. Head, sail and cradle/operator
secondary motion are evaluated locally, then inherit the body transform.

`motion-sequences.json` stores the clip timing, step order, travel, breathing and
turn parameters. `strider_sequence_viewer.html` is the offline viewer template.
Generated JSON files contain actual rendered frames; packing embeds all four clips
in one HTML file, with no network calls or account requirements.

The walk deformation refactor was compared with study 02 at four sampled poses.
The original study's validator continues to run in CI.

## Validation

Each clip checks 601 skeleton samples and 33 deformed-mesh samples for reachable
joints, fixed bone lengths, at least three supporting feet, zero planted-foot
translation/rotation, pad floor clearance, finite positions and visible pad stability.
Lift/plant boundaries have near-zero foot speed. Idle and walk close their loops;
start/stop and turning end with all feet planted in the new settled stance. Whole
mesh endpoint checks compare those poses with idle after undoing destination travel
and heading. Horizontal root movement starts and finishes at rest.

The viewer's actual JavaScript is tested with a minimal DOM for selection, pause,
restart, scrubbing, speed, loop wrap and final-pose hold. This is a logic test, not
real-browser visual certification. Rendered poses are inspected separately.

These checks do not certify physical balance, center of mass, full collisions,
soft-tissue behavior or final skinning. Additional lower-leg bends remain fixed
inside the preliminary two-bone chains. No production rig export, Unity import,
battle integration or device-performance testing is included.

## Regenerate on Windows

With Python 3.12 installed, from the repository root:

```powershell
py -m pip install -r BuildTools/Art/requirements-review.txt
py BuildTools/Art/sequence_great_strider.py
```

This renders all four clips and creates
`SourceArt/GreatStrider/MotionSequences/index.html`. It can take many minutes.
To render one clip and later rebuild the combined viewer:

```powershell
py BuildTools/Art/sequence_great_strider.py --clip turn --frames 32 --workers 2
py BuildTools/Art/sequence_great_strider.py --pack
```

Packing requires all four clip JSON files, previously rendered locally or extracted
from the workflow artifact. Use `--validate-only` for checks without rendering.
`node BuildTools/Art/test_strider_sequence_viewer.cjs` runs the viewer logic tests
when Node.js is installed. CI renders the four clips in separate jobs, then packs
the combined artifact only after every clip succeeds. Artifacts are kept 30 days;
tracked source regenerates them.
