# Mission preparation and results interface

`Views/MissionPresentation` owns a `MissionFlow` created from one validated `ContentCatalog`. It provides an engine-independent contract for the future mission menu, preparation screen and results screen. It does not render Unity screens or add authored narrative, mission content or balance values.

## Lifecycle and preparation

Construct with the catalog, existing battle/profile save adapters and profile ID. Explicitly call `CreateProfile()` for a new profile or `Open()` for an existing one; opening never silently creates or replaces a profile. `NeedsReload` blocks screen reads/commands after a persistence error. Opening clears transient menu choices. Discard all old battle-controller references after reload.

`Read()` returns immutable mission cards, collectible unit inspection views, campaign progress, saved player progress and current phase. Mission cards include the authored objective definitions, faction and squad requirements, first-clear or replay victory reward bundle, missing prerequisite IDs and the three-standard-unit faction ownership gate. Locked missions remain inspectable. Names, descriptions and localized lock text should be resolved from IDs by a future content/UI layer; no invented lore is supplied. Defeat has no resource reward under the current policy.

Call `SelectMission(id)` and `SetSquad(ids)`. Squad order controls deployment-slot order. Selecting another mission clears the old squad. Input is copied and bounded to eight entries, matching catalog deployment capacity. Invalid drafts remain visible with the existing `SquadFailure` reason; launch is disabled until `CanStart` is true. Ownership, faction restrictions, campaign gates, duplicates, size and the one-Apex limit are validated by the authoritative flow at launch, not just by UI filtering. A unit's `EligibleForMission` only describes ownership/faction compatibility; it does not promise an available slot or allow a second Apex.

Unit inspection excludes NPC templates and exposes base content plus saved advancement/customization-adjusted health, armor and ability power, initiative, movement, kit availability and all four ability components. Range and targeting are available on each field ability. Passive placeholders remain visibly marked. Battle-only status buffs, cooldown use and conditional damage/cover are not included in deployment stats. Locked units show base stats. Modifier calculations use the same functions as deployment; tests compare inspected and deployed values.

The start button must call `Start(view.PlanId)` with the displayed preparation token. Changing mission/squad invalidates that token. Starting revalidates and saves before returning a `BattleInteraction`; a duplicate launch cannot replace an active battle. The menu does not save drafts or assume a selected squad when first opened.

## Battle and resume

In the battle phase, `CanResume` and `Resume` expose mission/attempt identity, round, current actor, spent costs, units and battlefield state. `ResumeBattle()` returns the same interaction controller within this menu lifetime, preserving animation locks. It does not run enemies, advance rounds or reset costs. After reopening it creates a fresh interaction controller from saved state, without replaying old input or animation. Preparation and campaign claims are blocked while a battle or unclaimed result is pending. The Unity host owns screen transitions and animation completion.

## Results and collection

`Results` exposes victory/defeat, objective outcomes, expected rewards, first-clear status, saved victory count and any durable receipt. Expected grants are not credited until a successful claim. `ClaimRewards(displayedAttemptId)` must match the terminal attempt and returns the actual receipt with immutable before/after player progress, allowing UI comparisons of balances, mission completions and campaign gates. Unit ranks do not auto-increase on mission completion. A repeated same-attempt claim returns `AlreadyClaimed` and no additional grant or write. The results screen disables claiming after a receipt exists.

Call `ReturnToSelection(attemptId)` only after claiming that same result, including defeat's empty receipt. A reload before collection restores unclaimed results; a reload after a durable claim opens selection, with receipt history retained in `Progress.Claims`. No automatic second claim or animation is issued.

Campaign completion is separate: `Campaigns` reports completion/claim status, and `ClaimCampaign(id)` delegates to the existing idempotent claim while at selection. Only the starter campaign grants its Apex and the existing account-stable next-faction bundle; opening these screens does not award them. This adapter neither introduces random rerolls nor grants an Apex for ordinary campaign completion.

## Save failures and validation

Save exceptions propagate; no successful launch/collection result is returned. Reopen before continuing. A failed reward write recovers the previous unclaimed state when available; a durable write with a lost acknowledgement recovers the saved receipt once. A torn very first battle checkpoint has no previous battle fallback and fails closed under the existing store policy; preserve the files rather than silently start over. This PR does not change that storage policy or any save schema.

Tests cover lock explanations, pure reads, copied drafts, squad restrictions, inspected/deployed stats, stale launch tokens, the complete mission loop, resume, first/replay rewards, defeat preservation, duplicate/wrong-attempt claims, save interruptions and one-time campaign completion. All content is the existing abstract fixture. Unity rendering, real device interaction and production canon content remain separate work.
