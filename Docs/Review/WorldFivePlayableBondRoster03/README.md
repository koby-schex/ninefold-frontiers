# World Five playable starter roster — integrated battle models 03

**Scope:** the four existing ordinary playable standard-unit slots, not a new roster or new unlock scheme. The isolated `WorldFivePlayableAbilityStudy` maps the existing three starter IDs to Great Strider, Living Veil and Riftwalker, with Burdenbeast in the fourth standard slot. This PR adopts that mapping as **provisional visual presentation in the World Five campaign only**. The existing unlock schedule and three-unit beginning remain unchanged. The locked Apex slot remains outside this batch: no approved named Apex source model is available in the four standard asset directories.

| Existing slot | Whole paired model | Live presentation |
|---|---|---|
| `fixture-a-1` | Great Strider — Ilyth and mounted Avarin | Original four articulated leg/contact assemblies, living sensory sail, independent cradle/operator grouping |
| `fixture-a-2` | Living Veil — Olyra and standing Avarin | Original five-lobed membrane and exact resting/protective source correspondence, bounded protective morph on existing heal cues |
| `fixture-a-3` | Riftwalker — Vaskel and independent Avarin | Corrected legless Vaskel source, body and counterweight glide; no creature locomotor limbs or grounding |
| `fixture-a-4` | Burdenbeast — Tervan and independent Avarin | Original eight articulated limb/contact assemblies, rigid civic platform and separate standing partner |

## Actual implementation

Pack byte-identical existing authored OBJ, MTL and PNG sources, including Olyra's protective pose, as Unity TextAssets under a scoped Resources folder. Source files outside Assets are not implicitly available in a player build. This design avoids reliance on a separately generated local editor prefab. Committed source geometry is preserved; these are refined game-presentable versions of the existing art candidates, not a claim that newly sculpted commercial-quality models replaced the approved shape masters.

Runtime loads and batches original source surfaces by material, with separate transparent closed shells. A pure Core parser preserves exact source-index and UV correspondence, original smooth normals, and texture references. GPU meshes are generated once per type and shared across battlefield instances with independent articulated bones. Real source skin weights bind four/eight companion legs and rigid contact pads, mounted operator/cradle, separate standing partner head/limbs, and the legless counterweight. Original albedo, normal and packed finish maps drive URP materials; Olyra's RGBA membrane alpha is not multiplied twice. Native transparency sorting is still a device review requirement.

The whole pair is proportionally normalized to tactical scale (height 1.45, width/depth 1.42); no creature/partner is resized independently. Rest and Olyra's protective extent both contribute to fit. The battle renderer consumes already committed move, attack, heal, hit and knockout cues; the Olyra protective morph is visual and grants no new abilities or status. Post-action and hidden scenes reset to neutral. A legged companion uses source-matched multi-bone stance/lift weights; this is restrained provisional visual motion, not a validated planted-foot IK or collision animation system. Vaskel never receives creature legs. Combat authorities, source lore, catalog IDs, ownership, stats, movement, turn economy and saved progress remain unchanged. Existing approved-prefab priority and fallback remain intact. World Five collection, squad and combat labels follow the visual mapping while other fixture studies retain their generic labels.

## Single consolidated review

The new GitHub Actions workflow exports all four exact packaged runtime rest bodies and sampled poses, including Olyra's actual shifted protective form, using the same source and numerical skinning plan as the live renderer. It produces complete detail and tactical sheets from software depth rasterization with authored diffuse samples and approximate nearest-layer transparency. No Unity native render, production sculpt/retopology, mobile FPS measurement or canon/art approval is implied.

Core tests exercise package/source equivalence, real rig/limb coverage, all model sections, grounded contacts at idle, finite bounded transforms, Olyra vertex correspondence and five lobes, original starter gating and unchanged Apex identity. Structure checks verify actual runtime wiring and resource cleanup.

**Remaining native acceptance:** Unity 6000.3.21f1 compilation; confirm all three owned models appear in Mission 1 and the fourth appears only after its normal unlock; visually inspect both Olyra forms, Vaskel hovering, Ilyth and Tervan contact, source art fidelity, animation clipping, shader transparency, loading hitch and iPhone frame time. Source files are ~22.5 MB before compression; source meshes have roughly 60k–94k original triangles per pair. This is not a validated final mobile triangle budget. No approval/catalog assets or locked canon files are changed. No APEX model is invented or unlocked.
