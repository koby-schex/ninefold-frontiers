# Offline battle snapshots and recovery

## Scope and API

BattleSave.Capture(battle, contentRevision) returns detached bytes. Restore(bytes,
expectedContentRevision) creates a new controller graph directly, without replaying
commands, starting an activation, ticking cooldowns or evaluating objectives. Capturing
also validates the generated snapshot before it can be published. The caller swaps to
the restored instance only after a successful load; failures never mutate its live battle.

All operations occur on the simulation thread at completed-command boundaries.
Do not capture halfway through a scripted event batch or during concurrent mutation.
Existing enemy execution is a single synchronous command, so checkpoint after that
activation, not between its movement and attack. Pending UI intents, animation playback
and transient AI plans are not saved or replayed. Render the restored committed state.

The snapshot stores current rules/map/profile definitions alongside their battle-local
IDs, with an explicit caller-supplied content revision. This avoids silently applying
changed tuning on resume before a production content catalog exists. It does not copy
master-lore documents or visual assets. A future catalog can replace embedded definitions
with versioned references through a deliberate schema migration.

## Preserved state

- Unit definitions, modified initiative, eligibility, per-owner activation counters.
- Round number, frozen initiative queue, queue cursor, activation sequence and exact
  active turn ID, movement remaining and primary-action availability.
- Health/armor/team definitions and current health, including removed/defeated records.
- Main cooldown ready-at counters, Signature conditions and once-per-battle use.
- Map bounds, blockers, cover, difficult ground, placements, body proxies, attack/target
  offsets, effect profiles, healing eligibility and enemy behavior/guard definitions.
- Mission/attempt IDs, definitions, primary/optional progress and status, rescued flags,
  resolved-round checkpoint and immutable terminal result.

Deployment-only registration restrictions are not replayed during restoration; instead
the decoder checks identifiers, counts, enum values, counters, health, placement,
objective state and result consistency before returning a new graph. Restore never
resets a damaged unit to full health or a consumed ability to deployment readiness.
A defeated unit sharing its old location with a living unit remains nonblocking.

## Version and corruption handling

Schema version 1 is an explicit binary format using BinaryReader/BinaryWriter, without
BinaryFormatter, arbitrary type deserialization, reflection-based private-field
serialization, or third-party packages. This is a reviewable implementation choice,
not a lore revision. Decimal values retain their existing precision. SHA-256 covers
the header and payload; it detects accidental corruption, not cheating or tampering.

The format caps each envelope at 8 MiB, each collection at 4096 and identifiers at
1024 characters. Oversized runtime content therefore fails capture rather than writing
a snapshot that this reader cannot load. Nested local records incur a small additional
size overhead. Unsupported format versions and mismatched content revisions raise
IncompatibleSaveException. No migration is invented and no incompatible file is silently
discarded. Future releases need explicit supported-version/revision migration tests.

## Local storage

LocalBattleStore accepts IBattleSaveFiles. DirectoryBattleSaveFiles provides local file
IO; in Unity the caller will supply a directory under the app's appropriate persistent
data location. No platform path, cloud account or network service is assumed here.

Two alternating files, battle.0.save and battle.1.save, hold checksummed records with
monotonic generations and a complete snapshot. Loading validates both and selects the
newest valid generation. Saving overwrites only the other slot and flushes its file
contents with Flush(true), preserving the current valid generation. A truncated write
fails verification and falls back to the surviving copy. An exception after a complete
write can still leave a valid newest generation, which the next load discovers.

Load returns null only when both files are absent. Recovered is true when an invalid
copy was skipped. If neither existing copy is valid, report InvalidDataException rather
than starting a new battle or overwriting evidence. Unsupported versions/revisions in
either slot block load/save until handled. Permission and other IO errors propagate;
they are not mislabeled as corrupt saves. Two valid equal generations are rejected.

One store instance is serialized by a lock, but capture still requires the simulation
thread. There must be one writer/store owner per directory; cross-process or competing
instance writes, cloud conflicts, directory-fsync/power-loss guarantees and mobile
filesystem behavior are not claimed. Losing both files cannot be recovered by this
two-copy scheme. Never suppress write failures or advertise unsaved commands as durable.

## Game-loop integration

1. After a committed player command, completed enemy activation, resolved round-end
   event batch or terminal result, call store.Save on the simulation thread.
2. On startup, call store.Load. If present, render loaded.Battle and resume its existing
   activation/round checkpoint; do not call BeginNextActivation unless none is active.
3. At a saved completed-round boundary, ResolveRoundEnd remains idempotent. If the saved
   boundary was unresolved, resolve due events under the future event pipeline before
   granting the next round.
4. Surface failures/recovery through future UI. Resume uses the latest valid checkpoint;
   an unsaved command can be lost if its write did not complete.

No Unity lifecycle hook, installed app resume flow, animation replay, cloud sync,
campaign profile, rewards, XP/unlocks or durable reward ledger is implemented in this PR.
Terminal attempt/result state is saved, but it is not authorization to grant rewards
again. Exactly-once progression awards need a separate persistent transaction design.

## Verification

Core tests compare interrupted and uninterrupted battles, including enemy encounters,
partial movement/action budgets, health/defeat, cooldowns, Signatures, queue changes,
reinforcements, terrain, rescue/objective progress, round checkpoints and terminal
results. Injected file failures cover before-write, torn-write and after-write cases,
corrupt newest/all copies, incompatible headers and repair after recovery. A real
temporary-directory test reopens the store through a fresh adapter instance.

GitHub Actions validates repository structure and runs the full Core suite on Windows
and Linux. The authoring workspace was unavailable for this PR, so no local compile is
claimed. Unity import, IL2CPP, real iPhone suspension/termination and storage performance
remain required integration gates. No canon, PF1-8 or locked visual masters change.

## Schema 2 addition

Passive/status definitions, bindings and active counters are now saved. Schema 1
remains readable with empty effect state. See [effects](PassiveStatusEffects.md)
for event timing, migration and integrity details.

## Schema 3 addition

Field ability profiles now preserve standalone/compound status components and
target rules. Versions 1 and 2 remain readable. See [active ability effects](ActiveAbilityEffects.md).
