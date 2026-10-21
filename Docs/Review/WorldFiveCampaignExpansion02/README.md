# World Five Campaign Expansion 02 — PT-064

**Development classification: AbstractFixture · Candidate World Five narrative beats · Not locked canon.**

## Purpose

Ninefold: Frontiers is a portrait-first mobile tactics game. The World Five Avarin/Continuance campaign will be the launch tutorial campaign and must ultimately contain **at least 100 properly authored missions**, with varied terrain, objective goals, meaningful progression and lore-matched environments. Worlds One–Nine will eventually have their own campaigns. Other game modes, including special larger-squad **World Collision**, remain distinct and are not player-selectable map sizes.

This PR adds actual testable encounters **004–006** to the opt-in World Five prototype, following the existing 001–003 tutorial slice. It does not manufacture canonical enemy species, names, enemies' motives, Avarin unit names or abilities. All nonplayer adversaries are clearly `fixture-npc` *engineering proxies* awaiting lore review.

## Authored missions

| # | Candidate title | Prototype objective | Internal level design | What the player learns |
| --- | --- | --- | --- | --- |
| 004 | The Safe Boundary | **Secure** a marked zone for two **consecutive** round ends; a hostile test proxy may contest the zone | Intermediate-width asymmetric field, off-center protection walls, costly ground near a side approach | Move a unit's full body into a highlighted zone, recognize a contested hold, keep control until the counter completes |
| 005 | Shelter First | **Protect** one friendly test proxy for two completed rounds against a single hostile fixture | Compact shelter approach, partial sight-blocking and a distant enemy | Health and positioning matter; this is not a defeat-everything contract |
| 006 | Wait for the Window | **Survive** three completed rounds with at least one squad unit alive while facing two hostile fixtures | Different bounded lanes and a slow-ground ridge | Survival has a clear round counter and **no fake destination marker** |

The player must finish/claim 001 to unlock 002, then 003, 004, 005 and 006 **in order**. Minimum squad is two on missions 004–006, with three available slots (not extra World Collision capacity). Map dimensions, cover patterns, challenging terrain, deployment and enemy placement differ across missions. **The player never sees a map size or selects one.** Difficulty balance is provisional and requires Unity testing at phone aspect ratios.

The current approved **World Five-inspired art studies** (survey and fracture palettes) are reused while 3D world production continues. Layout geometries vary, but **two existing surface treatments are not six fully distinct scenery sets**; scene-by-scene production World Five environments remain future work. Do not claim these fixture settings are final representations of the Anchor Basins or Pale Corridors.

## Compatibility and saves

- The former two-stage carrier rescue from PR #82 remains in mission 003. No change to pickup, carrier, zone, serialization, or rescue objective semantics in this PR.
- Existing V1/V2 World Five prototype files and original `AbstractPlaytest-v1` saves are **left in place**. The six-mission catalog has a new **V3 profile ID, content revision, and storage folder**: `w5-slice-profile-v3` / `w5-slice-layout-03` / `WorldFiveSlice-v3`. This is intentionally a fresh six-mission profile, **not a migration or automatic import of V2 unlocks/wallets**. Users who played an earlier V2 prototype must replay 001–003 inside V3 to unlock the new missions. Do not delete the older folders.
- Core progression, battle checkpoint, rewards/fragment sources, actor kit definitions and all original Test Campaign A/B content remain untouched. The completion reward is updated only for the new **V3 prototype campaign**.
- The first three authored fields are copied unchanged into the V3 slice; the new catalog adds 004/005/006 and the existing Quick Battle fixture.
- `WorldFiveOpeningCampaignDraft` still carries the complete ten-beat *proposal* and does not become public canon. Mission scenarios remain additive candidates for narrative review.

## UI changes

Only minimal textual wiring and the playtest revision are modified:
- Home switch reads **TRY WORLD FIVE MISSIONS 001–006**.
- Mission details explicitly state whether to **SECURE**, **PROTECT**, or **SURVIVE** and give one short instruction. No new pop-ups, button rows, scrollbars, map-size selector, or world-agnostic fake quest marker.
- The three candidate mission titles appear in the existing campaign path.
- Launcher label becomes **PT-064**. The merged PT-062 unified Home/Campaigns/Units/Settings shell remains unchanged.

The existing mission radar/objective HUD displays a capture-zone destination for Secure, protects the actual NPC's location for Protect and presents **no fictional goal point** for Survive; these features depend on existing core objective data and do not require new rendering.

## Integration safety

This PR intentionally changes only content definitions, brief UI copy/revision, test code and documentation. The **separate Work conversation owns all 3D unit models, rigs, prefabs, materials, animations, prefab/model rendering adapters and eventual per-unit previews**. This PR does not modify them or any Unity `.meta` GUID.

No gameplay-core battle controller, mission/snapshot serialization, model geometry or scene file is altered. There is no fake Avarin monster lore. Any future real Avarin unit identity must be approved and integrated in a separate content/model interface pass.

## Validation

- Existing tests for rescue, fragment progression and checkpoints remain active.
- `WorldFivePlayableSliceTests` now checks **six** validated missions, the existing rescue carrier, sequential campaign prerequisites, appropriate Secure / Protect / Survive contracts, friendly/hostile actors, private geometry variation, correct scene-prefix use, and `CampaignProductionAudit` reporting six unique field layouts and five primary objective kinds.
- Windows/Linux GitHub CI can exercise these pure .NET missions and GitHub structure validation. GitHub tests **do not compile Unity UI Toolkit or substitute for actual combat balance/device review**.

## Unity test after merge

1. Stop Play, pull merged main, wait for Unity to compile (launcher reads **PT-064**). Confirm no Console errors and previously approved PT-062 tab presentation remains intact.
2. Select **TRY WORLD FIVE MISSIONS 001–006** from Home. This uses a **new V3 test profile**; V1 and V2 are preserved but will not appear inside V3.
3. Complete 001–003 and verify each unlock/claim before starting 004.
4. Mission 004: place a squad member **fully inside** the highlighted holding zone; see that the hold counter progresses at round boundaries, while a contesting hostile can block or reset consecutive progress. No victory just by entering once.
5. Mission 005: observe friendly proxy health; protect it through two round ends. Confirm actual death fails mission and that results/rewards work when successful.
6. Mission 006: **SURVIVE** three rounds with the test threats present. No waypoint should appear; defeat/loss handling remains consistent if the entire squad falls. Check enemy AI does not become stuck behind cover and round count is accurate.
7. Check that 004, 005, 006 have different field geometry and reasonable lengths without feeling too large. Test phone layouts at 540×960, 390×844 and 320×568.
8. Stop/resume during a saved 004–006 battle; verify map/turn order/objective counters are unchanged. Switch back to original test and make sure its saves persist independently.
9. Take screenshots of the three fields and Campaign screen for next review. Check goal clarity, difficulty and combat HUD tap controls; report any Unity-specific errors immediately.

## What's next

After **PT-062/063/064** are visually playtested, refine combat difficulty and objective wayfinding, author full canon-supported World Five enemies/roles and distinct approved environmental assets, then develop missions 007–010 with a more coherent escalation. Avoid expanding to 100 generic levels before validating this six-mission foundation.
