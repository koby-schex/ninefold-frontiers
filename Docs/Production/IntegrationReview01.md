# Integration review 01 — core to first playable

## Scope and finding

Reviewed the interfaces through PR 24: mission preparation/results, battle session/presentation/input, collection operations and shared flow/save lifecycle. This is a focused cross-system review, not complete security, performance or production-readiness certification. Abstract fixtures remain engineering content, not the canonical opening campaign.

Found a stale-controller gap: a battle session bound its attempt only on first use, and reloading the same flow did not expire retained sessions. An unused session could attach itself to a later attempt; a previously displayed screen could issue commands after reload. The host was instructed to discard old references, but the core did not enforce it.

Fix: the flow issues an in-memory session identity on reload and successful mission start. A session captures that identity and the current attempt when constructed. Reads and command ownership checks reject expired sessions before saving. A failed reload also expires prior sessions. Ordinary battle commands do not expire the current session. Construct sessions only from an opened flow and recreate them after reload/start; the mission menu already does this. Discard old screen references as before; the guard adds protection. No save-format changes.

## Added verification

- Unused sessions cannot adopt a later mission; selection-time sessions cannot adopt newly started battles.
- Retained player/enemy sessions and screen controllers cannot command a reloaded flow.
- Public-interface journey: profile, three-unit preparation, move, reload/resume, objective completion, rewards, unlock, advancement, customization, upgraded deployment, starter completion, next-faction entry and ordinary campaign completion.
- Combat journey: attack preview/confirmation, spent-action resume, enemy advancement, victory and reward collection.
- Real filesystem journey: directory-backed adapters, fresh menu instances, movement resume, reward receipt recovery, unlock/upgrade persistence and all four alternating save files. Unique temporary test directories are cleaned after each run.

These complement existing injected torn-write/lost-acknowledgement tests. Desktop directory tests do not establish mobile filesystem durability or app lifecycle behavior.

## Unity handoff gates

| Area | Repository evidence | Remaining work |
| --- | --- | --- |
| Rules/player flow | Core interfaces and automated journeys | Wire one owning menu/controller graph; surface validation and save errors |
| Project import | Pinned editor, package manifest, explicit foundation setup script | Import with pinned editor; run setup; review/commit generated settings, assets and package lock; compile Unity assemblies |
| Screens | Immutable menu, collection, battle and results views | Portrait rendering, always-visible health bars, safe areas and overlap testing |
| Input | World-space preview/confirm/cancel | Screen picking, touch tolerance, camera gestures, UI event isolation; avoid duplicate gesture dispatch |
| Animation | Post-save events and acknowledgement locks | Playback, reconciliation, acknowledgement, reduced-animation path and scene teardown |
| Saving | Shared single-writer flow, directory adapters, failure tests | Device save path, explicit create/open, suspend/termination/cold launch and error recovery |
| Content | Validated abstract catalog/classification | Canon-reviewed definitions and three authored opening encounters controlled by current Master Lore and locked masters |
| Art/audio | Presentation assembly boundary; no runtime presentation C# implementation yet | Models, rigs, materials, environment, lighting, effects and audio with separate quality review |
| Performance | No device measurements yet | Snapshot-read cost/allocation, frame time, loading, memory and thermals; avoid rebuilding all menu snapshots every frame |
| Release | No verified Unity/device build evidence in this review | Build/install and offline device playthrough with retained evidence |

Next: wire an abstract Unity integration scene using the tested interfaces, clearly labeled as a systems test. This establishes interaction feedback, not final graphics or canon approval. Production presentation and the canon-accurate opening have separate gates. The pinned Windows Unity editor and iPhone testing plan remain unchanged.
