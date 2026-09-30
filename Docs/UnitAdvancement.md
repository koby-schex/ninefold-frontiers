# Post-unlock unit advancement

This plain C# foundation adds permanent fragment-funded advancement. It is separate
from future player-selected stat customization. Test costs, rank counts and bonuses
are provisional engineering values, not production economy or locked canon.

## Definition and persistence

An optional `UnitAdvancementDefinition` on `RosterUnit` supplies the same unit and
fragment resource IDs as its unlock definition, a revision, per-stat caps and an
ordered list of steps. Each step has a positive fragment cost and a **cumulative**
bonus over authored base stats. The number of steps is the rank cap. Bonuses must
never decrease and every step must improve at least one configured bonus.

Owned units begin at rank zero. Call `MissionFlow.AdvanceUnit(operationId, unitId)`
from selection, reusing the operation ID for retries of that request. The store
reloads durable progress, validates ownership/rank/cost, and writes the exact spend,
new rank and cumulative bonus in one checkpoint before publishing anything.
Repeated operations return their original receipt, even after definitions change.
A different target with the same advancement operation ID is rejected. Operation
IDs belong to the advancement receipt namespace, separate from unlock requests.

New operations at cap or with insufficient fragments do not write or spend. Saved
bonus values are authoritative: if a changed definition disagrees with the saved
current rank's bonus, further advancement requires an explicit migration. New future
costs may differ, but historical spending is never repriced. Null advancement on a
unit means no advancement is authored for it yet; it does not erase saved progress.

The profile is shared across missions, campaigns and modes. Defeat does not remove
ranks. Starter units, fragment-unlocked units and campaign-awarded Apex units use the
same ownership and advancement rules. There is no automatic purchase or advantage
for paying players in this infrastructure.

## Bounded stat application

Bonuses are integer basis points: 100 = 1%. This implementation has a **provisional
10% engineering ceiling per stat**, and definitions can set lower caps. This ceiling
is not a locked design/balance decision and should be reviewed against the approved
progression design before production content is authored.

The initial supported stats are maximum health, armor, and power (damage/healing
amounts on registered single-target battlefield ability profiles). Power applies
equally to Normal, Main and Signature profiles; it does not change their activation
costs. Movement, range, initiative, cooldowns, actions per turn, Signature use limits,
factions, Apex status, geometry and targeting rules remain authored base values.

`MissionFlow.Start` applies each selected unit's saved cumulative bonus **once** to
a fresh factory-built battle, before its first checkpoint. Factories must supply
base stats, never pre-apply advancement themselves. Each integer stat becomes
`floor(base * (10000 + bonus) / 10000)`. Small values may not increase at every rank;
there is no forced +1 that could exceed the proportional bound. Zero armor stays
zero. Authored partial starting health is scaled by the health bonus with the same
floor rule. Checked overflow rejects deployment before saving a battle.

Fresh deployments may have round one and its first activation prepared, but no
movement/actions or completed activations. Registered health is required for advanced
units, and power bonuses require battlefield ability profiles. Low-level trusted
`HealthAction` calls bypass those profiles, so authored gameplay should continue
using the battlefield command path rather than constructing unrelated raw amounts.

The battle save stores resolved stats. `Open`/resume does not reapply bonuses or
update an in-progress battle to a newer profile rank. New battles use the latest
saved advancement; existing battles retain their original deployment. Unselected
units and enemies do not receive the player's bonus.

## Recovery and compatibility

Progress schema 4 adds advancement receipts. Schemas 1–3 remain readable, with rank
zero until advancement receipts exist. Reading does not rewrite files. The next
transaction writes schema 4, preserving mission claims, ownership, unlock spending
and campaign grants. Mixed-version checkpoint pairs are supported.

All transaction writers preserve advancement history. Generation and the existing
100,000-receipt limit now include advancement receipts. The 8 MiB bound and
single-writer ownership still apply. Bonuses, ranks and spending commit together;
ambiguous writes require a flow reload and retry with the original operation ID.
Corruption recovery can roll back one whole checkpoint; this is not protection
against arbitrary deletion, manual rollback or loss of both files.

Tests cover ownership, costs/caps, bounded definitions, sequential ranks, immutable
snapshots, historical receipts, concurrent retries on one store, failed/torn/
unacknowledged writes, corruption recovery, earlier schemas, campaign Apex awards,
subsequent transactions, real filesystem reopening and actual battle health, armor,
damage/healing, shared progression and resume behavior. Production balancing, player
customization, UI, Unity/IL2CPP and device validation remain deferred.
