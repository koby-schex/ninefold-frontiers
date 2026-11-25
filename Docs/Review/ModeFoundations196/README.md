# Ninefold: Frontiers — mode foundation PR #196

## Confirmed direction
- **Horde** is the player-facing name. Do not substitute “Survival.” World Nine association is a future art/lore direction, not an invented World Nine campaign or environment in this PR.
- **World Collision** reuses the existing six-to-eight-squad battle contract; the pilot tests seven deployment positions and mixed faction ownership. No unauthorized canon story, characters or world collision visuals.
- **Pathweave** is a **working title** for a Ninefold-native alternative to generic challenge missions. The design is not a gameplay modifier checklist or a reskinned campaign fight. It is about consequential choices over inter-world crossing routes and the records/protection/isolation that follow them. Current playable pilot is confined to known World Five fracture paths: stabilize a junction, choose shelter **or** containment, then play an entirely different mission in a distinct authored map. A completed branch locks the alternative in the same profile. No made-up law of Arcana, invented travel technology, unapproved region, named boss, or premature protected mystery is introduced.
- **All Nine Worlds** retain independent campaign slots. World One is **The Brightfall Reach**; World Two is **The Shattered Heights**; World Five is **The Fractured Meridian**. Only World Five has 100 authored campaign stages. Other slots are not fabricated as playable content.
- **PvP deliberately excluded**, including networking, matchmaking and PvP-centric balance.

## Actual implementation
- **13 new, real standalone missions** added to the existing live World Five catalog: six Horde assault waves, five Pathweave encounters with two mutually exclusive endings, and two World Collision large-battle exercises.
- Each Horde wave is a *separate*, saved battle with increasing distinct adversary count (2–7), new per-wave map dimensions and bounded first-clear/replay rewards. Full continuous reinforcements, persistent within-session health and endless waves do **not** exist yet: this is a gated wave-ladder pilot. Clearing a wave unlocks the next; earlier waves replay.
- Pathweave route choice is saved by the **existing durable battle victory receipt** and enforced in the core mission gate and UI even after reopening. Failure does not commit a choice; a different branch can be selected until a victory. The shelter outcome protects a traveler; containment defeats armed defenders. Outcomes also differ in map geometry and sequential objectives.
- World Collision is **not** a small standard arena renamed. It actually demands a 6–7 unit cross-faction squad in a larger authored arena, unlocks only after the full World Five completion milestone and sufficient owned units, and plays a two-stage route/defense encounter. Existing one-Apex-per-battle rule remains.
- All these pilots use the same health, abilities, AI, objectives, initiative, battle saving, player progression, account wallet and offline systems. Their availability predicates are enforced by the core, not merely disabled buttons. No new currency, ads, PvP code, paid mechanics or reset of existing campaign progress.
- Existing saves retain the exact **ContentManifest.SaveRevision** and all **100 original campaign definitions, reward policies and progression**. Mode pilots are strictly additive standalone entries in the existing profile. Existing replay/claim protection remains in place.

## How Pathweave should become genuinely Ninefold-specific
For the complete game, do **not** copy generic single-arena challenge missions. Extend the route-choice pilot into a nine-world *path consequence graph*, each node backed by actual lore-approved world identities, biological/Arcana/environmental constraints and factions. Prior choices should alter the map, available companions, which crossing needs protecting, downstream objectives, and archival record of the route that survived. Avoid simulated “alternate worlds” with palette swaps and avoid assuming all nine worlds share World Five’s fracture ecology. Every new world profile must pass archive continuity review before its mechanics become playable.

Player-facing clarity goals: short route-choice summary, two unambiguous outcome paths, saved chosen path marker, controlled replay, predictable reward ceilings and no opaque currencies. Future development must support repeatable distinct journeys without letting a reroll erase or duplicate a prior payout. Do not claim the two-branch pilot already supports a nine-world network.

## Balancing and quality gates
- New **WorldFiveBalanceAudit** reports opponents, objective types, route stages, variable fields, first-clear and replay supplies in ten-mission windows across all 100 original missions.
- Intentionally **reports release blockers** for abstract fixture content and duplicate playable stats; this PR does not falsify production readiness or silently edit stats in existing saved battles. Actual balance tuning and new combat identities are planned next.
- Core tests cover old checkpoints, profile preservation, all new legal maps, actual wave unlock gating, both route choices and their persistence, large squads after the real finale reward, and real live-AI Horde battle input. Unity/iPhone visual and performance testing remains necessary.

## Follow-on PRs
- **#197 (planned):** tuned character kits/stats, meaningful enemy behaviors and campaign pacing on a deliberate new content/battle revision with safe migration.
- **#198:** heavy 3D model and animation refinement against locked canon, with actual-device views.
- **#199:** atmospheric environments, World Collision-specific art treatment and battle VFX.
- **#200:** native mobile polish and mode UX/performance.
- Next mode-content passes: genuine continuous Horde wave controller with inter-wave choice/persistence, World Collision multi-faction event rules, and nine-world approved Pathweave consequence network. No PvP implementation.
