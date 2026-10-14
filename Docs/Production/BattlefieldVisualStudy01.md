# Battlefield visual study 01

Status: first runtime environment art pass, not finished production art or an
approved canonical mission location. Units remain abstract test tokens. This pass
applies to the shared fixture renderer, including the mixed-faction test arena;
it does not replace the production opening mission with those test missions.

## Source check — 14 October 2026

Re-read `Ninefold_World_Lore_Archive_v26.0.docx` authority and §26.6 World Five,
and the retained `Ninefold_World_Lore_Archive_v25.3.docx` section **World 5
Sanctuary — Material Palette, Lighting, and Environmental Atmosphere**. The source
continues to distinguish current factual canon, locked visual masters and proposed
execution. v26.0 is the reconciliation layer over the retained base archive.

These sources support restrained mineral-organic materials, smoky pearl/mauve-gray,
weathered ash-silver, charcoal framing, subdued vegetation and fractured-dusk light.
The sanctuary setting's palette is used as a study reference, not evidence that
this battlefield is Nareth Sanctuary. No sanctuary rings, anchor-spire, named new
location, portal mechanism, unit anatomy or story event is invented here. Existing
locked images and approved Home artwork are unchanged. Exact geometry, scenery
placement and lighting values in this pass are proposals for visual review.

## Runtime changes

- Replace the floor's engineering grid with deterministic mineral-grain texture.
- Apply the texture to existing obstacle shells and use muted ash-gray cover caps.
- Add layered irregular rock skirts below the playable surface and outside its
  edges, a far-side escarpment, outer shelves and low dusty-sage vegetation.
- Group decoration into four mesh renderers/material groups. These are generated
  once per loaded attempt, not per frame. The shared 128×128 mipmapped texture is
  generated once per renderer lifetime; no downloads or external art packages.
- Add one shadowless mauve fill light and a dusk background color. Actual light
  response depends on the installed URP settings and needs Unity inspection.
- Preserve exact legal rectangle, cover boxes, slow-ground bounds, objective
  positions, movement picking, camera framing, action previews and all Core rules.
- Keep low ash-silver perimeter lips and existing slow-ground tactical markings.
  Decorative scenery has no colliders and does not expand movement or camera bounds.
- Destroy generated meshes when replacing terrain and all generated resources on
  renderer disposal. The light is owned by the terrain object and hides with it.

## Verification and limitations

Repository metadata/art validation and whitespace checks pass locally. Core CI
covers simulation and persistence; it does not compile the Unity presentation
assembly. There is no Unity installation in this execution environment. No native
Unity screenshot, frame-rate result or device quality claim is supplied.

The scenery is a bounded procedural art study. It is not the final premium terrain,
a full authored opening mission, or finished unit art. Detailed meshes, authored
material maps, atmosphere and quality/performance tuning remain follow-up work.

## Unity review after pulling

Use the existing launcher; no scene regeneration or save reset is required.

1. Resume a saved battle. Confirm the new ground and outer geology appear and
   units still occupy their saved positions. Existing layouts remain intact.
2. Check portrait at 540×960 and 540×1170: the far escarpment must not obscure units,
   routes, health strips, objective markers or legal ground. Some distant scenery
   can crop because the camera intentionally prioritizes the legal arena.
3. Move toward each boundary and around cover. Confirm partial moves, target range,
   obstruction checks and slow-ground costs remain unchanged.
4. Return Home and resume: no stray light/scenery should remain on menu screens.
5. Finish the mission, claim and start another. Check its different layout and
   look for missing-material pink surfaces, visual seams or growing memory use.
6. Capture a Game-view screenshot and a diagnostics report. Approve or revise the
   composition before treating this as a visual benchmark for the franchise.
