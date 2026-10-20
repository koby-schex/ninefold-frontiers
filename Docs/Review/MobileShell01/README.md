# Mobile Shell Consistency 01 — PT-053

**Scope:** one four-tab navigation system, more consistent portrait density for the remaining **Settings**, **Squad preparation** and **Results** screens, and better diagnostics for screenshot feedback. No gameplay or new visual canon.

## Why

After the Home, Campaign overview, Mission Path/Briefing, and Collection PRs, the remaining menus still used a different 76–84-unit button scale and large header type, creating an inconsistent portrait experience. Settings, Squad and Results were especially dense. The Game-view zoom complaint must also be separated from the game's real panel layout.

## Implementation

- **Unified footer:** The existing `BuildNavigation` buttons receive `unified-navigation` / `unified-nav-button` classes and a final shared `MobileShell.uss` layer. Home, Campaigns, Mission List, Units and Settings now share height, icon sizing, label typography, focus/hover/pressed/disabled states and selection behavior.
- **World differentiation:** The Home tab retains its approved World Five silver-violet selected accent. The other menus use muted gold, not a new universal faction/world palette.
- **Navigation semantics:** The campaign tab selection is associated with its nested Missions / Briefing / Squad pages should the footer be shown there in future. Current Briefing/Squad designs still use explicit Back / Choose Squad flows rather than a competing footer.
- **Saved battle:** Campaigns/Units remain disabled from navigation while an active battle is saved, as before. Home and Settings are accessible. The battle HUD never shows this footer.
- **Support screen density:** Only `support-menu-mode` (Settings, Squad or Results) opts into smaller typography, card padding and consistent buttons. Squad and Results retain the same data, locked states, composition safeguards, reward controls and fixed Deploy action.
- **Preserved accessibility:** 50+ UI-unit primary buttons in normal portrait, ~60-unit nav touch heights, readable focus/selection/disabled feedback. The explicit short-window fallback is ~44 units but requires scrolling where appropriate; no decorative animations.
- **Collection filter correction:** When a filter switches to a shorter list, discard the previous scroll position so the first matching unit is visible. Only presentation state changes.
- **Diagnostic report:** Includes screen resolution, safe area, current UI Toolkit panel dimensions and calculated safe viewport, plus clarification that the Unity Editor Game-view Scale zoom does not change in-game UI size. Does not collect screenshots or save files.
- **Source milestone:** PT-053. The Playtest launcher resource preflight includes `MobileShell.uss`.

## Explicit non-goals

No changes to combat rules, mission availability, user saves, progression, unit models, 3D assets, approved sanctuary art, worldbuilding or faction names. Existing abstract unit badge graphics are **not** claimed as final. This is a preparation for a visual review, not a guarantee of production-quality art or device-correct layout.

## Batch Unity verification — after merge

Use **Unity 6.3 LTS 6000.3.21f1** on `main`.

1. **Confirm version & import:** Stop Play mode, pull merged changes via GitHub Desktop, let Unity finish. Open Ninefold → Playtest → Launcher; confirm **PT-053**. If the launcher reports missing styles or Console shows red errors, copy the first exact error.
2. **Fix Editor preview zoom:** In the **Game** tab set a fixed **540 × 960 portrait** resolution. Set the Game-view **Scale** to Fit / a value that shows the full frame, or maximize the Game tab. This zoom is not the game's panel scaling. Do not use the Scene-view zoom to evaluate menu legibility.
3. **Check actual layout:** Capture the full 540 × 960 Game frame (top brand, full body, four bottom tabs). Check the same flow at **390 × 844**, **320 × 568**, and a short landscape Editor window. Content scrolls; footer stays reachable; no text or buttons clip or overlap with safe areas.
4. **Navigate the tabs:** Home → Campaigns → mission list → Campaigns → Units → Settings → Home. Expect consistent tab height/label/icon size, a clearly selected tab, no competing visible primary actions and no screen-specific nav jumps.
5. **Saved battle:** From an active battle, navigate Home / Settings. Check that Campaigns and Units tabs stay disabled and cannot open. Resume battle; its HUD must be unchanged.
6. **Squad:** Briefing → Squad. Scroll the eligible unit list; remove and re-add a unit; inspect kit without changing selection; confirm composition and one-Apex restrictions. The Deploy button remains fixed and safe, and invalid squads cannot deploy.
7. **Results:** Finish a fixture battle, inspect Victory/Defeat summary and rewards, claim once, navigate to the next step. Reward content and one-time behavior must remain unchanged.
8. **Collection filters:** Open Units → Locked, scroll down, switch Owned. List should reopen at top instead of appearing empty due to an inherited scroll offset.
9. **Diagnostics:** Ninefold → Playtest → Launcher → Capture current report → Copy displayed report. It should show Screen and UI panel/safe viewport. Review before sharing. Game-view Scale is still set in Unity and isn't captured automatically.

### Screenshots for the next visual-production review

Capture **Home**, **Campaigns**, **Mission path**, **Briefing**, **Squad**, **Units roster**, **Unit detail**, **Settings**, and **Results** if accessible, all at the fitted portrait resolution. The first priority is layout/visual hierarchy, then quality of art, character models and background rendering. Screenshots are the closest available evidence for actual Unity UI appearance; browser sketches are not Unity renders.

## Validation scope

A static GitHub comparison can confirm style selectors/branch isolation, but cannot verify USS compatibility with the Unity Editor, device touch targets, high-DPI texture appearance, safe-area cutouts or actual frame pacing. No Unity compilation or on-device visual test was run in this coding environment.
