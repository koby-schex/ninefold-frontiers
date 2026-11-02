# Rift Forager — Depth-correct silhouette review 01

**Status:** diagnostic model-review tooling only. No model is approved for live battles, and this is neither a Unity Game-view render nor a finished art asset.

## What failed in the old comparison

The #181–182 static before/after sheets placed real source triangles on a canvas, but drew them in order of **average triangle camera distance**. An average cannot decide visibility for two surfaces crossing in screen space. That can bury original facial features, cause false triangular patchwork and make anatomy improvements impossible to evaluate reliably. The original ninefold RPG art workflow also needs careful distinction between a mobile actor's genuine on-screen size and inspection-only close-ups.

## What's now reproducible in GitHub Actions

- Existing **C#-exported exact review body** and original OBJ/MTL source remain the only geometries shown. The script still removes the twelve old body pieces and preserves the original living crescent, eyes, mouth and external clothing/gear in its comparison composite.
- **Software z-buffer**: actual triangles are projected using the existing matched camera, and each covered pixel uses barycentric camera-depth interpolation. The nearest surface wins regardless of triangle submission order. No generated/AI details, new textures or image-to-image enhancements.
- The three previous four-view / narrow / tactical sheets retain **exactly the same dimensions and camera framing**. This provides fair before/after and historical comparisons.
- Adds **RiftForager-BeforeAfter-Face-390x844.png**, two matched head-and-shoulder comparison angles at 18° tilt, center y=1.53, half-height .56. **This is an inspection zoom, not an in-game camera.** The tactical sheet keeps its real 58° tilt/half-height 4 and native 390x844 / 320x568 viewport.
- The artifact report explicitly identifies its rendering method; real-mesh static OBJ and generated geometry metrics continue to ship in the existing artifact.
- Executable synthetic tests verify correct visibility for reversed triangle order and *depth-crossing geometry* (a failure case that centroid ordering cannot satisfy), plus invalid-depth and offscreen safety. Both standard Repository checks and the Ashbound workflow run them.

## How to review

Open the latest PR's **Ashbound model source checks** → **Rift Forager connected anatomy comparison** → Artifacts → **rift-forager-connected-anatomy-before-after**. Compare BEFORE/AFTER front and three-quarter **face** images first, then look at the full **close** views and finally the **tactical** views. Treat the close-ups as an anatomy inspection: the Avarin species must stay true to the World Five approved references.

## Limits and next sculpt decision

This renders face-color swatches from existing MTL, not Unity URP Lit; it is flat-shaded and not PBR, animated, rig-deformed, mobile-profiled or canon-certified. Better visibility does **not** increase actual mesh quality. The current underlying Rift Forager still needs professional art direction and substantial sculpt/rig/material refinement.

Use the trustworthy preview to identify which specific facial, garment, shoulder and limb volumes are deficient, then make an independently reviewable physical geometry change rather than guessing and decorating a primitive. Keep the Master Lore & World-Building Archive v26.0 and locked World Five visual references controlling.

No live catalog binding, enemy stats, AI, battle code, existing source geometry, other Ashbound models, Riftwalker or playable units are changed.
