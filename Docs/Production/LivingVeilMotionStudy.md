# Living Veil / Olyra — motion study 03

## Purpose and canon

Evaluate the model in movement: resting idle, protective deployment, protective
idle and retraction. The controlling v26 reconciliation, retained v25.3 Olyra
anatomy, Production Foundations 1–8 and locked World Five master are unchanged.
Olyra remains an independent, buoyant organism with five membrane lobes. It does
not flap conventional wings. Protection is a deliberate cooperative movement,
not an instant force field. No gliding/suspended traversal is implemented.

Exact timing, route and amplitudes below are provisional animation choices,
not locked anatomy, ability rules, action costs or game balance.

## Movement

- Resting and protective idle: four-second study loop; 2.5 cm vertical amplitude
  and 0.6% membrane radial flex, with ribs/veins following the same deformation.
- Deployment: 3.6-second study sequence. Rise 1.35 m while folded, unfold overhead,
  move 1.70 m sideways / 0.35 m forward above the partner, then settle to shelter.
- Retraction: exact reverse path, separating shelter from the partner before folding.
- Avarin and Veil Harness stay in the resting source location throughout. Feet
  remain planted; no sliding, unanimated walking or partner teleportation.
- Idle loops join their corresponding transition endpoints. Transitions play once
  and stop at the end; only idle clips can loop, avoiding a deployment snap-back.

The static protective source places the partner closer to the organism. The
motion viewport retains the resting partner location and offsets the organism
instead. Its final pose has the same partner/organism relationship as the static
protective pose, translated together in world space.

A first deformation test found a vein cross-section collapse. Protective strand
rings are now cyclically reindexed to align the two poses. This preserves their
static surface geometry, counts and closed topology while repairing correspondence.
It does not replace a future rig, skinning, sculpting or membrane deformation pass.

## Review in Unity

1. Pull merged changes and open Unity 6000.3.21f1; finish import/compilation and
   configure the existing URP foundation if not already done.
2. Open **Ninefold > Art > Living Veil Motion Review**, or use the link in the
   existing Living Veil Review window.
3. Click **Load motion study**. Select RestingIdle, Deploy, ProtectiveIdle or Retract.
4. Play/pause, scrub, reset, or loop idle. Compare front, side and tactical views;
   use orbit/elevation/zoom for overlap inspection.
5. Close the window to release its temporary meshes, materials, texture and camera.

The viewport uses a private PreviewRenderUtility scene, neutral lighting and
transient meshes/materials. It never rewrites source assets, creates asset files,
modifies the active scene, touches a save/profile, or registers a UI preview
provider. Entering Play mode or assembly reload releases the preview. Reload source
recreates it; reopening does not accumulate persistent generated prefabs.

URP rendering is explicitly enabled for the preview utility. This uses Unity's
editor preview APIs; native compilation and actual viewport rendering remain a
required Unity verification, since Unity Editor is not available in CI here.
API reference: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/PreviewRenderUtility.cs

## Files and verification

- `LivingVeilMotion.cs`: pure C# editor motion math, also compiled into core tests.
- `LivingVeilMotionReview.cs`: private Unity viewport and lifecycle cleanup.
- `BuildTools/Art/living_veil_motion.py`: matching Python motion reference.
- `SourceArt/LivingVeil/motion-study.json`: durations and cross-language samples.
- `BuildTools/Art/review_living_veil_motion.py`: sampled geometry checks and renders.

The shared OBJ parser now retains each vertex's source-part name. This lets the
viewport keep the partner fixed even where opaque materials combine body and
creature parts into the same render section. It does not change static geometry,
material grouping or the existing Great Strider importer behavior.

Run `python BuildTools/Art/review_living_veil_motion.py --render` after installing
the existing art-review requirements. GitHub's Living Veil artifact contains four
GIFs, four contact sheets and motion-checks.json. These show actual source geometry
with software nearest-shell transparency; they are not native Unity captures.

Checks sample 121 deployment frames and 33 frames per idle, with reverse-path and
loop endpoint comparisons; validate expanded partner-box separation, finite points,
face area, outward volume and bounded frame-to-frame displacement. C# tests compare
motion parameters and vertex probes to the Python reference, import both OBJ files,
verify topology correspondence and check the fixed-partner/final-shelter result.

Limits: sampled checks do not prove continuous collision freedom or exhaustive
lobe self-intersection. Intentional lobe overlaps still require native inspection.
No production Animator, AnimationClip, skin weights, LODs, mobile-performance
certification or battlefield/Collection integration is claimed. This CPU-deformed
editor study is not a shipping animation implementation.

The UI preview contract from merged PR #68 is documented in ModelPreviewHandoff.md;
its implementation and all UI files remain untouched by this pass.

## Superseding movement revision — glide 08

The staged transition described above is historical. User Unity feedback requested
continuous, less robotic motion and a slightly smaller protective canopy.
[LivingVeilGlideStudy.md](LivingVeilGlideStudy.md) documents the overlapping glide,
8% radial-reach reduction, unchanged idle curves, 4.1 s retraction and 308-frame
validation. Native playback acceptance of the revised motion remains pending.
