# Ability availability and costs

## Implemented boundary

Each BattleTurnController owns one BattleAbilityController through `Abilities`.
Register each unit's UnitAbilityDefinition once, before its first activation. Kits
have the same four logical slots for standard, Apex and enemy units; this code adds
no faction-specific powers or canonical stats. A kit cannot be replaced mid-battle
to reset cooldowns or a spent Signature.

- Normal attack: one primary action; no separate cooldown.
- Main ability: one primary action and an owner-turn cooldown; starts ready.
- Signature: one primary action, its readiness prerequisite, and at most one use
  in this battle. Other turns and repeated readiness triggers never refund use.
- Passive: no active-use command and no action cost. Passive effect evaluation is
  a later system. Single-target damage/healing now use Health (see HealthResolution.md);
  passive modifiers remain deferred.

`GetAvailability(activationId, slot)` is a pure preview. `TryUse(..., out failure)`
commits availability/costs only. A rejected use returns a reason without spending
movement/action, starting a cooldown, meeting a prerequisite or consuming a Signature.
Stale activation IDs are rejected, including old turns from the same unit.

Target/path/effect prerequisites must be validated by the future command layer before
calling TryUse. Accepted use currently records activation, not resolved damage.
Attack/main readiness means successful activation, not kill, hit-health loss or an
arbitrary click. The battle-owned Health controller now validates supported health effects and target
verdicts before calling TryUse, then commits damage/healing. Geometry and full
unit-specific effect definitions remain deferred; see HealthResolution.md.
Contextual interactions may use SpendPrimaryAction directly; they do not count as
an attack or main ability and do not satisfy those Signature prerequisites.

## Owner-turn timing

The controller increments each owner's scheduled activation counter exactly once
in BeginNextActivation. Cooldown readiness uses this counter; previews, round starts,
other actors, changes to initiative, and wall-clock time do not tick it.

Implementation convention (unit balance remains unselected): if a main with cooldown
C is used on owner activation N, it is ready at the start of owner activation N+C.
For C=2, use on activation 1 → unavailable on 2 → ready on 3. C=1 permits use next
activation. C must be at least 1. This follows the prior design hypothesis; it is
explicitly documented so balance review can revise the convention if needed.

A scheduled activation that begins and is immediately forfeited still advances its
owner's cooldown. A future stun layer should use this path, not permanent unit
removal. Stun duration, prevention and status behavior are not implemented here.
Removed/defeated units receive no further activations. New reinforcements start their
own counter at zero and enter next round according to the existing scheduler.

## Signature readiness

Current configurable prerequisites:

| Requirement | Latching trigger |
|---|---|
| ReadyAtDeployment | Kit registration |
| AfterNormalAttack | First accepted normal attack activation |
| AfterMainAbility | First accepted main ability activation |
| ExternalCondition | Trusted battle-event layer calls MarkSignatureConditionMet |

ExternalCondition is an integration hook for later validated unit-specific conditions,
not a player command or a bypass for the other three prerequisites. The event may
occur outside the owner's turn; using the Signature still requires its own active
turn and available primary action. The event cannot restore a used Signature.
Readiness is distinct from use: AbilityStateView preserves RequirementMet and Used;
SignatureReady combines them, but action/turn availability must still be checked.

After attack/main readiness becomes true, the already-spent action prevents using
Signature in that same activation. No free action is created.

## Lifetime and future persistence

State belongs to this battle instance. Do not recreate a controller on wave changes,
round changes or UI reopening. A genuinely new battle starts fresh. Removal retains
its ability history; duplicate IDs/kit registration cannot reset it. EndBattle blocks
further use or readiness mutations. No revival API is added.

Disk saves/resume are not implemented. Future snapshots must include owner activation
counts, main ready-at counts, Signature requirement/use flags, kit definitions/IDs,
turn state and command identity. Constructing a fresh battle is NOT a restore path.

## Validation

The .NET 8 executable includes 21 turn-controller checks and 24 ability checks,
plus the health-resolution checks described in Tests/README.md. Both use the exact Unity Core source compiled for .NET Standard
2.1, independently of the Editor. Checks cover cooldown boundaries/owner ticks,
readiness variations, rejected/canceled previews, movement preservation, stale commands,
kit reset attempts, independent units, reinforcements and repeated Signature events.
Unity import, effects, UI and device testing remain deferred.
