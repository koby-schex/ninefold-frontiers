# Training Mission Difficulty & Fairness 01

## Intent
Protect a forgiving opening sequence without making promises that cannot be established from static game data. The first three training missions are non-hostile objective lessons; missions 004–010 introduce at most two hostile proxy units per encounter. More capable objective-targeting AI is authored only in mission 009, with the small carrier hint introduced previously.

## Automated authoring checks
- Missions 001–003 have **zero hostile actors** and use stabilization or carried-rescue learning objectives.
- Missions 004–006 and 007–010 have **one or two hostile actors each**. A rescue beat may deliberately reduce numerical pressure rather than continually ramp.
- Ten missions retain the intended three-unit deployment capacity.
- Exactly one objective-priority targeting hint is present in the prototype, on mission 009's hostile actor. Missions 001–008 and 010 contain none.
- Tests remain read-only, preserve the V4 profile/revision and print the authored hostile counts for CI review.

## Limits
These are **structural safeguards**, not win-rate or playability simulations. Zero hostiles does not prove the early missions are clear, understandable or completable; hostility count does not measure ability lethality, spawn fairness, turn budgets, positional traps, or perceived difficulty. Real Unity playtests and representative battle simulations must establish first-attempt win rates, failure reasons, recovery opportunities and completion time.

## Scope
Tests, runner registration and review documentation only. No enemy AI tuning, damage/health changes, mission geometry edits, content revision/save changes, Unity HUD, or Work-owned 3D meshes, materials, animation and prefabs.
