# Health, damage and healing resolution

## Implemented boundary

Each BattleTurnController owns one Health controller. Register health once, before
a unit's first activation: battle-local team, maximum health and armor, optionally
an injured starting health. Health cannot be re-registered to reset damage. Teams
represent sides in this battle, not species or faction restrictions.

The current single-target policy permits damage against opponents and healing of
self/allies. Friendly damage, enemy healing, revival and area effects are not yet
supported. A removed living target (for example, extracted) is ineligible without
being marked dead. Zero-health targets are defeated and cannot receive healing.
No permanent owned unit or account progression is removed.

## Preview and commit

Create a HealthAction from a trusted resolved ability definition: slot, effect kind,
target ID, amount, current targeting verdict and applicable mitigation. Do not accept
raw amounts/slots/"legal" flags from player UI as authority. Binding each unit's
specific allowed effects and ranges to its ability definition is future work.

TryPreview validates active ability availability, registered health, target eligibility,
team relation, supplied targeting verdict and whether healing would help. It produces
an immutable before/after preview without spending resources. TryApply revalidates
and recalculates from current health; it never applies an old preview object.
Only after validation/calculation succeeds does it spend the ability's primary action,
start its cooldown/mark Signature use and apply the health result. The battle is
single-threaded; no callbacks interleave validation and commit.

Rejected actions preserve movement, action availability, cooldowns, Signature readiness
and use, and health. Examples include missing/inactive targets, friendly damage,
enemy healing, full-health healing, unavailable abilities, and supplied out-of-range,
obstructed or incompatible verdicts. This is acceptance of the targeting layer's
verdict, NOT implemented geometry, line of sight or biological treatment validation.
The later targeting system must recompute those checks for each commit.

Defeat sets health to zero and removes turn eligibility, skipping future activations.
It does not itself declare mission victory, end the battle, grant rewards or delete
an owned unit. The mission layer will resolve objective/survival conditions later.

## Provisional configurable damage policy

A battle accepts an immutable DamageRules policy. Default tuning is a hypothesis,
not a locked formula or canonical unit stat:

- Armor scale K = 100. Armor multiplier = K / (K + armor).
- Apply applicable cover and temporary reduction multiplicatively.
- Cap their combined ordinary reduction at 70% by default.
- Round once at the end to an integer; exact halves round upward.
- Positive raw damage has a minimum of 1 by default; raw zero calculates as zero.

Implementation multiplies the numerator before dividing by K+armor to preserve
exact halfway cases. All inputs use bounded integers or validated decimal fractions;
there are no hit/miss or critical random rolls. DamageRules exposes armor scale,
ordinary reduction cap and minimum-one behavior; changing them requires balance review.
No general block, immunity, absorption shield, damage-type resistance, stacking-rule
resolver or status duration is implied. 100% ordinary modifiers still obey the selected
cap/minimum; explicit immunity/absorption will need a separate rule later.

Examples under provisional defaults: 40 raw versus armor 20 → 33 damage; armor 35 →
30 damage. For 40 raw, armor 100, cover 25%, temporary reduction 50%, the shared cap
limits reduction to 70%, producing 12 damage. A raw 36, armor 20 and cover 25% result
is exactly 22.5 before rounding → 23. Zero random outcomes are involved.

Damage preview reports the resolved post-mitigation amount separately from actual
health lost (overkill is clamped at zero health). Healing ignores damage mitigation,
restores at most missing health and cannot revive. Positive effect amounts are required;
an intentionally configured zero post-mitigation result can still consume the action.
This is deterministic reduction, not a miss roll.

## Deferred

Target geometry, full ability-effect definitions, multi-target transactions,
environmental damage, passive modifiers, defensive-effect stacking/durations,
absorption/immunity, revival, mission outcomes and persistence remain separate work.
Readiness based on "after normal/main use" advances only after an accepted health
command; unit-specific damage-taken prerequisites still use the existing trusted
external-event hook and are not automatically inferred here.

Tests run the exact engine-independent Core source in Windows/Linux GitHub CI.
Unity/IL2CPP, animation, touch interaction and device performance remain deferred.
