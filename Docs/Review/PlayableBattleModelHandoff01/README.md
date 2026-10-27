# Frontiers — playable battle model handoff 01

## What this PR actually enables
The battle renderer can now instantiate **explicitly approved, runtime-ready Unity prefabs** for exact gameplay UnitIds in a real mission. Those prefabs take the place of the blockout unit body, footing and collar; the Core tactical anchor, collision/movement rules, unit health, targeting, battle animations, saves and touch interactions remain authoritative and unchanged.

This is a **bridge**, not an assertion that the Great Strider, Living Veil or other World Five models have already been certified, imported as stable production prefabs, or automatically mapped to playable units. Existing reviewed OBJ assets in `SourceArt/` and fingerprinted prefabs in `Assets/Ninefold/ArtReview/` are **editor review studies**; they remain in the modeling workstream and are not implicitly selected here.

## How to use when a model is approved
1. In **Unity 6.3.21f1** run **Ninefold > Art > Create or Open Playable Battle Visual Catalog**. This creates or opens `Assets/Ninefold/Resources/Battle/BattleUnitVisuals.asset` and selects it in the Inspector. The asset is created locally by Unity; this PR does not ship a catalog containing unapproved prefab references.
2. In Inspector add a catalog Entry. Set **Unit Id** to the exact *gameplay* ID from the active catalog (not an organism's source-art ID). Assign the approved **runtime prefab** generated and reviewed by the 3D pipeline.
3. Enable **Approved For Battle** only after checking visual canon, no scripts, URP materials, rig, and performance on device. Duplicate ID entries are intentionally rejected. Tune **Maximum Width / Maximum Height** to preserve silhouettes without allowing large creature geometry to obscure other units.
4. Start or resume the test battle. The renderer auto-loads the stable Resources catalog (unless an explicit override is assigned to `FrontiersPlaytest.BattleVisualCatalog`). It instantiates approved visual art beneath the existing tactical token anchor. No additional gameplay registration or scene prefab change is required.
5. Remove approval or the entry to revert that model to the abstract placeholder, without touching battle saves.

## Technical guardrails
- Models are **render-only**: prefabs containing MonoBehaviour scripts are refused *before Instantiate*, the renderer disables embedded colliders, physics, cameras and lights, and prevents Animator root motion. It does not mutate the source prefab, its materials, textures or asset GUIDs.
- The model is fitted proportionally using all visible renderer bounds converted to the tactical token's local space. Height and footprint are capped. A prefab with missing/invalid renderers or bounds falls back to the unchanged unit placeholder with a warning, rather than trapping the player in a broken battle.
- Renderer instances, incl. their animation state, are owned by the combat token and destroyed on unit removal or new encounter; the catalog asset remains shared. No guessed faction/model mapping; in particular the abstract fixture-a unit IDs are **not automatically assigned** to named Avarin bonds.
- The existing wildlife behavior, rescue carry/exit logic, double-tap attack, HUD, save profiles, mobile camera, unit selection and contact shadows continue using the same battle token anchor.
- In the current Core-only CI, presentation code is **not compiled by Unity**; passing GitHub checks cannot certify Unity API compatibility, materials, model orientation or GPU cost.

## Mandatory Unity/device verification
1. With no catalog asset, verify original battles, Mission 3 and Mission 9 rescues, and UI remain unchanged.
2. With a locally approved test prefab on one exact UnitId, verify correct visuals in the actual tactical battlefield and fallback for unmapped allies/enemies.
3. Inspect non-zero root/prefab offset and asymmetric mesh positions; feet should stand on the arena, the whole model should remain visible, and the touch/selection ring should still target the same Core unit.
4. Turn, move, attack, take damage, and defeat the mapped unit; its model and contact shadow should follow the Core anchor. Defeat/retry must clean up previous instances.
5. Verify colliders, Rigidbody, embedded cameras/lights and scripts do not change battle simulation.
6. Test portrait iPhone performance, draw calls, transparency sorting, Olyra deploy/retract silhouette, Great Strider + partner integration and clipping. Current review mesh studies are **not certified mobile-ready**.
7. Test disabling the entry and removing the catalog: placeholders and previous saves must continue working.

## Next production step
Once the separate 3D-model workstream publishes stable, canon-approved gameplay prefabs, register the correct gameplay UnitIds, profile the assets and validate their in-battle animations. Do not package review-only scenes or assign source-art IDs speculatively.
