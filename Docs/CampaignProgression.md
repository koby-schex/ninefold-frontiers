# Campaign progression and completion rewards

This is authored-data infrastructure with abstract test content, not a production
campaign, story, drop table or canon revision. Campaigns support up to 4,096 mission
nodes, including the required 100+ short-mission production scale. Test campaigns
may be smaller; no mission-duration or combat-complexity rules change here.

## Mission order and progress

A `CampaignDefinition` supplies stable campaign/faction/revision IDs, an ordered
mission list, completion resources and an optional starter bonus. Each node declares
its prerequisites and whether it is required for completion. Prerequisites must be
earlier nodes in the authored list; this supports branches and joins while rejecting
cycles, duplicate IDs, unknown prerequisites and ambiguous order. Every prerequisite
must have a saved victory before that node opens. The list alone does not imply an
edge; authors must declare the previous mission when creating a linear campaign.

`CampaignCatalog` validates campaign entries against mission and unit definitions.
Each mission belongs to at most one campaign and must match its faction. A supplied
nonempty catalog must include all entries marked as campaign missions. Empty catalogs
retain the earlier standalone/test flow behavior, without authored campaign ordering.

Saved mission victories drive campaign progress across sessions. Defeat and an
unclaimed battle result do not count as a clear. Completed missions remain replayable,
even if an authored availability callback later closes them. Replays still use the
existing mission replay rewards. Faction ownership and squad restrictions still apply.
A campaign requires three owned standard units from its faction; an Apex never counts
toward that gate. `InspectCampaign` reports total/completed missions, entry access,
available mission IDs, completion and whether completion rewards were claimed.

## Completion transaction

After claiming the final mission result and returning to selection, inspect campaign
progress and call `MissionFlow.ClaimCampaign(campaignId)` when eligible. Future UI must
perform this handoff or display pending completion rewards. Merely displaying a
completed campaign does not mutate the profile. Optional missions do not block
completion unless they are prerequisites of required missions.

The completion claim saves the campaign ID/revision, required mission IDs, exact
resource grants, newly granted units and selected next campaign in one transaction.
A separate completion receipt makes interruption between the final mission claim and
campaign claim recoverable: reopen, inspect, and claim the pending reward. An already
claimed campaign returns its original receipt without granting or writing again,
even if the catalog was changed or removed. Completion is permanent for that campaign
ID; future expansion/reset semantics require an explicit content/migration decision.

Ordinary campaign definitions can grant authored resources, including fragments;
they have no direct-unit grant field and do not automatically unlock an Apex or
repeat the starter handoff. Empty completion bundles are valid and still recorded.

## Starter-only bonus

At most one campaign in the catalog may define `StarterCampaignBonus`. Flow profile
creation requires its three standard starters to match that campaign's faction.
The validated bonus specifies the starter faction's Apex and candidate bundles of
three standard units from other faction campaigns. Targets must be distinct factions
and their unit definitions must match; Apex units cannot substitute for those three.

Completion fully grants the starter Apex and a selected bundle, without spending
fragments. Already-owned units are skipped; there is no duplicate ownership or
invented fragment compensation. Existing fragments are retained. The resulting
profile must have at least one available mission in the selected next campaign or
the whole completion transaction is rejected as inconsistent authored content.

Selection prefers candidate factions whose three-standard-unit entry gate is not
yet met. Among eligible authored candidates it uses a stable SHA-256-based selection
from profile ID and starter campaign ID, independent of input ordering. If every
candidate faction is already accessible, it selects from the full candidate list.
The player does not choose a constant reward target. This is repeatable selection
infrastructure, not a production random drop-rate or security promise. The selected
target and actual grants are persisted; a retry cannot reroll a committed reward.
Production candidate pools and presentation remain unauthored.

A profile can consume the starter bonus only once, even if another catalog later
tries to attach it to a different campaign ID. Other campaigns cannot repeat it.

## Save compatibility and limits

Progress schema 3 introduced campaign receipts. The current writer uses schema 5
(customization); schemas 1–4 remain readable without
rewriting them on load; the next committed transaction writes schema 5. Schema-1
ownership is still empty because it was never stored. Schema-2 ownership, spending
and mission receipts remain intact. Mixed-version checkpoint pairs are supported.

Generation and the existing 100,000-transaction limit include mission claims,
fragment unlocks, campaign receipts, advancement and customization. Prefix validation checks all five histories.
Resources, unit ownership and completion receipts either commit together or remain
at the previous valid checkpoint. Failed/ambiguous flow writes require `Open` before
further actions. The existing 8 MiB bound, single-writer requirement and possible
one-checkpoint rollback during corruption recovery still apply; this is not cloud
backup or protection against arbitrary file loss/tampering.

Console tests cover 100 missions, branch prerequisites, replay, optional missions,
defeat, content validation, complete starter-to-next-campaign flow, ordinary rewards,
idempotency, existing ownership, stable selection, interrupted/ambiguous writes,
recovery, mixed-schema saves and subsequent rewards/unlocks. Unity menus, narrative,
production content, IL2CPP and device validation remain deferred.
