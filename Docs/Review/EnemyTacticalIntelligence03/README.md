# Enemy Tactical Intelligence 03 — Restrained Mission Integration

## Goal
Use the opt-in objective targeting hint from PR #91 in exactly one late-training World Five encounter, while explicitly **not** adding extra pressure to the first eight training missions.

## Integration
- World Five mission 009's hostile recovery threat is authored with the `$carrier` hint.
- The hint resolves against the actual active `RescueAndExtract` objective's designated extraction carrier rather than a hard-coded starter unit. Other hints retain the existing explicit-target behavior.
- The carrier receives just the existing +0.25 target-scoring preference, and **only** while the mission objective is active, the carrier is a supplied, valid perceived opponent, and a normal attack is legal.
- Higher-impact attacks and lethal-hit priority remain in force. Enemy movement, attack count, health, damage, perception, abilities and spawn geometry are unchanged.
- Missions 001–008 are explicitly protected by a no-priority-hints regression test. Mission 010 stays unmodified.

## Fairness limitations
This is not perfect strategic AI. The hint does not create sight or target knowledge; it cannot chase the carrier unless existing pathing and perception allow it, and it does not increase combat resources. **Passing tests does not prove missions are fair or completable in practice.** Future work should measure wins/losses, completion turns and injury rates before any substantial difficulty escalation.

## Save and development scope
No change to V4 profile/revision IDs, save formats, mission map layouts, scoring rules for enemies without hints, UI, Unity scenes, prefabs, materials, animation or Work-owned 3D assets.

## Tests
Regression tests assert that no early-mission enemy gains targeting hints, mission 009 alone opts in, the correct carrier is targeted on otherwise equal attacks, and unperceived carriers are not selected.
