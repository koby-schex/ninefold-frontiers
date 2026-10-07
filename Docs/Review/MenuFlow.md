# Menu flow review

Home → Campaigns → Missions → Briefing → Squad → Battle.
Home's primary action skips browsing when an unplayed campaign mission is available. A saved battle takes priority; a completed campaign's unclaimed reward takes priority over the next campaign. Recommendations use authored campaign order and never force a replay.

Briefing previews the actual map and first-clear/replay rewards. Squad is a separate, single-column screen with health, armor, movement, initiative and normal attack range. Locked units and composition limits explain why selection is unavailable. The deployment button remains outside the scrolling roster. Back navigation preserves the current draft; selecting a different mission creates a new draft. Application resume discards drafts and safely returns Home, as the existing persistence contract requires.

This changes presentation and read-only routing only. Save format, reward amounts, combat rules, and canon are unchanged. Short campaigns, abstract badges and test faction names remain engineering fixtures.

## Review preview

Open `MenuFlow.html` in a browser. This is an illustrative, interactive HTML approximation of the implemented Unity layout, using sample fresh-profile data. It is **not a Unity screenshot, playable build, or validation of UI Toolkit rendering**. It supports the opening-mission path and squad toggles; it does not load saves or grant rewards.

## Validation

Core regression tests cover progression from the opening mission through the finale, completion claim, next faction, and exhausted campaign content; recommendations are suppressed during battle/results. Existing tests cover faction restrictions, the one-Apex limit, stale launch tokens, rewards, replay and saves.

Repository CI runs core rules on Windows/Linux plus structural checks. Unity presentation compilation and device rendering require Unity and remain unverified in this environment.

## Next Unity smoke check (when convenient)

- On a fresh profile, Continue campaign opens Stabilization; campaign B explains the three-standard-unit lock.
- Open Briefing → Squad, remove a unit, go back and forward: selection remains unchanged. Select a different mission: default squad is rebuilt.
- At 320- and 390-wide portrait sizes, scroll all roster cards; Back and the fixed Deploy button stay accessible. Empty squad disables Deploy; adding an eligible unit enables it.
- In mixed mode, one selected Apex blocks another; removing it permits the other.
- Finish the starter campaign, collect its completion reward once, then Continue routes to the next faction. Replays show replay rewards.
- Pause/resume on briefing or squad: return Home without an error. Resume an active battle without losing its saved state.
