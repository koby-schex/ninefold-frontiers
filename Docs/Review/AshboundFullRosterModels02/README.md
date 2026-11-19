# Full Ashbound roster — connected model delivery 02

**Scope:** all six ordinary Ashbound NPCs on the live battle renderer; the five remaining models replace their disconnected body and generic drape while Rift Forager retains its previous six-joint body and improves shared wearables and material handling. Sillrunner and Seamstalker remain unchanged except rigid weighting of their fitted eye shells and 16-bit mesh indices.

This is a substantial implementation batch, not final art approval. Master Lore & World-Building Archive v26.0 and the approved Ashbound canon addendum control all biology, combatant identities, living sensory crescent, absence of bonded companions, engineered Fracture Wake and consequential Afterecho. Existing source art and canonical documents are not changed. New connected skins use the established Avarin face and body coordinates. Local torso profiles are provisional art choices, not new species.

| Enemy | Connected body / garment | Preserved equipment silhouette / motion |
|---|---|---|
| Rift Forager | Previous tapered six-joint body, tailored fieldcloth | Route reader and satchel; new shared wearable geometry |
| Parted Striker | Broader torso, shorter asymmetric front fabric, ten joints | Right shoulder/impact housing and blunt contact ridge; dominant right elbow |
| Seam Castigator | Narrower upright torso and split rear fabric, ten joints | Reflector array, right forearm cage, rear capture spool; controlled right-arm articulation |
| Anchor Wrecker | Broadest torso, short braced front panels, ten joints | Layered protection, load braces and contact pad; heavier knee flex |
| Afterecho Seer | Slim torso and uneven long rear fabric, ten joints | Offset capture cassette, damaged shroud and external instruments; bilateral guarded arm movement |
| Camp Witness | Medium torso and split rear fabric, ten joints | Registry, utility pouches and left relay; left-biased articulation |

## Shared real geometry, no stand-in export

The six former runtime gear recipes are centralized in `AshboundWearablePlan`. The obsolete disconnected torso, neck, arms, legs, foot, face/cheek bodies and generic drape are **not instantiated**. Remaining actual equipment and mantle are generated through shared closed loft geometry with refined centerlines, continuous section frames, oriented caps and area-weighted normals. Both Unity and the OBJ exporter invoke the same exact recipes. Gear is attached to actual upper/distal joints and batched by material per joint (head parts remain separate for living mantle motion).

The five new models add elbow/knee bends to the existing upper arm, hip, torso and head transforms. Each role has distinct bounded gait/attack pose, flinch and collapse sampled from existing battle events. No Core combat behavior, collision, targeting, mission placement, save data, stats, faction unlock, approved prefabs or player models change.

All six use separate surface-response textures for fabric and salvaged hardware (R metallic/A smoothness, linear), in addition to base colors and normal response. Original material colors and the normal material ownership lifecycle remain intact. Shared connected body geometry and garments use 16-bit indices where possible. GPU mesh skin buffers are reused per type in the current battle, with separate instance joints. Native iPhone memory/FPS is not measured in this environment.

## Automated and native review

GitHub Actions First encounter model review exports eight exact body meshes and the six complete Ashbound assembled rest models with wearable geometry, cloth, mantle and face. The full roster contact sheets are rendered from those exact OBJ vertices, triangles and color assignments, with no substitute concept art. This is a **static rest mesh** inspection, not native Unity, animated deformation or final PBR review. The complete meshes include original visible equipment; unlike #188 the mantle and gear are not missing from the preview.

Core tests verify actual body topology and joint coverage, original eye clearances, all six wardrobe fits/closed sewn borders, original role signature gear/mantle count, mesh normals, valid attachment joints, role differentiation and action envelope behavior. Existing approved-prefab priority is preserved. Run the native Unity 6000.3.21f1 game in Missions 11–14, inspect all six, and verify native compilation, material rendering, animation contact, approved art/canon, and iPhone portrait performance before treating them as production art. Existing abstract fixture NPCs and the two earlier wildlife candidates remain independent.
