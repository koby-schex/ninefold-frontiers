# World Five tutorial finale · integrated 1–10 progression gate

**Purpose:** certify the *same saved wildlife-study profile* winning the entire World Five starter tutorial in order, then entering Mission 11.

The test reuses real Mission 1–8 victories and actual save data from the merged journey tests. It then plays:
- **Mission 9:** second rescue-and-carry objective on its authored battlefield. Only the designated first-deployed unit performs pickup and escape; the recovered traveler is never incorrectly treated as a separate player-controlled actor. Navigation uses legal movement previews and waypoints around the cover. Reopen the profile after pickup and confirm the saved carried state.
- **Mission 10:** finale requiring two interactions with the primary stabilization objective. Optional defeat and survive objectives are not falsely required for victory. Reopen after the first completed stabilization.
- **Transition:** after each victory, collect rewards once (including duplicate-claim check) and reopen the saved profile to verify the next mission. Ashbound Mission 11 remains locked before completion of Mission 10, then unlocks. Start the *authored* Ashbound encounter, assert two hostile units and that it can be resumed.

The battle driver uses normal BattleInteraction move/interaction preview-and-commit and progress receipts. Enemy visibility is explicitly empty so this test isolates **objective solvability, progression and persistence**, not difficulty tuning or perception AI.

**Bug found and fixed in this PR:** Previously the rescue-interaction range was a 3D distance from the squad unit's *elevated target offset* to the civilian's ground-level pickup marker. Mission 9 could be physically adjacent but remain OutOfReach forever. Ground-objective interaction distance is now checked in the horizontal tactical plane, while the obstruction ray retains the elevated target origin. This changes the shared Core interaction rule but does **not** modify mission definitions, save formats or models. It must pass all general interaction, rescue and mission integration tests. The separate modeling workstream is untouched. The Unity Editor and iPhone still require hands-on testing of Mission 3's extraction UI, Mission 9, the finale and portrait HUD.
