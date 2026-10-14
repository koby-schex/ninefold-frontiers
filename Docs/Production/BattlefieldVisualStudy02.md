# Battlefield visual study 02 — PT-043

Continuation of study 01's proposed World Five palette; no new named site, lore,
mechanism, faction anatomy or approved visual master. Authority remains the source
check in BattlefieldVisualStudy01.md and the FirstMissionArtBrief.md constraints.
This is still the abstract integration fixture, not the canonical opening mission.

## Changes

- Flat irregular mineral slabs replace the plain walking surface. Three restrained
  shades, thin seams and consistent world-scale texture mapping add material detail.
  A solid floor remains beneath the seams: these are not holes, hazards or elevation.
- Cover uses solid-looking faceted sides and beveled ash-ceramic crowns, with face-
  projected texture coordinates. All vertices stay inside each authored obstacle
  box. The full box remains the navigation/line-of-sight authority, including the
  small visually beveled top corners. This is an explicit collision abstraction.
- Additional lower rock strata soften the outer plinth silhouette. A lower-contrast
  second horizon layer adds depth behind the existing far-side scenery.
- No scenery is added in walking lanes. Slow-ground tactical markings, objectives,
  range previews, health bars, camera framing and the minimal HUD are retained.
- Local revision is PT-043; diagnostics identify study 02 and actual mesh count.

## Runtime limits and lifecycle

Surface subdivisions are capped at 32×32. Terrain details use three paving batches,
two cover batches (omitted for maps without obstacles), and five surroundings
batches. Maximum for current fixture layouts: ten generated mesh renderers, using
shared materials. This is a renderer count, not a measured total draw-call budget;
URP passes and shadows can add work. There is still one shadowless fill light.
Geometry is generated only on attempt changes; no per-frame mesh generation or
network dependency. Owned meshes/materials/textures use the existing cleanup path.

## Verification boundary

Local metadata and whitespace checks passed. Core CI checks rules and persistence;
it does not compile the Unity presentation assembly. Code review checked triangle
winding, bounded paving tessellation, cover coordinates and resource ownership.
Unity import, native visual composition, material appearance, occlusion, input and
mobile performance remain unverified. No generated concept image or offline render
is presented as an in-engine screenshot. This is a refinement pass, not a claim of
finished premium art.

## Combined later playtest

After pulling, launcher/Home/Settings should show PT-043. Resume a battle; the report
should say study 02, with ten meshes for maps containing cover (eight without).
Check a low camera-facing cover side and its beveled crown; check that floor seams
don't suggest blocked movement. Move/attack around all obstacles and confirm costs
and obstruction match prior behavior. Check 540×960 and 540×1170 portrait for distant
scenery hiding health or objective markers. Also inspect the older large saved-map
layout if one remains. Return Home/resume and complete/start another mission to
check scenery cleanup and unchanged progression. Capture the Game view and report
for visual refinement. No save reset or scene regeneration is required.
