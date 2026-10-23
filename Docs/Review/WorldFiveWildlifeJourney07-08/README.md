# World Five live wildlife journey — 001–008

The test reuses existing **real Core combat victories for Missions 1–6**, including rescue pickup/return in Mission 3. These helpers are now parameterized by an explicit catalog and independent profile opener so the full journey uses the **actual WorldFiveTutorialWildlifeV1 catalog and profile from Mission 1 onward**. This avoids falsely copying progress from another campaign study.

After collecting the six actual mission rewards, the new test:
- Deploys the authorized three-unit squad to Mission 7 and engages the authored **two Seamstalkers** with legal ability selection, preview/second-tap committed attacks, and legal positional movement;
- Saves/reopens the active encounter, completes victory, claims rewards exactly once and validates that Mission 8 unlocks;
- Deploys to Mission 8 and advances into the secure zone against the authored **Sillrunner**, with live combat simulation and no invented enemy events;
- Confirms victory, reward accounting and that Mission 9 is next, while Ashbound Mission 11 remains locked.

For deterministic content certification the AI receives **empty perception**. The enemy units have authentic stats/positions and turns but cannot perceive the player, so this **does not certify combat difficulty or wildlife AI behavior**. It also does not certify Missions 9–10 or iPhone Unity visual behavior. No shipping content, rendered assets, gameplay save format or balance is changed. Full 1–10 and Mission 11 handoff is the subsequent test milestone.
