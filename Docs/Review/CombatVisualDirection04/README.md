# Ninefold: Frontiers — Combat visual direction 04

## Implemented
- **Settings icon:** replace platform-dependent Unicode gear/old bitmap treatment with an antialiased 96×96 generated icon. It is retained during redraws and destroyed on UI teardown.
- **Battle action effects:** eight pooled non-colliding directional shards burst from the real ability target. Heal effects use the existing friendly color, status-only effects use the existing status color, and damage uses the existing gold impact tint. Burst animation is synchronized with the existing authoritative combat action animation; it never invents new damage.
- **World Five terrain detail:** two deterministic mesh batches of narrow mineral threads and quartz flecks appear on top of existing floor surfaces; no blockers, collision, fake hazards, or hidden gameplay. Other-world neutral study scenes are not changed.
- **Active-unit HUD:** add a legible small portrait alongside the active unit. When an asset provider supplies a production model preview through `FrontiersPreviewRegistry`, use that texture without owning the model or altering its lifecycle. Otherwise show a concise fallback. Increased battle-dock height to support the portrait, including compact and rescue-specific layouts.
- **UI states:** last-loaded scoped battle styles refine buttons and preserve disabled/selected treatment. Core turn flow, double-tap to attack, passive sizing and one-Apex rule remain unchanged.

## Review in Unity 6.3, portrait iPhone viewport
1. Check home settings gear at ordinary and high DPI: no missing glyph or aliased bitmap.
2. Battle HUD on smallest supported portrait height, especially when a unit is inspected, rescue guidance shown or pending action. Ensure no clipping.
3. Test Main and Ultimate on opponents, support healing and status abilities; confirm effect colors follow authoritative results and bursts vanish after playback, including reduced motion.
4. Open World Five survey / fracture battles and check texture details do not imply blocked ground or hide movement/range rings.
5. Run Mission 3 pickup/escape to confirm the carrier indicator, exit guide, and objective completion survive.
6. Check no regressions when future detailed World Five unit previews become available.

## Constraints
This is **visual system integration**, not a photorealistic production art asset drop or comprehensive 3D-model replacement. The parallel unit-model work owns its meshes, rigs and texture production. No game Core simulation, save revision, mission progress or balance is altered. CI does not run Unity GPU/UX playtests.
