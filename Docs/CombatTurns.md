# Combat turn contract

## Implemented rules

- Explicit round start snapshots all currently eligible units, initiative descending.
- Each unit gets one activation; initiative never grants more activations.
- Initiative changes during a round apply to next round's ordering.
- Newly registered reinforcements wait for the next round.
- Each activation grants its configured movement allowance and one primary action.
  Movement can happen before, after or on both sides of that action.
- Attack, main ability, Signature and contextual interaction will all use the same
  primary-action slot. Their targeting/effects are not implemented by this controller.
- End activation forfeits unused resources. There is no wall-clock timer.
- Removing a defeated/extracted unit skips its pending turn or ends its active turn.
  IDs are unique for the whole battle and cannot be reused to gain another turn.
- EndBattle stops mutation; mission logic will decide when a battle ends.

Implementation choices pending broader combat review: equal initiative uses ordinal
battle-local ID order, independent of insertion order/faction. Use stable IDs, not
player-controlled display names. Movement budget uses decimal cost to avoid cumulative
floating-point overspend. Distances/pathfinding will convert to explicit validated
costs; this is not a new terrain or balance rule. Movement allowance is fixed from
registration for now; future effects need explicit adjustment semantics.

## Lifecycle

Create UnitTurnDefinitions (unique ID, initiative, movement allowance), then create
a BattleTurnController. Call StartNextRound, then BeginNextActivation. It returns an
immutable ActivationView with a unique ActivationId. Pass that ID to SpendMovement,
SpendPrimaryAction and EndActivation. A command with an old ID is rejected even if
the same unit is active again in a later round. Re-read CurrentActivation for new
budget values; old views remain unchanged.

BeginNextActivation throws if a turn is still active; it cannot refresh budgets.
It returns null when no eligible queued units remain. IsRoundComplete exposes this
boundary without starting another round. The mission/event layer must resolve its
round-end events/outcomes before calling StartNextRound. Reading state never spends
resources. All mutators are single-threaded; callers serialize commands.

Rejected commands throw argument/state exceptions and preserve resource budgets.
UI/network adapters should map them to user-safe feedback. Budget spending is not
full action validation: future command handling must validate targets, path legality,
cooldowns and costs before committing action effects. No movement positions or damage
are applied here. Removal represents battle eligibility, not permanent roster loss.

## Explicitly deferred

Owner activation counts now drive main-ability cooldowns; Signature readiness/use
is implemented in the battle-owned Abilities controller (see AbilityUse.md). Stun
resolution still needs a status layer: do not model a stunned scheduled turn as
permanent removal. Revival/re-entry, movement buffs, objective progress,
save snapshots/migrations, faction/squad
selection and geometry are separate changes. Save restoration must preserve activation
identity and queue state when implemented. No offline persistence is claimed yet.

Core builds using .NET Standard 2.1 / C# 9 and has no UnityEngine reference. Tests
run the exact Core source using a .NET 8 host. Unity integration and IL2CPP are still
unverified. Reference for Unity API surface:
https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html
