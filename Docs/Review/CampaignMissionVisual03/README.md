# Campaign & Mission Visual 03 — PT-057

## Purpose

The latest user playtests established that gameplay and menu functionality are becoming solid, but the screens contain too much permanent text and too little real visual information. This pass targets the **Campaign overview, Mission Path and Mission Briefing** while preserving the existing progression/availability/claim rules and the Work chat's 3D model pipeline.

## Visual changes

### Campaign destinations

- Continue to use the optional **GetCampaignPreview(campaignId)** bridge from Visual Foundation 02 when artwork is actually available. Increase the wide art-slot prominence, while keeping neutral noncanonical atlas fallback for the two abstract fixture campaigns.
- Reduce repeated copy and use a short `completed / total CLEARED` label alongside the authoritative meter. The 3-standard-units-per-faction prerequisite stays visible as one short instruction. All lock explanations and commands remain present.
- World Five's approved sanctuary is **not** painted over the unrelated campaign cards. No world, species, faction, or campaign art is newly asserted as canon.

### Mission path

- Replace the oversized plain mission numbers with compact route pins. A cleared/replay mission gets a checked marker; a locked one remains subdued; the **next available unplayed mission** gets gold emphasis. Status is read from the existing `MissionMenuView` and authored mission order.
- Render an **authored field scout** only for the next available unplayed mission, using the *existing* `MissionContent.Map.Bounds`, ground/obstacle geometry, objectives and deployment positions. No fabricated geography or new map file; completed replays and locked missions remain compact.
- Mission prerequisites, faction gates, read-only saved-battle mode, and all `SelectMission` callback/eligibility checks stay unchanged.
- The map scout is preview-only (picking ignored) and does not accept movement/attack commands or instantiate scene geometry. For 100+ mission campaigns, the UI adds at most **one** extra map schematic instead of creating a potentially heavy thumbnail for every entry.

### Briefing

- Keep the objective available **before the map**, but make it a concise strip. Give the existing authored battlefield diagram more visual area at normal portrait size.
- Replace a verbose plain-text legend with four swatches: Deploy (cyan), Cover (gray), Slow (amber), Objective (gold). The swatches use the existing map color semantics.
- Reward lines still display each actual granted resource/quantity. Shorten the explanatory note; do not change claim timing, campaign rewards or replay logic.
- The fixed Choose Squad action remains untouched. Replace the long one-line constraint paragraph with **UNITS / SQUAD / TIMER** summary chips based on `MinimumSquad`, `MaximumSquad` and `IsCampaign`. `NONE` under TIMER means no turn clock, **not** unlimited actions.

## Files changed

- `Assets/Ninefold/Runtime/Presentation/FrontiersPlaytest.cs`: presentation only, map helper gains an optional compact mode, with a static color-key helper.
- `Assets/Ninefold/Resources/Home/CampaignMissionVisual03.uss` and its Unity meta file: opt-in style rules scoped to overview/path/briefing.
- `Assets/Ninefold/Editor/PlaytestLauncher.cs`: new stylesheet import preflight.
- `Assets/Ninefold/Runtime/Presentation/PlaytestRevision.cs`: source milestone `PT-057`.
- This review note.

## GitHub multi-chat coordination

- Both previous PRs #67 and #68 and Collection PR #69 were merged before branching this pass.
- The separate Work chat owns `SourceArt/`, `BuildTools/Art/`, generated review/production prefabs, model materials, animations and render-camera integration. **This PR does not edit those** and does not map fixture campaign IDs to finished lore artwork.
- No changes to `IFrontiersPreviewSource`, Unit Collection, unit content, economy, combat, save persistence or authored mission definitions.
- `FrontiersPlaytest.cs` remains a conflict-sensitive shared host; compare the changed file and the branch's mergeability again before merge, and do not force-overwrite if a Work PR touches it.

## Unity review after merge

1. Stop Play mode; pull `main` and let Unity 6.3 LTS finish compiling/importing. Check launcher milestone **PT-057**, asset availability and Console import errors.
2. At **540×960 fitted Game view** and **390×844 / 320×568**, open Campaigns. Art surfaces should occupy more space but buttons/locked preview remain fully reachable. Read actual progress/lock prerequisites.
3. Open Mission Path. Check sequence numbers on *all* authored missions, including after completed replays; check pin states and the highlighted next mission. The scout should appear only for that next mission and display its existing authored cover/slow/deploy/objective geometry.
4. During a saved battle, Campaigns/Missions remain read-only and must not allow another mission to begin. Resume the checkpoint unchanged afterward.
5. Tap a mission briefing: objective text remains above the larger diagram. Confirm all four legend keys match colored geometry and all reward lines/costs use real saved-state values. The fixed Choose Squad button remains reachable and preserves the correct roster/squad constraints.
6. Confirm result claims/replays, Home, Units, battle HUD, and model/editor art review scenes behave as before.
7. Test short landscape, keyboard focus, disabled actions, safe areas and long objective text wrapping. The current abstract fixture content is still **not** canon.
8. The user should capture Campaign overview, an open Mission Path with scout, and Briefing at fitted portrait scale to direct the next visual art pass.

## Validation caveat

Repository CI and static GitHub checks do **not** compile the Unity UI Toolkit assemblies in Editor or certify the preview map's real-world screenshot quality. Run the Unity smoke check before calling this release-quality. Current art slots still require approved world illustrations from an independent producer.
