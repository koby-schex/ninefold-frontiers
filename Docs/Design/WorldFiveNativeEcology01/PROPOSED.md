# World Five — Native Ecology & Enemy Design Pass 01
**Status: PROPOSED, NONCANONICAL — approval required before adding to Master Lore v26.0 or replacing encounter fixtures.**

## Controlling continuity
World Five is **The Fractured Meridian**; the Avarin and the Continuance are its featured playable civilization. Stable Anchor Basins and Pale Corridors contrast with volatile Driftlands, Fractured Rim, and Lost Marches. Existing confirmed trust-based native partnerships are **Ilyth (Great Strider), Olyra (Living Veil), Vaskel (Riftwalker), and Tervan (Burdenbeast)**. These are not default enemies and should never be made hostile simply because they are wildlife. Nethra's true nature and Serai Venn's disappearance remain mysteries. No inter-world-government capital, no universal evil Below, no random Arcana spellcasting.

## Ecological rules (proposed)
1. Native organisms sense short-horizon changes in terrain stability through pressure, vibration, light scattering, and/or embodied Arcana-sensing adaptations; none inherently predicts the future.
2. Creature behavior reflects hunger, nesting, territorial boundaries, migration, or interference with routes. Hostility is situational, not an innate moral alignment.
3. Anchored sanctuaries are relatively protected; outer corridors, transition edges, and Lost Marches contain more dangerous encounters.
4. Displaced creatures may intrude into established routes after shifts; Continuance teams prioritize warning, diversion, and rescue where feasible.
5. No creature is assigned a permanent faction class, and no species automatically fills a generic Frontline/Damage/Support role.

## Creature candidates (names and designs are NOT canon)
| Working designation | Habitat/niche | Silhouette/anatomy | Tactical behavior | Nonlethal story rationale |
| --- | --- | --- | --- | --- |
| **Sillrunner** | Pale Corridor margin and Driftlands edges; small skittish omnivore | Six low-set legs; long segmented lateral sensory fins; charcoal-gray skin with pale dorsal folds | Erratic burst relocation; reluctant counterattack if cornered | Migrating herds cross a broken route; protect scouts without requiring extermination |
| **Riftback Grazer** | Anchor Basin outskirts and mobile mineral flats; massive browser | Four stout legs and a high transverse mineral sail grown from dermal ridges, not armor plating | Slow lane denial; stampede knockback when startled; high body mass | Frightened herd blocks evacuation; guide it away from crossings |
| **Seamstalker** | Fractured Rim cave shelves; ambush predator | Lean six-limbed body, paired anchor-claws, split sensory crown; no wings | Short lateral reposition and exposed-flank attack; telegraphed strikes | Parent defends a den exposed by route collapse; bypass or contain it |
| **Hollow Chorus** | Lost Marches weathered hollows; colony of small linked organisms, **not** a single sentient monster | Distributed palm-sized soft-bodied clusters; translucent resonance membranes | Area disruption and temporary navigation interference; vulnerable clusters can disperse | Colony uses route-hum as mating communication; disturbed by anchor equipment |

**Names, biology, powers and locations above remain editable working proposals.** Avoid presenting them in World Guide, storefront lore, and public game descriptions until user approval and archive reconciliation.

## Proposed encounter uses, pending approval
- Missions 001–003: guide, rescue, and environmental caution; do not add hostile animals where the tutorial currently teaches interaction.
- Mission 004 secure objective: a territorial Sillrunner group or movable environmental hazard could contest a route with an explicit retreat condition.
- Mission 005 protect: protect a stranded traveler from a panicked herd / terrain shift, with optional nonlethal deterrence.
- Mission 006 survive: weather and ecosystem disruption, not endless hostile spawning.
- Mission 007 defeat: Seamstalker den-defense, with an optional nonlethal route in later balancing.
- Mission 008 control: route pressure and displaced fauna, not generic enemy waves.
- Mission 009 rescue: ambush risk near exposed margins of a corridor; allow clever diversion.
- Mission 010 stabilize: environmental instability climax with ambiguous ecological fallout; do not reveal the protected route-crisis mystery.

## Integration sequence
1. Explicitly review and approve/revise names, native ecology and silhouettes.
2. Reconcile approved entries into a **new version** of the Master Lore archive under World Five (never silently overwrite v26.0).
3. Add per-creature lore-owned enemy specs (anatomy and scale, behavior, habitat, perception, movement, attacks, status rules, AI intent) as separate structured data.
4. Implement player-facing telegraphs, counterplay and balanced ability kits before swapping mission `fixture-npc` content.
5. Coordinate 3D visuals/rigging with the other development chat on separate files/branches; record exact authorized reference images.
6. Verify live missions 001–010 and difficulty on iPhone portrait plus Windows/Ubuntu core CI.

**A creature is an encounter participant, not necessarily a kill target.** Design primary goals as protect/escape/divert/control where applicable, matching the actual available game objective mechanics instead of claiming new options already work.
