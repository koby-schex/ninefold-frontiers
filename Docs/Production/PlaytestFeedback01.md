# First playtest feedback — navigation and battlefield clarity

Koby's first Windows Unity playtest succeeded after manually assigning PlaytestPanel.
The next priorities are clear home/mission navigation, bounded battlefields with good
flow, a unique battlefield for every mission, visible attack range before targeting,
and movement toward distant destinations up to the unit's remaining allowance.

## Accepted production direction

- Home must clearly separate campaigns, other battle modes, and collection/progression.
  A mission briefing and squad screen come after mission selection, before deployment.
- Every mission has an authored, bounded battlefield. Its edges must be legible and
  enforced by gameplay; camera motion cannot imply an endless navigable arena.
- Different missions must not use the same battlefield layout. Replaying the same
  mission may use its original map. World art assets may be reused, but each mission
  needs its own terrain arrangement, approaches, deployment and objective placement.
  A new name or cosmetic reskin alone does not establish a distinct battlefield.
- Map flow should offer readable routes and meaningful positioning without making
  ordinary short missions unnecessarily complex. Validate route access, deployment,
  camera readability, attack lanes and expected traversal time during mission authoring.
- Selecting Attack should reveal nominal range before a target is chosen. Legal-target
  indication must also account for line of sight, teams, ability rules and current state.
- A distant legal destination should produce the affordable prefix of a valid route.
  Show its stopping point before confirmation; no automatic movement on future turns.
  Map edges, occupied destinations and disconnected paths remain invalid.

These are gameplay/production requirements, not revisions to world canon. The current
Master Lore Archive, PF1–8 and locked visual masters remain controlling sources.

## This implementation

The abstract playtest now has Home, campaign list, mission briefing/squad, collection,
battle and results screens. Home can resume an active battle without permitting a second
battle or progression changes during combat. Existing progression stays in the original
profile; an unfinished legacy battle retains its original catalog until claimed.

Four fixture arenas have distinct bounds and obstacle arrangements. One has slow ground;
the combat map has central cover with two approaches. The renderer uses actual map bounds,
obstacles and ground regions, rather than a fixed decorative floor. It frames the arena
and shows edge rails. This is a first layout pass, not production environment art or proof
that hundreds of final missions have been authored or playtested.

Selecting an ability exposes immutable target verdicts from core rules. The flat fixture
renderer shows nominal range clipped to map edges and marks valid units. Target previews
remain authoritative when walls, occupancy, team or health rules make a target unavailable.

Destination input now previews an affordable path prefix using existing route/collision
and weighted terrain costs. The existing strict full-destination routing API retains its
old behavior for other callers. Confirmation still revalidates and saves before animation.

The setup command assigns/serializes Panel Settings after component creation and repairs
missing panel references when reopening a scene. Runtime startup reports a missing panel
as an explicit Console error instead of silently leaving a blank screen.

## Verification

Automated regressions cover clipping, terrain cost, detours, map bounds, occupancy,
disconnected paths, zero/stale turns, duplicate distant taps, saved positions, target
verdicts, unique fixture maps, and existing profile/claim preservation across layout versions.
Windows Unity visual/input verification of this pass remains a user playtest gate.

Generated scene/settings changes from the user's first import are still local to their
Windows clone. Preserve them when pulling; they have not been captured remotely yet.

## Consolidated visual and usability pass

Koby requested a larger batch before the next playtest because launching a test takes time
and the presentation must be readable enough to evaluate the systems. This pass therefore
covers the full home-to-results flow in one update, with a single later Windows review.

- Navy panels, cyan primary actions, gold highlights, clearer type hierarchy and consistent
  touch targets replace the undifferentiated button list. These are provisional interface
  styling choices, not new faction colors, symbols, logos or locked visual masters.
- Home, Campaigns, Units and Settings have persistent navigation. Resume takes priority
  when a saved battle exists. Campaign locks include actionable requirements.
- Mission briefing shows an overview derived from the actual map and explicit squad
  selection states, with Deploy fixed outside the scroll area. Squad toggles preserve scroll.
- Collection focuses on one unit at a time. Upgrade/customization confirmation shows actual
  before/after values and spending separately from browsing; navigation cancels previews.
- Battle objectives and initiative are separate from the action dock. Ability buttons explain
  cooldown, spent action and signature readiness; common rejected inputs use readable text.
- Cover tops, terrain stripes, objective plates, unit footing/collars and persistent health
  badges improve the abstract arena. Range uses polygon clipping to the arena and an unlit
  line above the edge rails. Legal targets still come exclusively from the combat rules.

This pass changes presentation only: no canonical content, reward tables, unit balance,
map gameplay geometry, save format or progression rules change. Reduced motion is a local
UI preference. Final 3D characters, environment assets, animation, sound and mobile GPU
profiling remain separate production work. Repository/core CI cannot certify Unity rendering;
the consolidated checklist in `Docs/UnityPlaytest.md` is the remaining visual/input gate.
