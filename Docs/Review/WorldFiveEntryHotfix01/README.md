# World Five Campaign Entry Fix — PT-064 hotfix

## User-reported issue

In Unity PT-064, the **TRY WORLD FIVE MISSIONS 001–006** control was visible underneath Continue Campaign on Home, but tapping it seemed to do nothing. This was a usability failure in the entry flow.

### Root-cause analysis from repository code

1. The button was built with `enabled: menuView.Phase == MissionFlowPhase.Selection`. If there was an active saved encounter, the CTA silently became disabled despite still looking like a normal destination.
2. When it **was** enabled, `SwitchCampaignPrototype()` selected the other content/profile but set `page = Page.Home` — it never navigated to the mission path. Thus the user remained on Home and had to discover the new campaign through a separate tab. There was no immediate confirmation that the button had worked.
3. An error opening the optional profile could trigger the generic fatal playtest screen instead of retaining the working original profile and showing actionable feedback.

The user's report did not include the current battle phase or Console output, so we cannot establish which exact circumstance occurred on their device; this fix addresses all three failure modes without inferring one unobserved runtime cause.

## Fix

- Home's World Five entry is **always clickable**. If the prior campaign has a saved active battle, tapping it displays the existing explicit **END BATTLE / KEEP SAVED BATTLE** choice, preceded by a short explanation. It does **not** auto-forfeit or overwrite the encounter. Existing safety rules still require resolving the battle before switching profiles.
- With no active battle, tapping the entry loads the isolated `WorldFiveSlice-v3` catalog and navigates **directly to the World Five six-mission path** (`Page.Missions`, `selectedCampaign=WorldFivePlayableSlice.CampaignId`). Missions 001–006 are visible immediately; Mission 001 is available; subsequent missions remain progression-gated.
- Return to original playtest remains available through the Home tab in prototype mode. Its action restores the separate `AbstractPlaytest-v1` profile and returns to Home.
- Profile switching now verifies that the expected campaign exists and safely **restores the previous catalog/menu/save path** if initialization fails. The Unity Console receives the exception; Home displays a visible explanation. No progress or checkpoint files are deleted or migrated by this feature.
- Independent V3 saves, selected unit eligibility, mission rewards, World Five rescue/extraction and battle geometry remain unchanged. No new canonical lore or UI modes introduced.

## Verification

A new .NET test, `SliceEntryPath`, confirms that opening the opted-in catalog exposes one enterable six-mission World Five campaign, six actual mission definitions, an available Mission 001 with valid starter unit eligibility, and a startable selected briefing. Core Windows/Linux CI and repository checks are required before merge. The button wiring and Unity UI itself cannot be clicked by these .NET tests.

### Unity steps after merge

1. Stop Unity Play mode, merge and pull the PR to latest main, reopen the editor and confirm zero Console errors.
2. At Home in the **original playtest with no active battle**, tap **TRY WORLD FIVE MISSIONS 001–006**. You should immediately see a **six-entry campaign path**, not another Home screen. Mission 001 should have View Briefing; 002–006 show progression locks. Tap Mission 001 and verify squad preparation.
3. Tap Home and confirm **RETURN TO ORIGINAL PLAYTEST** appears. Tap it, confirm the original Home campaign/progress returns.
4. With a saved original-playtest battle, tap the World Five entry: the app must show an explicit saved-battle explanation and keep/end confirmation, **not silently do nothing**. Choose **KEEP SAVED BATTLE**, confirm the battle remains resumable. Only if the user chooses the separate END action should it forfeit the active battle.
5. Enter World Five V3 again, confirm its mission selection and saved progress are independent of original and earlier V1/V2 prototype profiles. Try Unit Collection and Campaigns tabs in both profiles.
6. At 540×960 and 390×844 portrait, inspect the CTA and bottom tabs for overlap, hidden areas or unresponsive touch targets; swipe Home if needed. Send a screenshot of any unexpected error or dead control.

**GitHub CI does not compile or run Unity UI Toolkit**. This PR deliberately leaves the recently improved PT-062/064 battle visuals, environments and the separate Work chat's model assets untouched.

## Merge coordination

The patch changes only the central presentation entry handler, one existing non-Unity gameplay integration test, and this review file. It does not touch Work-owned generated 3D geometry, materials, prefabs, model pipelines, core combat content or saves. No new Unity metadata files are required.
