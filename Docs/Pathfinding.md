# Destination pathfinding

Battlefield.TryFindPath(activationId, destination, out preview, out failure) plans a
move without mutating the battle. Battlefield.TryMoveTo uses a fresh query and commits
through the existing supplied-path validator. Both accept a maximumNodes option
(default 256, allowed 2-256). Callers provide an exact legal world position; there is
no mandatory grid or snap. The existing six-decimal-meter coordinate precision applies.

## Route selection

A clear straight line at normal-ground cost returns immediately. Otherwise, build a
visibility graph from the current position, destination, and corners around movement
obstacles, eligible unit bodies and difficult-ground volumes. Expand each volume by
the moving unit's footprint and add one coordinate quantum (0.000001 m) of clearance
because touching is blocked. Omit illegal corners and volumes above/below the body.
Sort coordinates before deterministic Dijkstra search; equal-cost choices use stable
node order. Test every connecting segment with the same swept-footprint checks used
by manual movement, and weight it with the same rounded difficult-ground cost.

This chooses the least-cost route **on the generated graph**. It is not a proof of
the global continuous weighted-terrain optimum: optimal crossings through cost-zone
edges need not lie on corners. Tight/complex arrangements can need navigation samples
not in this graph. No path on the graph therefore does not prove geometric impossibility.
There is no fixed navigation grid, automatic snapping, or canonical-size normalization.

The maximum node count bounds graph search (quadratic edge checks; each checks current
map geometry), not total authored map complexity or mobile frame time. More than the
allowed number of distinct legal candidates returns SearchLimitExceeded without
returning a partial or possibly misleading route. At most 255 waypoints are returned,
within the supplied-path validator's 256 limit. No device performance claim is made;
larger production maps may need spatial indexing, cached navigation data or another
solver. Pathfinding is synchronous and single-threaded in this first Core slice.

## Failure and confirmation behavior

- Illegal or occupied destinations, elevation changes and no-op moves retain their
  existing validation failures. Missing placements and stale/ended activations fail.
- InsufficientMovement means the straight-line lower bound exceeds allowance, or the
  cheapest generated route exceeds it. It is not a continuous-optimality claim.
- PathNotFound means the candidate graph cannot connect start and destination.
- SearchLimitExceeded is distinct from PathNotFound; the UI must not present it as
  evidence the destination is physically unreachable. Invalid limits are InvalidRequest.

No rejected query/move spends movement, a primary action, cooldowns or Signature use.
There is no partial movement toward an unreachable/over-budget destination. Previewing
and canceling cost nothing. Successful moves spend only the route cost, preserving
movement-before/after-action and the existing round/initiative rules.

TryMoveTo explicitly replans from current state, so it may choose a different route
than an earlier preview after a reinforcement/move/defeat. For a UI that confirms
exactly the displayed route, pass that immutable preview.Path to TryMove instead:
it revalidates that route and rejects it if it is no longer legal or affordable.
The UI should request a new preview before displaying a changed route. No preview
serves as permission to bypass current geometry or budgets.

## Verification and deferred work

Tests cover exact fractional destinations, obstacles and unit detours, large-body
clearance, closed barriers, budgets, weighted terrain choices, repeatable equal-cost
routes, map enumeration order, revalidation, defeat, split movement, search limits,
missing/stale/ended commands, border destinations and overhead volumes. CI compiles
the Unity Core sources and runs the full suite on Windows and Linux.

Unity/touch integration, reachable-area overlays, mesh-derived/rotating footprints,
slopes, forced movement, traversal exceptions, movement discounts and performance on
real maps/devices remain deferred. The existing map volume must explicitly mark holes
as blockers. Routing does not grant flying, climbing, gap-crossing or faction powers.
No canon, PF1-8, locked visual masters or unit balance values change in this PR.
