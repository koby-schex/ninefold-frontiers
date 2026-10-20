# Campaign Clarity 01 — PT-050

## Motivation

The previous campaign overview displayed two large actions on every available campaign, long instructions and lock callouts, very large serif titles, and a footer that could crowd the Game view at small portrait sizes. The goal is not to introduce a new world illustration or canon claim, but to make it obvious what to do next while giving the official Ninefold visual system room to breathe.

## Implementation

- **One highlighted recommendation across the screen:** prioritize a claimable completion reward, then the authoritative `NextCampaignMission`, then the first eligible campaign. The recommendation is selected from the core's saved player state and the authored catalog; it does not bypass faction locks or change gameplay routing.
- Each card has an understated status, campaign name, `completed / total` count, a single thin progress track, and contextual action.
- **Locked:** show the current number of owned standard units (out of three) and a short direction to Units. Keep read-only **Preview missions** available.
- **Active, recommended:** the primary **Continue campaign** button opens the existing mission briefing; **View all missions** remains available as a quiet, secondary text-like button.
- **Pending completion claim:** one highlighted **Collect rewards**, with `View all missions` as a secondary option. No automatic claim or new reward logic.
- **Completed & claimed:** a single quiet **Replay missions** route.
- **Other available:** maintain same functionality with less visual emphasis, so the full screen doesn't contain competing gold buttons.
- The `campaign-overview-mode` class scopes typography, background, cards, actions and smaller bottom navigation to the overview page only. Mission lists, briefing, squad, Units, Home, Results, Battle and Settings are not restyled here.
- Align the neutral game-shell with the official `ninefoldworlds.com` website's deep ink (#111611), warm ivory (#F6EFDD) and muted gold (#D4B47B). **Do not portray the World Five sanctuary as the setting of every faction's campaign.** Existing faint approved backdrop remains atmospheric only.
- Keep all campaign names, progress and unit counts explicitly fixture data. No changes to authored mission count, three-standard gate, Apex policy, rewards, saves or canon.

## Files and integration

- `Assets/Ninefold/Resources/Home/CampaignClarity.uss` and its .meta file: opt-in style layer.
- `FrontiersPlaytest.cs`: only the Campaigns overview presentation and late stylesheet load.
- `PlaytestLauncher.cs`: required-style import preflight includes the new stylesheet and recent foundation files.
- `PlaytestRevision.cs`: `PT-050` for local review, not automatic GitHub version comparison.
- `Docs/Review/CampaignClarity01/Preview.html`: standalone browser *approximation* of overview hierarchy and action states. It is not Unity output or a test of campaign mechanics.

## Unity smoke checks to batch with other interface PRs

1. Pull merged main and verify **PT-050** in **Ninefold → Playtest → Launcher**; check Console for stylesheet import warnings.
2. Open Campaigns. Compare a fresh profile and a progressed profile; only one eligible recommended campaign should have the filled gold action.
3. Open locked campaign B. The count of owned non-Apex faction standard units should be accurate, preview available, and locked missions should stay non-launchable.
4. For a playable campaign, **Continue campaign** opens the correct briefing and **View all missions** opens its mission list.
5. For a completed unclaimed campaign, **Collect rewards** must claim once; refreshing removes the claim action, and starter rewards/next-faction unlocks remain unchanged.
6. For a completed claimed campaign, **Replay missions** routes to the list and existing replay reward logic remains unchanged.
7. At 320×568, 390×844, 540×960 and a short landscape Game view, confirm readable cards, scrolling, no horizontal clipping, reachable bottom navigation and focused/pressed/disabled states. Fit the **Game-view Scale** to the window; it does not alter game UI scaling.
8. Confirm the Home, mission list, briefing, squad, collection, settings, battle and results retain their previous UI appearance. No new 3D assets were introduced.

## Limitations / follow-up

This work is **structural UI improvement**, not finished high-fidelity artwork. Source fixture campaign labels and generic UI icons still need approved content and production art. Unity/device rendering has not been executed by the authoring environment. Next recommended pass: simplify mission list and briefing hierarchy without touching rules or 3D unit work.
