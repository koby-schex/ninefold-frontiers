# Ninefold: Frontiers — World Five Runtime Organic Visuals 01

**Status:** actual in-game visual-quality *study* / awaiting native Unity 6000.3.21f1 inspection and lore-master approval. Not a final model-release submission.

## Why this PR

The player reported that current models and arenas resembled disconnected stick figures and Microsoft Paint primitives. Repo inspection confirmed this literally: the battlefield renderer's missing-prefab path instantiated `PrimitiveType.Cube` / `PrimitiveType.Capsule` for every actor and built World Five wildlife from separate Unity spheres, rectangular sensory ridges and vertical capsules. Existing SourceArt studies never appeared in live gameplay unless separately approved and mapped. This PR makes concrete improvements to the **battlefield renderer**, not yet another detached review scene.

## What visibly changes after merging

**Actual in-world native wildlife** (only exact World Five Sillrunner and Seamstalker technical IDs in missions with those profiles, including the native wildlife study's Missions 7 and 8):

- Source-authored 3D *connected loft surfaces* create a continuous tapered trunk, muzzle/head, curved limbs, actual moving feet and small eye/relief details.
- Seamstalker: more strongly tapered living tail and layered back ridges; Sillrunner: shorter tail fan. These are still provisional silhouette studies, not new official anatomy details.
- Claws are parented to their respective moving limbs; eyes move with their head. Existing lawful movement, attack and tail/breathing animation signals are retained.

**Actual Ashbound hostiles** (only `WorldFiveActOnePrototype.EnemyTemplateIds`; first encountered in Mission 11, with additional variants in 12–14):

- Avarin biped mesh fallbacks with continuous faceted anatomical torso, temple-face, legs, shoulders and *living crescent environmental sensory mantle*. Face/mantle are biological, not engineered bonding hardware.
- Asymmetric *external* salvage and Fracture Wake equipment; distinct armor/gear density for the six ordinary locked archetypes. No bonded creatures, ritual trophies, soul capture, invented weapon class, named bosses or Apex.
- Legs, arms, torso and mantle respond to actual movement and attack playback; subtle ambient breathing and mantle movement while waiting for the next input. No damage/status is authored by art.

**Battlefield environmental improvement** (all existing encounter prototypes):

- Rectangular cover solids are replaced by layered eight-sided chamfered/stratified geometry, fully contained by the original authoritative obstacle AABB. Gameplay geometry/path costs/LOS unchanged.
- Existing World Five fracture material has a very subtle time-varying reflection rather than feeling entirely dead. This is visual texture variation, **not** a new hazard/Arcana ability.
- Three deterministic 128×128 mipmapped mineral/skin/gear texture tiles give these provisional organic mesh surfaces actual fine-color variation, not only flat colored primitive fills.

## Strong safety boundaries

- **Approved prefab precedence:** `TryCreateApprovedVisual` executes first. Approved playable 3D models and their own Animator/surface work are untouched and never overwritten by this study.
- **Known IDs only:** native wildlife are selected only when the authoring catalog provides their specific `UnitId`, and Ashbound only by the approved six technical NPC template IDs. Old tutorial/abstract fixtures remain visibly marked as training proxies, not mislabeled Avarin or wild creatures.
- No `MonoBehaviour`, `Collider`, `Rigidbody`, gameplay activation, pathfinding, physics, target data, effect calculations, battle catalog mapping, enemy rebalancing, new saved-state fields or campaign changes.
- Cosmetic meshes are tracked and disposed when the stage is retired or enemy roster changes. No per-frame mesh allocation. Owned textures are reclaimed with the battle stage. Several independent mesh renderers per body still have an **unmeasured** mobile draw-call cost.
- The prior six Ashbound `SourceArt` OBJs remain independent, **not** automatically promoted as production runtime assets. The prior unfinished rig is not imported or used by the battle.

## Important remaining quality limitations

This is a first visible runtime improvement, not a claim that the total game meets final character, terrain, interface or animation quality. These are small procedural Unity meshes with material micrograin and simple part-level motion. They are **not** sculpted cinematic skins, PBR authored UV/normal-map suites, production rigs or final lore-master silhouettes. Generic training units in Missions 1–6 remain abstract proxies and are deliberately not invented as canonical units. The major art quality overhaul (playable Continuance roster, wildlife, all six Ashbound, terrain detail and responsive movement) remains open. Next milestone should replace these study fallbacks with properly sculpted, UV-textured, skinned, performance-tested approved battle prefabs, at **mobile portrait camera scale**, and remove generic proxies.

## Native Unity visual acceptance steps

1. Pull main and compile in **Unity 6000.3.21f1**. Console must be empty. Verify Game view and built-in Playtest Launcher still function; Android/iPhone builds require device-specific testing separately.
2. In the **World Five Native Ecology** prototype, play Mission 7 (Seamstalker) and Mission 8 (Sillrunner). Compare before/after: torso silhouette is continuous instead of stacked spheres, individual limbs/tails move, eyes stay attached, and subtle breathing is visible when idle.
3. In the **Act One / Encounter Variety** prototype, inspect Mission 11 Rift Forager + Parted Striker and one later encounter containing Castigator/Seer/Wrecker/Witness. All should read as Avarin, with temple face and paired natural crescent mantle and no bonded creature.
4. Verify hostile model classification by **authoring UnitId**. Reuse a fixture actor ID in another study and make sure the wrong skin never appears. Approved prefab must visibly take priority if one has been registered.
5. Move and attack. Confirm visuals respond to input playback but movement costs, damage, turn order, rescue, capture and saves behave identically. No hidden collision or extra world map blockers.
6. Examine old Mission 4 Gold HOLD zone; cover facial shifts must not obscure the zone. Confirm camera frames objectives and field.
7. Review 390×844, 320×568, and iPhone 14. Record triangle counts, draw calls, texture memory, frame pacing, frustum clipping, thermal stability and particle/shader behavior before shipping. None have been device measured by this PR.
8. Compare face, mantle, material and silhouette against Master Lore v26.0 and the approved World Five visual master. Any discrepancies are **rejection gates**, not implied authorizations to add lore.

### Automated regression

`python BuildTools/Art/validate_worldfive_battle_organics.py` is run by repository CI. It checks source identity, actual mesh generation, ownership/disposal, gameplay component absence, prefab precedence, animation connections and cover work. Core Windows/Linux gameplay tests run independently. **Neither test runs the Unity compiler or visually approves these designs.**
