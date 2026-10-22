# Ashbound Enemy Integration — First Gameplay Pass
**Status:** implementation contract, not yet playable Ashbound enemy assets.

## Six ordinary campaign-only variants
| Enemy | Combat intent | Fracture Wake usage | Afterecho counterplay |
| --- | --- | --- | --- |
| Rift Forager | Fragile mobile scout; retreats from stronger opposition | None (not initiated) | Not applicable |
| Parted Striker | Short-range aggressive attacker | Once-per-engagement burst step | Post-burst vulnerability |
| Seam Castigator | Area pressure / movement disruption | Telegraph then short disturbance | Recovery turn after burst |
| Anchor Wrecker | Objective sabotage | Amplifies unstable anchor hardware | Vulnerable during setup |
| Afterecho Seer | Medium-range sensory disruption | Volatile short perception spike | Occasionally exposes own position and loses tempo |
| Camp Witness | Supports and directs nearby grunts | Not assumed to be initiated | Support breaks if displaced |

## Implementation guardrails
- Ordinary duplicate-able enemies, anonymous IDs; never enlist them in `catalog.StarterUnits` or player progression.
- Do not author unique special/Apex units or boss-specific biographies in this pass.
- Do not grant supernatural perception: AI may act only on genuinely observed units.
- Telegraphed dangerous attacks and deterministic counterplay; no arbitrary attack misses.
- Keep action budgets, turn initiative, 1 Apex-per-team rule, and save compatibility.
- Establish data-driven ability/profile definitions first, then map appropriate variants to the ten mission beats only after automated objective completion testing.
- Separate 3D model chat owns textures, rigs, geometry, prefabs and animations; this branch does not touch those paths.
- Fracture Wake and Afterecho are canonical story concepts; final numerical balance, cooldowns and visual effects are **not yet canon locked**.
