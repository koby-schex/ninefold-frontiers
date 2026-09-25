# Architecture boundaries

This document specifies planned responsibilities, not implemented game systems.

- **Core:** plain C# rules and committed state; no UnityEngine references. Combat,
  missions, progression and saves have reserved folders. Commands validate legal
  targets, movement and action budget before changing state.
- **Presentation:** Unity components render committed state and collect intent.
  Animation timing must not determine damage, objective progress or reward grants.
- **Content:** stable IDs and versioned definitions reference unit/ability/mission
  data. Saves store IDs and state, never scene-object references.
- **Persistence:** local versioned snapshots, previous-good recovery, migrations,
  attempt IDs and once-only reward records. No implementation or format is locked
  by empty folders. JSON is a candidate, not a claim of tamper-proof storage.
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
