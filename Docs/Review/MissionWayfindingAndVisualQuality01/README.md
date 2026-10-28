# Ninefold: Frontiers — mission wayfinding hotfix and art acceptance gate

**Status: gameplay presentation correction and playtest QA; not final environment or model art.**

## Player-reported defects (2026-10-28)

1. Mission 4 instructed the player to capture and hold an objective, but the 3D scene had **no visible capture marker**. Root cause: `PlaytestBattlefield.BuildMap` only rendered `ObjectiveDefinition.Interaction` markers, whereas Mission 4 and Mission 8 use `ObjectiveKind.Secure` and a full `Zone`.
2. Across missions, the player could not tell where to go, which action to perform, or when to end the turn. The prior objective header showed type/progress without consistently saying what to do.
3. Game art—including world scenery and character/creature proxies—is far below the desired standard and was described as stick-figure-like. **Do not call these models finished, accepted, or ready for marketing/launch.** Asset quality is a separate, high-priority production blocker.

## Fix in this PR

- **3D battlefield**: Secure/Rescue-extraction zones now have persistent visible gold boundaries, corners, and a grounded interior footprint. Mission 4's real authored 3x4 zone now has a followable `HOLD 0/2` (etc.) on-field label; Protect goals get a `GUARD` label following their existing NPC. No collider, gameplay trigger, new objective, or updated battle rule is created by the 3D art.
- **Briefing/mission scout**: shows the full extent of Secure zones and Rescue extraction areas, instead of only the interactive point markers. Zone depiction comes directly from the mission definition and matches Core geometry.
- **In-battle dock**: brief dynamic "DO THIS NEXT" instructions for the active primary/bonus goal, derived from its actual kind and progress. Mission 4 explicitly teaches **move into the outlined gold area and keep hostile units out for two consecutive rounds**. No capture button is needed. Rescue, Stabilize, Protect, Defeat and Survive have their own specific instructions.
- **Tactical inset**: outlines Secure/active extraction zones rather than suggesting only an unclear central dot. Read-only pointer safety is retained.
- **QA**: Core test verifies Mission 4's exact capture area, its consecutive two-round rule, lack of an interaction point, and separation from blockers. A source-level regression script guards all three surfaces (3D, briefing, radar) and the actionable instructions. Repository CI runs the script on every change.

## How to playtest in Unity 6000.3.21f1

1. Start/resume World Five Mission 4 (Hold the Passage). Confirm gold field boundary surrounds the capture area; it is not a point to interact with.
2. Read the dock: **MOVE an ally INTO the GOLD outlined zone. Keep enemies out. Hold it for 2 consecutive rounds.**
3. Use movement to step a friendly unit inside the area, finish the unit's turn with **END TURN** when appropriate, and advance rounds. Verify `HOLD 0/2` becomes `HOLD 1/2` then completes after the second uncontested round. The hostile test NPC can contest the zone; a contested/empty round should not advance the counter.
4. Inspect the mission briefing diagram and in-battle radar. All three must agree on the actual location and footprint; taps on the radar must not move units.
5. Check Mission 8's Secure area; Mission 3/9 extraction outlines; Mission 5's GUARD label follows the protected traveler. Check one Stabilize, Defeat and Survive mission's dynamic "DO THIS NEXT" text.
6. Test 390×844 and a narrow 320×568 Game view, and inspect the dock/radar/map for overlap, offscreen labels and obscured characters. Confirm there are no Unity Console compile errors.
7. Try saving during Mission 4 and resuming. Objective zones and progress must appear correctly with no save migration.
8. Run a normal move, target attack, ability and rescue: this PR does not change turn legality or the core double-tap attack contract.

## Visual production blocker — separate art PR, not concealed by UI changes

Do **not** treat the present primitive proxy geometry, preliminary Ashbound OBJ studies, or unapproved ArtReview skeletal rigs as satisfactory final assets. The next visual milestone must show visibly higher-quality *actual Unity meshes* and animation, not another plan or generic asset index:

- Replace disconnected primitive-looking forms with believable anatomy, surface transitions and proper silhouette at portrait battle zoom; compare with locked World Five showcases and Master Lore v26.0.
- Properly weight and skin the Avarin face, crescent **environmental sensing** mantle, arms, legs and equipment. Prove real contact/weight shifts, idle life, attack, hit, strain (Afterecho), and defeat motion.
- Replace flat placeholder colors with authored PBR materials and deliberate detail variation; avoid plastic shine, random horns, creature trophies or lore-incorrect outfits.
- Extend the same standards to the Continuance's Great Strider, Living Veil, Burdenbeast, Riftwalker, plus world scenery; coordinate the **separate playable-unit model workstream** rather than overwriting it.
- Review before/after *actual Unity renders*, rig motion and frame-time/draw-call/texture metrics on iPhone 14. Maintain runtime ArtReview separation and explicit approval before battle catalog mapping.
- Keep gameplay instructions, salvage chemistry and Afterecho's genuine instability clear, not an excuse for stylistic noise.

This PR fixes a real gameplay visibility bug first, then leaves model-quality work as an explicit unresolved production acceptance gate. GitHub checks cannot verify appearance, Unity compilation, or on-device performance.
