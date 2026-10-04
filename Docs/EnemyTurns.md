# Initial enemy turn behavior

This implements abstract aggressive/defensive behavior, not faction personalities,
canonical instincts, a production enemy kit, or a full tactical AI. Register an
EnemyBehavior on Battlefield before that unit's first activation. Registration
requires an eligible placement, health and ability kit; it cannot replace an existing
profile. The normal turn controller still owns initiative and activation frequency.

## Caller contract

TryPlanEnemyTurn takes the active activation ID plus at most eight perceived opponent
IDs and returns an immutable plan without changing position, health, budgets, cooldowns
or Signature readiness. TryRunEnemyTurn replans; it does not accept an old plan as
permission. It executes at most one route and one primary ability through TryMove and
TryApplyEffect, then ends that same activation if it remains active. If movement or
attack ends the mission, it stops without starting another turn or round. A normal
blocked/no-target/no-affordable-path case produces an idle or approach result and ends
the turn. Invalid/stale activation, unregistered actor or malformed perception returns
failure without ending someone else's turn. Unexpected programming exceptions are not
silently swallowed; the executor's finally block releases its active turn.

The game loop must explicitly start/finish rounds and call Mission.ResolveRoundEnd
after due events. AI never bypasses that gate. The caller may invoke planning repeatedly
for future intent displays, but presentation/animation scheduling is not implemented.
Execution currently commits a whole enemy activation synchronously; animation will
need to render those committed outcomes or use a future stepped executor.

## Knowledge and choice rules

Perceived IDs come from the future sensing/mission layer. The planner ignores duplicates,
unknown IDs, missing health, removed/dead units, its own ID and same-team actors. It
never discovers opponents by enumerating the whole roster. It may inspect the supplied
targets' current positions, health, defenses and available damage previews. Full map
geometry/current occupancy remain available to pathfinding. This is not fog-of-war or
a hidden-information security model, and it reads no future player actions.

- Aggressive: choose a legal damaging action, otherwise useful self-healing, otherwise
  an affordable sampled approach that strictly reduces distance to a perceived target.
- Defensive: prioritize useful legal self-healing, otherwise choose a damaging action
  whose position and entire route stay inside the authored guard box; otherwise hold.
- Attacks prefer defeating a target, then greater actual health loss, then lower
  movement cost, then Normal/Main/Signature order, then ordinal target ID. Equal
  positional choices use stable sorted coordinates. These are provisional heuristics.
- Self-healing uses registered profiles and ordinary compatibility/range/cooldown checks.
  There is no free healing, invented self-support power or automatic allied healing.
- An already-spent primary action yields an idle plan, forfeiting leftover movement
  rather than giving a second action. The intended entry point is a fresh enemy turn.

The AI does not override damage, cover, obstruction, unit size, action budgets, cooldowns
or Signature readiness/use. It can use a stronger ready Signature as soon as it scores
best; long-term resource conservation is not yet modeled. Readiness can latch through
an ordinary attack without granting another action during that activation.

## Candidate positions and limits

Consider the current position; eight directions at half/full remaining movement
(radius capped at 10,000 m); and four cardinal offsets around each perceived target at
the behavior's PreferredDistance (0 < distance <= 100 m). Round to existing coordinate
precision, deduplicate, filter illegal destinations and sort. At most 49 positions
are considered for eight opponents. These are internal samples, not mandatory player
stopping points or guaranteed ideal attack positions. Candidate evaluation uses pure
hypothetical-origin targeting with the same BuildAction and health-preview logic;
it never temporarily teleports the actor in shared state.

For each non-current candidate, use the existing weighted, footprint-aware pathfinder
with a configurable node limit (default 64, range 2-256). Reject unreachable,
over-budget and search-limited candidates. Guard-area containment applies to every
waypoint/body including the start; convexity keeps connecting segments inside. Movement
confirms the exact planned path and attacks revalidate actual committed position.
Search counts are bounded, but map complexity, allocations and device frame time are
not benchmarked. Candidate sampling can miss legal attacks or useful detours; an idle
turn does not prove that no tactical opportunity exists. No complete tactical search,
cover safety evaluation, retreat planning or multi-turn planning is claimed.

## Verification and deferred work

Behavioral tests exercise pure plans, move-and-attack, approach/hold, guarded routes,
self-heal priority, main cooldowns, Signature readiness and once-use, blocked turns,
perception filtering, invalid invocations, stable ties, search-limit handling, stale
plans, terminal outcomes and a complete abstract multi-round encounter. Existing
combat and mission tests remain in the suite. GitHub CI builds Core on Windows/Linux.

Later work includes actual enemy definitions/canon review, mission-specific territory
and target exclusions, objective interactions, support allies, perception and intent UI,
AI difficulty/balance, stepped animations, performance and Unity/device testing. The
starter enemy and protected traveler rules must be configured in a production adapter;
these profiles do not supersede them. No canon, PF1-8, locked visual masters or player
progression are changed.

## Support and debuff extension

Explicit ally perception, standalone status choices and compound previews are now
supported. See [enemy support AI](EnemySupportAI.md) for updated selection rules.
