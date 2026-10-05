# Architecture boundaries

Core turn scheduling, action/movement budgets, ability availability/costs and
single-target health resolution, supplied-path movement validation, destination routing, direct targeting,
finite mission objectives/outcomes, and bounded enemy turn behavior are implemented. Other responsibilities below remain planned; see CombatTurns.md and
AbilityUse.md, HealthResolution.md, Battlefield.md, Pathfinding.md, Missions.md,
EnemyTurns.md, BattleSaves.md, MissionRewards.md, MissionFlow.md, UnitOwnership.md, CampaignProgression.md, UnitAdvancement.md, UnitCustomization.md ContentCatalog.md PassiveStatusEffects.md ActiveAbilityEffects.md EnemySupportAI.md and BattleSession.md for exact implementation boundaries.

- **Core:** plain C# rules and committed state; no UnityEngine references. Combat,
  missions, progression and saves have reserved folders. Commands validate legal
  targets, movement and action budget before changing state.
- **Flow:** headless mission selection, squad checks, saved-command execution,
  result claiming and resume coordinate Core stores. See MissionFlow.md.
- **Presentation:** Unity components render committed state and collect intent.
  Animation timing must not determine damage, objective progress or reward grants.
- **Content:** the immutable typed catalog validates shared unit/ability/mission,
  resource and campaign definitions, then builds mission flow entries. An abstract
  package exercises the full loop; production content and asset import are pending.
  Saves store IDs and state, never scene-object references.
- **Persistence:** versioned battle snapshots and alternating-file recovery are implemented.
  Separate profile checkpoints now persist mission completion and reward receipts together.
  Content/version migrations, production campaign content and economy remain deferred.
  Binary schemas and SHA-256 are for integrity, not tamper-proof storage.
- **Services:** optional cloud sync, purchases and future online play sit outside
  the installed offline solo loop. No service SDKs are included in this scaffold.

Core and Presentation assembly definitions enforce the initial dependency direction.
Editor setup lives in an Editor-only assembly. Add packages only for a concrete need;
input, UI and testing packages can be pinned in the PR that implements them.

Gameplay constraints include untimed solo turns, initiative affecting order rather
than activation frequency, movement plus one primary action, no chance-to-miss,
four ability components and a maximum of one player Apex per battle. Implementing
these rules still requires the approved design specifications; this file does not
supply balance values or supersede the master.

Presentation consumers use `Views/BattlePresentation`: immutable snapshots and previews read detached state; commands return animation hints only after the session checkpoint succeeds. See [Battle presentation](BattlePresentation.md).

`Views/BattleInteraction` owns transient input intents and animation locks above that facade. Intent validity is bound to an in-memory flow checkpoint identity. Health overlays are always available independently of selection. See [Battle interaction](BattleInteraction.md); actual Unity rendering and gesture recognition remain separate.

`Views/MissionPresentation` constructs its flow from the same content catalog used for mission and unit display. Preparation delegates launch validation to that flow; results consume its durable claims. See [Mission preparation and results](MissionPresentation.md).
