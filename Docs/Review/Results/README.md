# Results and rewards presentation

Completes the current interface pass from Home through mission, squad, battle and results. Uses the existing sanctuary palette/background and abstract test content. No new canon, art, reward amounts or progression rules.

## Behavior

- Victory/defeat summary, mission/round, preservation assurance and explicit ready-to-save/saved status.
- First-clear versus replay labels use the authoritative receipt/result view. Each grant displays amount and current resource balance; after collection the balance is the saved total.
- Defeat and zero-grant results use Save result. Result saving still calls the existing attempt-bound ClaimRewards method; repeated UI taps are gated and core claims remain idempotent.
- After collection, affected fragment units show their current total/requirement and highlight sufficient fragments for unlock or advancement. No automatic unlock, rank purchase or stat modification occurs; Review unit opens the existing collection confirmation flow.
- Next mission stays within the completed mission's campaign and uses authored availability/order. Replay/retry opens briefing for squad preparation. A completed campaign with pending completion rewards links to Home for its separate explicit one-time claim.
- Results cannot be left before saving. Afterward: next mission where available, replay/retry, individual unit inspection, collection, campaigns and Home. Attempt identity remains checked when leaving.

## Verification

Existing MissionMenuTests cover first-clear and replay receipts, duplicate claims without extra writes, wrong-attempt/premature-exit rejection, defeat preserving ownership/resources, unclaimed reload, claimed reload, interrupted claim recovery and a write that succeeded before reporting failure. IntegrationJourney also covers the campaign → rewards → unlock/advance → next-faction journey. These run in the existing Windows/Linux CI suite; no replacement reward-saving implementation is introduced.

Local repository metadata/structure, existing art validation and whitespace checks pass. Preview.html was checked at 320×568, 390×844, 768×1024 and 844×390 for horizontal overflow, button heights of at least 44 physical pixels, collection/saved state and reachable navigation. Portrait samples were visually inspected.

Preview.html is a browser approximation with abstract sample values and review-state buttons, not a Unity render or a working save system. Query states: Victory, Defeat, Replay, Complete.

Unity is unavailable here: presentation C# compilation, USS import, actual device layout and UI-to-save interactions remain unverified.

## Focused Unity playtest

1. Win the opening test mission. Verify first-clear grants and held balances before collection; collect once and compare saved totals. Rapid repeated taps must not duplicate rewards.
2. Inspect an affected unit from results, cancel an upgrade, and verify no fragments were spent. Return and advance/unlock through the normal confirmation.
3. Replay the opening mission and check replay rewards. Next mission opens briefing without starting a battle automatically.
4. Lose a mission; verify Save result, no ownership/progression loss and retry preparation.
5. Close/reopen before collection, then after collection. Pending results should return; an already saved result reopens selection with its receipt retained and no second reward.
6. Complete the starter campaign; verify the separate completion-reward route and one-time claim behavior.
7. Check portrait, smaller phone, tablet and landscape scrolling through the last navigation button.
