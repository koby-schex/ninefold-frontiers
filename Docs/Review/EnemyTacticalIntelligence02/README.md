# Enemy Tactical Intelligence 02 — Objective Target Hints

## Summary
An optional `objectivePriorityTargets` collection on `EnemyBehavior` lets encounter authors mark up to eight distinct enemy-perceived opponents as strategically important (for example, a rescue carrier or a zone holder). When attacks have the same immediate impact, these targets receive a bounded +0.25 utility bonus in the existing selection heuristic.

## Constraints
- Hints do **not** supply perception. An unperceived target is never added to legal targets.
- Hints cannot bypass line of sight, collision, abilities, range, status, cooldowns or turn economy.
- Lethal attacks remain preferred over nonlethal attacks; higher immediate damage remains preferred.
- Defaults are empty, meaning all preexisting mission actors retain their former behavior.
- Existing mission definitions and spawns are unchanged. This PR adds the authoring capability, **not** fully automatic mission-driven AI inference or active World Five mission retuning.
- No changes to save formats, Unity HUD, World Five geometry, or Work-owned 3D models and assets.

## Tests
Tie-breaking, hidden/unperceived target exclusion, legal selection of lethal priorities, and invalid hint lists. Windows/Linux CI and repository checks must pass before merge; actual dynamic encounter balance remains a future playtest task.
