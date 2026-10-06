# First polished mission: asset checklist

Companion to the [art brief](FirstMissionArtBrief.md), scoped to `FP-C01-M01`.
Source IDs below resolve to its source register. Status as of 6 October 2026:
references inspected; production assets and their acceptance evidence outstanding.
Existing playtest proxies do not count as completed assets in this checklist.

## Deliverables and dependencies

P0 establishes references and clearance; P1 builds the mission; P2 verifies delivery.
These are proposed production priorities, not new canon. Asset IDs are internal.

| ID | Priority | Deliverable | Depends on | Source / acceptance focus | Status |
| --- | --- | --- | --- | --- | --- |
| REF-01 | P0 | Starter-trio reference register and open-question log | Brief | S1–S3; record exact source section and crop/panel for each feature | Register present; detailed feature log pending |
| REF-02 | P0 | Great Strider scale and turnaround sheet | REF-01 | S2 PF5; full pairing, cradle, Waycaster, neutral/deployed silhouettes | Pending reviewable sheet |
| REF-03 | P0 | Living Veil scale and turnaround sheet | REF-01 | S2 PF5; five lobes, partner and shelter configuration | Pending reviewable sheet |
| REF-04 | P0 | Burdenbeast scale and turnaround sheet | REF-01 | S2 PF5; eight limbs, platform, Avarin and turning envelope | Pending reviewable sheet |
| MAP-01 | P0 | Unique meter-scale mission blockout and portrait camera study | REF-02–04 | S4; complete trio fits every required route and destination | Pending |
| UNIT-01 | P1 | Great Strider production pairing | REF-02, MAP-01 | S2–S3; preserved silhouette, anatomy, equipment and scale | Not built |
| UNIT-02 | P1 | Living Veil production pairing | REF-03, MAP-01 | S2–S3; membrane readability, correct lobe count and independent partner | Not built |
| UNIT-03 | P1 | Burdenbeast production pairing | REF-04, MAP-01 | S2–S3; weight, civic infrastructure and correct contact anatomy | Not built |
| ENV-01 | P1 | Modular traversable ground, edge and broken-ground kit | MAP-01 | S2 PF6 / S3; bounded readable geometry at creature scale | Not built |
| ENV-02 | P1 | Route props and interaction/destination dressing | MAP-01, UNIT-01 | S2–S4; preserve Waycaster identity; proposed prop designs require review | Not built |
| ENV-03 | P1 | Distant environment, sky and lighting setup | ENV-01 | S2 PF6 / S3; depth and World Five identity without inventing a named site | Not built |
| FX-01 | P1 | Survey anticipation, deployment, feedback and completion | UNIT-01, ENV-02 | S2 Waycaster / S4 contextual interaction; no invented combat effect | Not built |
| FX-02 | P1 | Debris forecast, event and settled aftermath | MAP-01, ENV-01 | S4; visible safe response, effects match affected area | Not built |
| UI-01 | P1 | Unit portraits and persistent health/turn/selection indicators | UNIT-01–03 | Approved game abstractions; readable in portrait, one logical entry per pairing | Production pass pending |
| UI-02 | P1 | Objective, interaction and movement feedback treatment | MAP-01, FX-01–02 | S4; compact instructions, clear states, no dependence on color alone | Production pass pending |
| AUDIO-01 | P1 | Locomotion, equipment, survey, debris and ambience cues | UNIT-01–03, FX-01–02 | Source-compatible sound direction; no new voice/dialogue canon | Not built |
| QA-01 | P2 | Reference comparison and scale/clearance evidence | All P0/P1 | S1–S3; discrepancy log resolved before art acceptance | Not run |
| QA-02 | P2 | Unity integration and offline lifecycle evidence | All P1, QA-01 | Correct anchors, animation/state synchronization, resume and local asset availability | Not run |
| QA-03 | P2 | Mobile readability and measured performance report | QA-02 | iPhone 14 plus additional environments; record build/device/settings | Not run |

## Required unit handoff

Each UNIT deliverable includes editable source files, exportable mesh, UVs,
material/texture sources, rig, required clips, LODs, collision/selection anchors,
portrait source and Unity prefab with stable metadata. Keep a recorded asset
revision and approved reference-sheet revision. Record import settings and verify
meter scale, pivot, bounds, attachment points and materials after import.

Minimum animation coverage for this opening slice: idle, start/travel/turn/stop,
partner coordination and required environmental reactions. Great Strider also
needs the contextual survey sequence. Any incapacity presentation required by
mission failure must preserve pairing identity. This is not the full combat
animation list; normal attack, main ability and Signature coverage remains required
for complete roster production in later encounters.

LODs must retain defining anatomy. Texture sizes, bone counts, mesh budgets,
transparency strategy and compression settings remain measurement-driven decisions.
Do not claim mobile readiness from a high-quality still render.

## Review record for each asset

- Asset ID, source-file revision, responsible creator and review date.
- Canon references: exact archive version/section and visual-master panel.
- Approved dimensions, neutral/operational bounds and attachment/contact points.
- Status: working / ready for review / approved reference / integrated / device verified.
- Canon facts, approved game abstractions and new proposals explicitly separated.
- Open discrepancies with resolution and Koby's approval where required; no silent fixes.
- Evidence paths: turntables, material views, portrait capture, clearance capture,
  import settings and device measurements appropriate to the stage.
- License/provenance for externally sourced components; no untracked stock designs
  replacing locked species, equipment or architecture.

## Completion gate

- [ ] All three pairings match reviewed scale sheets and locked masters.
- [ ] All required movement and interactions fit actual unit envelopes.
- [ ] The compact layout is unique, bounded and visually navigable.
- [ ] Units, health, current turn, interactions and hazard states remain readable.
- [ ] Temporary geometry is identified and excluded from final-art approval.
- [ ] Essential assets load offline; retry/resume and presentation agree with game state.
- [ ] Unity captures and measured device results support final acceptance.
- [ ] Unresolved lore or design questions are recorded, not filled with assumptions.

Enemy models, rescue actors, Riftwalker, Apex, additional campaigns and a complete
home/menu art overhaul are separate packages. This list neither completes nor
cancels those broader requirements.
