# Enemy support and debuff decisions

Enemy planning now considers perceived allies, standalone buffs/debuffs and compound
health/status abilities through the same previews and commands used by players.
These priorities are provisional engineering tuning, not production faction behavior.

## Perception and execution

The new `TryPlanEnemyTurn` and `TryRunEnemyTurn` overloads accept separate opponent
and ally ID lists, each limited to eight supplied entries. Self is always considered.
Unknown, removed, defeated and wrong-team entries are filtered; null, blank or
excessive lists reject without changing state. Lists are sorted and deduplicated for
stable decisions. The old overload considers opponents and self only; it does not
silently discover allies. The future sensing layer must supply visible actors.

Support respects authored allied compatibility, target rules, range, line of sight,
movement budgets and guard boundaries. Candidate destinations include positions near
perceived allies as well as opponents. Path search remains bounded. Execution replans,
uses `TryUseAbility`, then ends exactly one activation. Main cooldowns, Signature
requirements, action budgets and status durations have no AI exceptions.

Plans and results expose `AbilityEffect` for compound health/status information;
`Effect` remains a compatibility view of its nullable health component.

## Provisional choice rules

Defensive actors prefer useful support, then offense. Aggressive actors prefer useful
offense, then support, then approach movement. Within a category, lethal damage wins;
otherwise choose the highest local utility, then least movement, lower ability slot,
ordinal target ID and the first deterministically ordered destination.

Utility adds actual health restored/damaged to a status estimate. Armor and power
changes are measured as percentage points of the target's deployed base, after
integer rounding and aggregate status caps. Power uses the largest authored health
profile amount; zero-armor and status-only targets receive no value from scaling a
stat they cannot use. Allied improvement and hostile reduction score positively;
the opposite scores negatively. A compound ability may remain worthwhile because
of its health result even if its status component is neutral or disadvantageous.

Adding a useful stack has value. Refreshing useful existing strength has discounted
value proportional to the added duration (one quarter weight). A capped,
full-duration refresh has none. Other effects already saturating the aggregate cap
can also make a new application worthless. Nonpositive total utility is skipped.
Self effects that disappear when this activation ends have no status value; longer
self effects are evaluated with that immediate duration decrement included.

This is a deterministic local heuristic, not multi-turn tactical search. It does not
predict which other effects will expire first, future allies' choices, objectives,
auras or future reinforcements. The health/status weighting needs encounter playtests.
No hidden damage, movement or resource bonuses are added.

## Saves and tests

No save schema changes are needed. Planning is pure; all decisions are derived from
restored battle state plus caller-supplied perception. Plans are not persisted or
replayed, and perception must be supplied again after reload. Normal saved-command
execution remains the durable boundary.

Tests cover ally perception, team filtering, useful healing/buffs/debuffs, caps,
refresh, self expiry, harmful effects, deterministic ties, range/obstruction, movement
and guard limits, style priorities, cooldowns, Signatures and identical resume behavior.
Unity import, device performance and presentation testing remain pending.
