# Ninefold: Frontiers — starter campaign, ecology and tactical scale guardrails

## Locked design direction
- **World Five / The Fractured Meridian is the sole starting campaign.** It introduces controls and systems through one integrated tutorial (Missions 1–10 in the current prototype). All eight other worlds remain locked at game start and unlock through campaign progression. **None of the other campaigns repeats the tutorial.**
- Tutorial encounters must introduce a **small amount of native creature opposition**, alongside exploration, rescue and terrain problems. Wildlife is not an Ashbound proxy, nor innately malicious; hostility is situational and ecology grounded. Ashbound's first reveal remains strictly **Mission 11**.
- **Unit gameplay scale is normalized across species and sizes.** Standard fielded units should have comparable screen presence and usable tactical footprint; select exceptional units and Apex units may be modestly larger where their identity benefits. Real-world/fictional biological scale does not automatically produce superior reach, collision, health, damage or map control. Large lore creatures may be represented by a gameplay-scaled silhouette or framing rather than occupying an enormous battle footprint.

## Implemented in this PR
Normalize abstract battlefield token silhouettes to a common visual height. This affects only the current placeholder renderer, not character models, rig scale, health, hitboxes, collision or simulation movement.

## Planned content development
1. Add 1–2 situational native-creature encounters in the early World Five tutorial, with distinct roles and minimal grind; prefer a separate upgraded catalog to avoid invalidating V4/tutorial and Act One saves.
2. Protect the first-available-only-World-Five campaign selection rule as multi-world catalogs and unlocks arrive.
3. Add gameplay footprint and rendered-model size budgets with intentional Apex exceptions, validated against phone portrait framing.
4. Preserve no Ashbound encounters in tutorial Missions 1–10 and preserve the mandatory reveal at Mission 11.

This note is a game-production requirement, **not** a claim that tutorial creature fights or other-world unlock gating have been implemented yet.
