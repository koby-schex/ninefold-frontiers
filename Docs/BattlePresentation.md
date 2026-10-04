# Battle presentation boundary

`Ninefold.Core.Views.BattlePresentation` wraps one `BattleSession`. It is an engine-independent contract for a future Unity presenter, not a Unity scene, renderer, touch input implementation, or new gameplay rule. Create a new session/presentation pair for each mission attempt. Use on one thread and serialize commands; concurrent reads/writes are not supported.

## Reading and previews

`Read()` returns immutable, detached battlefield bounds/obstacles/ground, unit positions and bodies, health, effective armor, kit cooldown/signature readiness, base ability profiles, statuses, objectives, result and current activation. The upcoming queue includes the current actor followed by eligible units still waiting this round; it is empty after battle end. Unit records include removed units so the presenter can reconcile deaths. Friendly non-squad NPCs are distinct from player squad members and AI-controlled enemies.

`Actions` separates non-player turns, missing field profiles and core rule failures. Selectability only indicates that the action can be chosen: a target may still fail range, obstruction, allegiance, health or status rules. Passive is never selectable. `PreviewTarget` and `PreviewTargets` expose authoritative compound effect previews and field/health failure reasons. Profiles contain base effect amounts; render resolved numbers from previews. Target enumeration currently includes all battle units, including invalid/removed targets. It is not a fog-of-war or visibility filter.

`TryPreviewDestination` uses bounded pathfinding; `TryPreviewPath` validates a supplied route. Previews never spend movement, consume abilities, evaluate objectives, advance scheduling, or save. They require the current player activation ID; stale IDs, non-player phases and a session requiring reload throw. Invalid destinations/targets return validation failures. `PreviewInteraction` exposes mission interaction validation. Selection returns an empty battle view with all actions blocked.

## Saved commands and animation

Use `TryMove`, `TryUseAbility`, `TryInteract`, `EndPlayerTurn` and `Advance` through the facade. They delegate all rules and persistence to the existing session. A successful command returns `BattleUpdate` with detached before/after views and a read-only event batch. Animate the batch, then reconcile to `After`. Rejected typed commands return false and a null update. A waiting `Advance` returns unchanged views and an empty batch without saving.

Events are post-commit animation hints, not an event-sourced combat log. Explicit movement/ability/interaction hints come first (enemy movement before enemy ability), followed by deterministic net changes: unit health/status/removal, activation end, round start, activation start, objective change and battle end. A compound ability includes its resolved effect even if a status expires at the end of that same enemy turn. Net status hints include duration/stack changes. Before/after views supply final health, positions, status counts and objective values. Intermediate passive trigger ordering is not reconstructed. Changes made outside this facade do not generate batches.

No callbacks execute inside save transactions and events are never saved or automatically replayed. On a save exception, no batch is returned; honor `MissionFlow.NeedsReload`, reopen the flow, construct a new session/presenter and render `Read()`. An ambiguous write may have committed: reloading decides the authoritative state, without replaying an old animation or duplicating an action. Reward claiming and return to selection remain explicit `MissionFlow` operations.

## Validation and scope

Presentation tests cover immutable snapshots, pure previews, command rejection, action availability, player/enemy event batches, scheduler/status changes, victory, selection and torn/ambiguous checkpoint recovery. Abstract fixtures only; no canon, content balance, save schema, Unity dependency or device performance claim changes.
