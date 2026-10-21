# World Five Rescue & Extraction 01 — PT-063

**Scope:** experimental three-mission World Five playable slice, gameplay/rescue authoring and current abstract marker presentation. This does not lock new lore, name a finished Avarin unit, or change the Work chat's 3D model pipeline.

## What changes

Mission 003 ("Return the Stranded") no longer automatically resolves at the recovery interaction. The mission uses the existing `RescueAndExtract` core objective in an **explicit opt-in carried extraction variant**:

1. Deploy 1–3 abstract standard fixtures, choosing the first deployed unit as the designated **carrier**.
2. Move that unit to the stranded friendly proxy at (6,3) and tap its gold rescue marker. This consumes **one primary action**, subject to reach, line of sight, phase and actor validity checks.
3. The objective remains active, `Rescued=true`, until that designated unit returns alive to the independent extraction zone near the starting edge of the map. The gold marker and small radar shift to the extraction zone; the objective heading changes from **RESCUE / EXTRACT** to **EXTRACT**.
4. Victory happens only when the full footprint of the carrier is legally inside the extraction region and the recovered character is alive. If the recovered character or carrier dies/becomes unavailable, the rescue fails. The carrier's movement uses all the existing collision, speed, path, terrain-cost, turn-budget and save rules. No arbitrary auto-teleport or free movement order is introduced.

The recovered proxy's physical 3D token still remains at its pickup position in this **abstract study**. The objective represents a recovered passenger carried by the squad unit, rather than a separately controlled companion character. Future 3D model production can animate/attach a suitable rescue payload to the carrier; this pass does **not** make a fragile follower NPC magically move through obstacles.

### Compatibility

The default existing `ObjectiveDefinition.Rescue(...)` and all earlier rescue missions stay unchanged: those still check the actual subject body in the extraction zone. The new factory `ObjectiveDefinition.RescueCarried(..., carrier)` is explicit opt-in; it stores the special extraction actor reference in the **existing persisted objective actor-list field** already written by `MissionSave`. This means:

- No changes to the binary battle checkpoint layout or serialization version.
- New carried rescue actor ID, status and rescued state survive an ordinary checkpoint reload.
- Existing old rescue checkpoints still deserialize without a carrier and retain old subject-to-zone behavior.
- The `$leader` content placeholder is resolved to the first deployed unit when a World Five mission starts; only that carrier may perform pickup for this prototype. No new global unit ability is created.
- Core authoring validation still requires proper alive/eligible references and distinct subject/carrier identities.

The World Five opt-in prototype uses a **new separate save namespace** (`WorldFiveSlice-v2`, profile ID `w5-slice-profile-v2`, content revision `w5-slice-layout-02`). The previous `WorldFiveSlice-v1` folder is **not deleted, migrated, overwritten or silently reinterpreted**. Entering the World Five prototype from Home opens the new V2 version, so V1 progress is *preserved on disk but not automatically imported into V2*. Original `AbstractPlaytest-v1` and its legacy V2/V3 terrain-layout folders remain untouched.

### UI clarity

- Live objective header changes to **EXTRACT** immediately following pickup.
- Existing compact radar was already prepared to switch a rescued objective to its exit. It now points at the new actual extraction zone (not the original pickup).
- The on-field gold objective shape and its label move together to the extraction zone; the old pickup is no longer incorrectly shown as the active task. A simple **EXIT** text label identifies the second phase. Objective shapes remain purely decorative and have no collision or direct order behavior.
- Tapping the goal after rescue does not give a second interaction. Actual success requires moving the carrier's physical footprint inside the zone.
- No permanent new HUD buttons, pop-up help overlays, screen scrollbars, altered main menus or map-size choices.

### What this is not

- It is not a full autonomous NPC escort/follow system. There is no separately simulated mobile traveler, caravan, shield formation, companion AI navigation or attached high-poly mesh.
- No new named species, bonded creature, environment, canonical ability, unit unlock or World Collision encounter.
- No changes to difficulty, pay-to-win, player item stores, damage rules, injury/permadeath or Apex limits.

### GitHub coordination

Only mission objective mechanics, content binding, V2 World Five prototype mission definition, its minimal objective HUD/marker presentation, revision string, existing gameplay/save tests and this review note are changed. Work chat model/rig/animation/material/prefab files are untouched. Use a feature branch based on latest main and PR; do not commit to main directly.

### Test evidence and Unity follow-up

Automated .NET tests specifically cover: new carried pickup then distant exit; no instant completion on pickup; matching carrier and subject status; carrier loss; preserving old rescue behavior; unchanged checkpoint schema after pickup; correct result after saving/reloading; World Five authored extraction zone distinct from pickup and near deployment; independent V2 playtest save IDs.

After PR passes and merges:

1. Pull the new `main` after stopping Unity Play. Launcher should display **PT-063**. Confirm the new visual shell from PT-062 still appears correctly (Home, Campaigns, Units and Settings remain peer tabs).
2. Enter `TRY WORLD FIVE MISSIONS 001–003`. This intentionally starts the **V2** prototype profile. Previous V1 prototype progress is still present in its folder but not shown in V2.
3. Complete 001 and 002 to unlock 003, or replay them in V2 if already completed in V1. Keep screenshot notes about visibility/contrast.
4. Start 003 with 1–3 units and note the **first deployed unit is the designated carrier**. Have a different unit approach the pickup: the interaction must not become available.
5. Use the carrier to reach the gold marker at the stranded character. Tap the marker; verify rescue consumes one action, changes objective heading to **EXTRACT**, and **does not immediately award victory**.
6. Look at the **same physical field after pickup**: the gold marker and inset radar now indicate the separate exit near the starting side. The original pickup should no longer claim to be the goal.
7. Move the carrier legally back into the exit: verify full-body inclusion is needed, movement can take multiple turns and ordinary obstacles/slow ground still apply. Confirm the match ends only after extraction and gives existing fixture rewards.
8. Stop/restart Play mode after pickup but **before extraction**; reenter V2 and resume. The objective must still say EXTRACT and point at the same exit. Winning after reload must give the same result.
9. Make sure earlier abstract fixture rescues and unrelated campaign battles still function, no new Console errors occur and the World Five 001–003 prototype doesn't wipe original Test Campaign A/B progress.
10. Capture a before/after screenshot at 540×960, 390×844 and 320×568. Specifically check EXIT label contrast, gold marker/radar location, and that the carrier/nearby unit health strips remain visible. GitHub CI **cannot** certify Unity visual compilation or on-phone touch accuracy.

## Next milestone

Refine the carrying animation/status preview once approved models become available, then author Missions 004–006 with world-canon scenarios and different objectives. Do not add large amounts of untested content simply to approach the 100-mission minimum.
