# World Five Opening Ten — Content Production V4 (Unreleased Preview)

**Status: authoring/prototype only. Not connected to Home or Unity launcher.**

## Why this is deliberately separate from the current game

The user wants a few more development merges before the next full Unity playtest. PT-065 **Touch Combat Clarity 01 (PR #85)** is currently open and modifies shared Unity HUD/presentation. To avoid having two chats or two overlapping PRs editing those files, this content-only branch prepares the next four missions **without editing any Unity UI, version labels, battle HUD, prefab, scene, material, animation or save path**.

**Important:** Merging this PR **does not** make Missions 007–010 accessible from the Home button. The existing six-mission V3 campaign remains exactly as it was. Once PR #85 merges, the next integration PR will explicitly connect this new ten-mission catalog to the UI and isolate its V4 saved data. We must not claim the new content is already playable from the Home menu.

## Canon boundaries

The Master Lore Archive v26.0 is controlling canon. World Five is **The Fractured Meridian**; the Continuance route team begins with a reliable route showing an inconsistent destination and slowly learns about its local disruption. Its opening story must not reveal protected cosmology. The first ten candidate mission titles in `WorldFiveOpeningCampaignDraft` are **internal design candidates**, not locked publication canon. All adversaries are the existing `fixture-npc` test stand-ins; they do not introduce new species, enemy factions, final ability names, or approved playable Avarin models.

## V4 mission program

| # | Candidate title | Real objective | Geometry / teaching intent |
| --- | --- | --- | --- |
| 007 | Clear the Approach | Defeat two explicitly noncanon hostile test proxies | Mid-sized cover lanes with elevated visual threat silhouette; teach tap-to-inspect and validated normal attack |
| 008 | The Divided Route | Secure one zone for two nonconsecutive round completions while a hostile proxy may contest | Slightly wider but bounded split-corridor arena, two difficult-ground patches; unit spacing and approach choice |
| 009 | Keep the Bond | Recover one friendly proxy with the first deployed unit, then return to a separate extraction area under hostile pressure | Intermediate area with side cover, difficult ground and one hostile proxy; ability timing **encouraged, never required** |
| 010 | A Line That Holds | Stabilize a nearby route point twice; optional defeat-one-proxy and survive-two-round goals | Bounded wider finale with distinct obstacles and slow lanes; one primary gold waypoint, at most two optional goals |

These four follow existing authored 001–006 in a **ten-step strictly prerequisite-gated chain**. They all use the existing three-slot standard battle system and do **not** enable World Collision or a player-visible map-size selector. The actual battleground sizes are design inputs, never UI labels.

Actual objectives, interaction reach, target IDs, terrain bounds, nonoverlapping deployment slots, authored hostile behavior and reward grants are validated by the existing `ContentCatalog` rules. The setup includes only the existing `study-w5-survey` and `study-w5-fracture` environment studies. Unique map geometry does **not** mean a new, approved visual environment; later production must author distinct World Five scenery kits.

## V4 content and checkpoint separation

New **`WorldFiveOpeningTenPrototype`** in `Assets/Ninefold/Runtime/Core/Content/WorldFiveOpeningTenPrototype.cs` exposes:
- `Create()`: validated ten-mission `ContentCatalog` whose first six mission definitions come unchanged from the existing V3 catalog, whose last four are the new actual authored encounters, and which retains the existing Quick Battle fixture.
- `Open(...)`: profile/session factory which explicitly accepts **caller-owned V4 storage** and refuses a non-V4 catalog revision.
- `MissionIds`: ten ordered read-only candidate IDs matching `WorldFiveOpeningCampaignDraft`.
- `CampaignId`: existing World Five prototype campaign identity `w5-slice`, so eventual UI path changes can be isolated.
- `ProfileId`: **`w5-opening-ten-profile-v4`**.
- `Revision`: **`w5-opening-ten-layout-04`**.

Do **not** route this V4 catalog through `WorldFiveSlice-v3`, `WorldFiveSlice-v2`, `WorldFiveSlice-v1` or `AbstractPlaytest-v1` storage. Those folders must remain untouched. When V4 is connected to Unity, choose a separate `WorldFiveOpeningTen-v4` path. This merge alone cannot affect any profile because nothing in Unity selects this new factory yet.

The original 001–006 fixture missions, rewards, enemy AI, NPC rescue rules, content metadata, promotion/unlocks and original Saved Battle checkpoint formats remain unchanged. V4 rewards are isolated fixture currency and fragments, not monetized items.

## Automated quality

`Tests/Ninefold.Core.Tests/WorldFiveTenMissionTests.cs` covers:

1. All ten mission definitions and the prerequisite chain, proper standard squad sizes, and prototype classification.
2. Exact primary/optional goals and real actors for 007–010: Defeat, Secure, two-stage carrier Rescue, and double Stabilize with optional goals.
3. V4 retained fidelity to the first six V3 authored encounters; no mutation of V3 or original abstract content.
4. Independent prototype profile creation, first-mission selection, initial locks, saved-battle reload and safe V3/V4 IDs.
5. Campaign audit expecting **10 distinct geometric layouts** and **all six primary objective kinds**, with zero consecutive repeats, and still failing the commercial-release gate because it contains only 10 missions, 2 environment study treatments and abstract fixtures.

These tests run in the Windows/Linux GitHub core test workflow and do not invoke Unity. Github passing does **not** certify balance, objective discoverability, camera comfort, visuals, safe-area sizing or canon approval.

## Next PR (after PR #85 merges)

**World Five Opening Ten Integration 01 — PT-066 candidate** should wire the Home opt-in selector to this V4 catalog and the isolated `WorldFiveOpeningTen-v4` files, expose the titles/descriptions of Missions 007–010, update the launcher revision, and provide clear UI acknowledgement that older V3 progress stays preserved but V4 starts fresh. Avoid refactoring PT-065 battle HUD before the user has tested it.

Until that follow-up, **do not tell the user Missions 007–010 appear in Unity**.
