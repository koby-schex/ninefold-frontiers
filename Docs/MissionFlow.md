# Offline mission flow

`MissionFlow` connects mission selection, squad validation, battle checkpoints,
results and reward receipts without Unity. This is a headless integration boundary;
there are no menus, production missions, production unit progression or new canon.

## Ownership and setup

Construct one flow on the simulation thread with profile-specific battle/progress
file adapters, a stable profile ID, a content revision, authored mission entries
and an immutable unit definition catalog. Call `Open` to load an existing profile, or
explicitly `CreateProfile(starterUnits)` for a new one. Never respond to load failure by resetting
files. Only this flow may write its stores during the session. Saved profile ownership supplies unlocked state; `UnlockUnit` refreshes it immediately.

Battle saves use a flow-version/profile/content identity derived from these IDs.
Raw battle-store saves from earlier scaffolding are not automatically imported;
keep them separate or provide an explicit migration. Profile saves use progress schema 5 with schema-1/2/3/4 read compatibility (see UnitCustomization.md). Content revision changes also require migration or a
separate development save directory.

`Missions` exposes the authored catalog. `IsAvailable(progress)` supplies its
mission-specific progress gate. An optional validated campaign catalog now supplies authored prerequisite structure
(see CampaignProgression.md); no production campaign content is invented here. Mission reward policies remain explicit trusted authored inputs.

## Squad validation

`ValidateSquad` and `Start` enforce the same rules: authored size limits, known
unlocked units, no duplicates, at most one Apex, and any faction restriction.
Campaign entries require a faction and at least three unlocked standard units
from it in the owned roster. This entry requirement does **not** force all missions
to field three units; each mission supplies its own squad bounds. Unrestricted
non-campaign entries permit mixed-faction squads while retaining the Apex limit.
Apex status and faction ownership must come from trusted unit definitions.

The factory receives a fresh attempt ID and an immutable selected squad. It must
build a fresh battle whose mission ID, attempt ID and ordered squad match exactly.
The normal mission/snapshot validators check its battle setup. No battle is saved
when validation or construction fails.

## Lifecycle

1. **Selection:** start a validated mission; checkpoint before publishing it.
2. **Battle:** call `Execute` for one completed Core command. The command runs on a
   private copy; the resulting snapshot is saved before it becomes visible.
3. **Results:** a terminal result is already checkpointed. Call `ClaimRewards` to
   commit its receipt and progress, then display that returned receipt.
4. **Selection:** call `ReturnToSelection` after a successful claim. A new replay
   receives a new attempt ID and the authored replay reward bundle.

`ReadBattle` always returns a detached copy for rendering/inspection. Mutating it,
or a battle reference retained by a factory/command, cannot change flow authority.
The application must route gameplay commands through `Execute`. A rejected rules
operation may leave an identical checkpoint; command callbacks must not perform
external side effects or invoke nested state-changing flow operations.

An active or unclaimed battle blocks replacement. The flow has no implicit abort,
forfeit or destructive reset. An externally ended battle without a mission result
is rejected. Defeat results use the same claim/return sequence and preserve prior
progress under the current no-defeat-rewards foundation.

## Reopening and interruption

`Open` reads both stores before publishing state:

- No battle: selection.
- Unclaimed active battle: resume battle with saved costs and objective state.
- Unclaimed terminal battle: results, ready to retry its claim.
- Battle with an existing matching attempt receipt: selection. This includes an
  older active checkpoint recovered for an attempt already claimed in the profile.

After a claim is durable, returning to selection needs no additional file write or
battle deletion. The receipt is the completion marker. Reopening immediately after
a successful claim therefore goes to selection even if its result screen was not
dismissed; product UX may later offer a receipt-history view.

Any uncertain save/claim outcome sets `NeedsReload`. Do not use stale `Phase`,
`Progress` or `Receipt` for actions while it is true; call `Open` and render the
newly loaded state. Commands that throw before persistence leave committed state
unchanged. An interrupted claim reloads either unclaimed results or selection with
the saved receipt; it must not award resources in presentation code.

`Recovered` reports damage encountered during this session. The underlying two-file
stores still have bounded recovery: corruption may roll a store back one checkpoint.
If no valid copy exists (including a torn first-ever battle write), loading fails
closed and requires explicit recovery. This flow does not promise survival of
arbitrary file loss or simultaneous rollback of both stores. Filesystem/device
recovery UI, migrations, cloud sync and mobile lifecycle hooks remain future work.

## Verification

Console integration tests exercise the whole flow, reopens at battle/results/
claimed/selection boundaries, replay identity, defeat, squad and availability gates,
detached snapshots, invalid factories, nested callbacks, interrupted command and
reward writes, lost acknowledgements, recovered claimed attempts, profile identity
and missing content. Windows/Linux CI runs these against the same Core code Unity
will use. Unity scene wiring, touch UI, IL2CPP and iPhone testing remain deferred.

Saved advancement is applied once to new deployments by the flow; resumed battles
retain their stored stats. See UnitAdvancement.md for supported bonuses and caps.

Free per-unit customization is available at selection; new deployments combine it
with advancement additively. See UnitCustomization.md for replacement/reset semantics.
