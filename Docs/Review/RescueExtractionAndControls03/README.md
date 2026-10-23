# Rescue extraction & unified controls — integration review

## Player-reported issue: World Five Mission 3
**Design decision:** The rescue mission is a *carry-and-extract* scenario. The stranded civilian is not a playable squad member and should not need to be controlled. After the eligible first-deployed unit picks them up, the carrier must enter the gold extraction zone. A core playthrough has previously completed this route, but the original UI left the stranded civilian visibly behind and failed to identify the actual controllable carrier during battle.

### Changes
- BattleView now exposes the resolved **saved mission** RescueCarrierId, rather than guessing which player unit corresponds to the authored `$leader` token.
- In-battle rescue guidance shows the pickup step and, after pickup, identifies the exact carrier and gold exit; it explains that the NPC is carried automatically.
- The rescued NPC is hidden at its original pickup location once picked up, and a small attached golden carried-traveler indicator appears on the carrier's battlefield representation. The former NPC cannot be independently selected after pickup.
- The gold exit marker already moves to the extraction zone in the existing renderer; the HUD now reinforces it with an explicit **GUIDE CARRIER TO EXIT** action using the existing validated movement/commit code.
- A regression checks a non-default first-deployed leader, save/reload carrier identity and the existing completed Mission 3 rescue route.
- These are interface and read-model updates only: no mission definitions or saved combat formats are altered.

## Buttons and presentation
Shared styles loaded last introduce unified slate/brass game controls, a rebuilt gear shortcut using a scalable typographic glyph instead of an image texture, a readable **END TURN** button, responsive ability states, and improved 44px-class touch targets where appropriate. Battle controls retain single-tap inspection, double-tap basic attack, no separate Attack button, and one Apex per team. Standard unit gameplay size is unchanged.

## Manual Unity checks (not covered by GitHub Core CI)
1. Start Mission 3; pick up the traveler using the first deployed unit.
2. Verify the traveler no longer appears stranded and a gold carry marker follows the player unit.
3. Verify the guide button handles a legal partial movement toward the exit, even if a direct path is not available.
4. Finish by placing the designated carrier inside the gold exit; confirm mission completion and Mission 4 unlocked.
5. Check settings gear appearance on Home and long/tall portrait device layouts.
6. Check End Turn, inspect, ability, passive and confirmation buttons across battle sizes; observe states for hover/tap/disabled.

**Limits:** This is not yet a production graphics rebuild or a complete animation/model replacement. It fixes the rescue confusion through explicit carry presentation. Device testing is still required to certify that the revised HUD is readable and that every real touch/route behaves correctly.
