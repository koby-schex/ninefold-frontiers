# Rift Forager — Connected Anatomy Sculpt 06

**Status:** provisional, review-only physical geometry. NOT a locked canon model, production rig, native Unity screenshot, or in-game asset promotion.

## Problem grounded in the PR #183 real depth-buffered comparisons

The new face sheet reveals broad triangular forehead/cheek facets and an over-rounded neck-to-chest transition in the **actual reconstructed anatomical mesh**, independent of the static render's occlusion errors. The Avarin's original living crescent mantle, temple relief, paired eyes, mouth and worn field equipment survive intact, but the body still reads as a low-resolution rough sculpt.

## Changes made to real source geometry

1. Increase connected-anatomy sampling resolution from **0.029** to **0.023 world units**. This changes the actual generated indexed rest mesh that Unity skins and the CI C# OBJ exporter generates, rather than merely increasing texture resolution or adding props. Do not claim this is final high-poly retopology.
2. Add a small **localized lateral lower-face/mandible contour** inside the existing analytic field, tapering the existing jaw side without moving the original paired eyes, forehead ridge or mouth. It is symmetrical, narrow in location and falls to essentially zero outside the face.
3. Slightly narrow and smooth the **existing natural neck** through its original connected surface field, without introducing a separate ring/prop, changing biology or affecting the living crescent mantle.
4. Bump the generation revision so Unity's `ArtReview` tool rebuilds the changed candidate; originals and approved live visual catalog stay untouched.

## Regression gates

Executable Windows/Ubuntu tests check: more detailed actual mesh triangle count within a review budget (32k–90k); symmetrical local jaw contour and bounded falloff at source eye, mouth and torso anchors; existing skin-weight continuity, watertight manifold, UV seam preservation, normal finiteness, pose anatomical bounds, and source feature clearance. Repository structural validation enforces the new source generator and tests.

The **Ashbound model source checks** GitHub workflow exports the *exact changed C# source* to a static OBJ and provides **depth-correct before/after face and battle-camera sheets** plus metrics under the same `rift-forager-connected-anatomy-before-after` artifact. Those sheets still compare against the original detached-body source, *not* an exact #183-to-#184 delta. To judge this pass, compare the latest artifact with the #183 artifact at identical face and tactical sizes.

## Explicit limits

This is a modest underlying shape refinement, **not** a major hand-sculpted redesign, professional material pass or full game-ready character. The original geometry still needs more detailed sculpt direction, true material/skin/cloth work, native Unity rig/animation evaluation, mobile performance measurements and explicit visual/canon approval before any runtime promotion.

No edits to missions, player units, enemy abilities or stats, the six active Ashbound NPC gameplay candidates, original checked-in SourceArt OBJ/MTL, Riftwalker, or the Master Lore & World-Building Archive v26.0.
