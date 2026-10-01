# Passive triggers and temporary effects

This headless slice implements one self-targeted passive rule per unit, bound to a
shared status definition. It works for player units, enemy templates and mixed
factions. It introduces no production Ninefold abilities, names or balance values.

## Event order

- `OwnerActivationStarted`: fires after the scheduler assigns the activation and
  increments its counters, before `BeginNextActivation` returns. It does not spend
  movement or the primary action and cannot grant another activation.
- `OwnerSurvivedDamage`: fires after an accepted health command commits its costs
  and actual health loss, before mission evaluation. The target must survive with
  positive health. Previews, rejected commands, zero damage, healing and lethal
  hits do not trigger it. It affects subsequent effects, never the triggering hit.

Passive results only apply status state. They do not emit damage/healing or invoke
other passives, preventing recursive reaction chains. A rule can activate for each
successful event; there is no once-per-round rule in this slice. Reload restores
state directly and never replays events. There are no wall-clock timers.

## Status contract

A `StatusDefinition` specifies signed armor and power basis points, duration,
stack cap and either `Refresh` or `AddStackAndRefresh`. Power applies to both damage
and healing. Each owner has one entry per status ID, shared across applications:
no per-source stacking. Refresh resets the whole entry's duration; additive mode
adds one stack up to the cap and also refreshes at the cap. All stacks expire
together. Refresh-only definitions require a cap of one.

Duration counts **owner activation ends**, including a currently active turn when
the effect was granted during that turn. Other units' turns and round boundaries
do not decrement it. Start-of-activation duration one lasts through that activation.
Ending early still counts. Removal/defeat/extraction clears all active effects on
that owner. Ending the battle freezes remaining state for result inspection.

`Statuses.Apply` and `Remove` are trusted simulation operations for authored rules,
not arbitrary client commands or cost-free player controls. They reject terminal
battles. Unknown definitions reject; applying to an ineligible owner returns false.
Definitions/passives must be registered before the first round. Active field abilities now validate targeting and commit action costs and effects
as one command; see [active ability effects](ActiveAbilityEffects.md). UI remains
deferred.

Temporary values are summed across IDs and stacks, then clamped to +/-50% per stat.
Individual definitions are limited to +/-50%, 1–10 stacks and 1–100 owner activations.
These are provisional engineering bounds, not locked unit balance. Armor/power
scale the deployed base (including progression/customization) on demand, with the
signed delta truncated toward zero. Power remains at least one, armor at least
zero, and integers saturate at `int.MaxValue`. Removing an effect restores the
unmodified baseline; it never reverses a previous rounded calculation. Maximum
health, initiative, movement, chance-to-miss and turn frequency are unaffected.
Use `EffectiveArmor`/`EffectivePower` for derived stats; health definitions and
ability profile base values remain unchanged. Existing mitigation limits still apply.

## Content and saves

`AbilityContent` accepts a `PassiveDefinition`; catalog `Statuses` resolves its
status ID and registers all bindings before activation. Explicit inert placeholders
remain available. `AbstractContentPackage.Create(true)` opts into an engineering
focus passive and a distinct content revision. Default fixture behavior is retained.
External signature readiness remains unsupported; passive events do not satisfy it.

Battle snapshot schema **2** appends definitions, owner passive bindings and active
stacks/durations. Schema 1 loads with empty status state and is upgraded on the next
save. Checks reject unknown IDs, duplicate entries, invalid counters and effects on
removed/defeated owners. Old content revisions still need their existing revision
match; schema compatibility does not bypass content compatibility. The local record
envelope accepts supported battle versions (currently 1–3), and unknown future versions still block writes.
Profile schema is unchanged. Checksums detect corruption, not malicious save edits.

`MissionFlow.Execute` checkpoints a detached command result. A torn write recovers
the previous entire hit/action/passive state; an acknowledgement lost after a durable
write reloads the committed event once. Recovery may roll back one whole checkpoint,
as in the existing store contract. Callers must reload after ambiguous I/O.

Tests cover both triggers, preview purity, rejected/zero/lethal hits, expiration,
stack caps, positive/negative modifiers, removal, healing, enemy planning, catalog
bindings, save migration/corruption and interrupted/ambiguous writes. This is not a
general aura, damage-over-time, stun, displacement or reactive attack engine. Those
mechanics require authored semantics and focused later implementation. Unity import,
IL2CPP, presentation and device testing remain pending.
