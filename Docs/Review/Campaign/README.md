# Campaign and briefing presentation

Extends the approved Home interface typography, ash-silver/violet colors and champagne actions to campaign selection, mission lists and briefing. Uses the existing sanctuary image only as subdued interface atmosphere; it is not new faction artwork, a mission map or a statement that every campaign occurs on World Five.

## Behavior

- Campaign panels show saved mission progress, the three-standard-unit entry requirement, the next authored available unplayed mission, and pending completion claims. Locked campaigns can be previewed but not launched.
- Mission lists preserve authored order, mark the next unplayed available mission, distinguish completed/replay and locked states, and explain faction/prerequisite locks.
- Briefing renders the selected mission's actual map, objective and first-clear/replay resource grants. The battlefield schematic retains its deployment/cover/slow-ground/objective legend. Choose Squad stays outside the scrolling area.
- Styles are scoped to selection/mission/briefing pages. Home, squad, collection, settings, battle and results retain their existing presentation. No scene or Inspector setup is needed.
- Fixture names, short campaigns and abstract maps remain labeled as development content. No new lore, character designs, campaign geography, progression rules or reward amounts are introduced.

## Review

Open Preview.html from the local repository. This is an interactive browser approximation using the actual stylesheet/font/art assets, not a Unity render or build. It includes Campaigns, Missions, Locked, Briefing and ReplayBriefing views. Use ?screen=ReplayBriefing for the replay sample. Non-target screens show an explanatory placeholder; they remain functional in Unity.

Checked the browser approximation at 320×568, 390×844, 768×1024 and 844×390: five presentation states, no horizontal overflow, visible footer, buttons at least 44 physical pixels high, disabled locked missions, and campaign-to-briefing-to-squad navigation. These checks do not prove Unity layout or compilation.

Existing core regression tests cover campaign eligibility, authored prerequisites, recommendation routing, replay rewards, one-time claims and preparation. Repository checks validate Unity metadata. Unity itself is unavailable in this environment; presentation compilation/USS import and real device rendering require a later playtest.

## Later Unity smoke check

1. Open Campaigns from Home: confirm progress and entry counts match the saved profile.
2. Preview the locked faction; verify the mission button is disabled and explains the requirement.
3. Continue the available campaign, inspect map/objective/rewards, then Choose Squad. Return to briefing and confirm draft selections persist.
4. Replay a cleared mission and verify the smaller replay reward preview. Claim a campaign completion once and confirm the panel updates.
5. At narrow portrait and short landscape sizes, scroll all content; navigation and Choose Squad remain usable. Return to Home and enter a battle to verify the campaign theme is removed.
