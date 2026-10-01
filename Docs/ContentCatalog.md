# Versioned content catalog

`Ninefold.Core.Content.ContentCatalog` connects typed unit, ability, kit, mission,
reward and campaign definitions to the existing offline `MissionFlow`. Catalog
construction validates the package before a flow can write any save files. This
is a headless authoring foundation, not a Unity scene, importer or playable build.

## Identity and revisions

`ContentManifest` schema 1 records package ID, content revision, balance revision,
classification and an optional canon-source reference. Its length-framed SHA-256
revision binds battle saves through the existing flow. Bump content revision for
any changed definition/reference and balance revision for numeric tuning; this
is explicit author discipline, **not an automatic hash of every content field**.
A revision change rejects incompatible active battle saves; migration remains
future work. Profile receipts retain their existing revision and schema behavior.
Reward, campaign, unlock, advancement and customization revisions remain explicit
on their own definitions as well. Their receipt semantics are unchanged.

`AbstractFixture` marks engineering-only content. `CanonCandidate` requires a
source reference, but does not mean canon approved or release ready. No validator
can establish lore accuracy. Before production authoring, verify the current
Master Lore Archive, applicable Production Foundations and locked visual masters,
and record the relevant sections and review decisions under the canon policy.
This PR contains no production lore or visual masters.

## Definitions and validation

- IDs are ordinal, bounded, unique within each collection and cannot start with
  `$`. Dictionary/list views and nested definition inputs are immutable.
- Kits resolve exactly one passive, normal attack, main and signature ID, in the
  correct slots. Active effects use existing deterministic damage/healing rules.
  Passive definitions may bind a validated status rule (see
  [passive/status effects](PassiveStatusEffects.md)) or remain explicitly inert
  placeholders. External-condition signature readiness is rejected
  until there is a supported authored condition binding.
- Units own base health, armor, initiative, movement, body and kit. Range/power live
  on abilities. Optional roster definitions supply faction, Apex classification,
  fragment unlocks, advancement and customization. Units without roster data are
  NPC templates and cannot be collected or selected by players. Supported stat
  modifiers must not overflow. Individual fragments must resolve to declared,
  distinct resources. Existing progression limits remain provisional.
- Missions reference NPC templates by unique battle-local spawn IDs. Spawn IDs
  cannot collide with unit definition IDs. Hostile spawns require enemy behavior;
  friendly NPCs cannot register enemy behavior. Allied healing compatibility is
  assigned across all friendly factions at battle creation.
- Missions support one primary and up to two optional objectives. `$squad` expands
  to selected unit IDs; `$leader` means the first selected unit, not a new unit
  role or an initiative override. Fixed references must name a mission actor.
  Single-target objectives cannot use `$squad`. Duplicate/overlapping aliases
  are rejected after binding. All six existing objective kinds are supported.
- Squad slots must be fillable with eligible units and at most one Apex. Campaign
  factions need three standards. All deployment slots are checked using the
  largest eligible body envelope; this deliberately conservative rule guarantees
  space for any valid ordering, including later Apex unlocks. Bounds, movement
  obstacles, actor overlaps, enemy guard areas, objective zones and interaction
  bounds are checked. Validation does **not** prove every mission solvable or
  balanced; reachability, encounter tuning and player experience need playtests.
- Every mission has a matching reward policy; every grant references a declared
  resource. Existing campaign graph and starter-bonus validation is reused.
  Errors fail fast with the mission/unit/kit/reward context where applicable.

Catalog v1 is a trusted, typed C# authoring API. It does not parse untrusted data,
load remote packs or provide JSON/ScriptableObject serialization. The existing
low-level `MissionEntry` API remains available for focused tests and future adapters.
Catalog squads are limited to eight slots to match the current enemy perception
budget; this is an engineering limit, not a new locked design decision.

## Abstract package and execution

`AbstractContentPackage.Create()` (or `Create(true)` with an abstract passive) contains two artificial factions, each with four
standards and one Apex, one noncollectible enemy template, two starter missions,
one follow-up mission and a mixed-squad encounter. Its names, stats, body boxes,
fixed reward bundles and short campaign lengths are test values only. Production
campaigns still require at least 100 missions; random reward selection and the
production economy are not implemented by this fixture. No World Nine content is
implied by its enemy template.

An adapter can construct the catalog, call `CreateFlow(battleFiles, progressFiles,
profileId)`, then `CreateProfile(catalog.StarterUnits)` once for a new profile or
`Open()` for an existing one. Flow remains the authority for ownership, one-Apex
limits, campaign gates, saved commands and claims. Mission factories are generated
centrally from catalog definitions; content authors no longer supply battle lambdas.

The fixture tests exercise victory, saving/resuming, reward claims, fragment
unlock/advancement, customization, the one-time starter Apex and next-faction
handoff, ordinary completion, mixed-faction compatibility and enemy behavior.
Negative cases cover missing/duplicate references, slots, geometry, aliases,
resources, revisions and immutable collections. Run through the existing .NET
console suite and repository checks. Unity import, IL2CPP and devices remain pending.

Active field profiles can now reference declared status definitions and explicit
target rules, including standalone support effects; see [active abilities](ActiveAbilityEffects.md).
