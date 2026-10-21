# Enemy Tactical Intelligence 01

## Change
When an enemy has two otherwise equally effective offensive actions, it now slightly favors the target with the greater available health-effect potency in its configured ability kit. The bonus is bounded to at most 0.5 scoring points so that an additional whole point of real immediate effect wins; lethal-hit precedence is unchanged. Healing/support scoring, movement, attack ranges, guard areas, initiative, and perception input are unchanged.

This is a small, deterministic offensive threat tie-break, **not** objective-aware AI, simulated multi-turn planning, or an adaptive difficulty system. Future work may incorporate explicit target roles, mission goals and defensive positioning after playtest review.

## Tests
- Equal impact attacks favor an 80-potency opponent over a 10-potency opponent regardless of ID ordering.
- A lethal hit against a low-potency target outranks a nonlethal hit against a powerful target.
- Calling the plan/preview does not spend an activation or damage a target.

## Boundaries
Only `EnemyBehavior.cs`, the new pure .NET test suite, test runner registration, and this document. No changes to saves, mission definitions, canon, 3D assets, Unity visual components or any Work-chat-owned art.

Windows/Linux CI must pass before merge; no Unity playtest is claimed.
