# Persistent mission progress and reward claims

This is a plain C# offline foundation, not a finished campaign UI or production
economy. All test resources and amounts are abstract placeholders. No lore,
production rosters, fragment costs, Apex unlocks or campaign completion awards are authored here.
UnitOwnership.md describes the added configurable fragment-unlock infrastructure.

## Transaction contract

Use one `LocalProgressStore` and one writer per profile directory. Call `Load` at
startup. A null result means both profile files are absent; create a new profile
explicitly with `Create`. Never catch a load failure and silently create/reset a
profile. Keep a stable profile ID; a mismatched ID requires recovery/migration.

After the mission controller produces a terminal `BattleResult`, call
`Claim(result, authoredPolicy)`. Keep the attempt ID unique across every mission
and mode for that profile and retain it through battle save/resume. A replay gets
a new attempt ID. The caller must provide trusted results and matching authored
policy data; this is not an anti-cheat or purchase validation boundary.

Each completed attempt records one immutable receipt containing the mission,
result fingerprint, policy revision, first-clear flag and actual resource grants.
The first victory for a mission selects the **entire first-clear bundle**. Later
victories select the **entire replay bundle**; the bundles are not added together.
Empty bundles are valid. Defeat records an attempt with no grants and does not
consume the mission's first clear or alter earlier progress. Optional objectives
are fingerprinted but have no separate reward rule in this implementation.

Mission records are derived from these receipts; balances now also subtract committed
unit-unlock and advancement costs and include campaign completion grants (see UnitOwnership.md
and CampaignProgression.md). A claim
publishes a new snapshot only after the complete ledger is flushed. Checked
arithmetic, invalid definitions or size limits reject before writing. There is
no API that saves a stale caller-owned profile over newer durable progress.

Repeating the same result returns its existing receipt with `AlreadyClaimed`,
without a write or a second grant. This remains true after a battle reload, store
recreation, or policy change/removal (a null policy is allowed only for a known
receipt). Reusing an attempt ID for a different result is rejected. The original
receipt always determines what that attempt earned.

## Integration order and recovery

1. Persist the terminal battle snapshot using the battle store.
2. Claim its result using the progress store.
3. Display the returned receipt and committed progress; do not award resources
   again in presentation code. Retain the terminal battle until claim succeeds.

If a write throws, reload/retry the same result. A failure before or during a
write leaves the previous complete transaction available. If bytes were fully
written but acknowledgement was lost, retry finds the receipt and does not grant
again. An active/restored battle itself cannot prove that rewards are unclaimed;
always consult the current progress store.

`progress.0.save` and `progress.1.save` are separate from battle save files. Each
uses an explicit progress schema/profile identity inside the existing bounded,
checksummed save envelope. The combined mission-claim, unlock, campaign and advancement receipt count determines the generation
(empty profile = 1). Two valid files must be consecutive generations with identical
history prefixes. Incompatible schemas/identities or conflicting histories block
writes; malformed data may fall back to the other valid checkpoint.

`Recovered` reports that a damaged file was encountered. Recovery from corruption
of the newest file can lose **one whole checkpoint**, including its receipt,
completion and grants together. Retrying its saved terminal battle restores that
transaction once. This does not guarantee preservation against deletion of both
files, device loss, deliberate rollback or tampering. Keep recovery visible to
the future application and do not claim this is cloud backup. A duplicate claim
is read-only and does not repair the damaged slot; the next new claim replaces it.

## Current limits

- Single-process, single-writer ownership; the store serializes calls on one
  instance. Multiple concurrent store instances/cloud writers are unsupported.
- Resource totals now support fragment spending for unit unlocks. Player-selected customization,
  purchases, randomness and production starter campaign reward content remains deferred.
- Full-ledger checkpoints, up to 100,000 combined claim/unlock/campaign/advancement receipts and the envelope's 8 MiB bound.
  Hitting either limit rejects safely. Receipts are never evicted to make space;
  scalable journal/compaction and migration are needed before production scale.
- No Unity lifecycle integration or device/IL2CPP validation yet. The filesystem
  adapter flushes writes, but mobile pause/termination behavior needs device tests.
- Future random rewards must persist the selected grants once; never reroll a
  receipt during retry. Real-money flows need a separate verified transaction path.

The console suite covers first clears, replays, defeat, cross-mission isolation,
restored results, duplicates, policy changes, identifier conflicts, immutable
snapshots, concurrent calls on one store, overflow, interrupted/ambiguous writes,
corruption, incompatible or divergent histories, and real-directory coexistence
with battle saves. GitHub runs the suite on Windows and Linux.
