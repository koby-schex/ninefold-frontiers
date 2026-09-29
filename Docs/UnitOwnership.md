# Saved unit ownership and fragments

This foundation persists ownership and fragment-based unlock transactions. Unit IDs,
fragment resources and costs in tests are placeholders, not production balance or
canon. Unit level/stat upgrades, post-unlock fragment advancement, special campaign
completion grants and the starter Apex award remain future work.

## Definitions and bootstrap

The authored `RosterUnit` catalog supplies ID, faction, Apex classification and a
matching `UnitUnlockDefinition` (unit ID, fragment resource, positive cost and
revision). Each unit in the flow catalog has its own fragment resource ID. The
catalog no longer contains externally supplied unlocked flags.

`MissionFlow.CreateProfile(starterUnits)` requires exactly three distinct standard
units from one faction. Their IDs become immutable initial ownership in the saved
profile. The selected faction/units must come from approved authored content.
Creation cannot reset an existing profile or run over an existing battle save.
The lower-level progress store accepts an explicit initial ownership list for
trusted setup/test tooling; do not expose that as a player grant API.

## Earning and unlocking

Fragments use the existing durable resource reward ledger. Authored mission bundles
can include a unit's fragment resource, other resources, both or neither. This PR
does not add random reward selection, drop rates, purchases or new reward sources.

From mission selection, call `UnlockUnit(operationId, unitId)`. Generate a unique
operation ID before the request and reuse it when retrying that request. The store:

1. Reloads the latest profile and returns an existing matching operation receipt.
2. Rejects conflicting operation reuse, an already-owned unit or insufficient
   fragments. A different operation cannot buy the same unit again.
3. Saves the exact fragment spend and ownership receipt in one checkpoint.
4. Publishes updated progress only after the save succeeds.

A receipt retains its original cost/resource/revision even if the authored
requirement later changes. Retry of that operation does not spend again. New unlocks
use the current authored requirement. The flow looks up the unit's trusted definition;
the lower-level store is not an anti-cheat boundary for arbitrary definitions.

Fragments left over after unlocking remain in the balance, and later fragments can
continue accumulating. They are reserved for the future post-unlock advancement
system; this PR does not invent that system or convert fragments automatically.

## Squad and campaign integration

`PlayerProgress.Owns` is the authority for squad validation. Successful unlocks
refresh the flow's progress immediately, and reopening restores the same ownership.
The campaign gate counts owned standard units from its faction; an Apex does not
count toward the required three. Mixed-faction modes still enforce at most one Apex.
Unlocking is currently allowed only at mission selection, preventing roster changes
partway through an active attempt. This is an integration boundary, not final UI.

## Storage, compatibility and recovery

Progress schema 2 adds immutable initial ownership and append-only unlock receipts.
Balances are earned resources minus committed unlock costs; ownership is the initial
roster plus committed unlock receipts. A generation advances for each mission claim
or unlock. Both files must contain consecutive, consistent transaction histories.
The combined ledger retains the existing 100,000-transaction/8 MiB limits and
single-writer constraint. No receipt is silently removed.

Schema 1 profiles remain readable with all mission receipts and balances intact.
Because they never stored ownership, they yield **empty ownership**, not an invented
starter roster. Reading does not rewrite files. The next transaction writes schema 2,
and a mixed schema-1/schema-2 checkpoint pair is supported. Older development
profiles need a future explicit authored ownership migration before use as a playable
starter account; do not reset them or reissue rewards automatically.

If an unlock write fails, the flow sets `NeedsReload`. Reopen before proceeding.
A complete but unacknowledged write restores ownership and its spend together; a
partial write restores the previous checkpoint with neither applied. Corruption of
the newest checkpoint may roll back one whole transaction. Retry against recovered
state is safe, but device loss, deletion, deliberate rollback and multiple concurrent
writers remain outside the guarantee. See MissionRewards.md and MissionFlow.md.

Tests cover persistence, leftover balances, repeat earning, defeat, immutable
snapshots, retries, insufficient/wrong fragments, operation conflicts, concurrency
on one store, interrupted/ambiguous writes, recovery, schema-1 compatibility, starter
validation, campaign entry, Apex exclusion and real-directory reopening. Unity UI,
IL2CPP and device testing remain deferred.
