# Great Strider — offline motion study 02

This is an editable, provisional locomotion experiment on the existing study 04
mesh. It does not revise the Master Lore, Production Foundations 1–8, locked visual
masters or the neutral model. It is not a final species gait approval.

The direction is deliberate terrain-crossing movement: a six-second crawl cycle,
0.36 m forward travel, 0.16 m foot lift, and an 80% stance period. A small 0.08 m
body settle makes room for joint articulation. The phase order keeps at least three
feet planted. Study 02 adds a 3.5 cm lateral load-shift amplitude and 8 mm body
bob, with quintic step easing and soft lift/plant boundaries. These are study parameters, not game movement speed or balance stats.

## Review without Unity or Blender

On the PR, open **Great Strider art review**, then download the
`great-strider-motion-…` artifact. Extract the ZIP and open `index.html` in a browser.
It is self-contained and works offline. Use Play/Pause, the frame slider and the
0.25× / 0.5× / 1× speed selector. `walk.gif` offers a simpler looping preview.
`checks.json` records the numerical validation results. Artifacts expire after
30 days; the tracked source regenerates them.

To reproduce on Windows with Python 3.12 installed, from the repository root:

```powershell
py -m pip install -r BuildTools/Art/requirements-review.txt
py BuildTools/Art/move_great_strider.py
```

Outputs appear in `SourceArt/GreatStrider/MotionReview/`. Rendering may take several
minutes. Use `--validate-only` to run the motion checks without producing frames.
`--frames 32 --workers 2` is the default; no network is used during generation.

## What the prototype contains

- Four two-bone IK chains, with explicit rejection of unreachable targets.
- Quintic blended skin weights around each main leg joint and the planted ankle.
- Rigid contact pads with smooth swing trajectories and stationary world-space stance.
- Small head pitch with a blended neck, inherited sail motion and a delayed sail response.
- Shared cradle/operator pitch, fixed body attachment pads and a blended load frame.
- Synchronized three-quarter, front and side mesh renders with a tracking camera.
- World-fixed ground grid (0.36 m longitudinal / 0.75 m lateral spacing), repeated
  every stride for loop playback. It is a review aid, not a battlefield design.
- A JSON parameter file and repeatable rendered browser review.

The lower leg's additional authored bend stays fixed within one bone. The foot
contacts remain rigid, and the preliminary skinning still requires refinement.
Head pitch is limited to 0.012 radians, sail response to 0.008 radians and shared
cradle/operator pitch to 0.003 radians. These small art-directed responses are not
a physical suspension simulation. The operator stays seated relative to the cradle.
This is a Python rig study, not an exported FBX, Unity animation, production
skeleton or native Blender rig. The source OBJ remains unrigged.

## Validation and review limits

The script samples 601 poses over three cycles to check bone lengths, reachable
IK targets, minimum support count and zero world-space stance slide. It also checks
visible contact-pad floor clearance at 17 poses, finite deformed vertices, named
limb binding and exact root-relative loop closure. Additional boundary checks verify
near-zero lift/plant velocity; assembly checks verify operator/seat rigidity and
fixed body attachment pads. Checks fail the CI job.

Three supporting feet do not prove stability: no center-of-mass simulation, physical load
transfer or ground reaction forces, full collision testing or anatomical deformation
certification is included. The studio shadow is illustrative and the renderer uses
two-sided shading. These are not Unity or device-performance captures.

Review foot timing, silhouette, knee direction, visible joint transitions and whether
the movement reads as a living terrain-crossing creature. After gait feedback,
refine weight transfer and joint deformation before exporting a production rig or
integrating into battles. No game settings, menus or battlefield units change here.
