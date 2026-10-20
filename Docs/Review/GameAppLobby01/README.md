# Game App Lobby 01 — Saved Battle Switching & Read-only Navigation (PT-054)

## Why this pass exists

The real Unity 540 × 960 Home screenshot taken October 20, 2026 showed major improvement over the old prototype, but the player could only **Resume Battle** and saw disabled Campaigns/Units tabs. The visual hierarchy also still felt like a static website on top of a background image, rather than a tactile mobile tactics game.

The old behavior was intentional: MissionFlow will not replace a saved active encounter or grant upgrades/rewards while one is ongoing. This pass improves player control **without bypassing that persistence invariant**.

## What is changed

### Active encounter / battle switching

- The Home primary action remains **Resume Battle** when an attempt is active; no saved position, cooldown or turn state is reset on resume.
- The secondary **New Battle** button opens an explicit, reversible **End this battle?** confirmation. Cancel/Keep simply closes the prompt and leaves the saved checkpoint intact.
- The confirmation explains the exact consequence: **record a defeat**, award **no resources**, retain owned units/unlocks/completed missions, and allow another encounter. This action cannot be undone.
- The action calls `MissionPresentation.AbandonBattle()`: `MissionFlow.Execute` commits a terminal **PlayerRetreated** defeat on the existing authoritative battle; `ClaimRewards` persists an empty reward receipt; `ReturnToSelection` clears the runtime pending battle. This **does not delete or overwrite profile files**.
- If a commit or claim fails, the existing flow recovery and fatal diagnostic behavior remain authoritative; it does not silently create a new attempt. A durable claimed receipt allows a normal profile reopen without replaying the abandoned battle. The alternating battle save remains intact until the next normal attempt checkpoint is committed.
- After confirmation, Home offers **Quick Battle** or **Campaigns**; the player chooses the next mission normally. No automatic new encounter, no skipped squad validation, and no bypass of the one-Apex rule.
- The saved battle is still the **only** active attempt. Browsing a menu does not unlock the ability to start an additional simultaneous battle.

### Read-only Campaigns / Units while active

- Bottom navigation is always reachable, including during a saved encounter; Home, Campaigns, Units and Settings can be browsed without abandoning.
- Campaigns and Mission List display a saved-battle banner. Mission briefing/launch and completion reward collection remain disabled until the current attempt ends. Read-only mission previews stay available.
- Units can be browsed and their stats inspected. **Unlock, advance and customization actions remain blocked** by the existing core phase guard; the presentation disables customization explicitly as well. No resources are spent merely by viewing a unit.
- The battle HUD remains separate and does not inherit the new menu-specific styles.
- Core `MissionFlow.Start` still rejects a second active mission; UI callbacks do not bypass its guard.

### Game-app presentation

- Reorganize Home as a Ninefold tactical **mission lobby**: compact World Five + actual unlocked-unit count HUD, the approved sanctuary visual hero, and a lower mission-control dock.
- Replace website-like large paragraph/card layout with short saved/next/reward status, a single icon-led **primary action**, a quieter secondary action, and a compact saved-state explanation.
- Use the existing `routes` icon asset and the official deep ink/ivory/gold palette, with the approved World Five silver-violet highlight. No new heraldry, maps, fake achievements, lore statements or unit graphics were added.
- The shared footer uses a more tangible, icon-first game-command-bar treatment with readable active and focus states.
- Give Campaign/Mission/Collection panels slightly stronger game-console surfaces while retaining the low-clutter standards. New stylesheet `Home/GameApp.uss` is loaded **last**, after earlier interface passes, and remains scoped to presentation.
- The background JPEG itself is **unchanged** and is not reclassified as final-resolution art. This is a native-feeling UX pass, not a replacement for the later production 3D/art pass.

## Core serialization and regression

New public `MissionController.Forfeit()` intentionally records a `MissionOutcome.Defeat` with a strict `PlayerRetreated` terminal reason. `MissionSave.ReadSave` accepts this exact defeat-only reason and still enforces normal objective/round/result consistency for regular wins and losses. No existing save header or schema changed. The progress ledger already awards **zero** grants on defeat; no new economy mechanism was added.

The new Flow regression test verifies:
1. Saving a voluntary retreat yields a terminal defeat.
2. The unclaimed retreat survives restart and retains attempt identity.
3. Claiming provides no resources or mission completion.
4. Returning/reopening goes to selection.
5. A new attempt uses a different ID.

## Required Unity test after merge

Open `main` in Unity 6000.3.21f1 and confirm launcher revision **PT-054**. Use Game tab 540 × 960 at fitted Editor preview scale; inspect at 390 × 844 and 320 × 568.

1. From existing saved battle, confirm new HUD and **Resume Battle**. Resume, act, return Home; check exact progress remains.
2. While saved, open **Campaigns** and **Units** via bottom tabs. Verify both are visible and show read-only warnings; you can inspect mission requirements and owned units, but cannot deploy, claim, unlock, advance or customize.
3. Home → **New Battle** → **Keep Saved Battle**. Confirm battle/attempt ID and resources unchanged.
4. Home → **New Battle** → **End Battle · Choose Another**. Confirm outcome is recorded as defeat and Home offers **Quick Battle**. Verify no resources, unlocks, cleared missions or character progress are lost, and no battle reward is granted.
5. Start **Quick Battle** from the Home secondary control, enter normal briefing/squad, deploy and confirm the new attempt ID differs. Also verify Campaigns → missions works once no battle is active.
6. Reopen the project/app before and after retreat and confirm no duplicate reward receipts, lost profiles or stuck lockout.
7. Verify a mission ended normally (victory or defeat) still follows the standard Results → Claim/Save path and does not show **New Battle** while a result is pending.
8. Check navigation and prompt at 320×568/390×844/540×960 plus short landscape; ensure prompt text can scroll without hiding buttons. Verify keyboard focus, pressed states and safe areas.
9. Capture Home, battle confirmation, Campaigns/Units read-only, and fresh Quick Battle screenshots. The next production art pass should use actual Unity renders, not browser approximations.

## Scope

Core results/persistence change only for **voluntary retreat**. No mission balance, authored progression, unit kits, battle HUD, saved profile format, production models, world canon, or approved sanctuary image changes. Until Unity tests and screenshots are reviewed, do not claim perfect visual fidelity or device-safe layout.
