# Saved battle session

`BattleSession` coordinates a running `MissionFlow` for a future presentation layer.
It is a single-threaded, untimed adapter, not a Unity component or an autoplay loop.
Each `Advance` commits at most one completed step, allowing the caller to animate
that step or yield to the next frame. Do not run an unbounded loop while waiting for
player or enemy input.

## Presentation contract

Create/open the profile and start a mission through `MissionFlow`, then create a
`BattleSession`. `Read()` returns a detached immutable view with phase, round,
activation and terminal result. It also works at mission selection. The session
binds to the first mission attempt it observes: create a new instance for a new
attempt, so delayed commands from an earlier battle cannot act on reused activation
numbers. Reopening the same attempt through the existing flow is supported.

| Phase | Caller action |
|---|---|
| Selection | Use mission flow to select/start a mission. |
| PlayerInput | Preview on `flow.ReadBattle()`, submit a player command, or end the turn explicitly. |
| EnemyInput | Supply `EnemyPerception` and call `Advance` once. |
| AdvanceRequired | Call `Advance` once to pass an NPC or advance the scheduler. |
| Results | Show the result; claim rewards and return to selection through mission flow. |

Player movement, compound abilities and interactions use typed session methods with
the expected activation ID. Only active squad members accept player commands; a
unit registered as both squad and enemy is rejected. `Try...` methods return normal
rule failures for invalid actions and create no checkpoint. Wrong-owner/stale
activation commands throw before mutation. Successful player commands do not end
the turn: movement can still be spent before/after the action until `EndPlayerTurn`.

Enemy perception copies bounded lists of at most eight opponents and eight allies.
The caller supplies sensing; the session never grants omniscience. `Advance` without
perception waits at an enemy activation without writing a save. Valid perception
runs one enemy turn through ordinary AI rules and ends that activation. Non-squad
actors without registered enemy behavior hold position and forfeit their activation;
scripted escorts or special NPC behavior require a later explicit controller.

## Scheduler and checkpoints

For an unstarted battle, one step starts the round and the next begins its first
activation. After a turn ends, the next step begins the next eligible activation.
At the end of a round, one step resolves mission round-end objectives; a later step
starts the next round only if no result was reached. `LastResolvedRound` prevents
repeating a round-end resolution after reload. Mission evaluation stops scheduling
on victory/defeat, including when a completed command ends the battle.

Status duration still decrements once at its owner's activation end. Starting an
activation still triggers its passive once. The session does not introduce a second
status tick or passive event. No new round-end hazard/script hook is added here.

Every successful command/advance uses `MissionFlow.Execute`: mutate a private copy,
validate and save it, then publish. Failed rule commands do not write; failed saves
require `flow.Open()` before further session reads or actions. Torn writes may roll
back one whole checkpoint; an acknowledgement lost after durable writing restores
the completed step. The UI must refresh from the recovered state rather than replay
a previously submitted action automatically. No new save schema or session cursor
is needed; all authority is already in the saved battle and mission state.

Session methods are the intended presentation command boundary; `MissionFlow.Execute`
remains a trusted low-level integration API and should not be used by UI to bypass
ownership checks. One owner must coordinate flow and session calls on the simulation
thread. Rewards remain explicit and are never automatically claimed by scheduling.

Tests cover player waiting/costs, invalid and stale commands, one-step enemy turns,
NPC holds, round gates, victory/defeat, full survival encounters, detached views,
resume, torn/ambiguous writes and cross-attempt command rejection. Abstract fixtures
only; no canon or production mission content is added. Unity import, UI animation,
IL2CPP and device tests remain pending.
