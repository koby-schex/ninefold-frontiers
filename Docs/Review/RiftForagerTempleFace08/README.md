# Rift Forager — Avarin Temple-Face Study 08

**Status:** provisional review-only geometry and Unity look-development. Not official locked canon, not a finished character, not gameplay or phone performance approval.

## Evidence and direction

The depth-correct static #185 face sheet shows the exact reconstructed C# anatomy is **rounder and broader than the original source**. Its cheek/temple volumes make it resemble a human head, in conflict with the desired distinct Avarin design language. The source eye, mouth and brow geometry, and the original living crescent sensory mantle, already exist at their authentic coordinates; the current source does **not** authorize adding eyes, horns, new biological membranes or Ashbound mutations.

This pass **reshapes the one continuous physical face** instead of adding ornaments, fake shading, separate sculpt primitives, or invented anatomy. The resulting review art remains unapproved until compared against the locked visual masters and Master Lore & World-Building Archive v26.0.

## Changed actual 3D surfaces

- Main skull reshaped from a broad .105 × .143 × .091 ellipsoid to a **longer narrower .088 × .164 × .086 anatomical field**, with anterior positioning adjusted to retain all original eye/mouth/brow clearance.
- Smaller, better blended **temple and cheek planes**, more tapered jaw volume, and a narrower organic neck. A shallow symmetric TemplePlane is integrated into the same SDF, affecting only the lateral temple, not the source landmarks.
- Retains the exact previous **.023-unit connected mesh resolution**; this is shape direction, not a useless extra triangle-density increase.
- New **surface-05** fingerprint creates a fresh Unity ArtReview source geometry. Original face landmarks, crescent mantle, old SourceArt OBJ/MTL and new tailored fieldcloth are unchanged.
- The compiled .NET tests now check measurable lateral upper-skull slimming while the existing tests still enforce closed topology, finite normals, 4-bone skin weights, UV seams, genuine skull interior, facial feature clearance and tailored clothing clearance.

## Provisional materials, not invented hues

- Upgrade the Unity ArtReview study maps from **256 × 256** to **512 × 512**. This is a review-resolution step only and does not establish the game's eventual texture budget.
- Replace obviously repetitive pure sine patterns with deterministic **tileable multi-scale smooth noise**; keep subtle directional fabric weave, natural skin microvariation, mantle relief and worn hardware texture differences.
- Preserve all eight existing material color swatches and URP Lit usage. No new canonical palette, Arcana symbol, new species feature or shiny fantasy details.
- The repository structure gate checks the correct generator wiring and model/texture revision. The existing Unity source hash includes the PBR source, so these changes cannot silently reuse an old ArtReview prefab.

## Critical review boundary

The GitHub Actions **Ashbound model source checks → Rift Forager connected anatomy comparison** renders the *exact changed C# mesh* with the original static OBJ accessories, allowing a fair new-versus-#185 **face and tactical silhouette** comparison. It uses source MTL colors and **cannot evaluate the updated Unity PBR textures, actual shadows or animated joint motion**. Inspect those only in a native Unity ArtReview session. Do not claim Unity verified the texture appearance or six-pose interactions from .NET CI.

**All live battle prefabs/catalogs, gameplay, stats, missions, source art, original creature bonds, Riftwalker and other Ashbound archetypes remain unchanged.** Promotion to gameplay still requires explicit art approval, a proper retopology/material/animation review and on-device performance validation.
