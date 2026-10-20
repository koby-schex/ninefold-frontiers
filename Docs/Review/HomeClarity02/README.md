# Home clarity pass 02 — PT-049

## Why this pass exists

Unity playtest feedback on October 20, 2026: the Home menu felt confusing, outdated and low-detail; at 540 × 960 the portrait preview appeared oversized unless the user reduced the Editor's Game-view zoom. The original Home polish pass largely changed colors and measurements without addressing underlying duplicate choices.

**Do not mistake Game-view Scale for UI scaling:** the reference panel remains 540 × 960 with width-based matching. In the Editor choose 540 × 960 and reduce the Game tab's Scale (or maximize the Game tab) to show the whole phone. This modifies the Editor preview, not in-game layout. If controls still clip or overlap after fitting, that is a separate defect. Capture the Game tab screenshot and selected resolution, and check the Unity Console.

## Design decisions

- Preserve the **approved World Five sanctuary** source artwork, with no unapproved extra emblems, creatures, geographic claims or mixed world imagery.
- Eliminate Home's redundant **Your Units** large shortcut card. The established bottom **Units** tab remains the obvious location for collection and progression.
- Remove both bulky Home shortcut cards. **Quick Battle** becomes one quiet secondary control beneath the main campaign action; it is hidden during a saved battle.
- Replace large placeholder campaign names as Home hero titles with **player-context headlines**. Fixture identifiers remain explicitly visible only in the supporting line, and other screens remain unchanged.
- Keep existing player state priority and operations: **Resume battle > Claim campaign rewards > Next mission briefing > Explore campaigns**. The Quick Battle action still opens the current abstract combat mission briefing.
- Retain a concise saved-battle restriction notice; preserve nondefault Home feedback messages. The build ID and fixture caveats remain available in Settings/launcher/diagnostics rather than occupying the hero.
- Frame existing scenery with restrained deep ink, gold, warm ivory and the previously approved World Five silver-violet selected-nav state. Nothing should flash or force motion.
- Set `Sanctuary.jpg.meta` import compression preference to **CompressedHQ** instead of ordinary compressed; this may lessen secondary GPU texture artifacts depending on target/import path but does **not** add resolution or recover missing details from the JPEG. Source asset and composition are unchanged.

## What's *not* solved

The source Home JPEG is still 853 × 1844 (documented in the approved Home review), a single static, already-compressed illustration; high-DPI devices and a strong premium art direction will eventually need higher-resolution, art-director-approved production imagery or a 3D-driven background. Other menus currently retain abstract placeholder content; those require separate screen passes. This PR does **not** claim console-grade graphics, production art completeness, or device-verified quality.

## Check in Unity 6.3 LTS / 6000.3.21f1

1. Pull merged main after exiting Play mode. Use **Ninefold → Playtest → Launcher**, verify **PT-049**, start/resume. Confirm no USS errors and the correct scene.
2. Game tab: fixed 540 × 960; adjust **Scale** so entire screen fits. Then review 390 × 844, 320 × 568, tablets and a short landscape size. Confirm dock scrolls when necessary and primary/nav controls stay reachable, with no obstruction by safe areas.
3. Home in default state shows branded masthead, large sanctuary view, **one** contextual mission title and **one** gold primary action; **Quick Battle** is smaller and secondary. The four-icon bottom nav remains.
4. Confirm **Units** appears only in bottom nav (not a duplicate shortcut), and the same nav buttons work.
5. With a battle saved: **Resume battle** and the concise notice appear, **Quick Battle** is absent, and Campaigns/Units stay disabled; Home/Settings are usable. Verify resuming does not restart the encounter.
6. With campaign completion unclaimed: **Collect rewards** appears once. With a next mission: **View mission** opens briefing. With no playable mission: **Explore campaigns** opens selection. None claim resources until the corresponding explicit action completes.
7. Confirm Settings/diagnostics still show current source milestone; the Collection, Campaign, Battle and Result screens remain unchanged in this PR.
8. On iPhone later, check high-DPI sharpness, GPU memory, thermal behavior and art fidelity. Do not infer device quality from Game-view zoom alone.

No save schema, unit balance, economy, gameplay or canonical designs were changed. Browser preview is a **mockup**, not a Unity render.

Next direction: production-grade graphic assets and screen-by-screen information architecture, starting with Campaigns; do not repeat superficial global recolors.
