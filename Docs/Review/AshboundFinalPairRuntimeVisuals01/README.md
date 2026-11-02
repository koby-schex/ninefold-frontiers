# Ninefold: Frontiers — Ashbound Final-Pair Runtime Visuals 01

**Review status:** provisional 3D geometry in the actual battlefield; not canon-approved final character art, not a production skin-weighted rig.

## Why this PR

PRs #173 and #174 gave the Rift Forager, Parted Striker, Seam Castigator and Anchor Wrecker more developed *live* 3D silhouettes. The last two enemy templates—**Afterecho Seer** and **Camp Witness**—still rendered with sparse generic Avarin geometry. This PR gives both an individually designed, motion-attached runtime art model. The six Ashbound ordinary enemy types now all share the same detailed biological Avarin face and living crescent sensory mantle.

The implementation is limited to visual character models. It never modifies combat rules, unit IDs, abilities, damage, hit certainty, campaign save structures, enemy AI, mission design, or playable Continuance creature models.

## Afterecho Seer — appearance and motion

- Narrow ordinary Avarin body and proper temple-face / naturally living crescent sensory mantle; no animal companion and no invented anatomical sensory organ.
- Off-balance field shroud, mismatched **external** sensing baffles, physical mounting hardware and separate exposed wiring—never a floating mystical halo.
- Chest-mounted capture cassette with uneven equipment readings, worn external forearm stabilizers, rear Wake hardware reel and damaged trailing cloth.
- Distinct light gait; subtle head unease at idle communicates Afterecho's harmful distortion, not an invulnerable status effect or power free from cost. Ordinary battle action events still determine attacks and damage; visual cues cannot fabricate game effects.
- External equipment is a **visual proposal** for engineered Fracture Wake, not locked canon technology.

## Camp Witness — appearance and motion

- Ordinary Avarin biology, more rectangular **logistical support silhouette**, reinforced asymmetrical coat and shoulder wraps; not a boss, leader or Apex Unit.
- Registry satchel, physical camp equipment straps, rear communications relay, three field utility pouches and a hinged/slatted **left-arm camp signal panel**, all equipped externally.
- Deliberate slower gait, gentle surveillance head sweeps and restrained defensive arm action. These are animation suggestions, not new support abilities or overwrites of existing mechanics.
- No bonded companion, creature handler, trophies, firearm, spiritual gadget or invented camp hierarchy.

## Direct Unity game integration

- `WorldFiveAshboundFinalPairDetails.cs` creates actual owned, triangulated loft meshes using the same `WorldFiveBattleOrganics.Part` geometry as the first four types.
- The `WorldFiveBattleOrganics` entry point now gives Seer/Witness the **same detailed Avarin anatomical construction** and calls the same six-pivot model hierarchy (core, head, both arms, both legs).
- Seer forearm stabilizers and Witness left-arm signal hardware follow their corresponding moving arms. The natural face and crescent mantle move with the head; cloth and utility equipment remain body-attached.
- Meshes with identical material on a given moving body part are combined by the established batching function and disposed using the existing battlefield mesh ownership flow.
- `PlaytestBattlefield` now binds all six exact approved enemy **technical UnitIds**, with lighter Seer gait and slower Witness stance. Active attack/hit animations still come from Core-authorized battle update playback. No Rigidbody, Collider, new MonoBehaviour or gameplay Animator is added.
- If a properly approved model is in the visual catalog, that model **still overrides** these fallback studies. There is no model-catalog promotion in this PR.

## Native visual and functional acceptance

1. Merge only after GitHub checks are green and compile in pinned **Unity 6000.3.21f1**. There must be no Unity Console errors. Repository Core checks do not compile the Unity runtime presentation scripts.
2. Select the opt-in **World Five Encounter Variety** content. Inspect **Mission 14** (Seer with Castigator) for two visibly different ranged archetypes, and **Mission 13** (Witness with Forager, Striker and Wrecker) for a distinctly logistical support silhouette.
3. In the portrait tactical view, verify that all six ordinary Ashbound variants can now be recognized by body silhouette and attached equipment—not solely their HUD names or flat colors. Compare against Missions 11–12 for Forager/Striker/Castigator and 13 for Wrecker.
4. Inspect the Seer's face, paired living crescent environmental sensory mantle, restrained external baffles and exposed unstable **mechanical** capture equipment. Reject extra creature limbs, mystical halos, floating armor or anatomy that implies biological bonding.
5. Inspect the Witness's left signal panel, rear relay, repaired coat and right satchel. Confirm that the signal panel follows the left forearm during movement and attacks; registry/pouches remain on the body.
6. Observe actual enemy turns and authorized impact reactions; the model should never independently alter health, AP, initiative, direction rules, movement bounds or target selection. No standalone "attack" button is introduced.
7. Review narrow 320×568 and standard 390×844 portrait layouts; capture *real Unity* front, three-quarter, side, and battle-camera renders. Compare carefully to the controlling World Five locked visual master and Master Lore & World-Building Archive v26.0.
8. Smoke-test Mission 4 capture-zone clarity, Mission 3 rescue/extract, tutorial wildlife, and a known approved prefab, confirming the previous systems remain intact.
9. Profile on **iPhone 14**: meshes, draw calls, dynamic geometry cost, shader/material counts, FPS and thermal/memory budget. No device metrics are asserted from the GitHub automated checks.

## Regression and remaining production gates

- Three existing visual-source checks continue to pass; the new `BuildTools/Art/validate_ashbound_final_pair_runtime.py` verifies exact six-type routing, real geometry, shared Avarin anatomy, body part attachment, no extra physics/AI, and approved prefab precedence.
- Windows and Ubuntu Core suites must remain green.
- This does **not** deliver hand-sculpted UV/PBR materials, retopologized rigged humanoid skin meshes, correct IK grounding, authored production combat animations, LOD systems or full lore-approved detailed silhouettes. The six models remain **provisional**.
- Once the six-enemy review passes, stop accumulating procedural detail PRs. The next high-priority **art-production** milestone should compare actual in-game renders to the locked visual masters and replace the weakest model with a truly sculpted, UV-textured, skinned, animated and profiled asset. Follow the same review/approval method for all six.

The canon addendum `Docs/Canon/WorldFiveAshboundCanonAddendum01.md` remains unchanged and controlling. Fracture Wake is engineered unstable Arcana capture; Afterecho is damaging sensory distortion. Avarin mantles detect environment/Arcana, **not** bonds.
