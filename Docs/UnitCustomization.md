# Player-selected unit customization

This foundation lets an owned unit retain one authored stat trade-off, separately
from permanent fragment advancement. Selecting, switching and resetting are free
between battles. Options, magnitudes and weighting in tests are placeholders, not
production balance, lore or locked unit designs.

## Authored choices and saved state

An optional `UnitCustomizationDefinition` on the roster supplies a unit ID, revision
and named `CustomizationOption` values. Options contain signed health, armor and
power (damage/healing) basis-point adjustments. The current provisional authoring
model requires a zero sum across these three basis-point values, with a maximum
absolute adjustment of 500 (5%) per stat. A nonneutral choice therefore has both a
benefit and a downside. Production balance must still account for actual stat values
and nonlinear armor mitigation; equal basis points do not prove equal combat value.
Do not author options whose intended downside is ineffective for that unit.

`MissionFlow.CustomizeUnit(operationId, unitId, optionId)` selects an option from its
trusted definition. Pass null for `optionId` to reset to neutral. The unit must be
owned and the flow must be at selection, with the prior result already claimed.
No fragments, currency, advancement ranks or ownership are spent or removed.
Neutral is the default for owned units with no customization history.

Each request commits a receipt containing the exact option, revision and signed
stats. Current state is the **last receipt per unit**, not a sum of choices. Switching
replaces the previous selection. Reset clears only customization; permanent
advancement remains. Saved choices are shared across campaigns and other modes.

Use one stable operation ID per request and reuse it on retry. Duplicate requests
return their historical receipt and the **current** saved profile without writing.
Retrying an old choice after a newer choice never reverts current state. Reusing the
operation ID for a different unit or option is rejected. A genuinely new operation
selecting the same option records a new receipt but still cannot stack bonuses.

Saved stats remain authoritative when content changes. A new explicit selection
uses the current authored option; retry uses its original receipt. Reset requires
no customization definition and remains available if options were removed (the unit
must still exist in the roster catalog). No resource compensation or automatic
reselection is invented.

## Deployment and combined limits

The flow combines saved advancement and customization **additively over authored
base stats**, once at the start of a new battle. It never multiplies already improved
stats. The provisional per-stat range is -5% to +15%: permanent advancement remains
capped at +10%, and customization can move up to 5 percentage points either way.
These are engineering limits for testing, not final progression balance.

Example: +10% permanent health and +5% health customization produce +15%, not +15.5%.
Armor with +10% advancement and -5% customization receives a net +5%. A reset restores
the advancement-only values on the next deployment.

Integer calculation is `base + truncate(base * netBasisPoints / 10000)`. Truncation
rounds the change toward zero, keeping both gains and losses within the proportional
bound. Very small stats may not change at a given percentage; positive health/power
cannot round down to zero, and zero armor remains zero. Authored partial starting
health uses the same rule. Overflow aborts before saving the new battle.

Factories must supply base stats. The existing deployment path applies the combined
modifiers to selected units' health, armor and battlefield damage/healing profiles;
enemies and unselected units receive none. Initiative, movement, range, cooldowns,
turn budgets, Signature limits, geometry and targeting rules are unchanged.

Battle saves already contain resolved values. Resuming never reapplies modifiers or
substitutes a newer choice. New battles use current saved choices. Rendering should
read current profile state (not just an old operation receipt) and resolved battle
stats. There is no customization UI or finalized player-facing option naming yet.

## Transactions, saves and recovery

Progress schema 5 appends customization receipts. Schemas 1–4 remain readable and
start with neutral customization without resetting their other progress. Reading
never rewrites old files. The next committed transaction writes schema 5; mixed
supported-schema checkpoint pairs are validated against consistent history prefixes.
All mission, unlock, campaign and advancement writers preserve customization.

Interrupted or ambiguous writes set `NeedsReload` on the flow. Reopen and retry the
same operation. Selection is either wholly saved or absent; reset/switch never
partially modifies other progress. Corruption recovery can roll back one whole
checkpoint. Single-writer ownership, the 8 MiB size bound and combined 100,000-receipt
limit remain. Repeated free changes consume ledger entries, so production-scale
compaction is still needed; old operation identities must not simply be discarded.

Tests cover selection/switch/reset, no resource cost, immutable state, invalid
options, old retries, changed content, concurrent duplicate calls, interrupted and
unacknowledged writes, corruption, older schemas, other transaction types, combined
limits, actual deployment, rounding, shared modes and resume. Production option
balance, UI, Unity/IL2CPP and device validation remain deferred.
