# Great Strider — offline motion study 01

This is an editable, provisional locomotion experiment on the existing study 04
mesh. It does not revise the Master Lore, Production Foundations 1–8, locked visual
masters or the neutral model. It is not a final species gait approval.

The direction is deliberate terrain-crossing movement: a six-second crawl cycle,
0.36 m forward travel, 0.16 m foot lift, and an 80% stance period. A small 0.08 m
body settle makes room for joint articulation. The phase order keeps at least three
feet planted. These are study parameters, not game movement speed or balance stats.

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
- Deterministic preliminary height-based skin weights around each main leg joint.
- Rigid contact pads with smooth swing trajectories and stationary world-space stance.
- Body-following head, cradle and operator, plus a restrained sail response.
- Actual mesh renders with a tracking camera; the camera removes forward root travel.
- A JSON parameter file and repeatable rendered browser review.

The head currently follows the body; independent head motion is deferred. The lower
leg's additional authored bend stays fixed within one bone. Rigid feet can reveal
ankle transition artifacts. The cradle/operator have no independent secondary
motion yet. This is a Python rig study, not an exported FBX, Unity animation,
production skeleton or native Blender rig. The source OBJ remains unrigged.

## Validation and review limits

The script samples 601 poses over three cycles to check bone lengths, reachable
IK targets, minimum support count and zero world-space stance slide. It also checks
visible contact-pad floor clearance at 17 poses, finite deformed vertices, named
limb binding and exact root-relative loop closure. Checks fail the CI job.

Three supporting feet do not prove stability: no center-of-mass simulation, load
transfer, ground reaction forces, full collision testing or anatomical deformation
certification is included. The studio shadow is illustrative and the renderer uses
two-sided shading. These are not Unity or device-performance captures.

Review foot timing, silhouette, knee direction, visible joint transitions and whether
the movement reads as a living terrain-crossing creature. After gait feedback,
refine weight transfer and joint deformation before exporting a production rig or
integrating into battles. No game settings, menus or battlefield units change here.
