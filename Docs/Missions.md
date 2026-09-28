# Finite mission objectives and outcomes

This Core layer supplies reusable contracts for abstract encounters, not approved
campaign missions, story, maps, rewards or a complete noncombat traveler controller.
Configure one MissionDefinition through BattleTurnController.ConfigureMission after
field, health and placements are registered, before round one. Definitions identify
one primary objective, zero to two optional objectives, a nonempty continuation squad,
mission/attempt IDs and an explicit OutcomePriority. All referenced actors must already
be registered, placed, eligible and alive. Setup copies caller collections and cannot
partially attach a failed configuration. No faction powers or canonical anatomy follow
from an actor's ability to participate in an abstract fixture.

## Implemented objective contracts

| Family | This slice's success and failure rules |
| --- | --- |
| Defeat | All explicitly listed targets have zero health. Removal while alive fails this objective; extraction is not silently counted as a kill. |
| Protect | Named actor remains alive and eligible through the configured number of resolved rounds. Loss before completion fails the objective. |
| Rescue and extract | Perform the one-time authorized rescue interaction, then keep the target alive with its entire body proxy inside the exit. Already being inside does not bypass rescue. Target loss before completion fails it. |
| Secure | At a resolved round boundary, at least one listed holder fits completely in the zone and no listed living contestant overlaps it. Add one mark. Authored consecutive mode resets on an unheld/contested boundary; cumulative mode pauses. |
| Stabilize or contain | Complete the configured number of authorized interactions. Each costs a primary action and grants one mark; progress is cumulative, not implicitly one mark per round. |
| Survive | A viable listed squad actor remains at the configured number of resolved round boundaries. This is finite round survival, not waves/endless scoring. |

Completed and failed objectives latch. Completed protection covers its stated interval,
not an additional unstated lifetime. An optional failure never becomes a primary failure;
optional completion never ends the mission by itself. Optional objectives still active
when the mission ends remain incomplete in the result. Unlisted enemies never create
an elimination requirement. Losing every continuation-squad actor is a mission failure.
The roster is explicit; reinforcements are not automatically added to objective lists.

## Interaction contract

InteractionDefinition supplies a world point, maximum reach and explicit actor allowlist.
This version always costs one primary action. PreviewInteraction is read-only. TryInteract
rechecks current activation, objective state, actor eligibility, action availability,
reach and blockers before committing progress. Rescue cannot repeat. Stabilization can
repeat on a later activation until its target count is reached. Interactions are not
normal/main/Signature uses and cannot satisfy those ability-use readiness conditions.

Physical reach currently uses the body's authored TargetOffset as a provisional
interaction origin and a direct 3D segment. Movement or shot blockers obstruct it.
No actor gains an anatomical reach, tool, telekinesis or traversal power from an
allowlist; actual offsets, reach, approach areas and supported actors require content
and model review. This is not final mesh-contact validation.

## Resolution order and integration

Committed health effects, spatial movement and successful interactions evaluate
objectives automatically after their complete state change. Reading and previews do not.
End a finite mission immediately when the primary succeeds or mission failure occurs,
using the explicitly supplied FailureFirst/SuccessFirst policy when both occur at the
same evaluation checkpoint. No implicit universal tie policy is introduced.

For scripted changes made through trusted low-level APIs (including direct RemoveUnit),
call Mission.Evaluate after the complete event batch, before accepting the next player
command. Those primitives intentionally do not evaluate intermediate state. Ordinary
health/movement commands evaluate immediately and must not be used to simulate a future
simultaneous multi-effect event without an event-batching adapter. That adapter, status
triggers and environmental damage are not implemented here.

At round end, finish scheduled activations and **all due events first**, then call
Mission.ResolveRoundEnd(currentRound). This refreshes conditions, increments qualifying
round-based progress once, applies the mission's outcome policy and records the resolved
round. Repeating that round check cannot award extra progress. Active, stale and future
round IDs throw without progress. StartNextRound refuses to bypass this checkpoint for
mission-enabled battles. Missionless combat tests retain their prior lifecycle.

The event pipeline is a caller obligation, not an implemented hazard/escort scheduler.
The starter traveler, end-of-round movement step, two-round survey eligibility, safe
extraction removal, future phases, wave survival and mode-specific outcome overrides
need their own adapters/contracts. The rescue fixture uses ordinary combat-registered
actors to verify the objective; it does not approve the traveler's production turn model.

## Result and permanent progression

Resolution creates one immutable BattleResult containing mission ID, attempt ID, outcome,
round, reason code and objective snapshots, then calls EndBattle. The existing terminal
battle guards reject subsequent movement, attacks and interactions. Re-evaluation returns
the same result object. An externally ended battle fabricates neither victory nor defeat;
it has no mission result. Abandonment/retry UI will need an explicit contract later.

This is in-memory once-only resolution, **not** persisted reward idempotency. Attempt IDs
are supplied by the future session/save layer; rewards, XP, unlocks, economy, save/load
and offline recovery are deferred. No owned-character or permanent-progression storage
is modified. Abstract battle defeat never deletes health records or an owned roster.

## Verification

Behavioral tests cover all six families, optional isolation, contested control, rescue
ordering/footprints, interaction legality/resources, duplicate round boundaries, explicit
simultaneous policies, immutable results, setup rejection and terminal behavior. GitHub
CI compiles and tests Core on Windows and Linux; Unity and physical-device verification
remain deferred. These implementation policies do not revise the Master Lore, PF1-8 or
locked visual masters. Concrete campaign missions still require their approved contracts.
