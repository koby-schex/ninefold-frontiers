# Active abilities with status effects

Active field profiles now express damage, healing, a standalone status, or a health
effect plus one status on the **same target**. Normal attacks, main abilities and
Signatures all use the existing action, cooldown and readiness rules. All examples
are abstract; this is not approval of any production unit kit or tuning value.

## Authoring and targeting

The health `FieldAbility` constructor accepts an optional status ID and target rule.
A second constructor accepts slot, status ID, target rule and range for standalone
support/debuff abilities. Damage requires `Enemy`; healing allows `Self`, `Ally`
(excludes self) or `AllyOrSelf`. Standalone status profiles support all four rules.
The author chooses appropriate positive/negative values; this API does not infer
whether a particular status is helpful or harmful from the target rule.

Allied healing/support uses the existing authored compatibility list, independent
of faction. Catalog deployment makes all team members compatible across factions.
Explicit `Self` requires no compatibility-list entry. Self-targeting ignores range
between the unit's own attack/target offsets and line of sight; other targets require
range and an unobstructed shot line. Cover reduces damage, never status potency.

The catalog validates attached IDs against its shared `Statuses` definitions.
Deployment advancement/customization preserves target rules and attachments, and
scales only a profile's health amount. Standalone statuses are not scaled by power.
No area, multi-target, status removal ability, movement effect or chain is added.

## Preview and commit

Presentation should call `TryPreviewAbility`, then `TryUseAbility` on confirmation.
Only the slot and target are player selections; values come from registered content.
The command recalculates all validation, never trusting a cached preview. Failures
leave action/cooldown/Signature resources, health and statuses unchanged.

A preview contains a nullable health component and nullable status application:
expected final stack count and refreshed duration, or `Applies = false` if the
health component defeats the target. Damage passives that add the same status
are included in this final stack count. Other passive effects remain discoverable
through the passive definition/state; the preview is not a general combat event log.

The command order is:

1. Validate target, ability availability, range/line of sight, health and status ID.
2. Commit the ability's one primary action and its slot costs once.
3. Resolve health and its survived-damage passive, if applicable.
4. Apply the attached status if the target survives.
5. Evaluate mission outcome after all components finish.

Thus a newly attached armor debuff affects later hits, never retroactively changes
its own damage. Lethal hits grant no status or survived-damage passive to the defeated
unit. A heal with an attached status may target a full-health unit and heal zero;
ordinary healing still rejects full health. Refreshing a capped/full-duration status
is allowed and still spends the action. Duration semantics are unchanged: owner
activation ends count, including the current activation for self support.

Legacy `TryPreviewEffect`/`TryApplyEffect` health-shaped APIs still work for health
profiles with attachments, but reject standalone status profiles before spending.
Use the compound API to display attached status results. Enemy health-based planning
now validates compound profiles and executes their attachments. It still chooses
by damage/healing value; strategic standalone support/debuff selection is deferred.

## Persistence and validation

Battle schema **3** stores health-presence, target rule and optional status ID for
each field profile. Versions 1 and 2 remain readable; their profiles become plain
health effects with the original default enemy/allied targeting. The existing
content revision match remains required, and unknown future versions block writes.
After restore, status references must resolve to saved definitions. Profile ledger
schema is unchanged. Saved commands contain costs, health, passives and attachments
in the same checkpoint; torn writes recover the previous whole checkpoint. No
network service, purchase entitlement or clock participates.

Tests cover target matrices, cross-faction support, full-health support, Signature
limits, stale/invalid commands, preview purity and revalidation, lethal mission
completion, passive ordering, schema migration, progression, enemy execution and
interrupted/ambiguous writes. Unity import, IL2CPP, UI and device testing remain
pending. See [passive/status effects](PassiveStatusEffects.md) for shared effect rules.
