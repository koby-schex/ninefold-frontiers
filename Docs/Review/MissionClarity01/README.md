# Mission Selection & Briefing — Clarity 01 (PT-051)

## What this pass solves

The existing Unity integration playtest displayed enormous mission buttons, repeated labels, a battlefield schematic before any objective explanation, and equally prominent navigation on several screens. This pass improves what the player must **understand and choose**, rather than adding decorative UI.

The Ninefold canon, locked World Five sanctuary image, core combat, progression, mission ordering, reward definitions and artwork files stay untouched.

## Mission path: which encounter is next?

- Back returns to the Campaigns overview and preserves the existing selected campaign.
- The page starts with a short campaign title, completion summary and a thin progress meter.
- Authored mission order and core `Available`, `IsReplay`, `FactionRosterLocked`, `MissingPrerequisites` decide status and action, rather than any UI guess.
- Each mission shows a small ordinal, status and title. Available missions show a brief objective and one **View briefing** button. Replays use **Review replay**.
- The first available unplayed mission has the only *gold* primary button. Existing unlocked replays/alternatives use secondary buttons.
- Locked missions have concise eligibility/prerequisite explanations and **no launch button**. They remain visible without suggesting an invalid action. The authoritative core still validates launch independently.
- Abstract fixture mission and campaign names remain temporary labels, not canon.
- Bottom navigation is visible, compact and consistent with Campaign Selection. A fixed footer must not obscure mission cards.

## Briefing: what will I do?

- Back returns to Mission List for campaign missions and Home for the standalone Quick Battle, as before.
- Preserve **1 BRIEFING / 2 SQUAD NEXT** and the current mission title; only show an extra context label for a replay.
- The **objective** is the first substantive content, followed by the **authored battlefield schematic** and then the **actual first-clear or replay reward grants**. No map content is invented or rewritten.
- More compact legend and reward presentation reduce scrolling. The existing map legend, objective/cover/slower terrain/deployment markings remain.
- The **Choose Squad** action stays in the fixed dock, with actual min/max squad size, faction restriction and untimed turns. It navigates to the unchanged Squad screen; it does not deploy automatically or alter save state.
- The no character/progression loss on defeat rule is unchanged. This screen shortens some explanatory text, but gameplay rule remains authoritative and described in existing help.
- All navigation and command callbacks remain the same. UI reads do not write saves, award resources or change squads.

## Style scope

The new `Assets/Ninefold/Resources/Home/MissionClarity.uss` loads after existing Controls and CampaignClarity and styles only the two mutually exclusive root classes:

- `mission-path-mode` for Page.Missions
- `mission-briefing-clarity-mode` for Page.Briefing

These classes are cleared when leaving the screen. No changes to Home, Campaign overview, Squad, Units, Settings, Battle or Results. No new world symbolism or new assets. Deep ink, ivory and muted gold follow the project's Ninefold website reference while the sanctuary stays only a subdued background.

## Batch Unity test when ready

1. Pull merged main while out of Play mode and open the launcher. Check **PT-051** or the later milestone if more UI PRs have merged.
2. In Unity 6.3 LTS (6000.3.21f1), verify no USS import or script compilation errors; the stylesheet should load without a missing-resource warning.
3. From Campaigns → an unlocked campaign, verify the next mission is the only highlighted mission; examine later locked missions for accurate prerequisites. Confirm locked mission UI cannot open a briefing. A completed mission uses **Review replay** and saved rewards are not changed.
4. Tap **View briefing**: check objective → map → rewards ordering, authentic geometry and correct first/replay grant values. Use Back → Missions, then re-enter briefing without losing progress.
5. Tap **Choose Squad** and verify the existing Squad page operates as before, including its faction, min/max and one Apex restrictions. Return Back to briefing and confirm the draft persists.
6. Open Quick Battle from Home. Briefing Back must go to Home, not a campaign mission list. Mixed-faction squad text stays accurate.
7. At 320×568, 390×844 and 540×960 portrait, plus a short landscape Game view, fit the **Game-view Scale**, then verify readable line wrapping, no horizontal clipping, scrolling, visible bottom nav/Choose Squad action and no overlap with device safe areas.
8. Check touch/keyboard focus, disabled and pressed states and that the Home, Campaign overview and Battle HUD have not changed in this pass.

## Validation limits

Static repository analysis can confirm file scope, selector targeting and expected callback paths; it cannot compile Unity, reproduce physical touch behavior, or certify the finished look. Review `Preview.html` for a deliberately simplified browser approximation only. This PR does not claim production-resolution artwork.
