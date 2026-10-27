# World Five — roster detail and idle study 02

Burdenbeast and Riftwalker continue from study 01 under the same v26.0 / retained
v25.3 entries, Production Foundations 1–8 and locked World Five showcase.
No canon is revised. The source forms remain substantially below the final
visual master's detail and must continue to be refined against it.

Tervan has fuller muscular upper limbs, narrower flexures, charcoal joint folds
and fitted civic module edging/panel ribs. Eight circular contact pads remain
grounded. Civic cargo and the folded Convergence housing remain separate from
the organism. Vaskel has stronger unequal limb profiles and nested organic lips
along the sensory crescent. Its counterweight remains clear of the floor and
its two locomotor contacts remain fixed. Both independent Avarin partners retain
their existing geometry and positioning.

Deterministic sRGB color maps add restrained slate-umber hide, plum-black tissue,
pale mineral variation and cloth grain. They use rest-space projected UVs with
smooth source normals across seams. These are study textures; a production
unwrap, sculpted normal maps and final material acceptance remain outstanding.

## Unity review

Use **Ninefold > Art > Burdenbeast Review** or **Riftwalker Review**. Create a fresh
review scene and press **Play** to see the 12-second looping idle. The generated
prefab has a local Animator controller and three mesh blend shapes: breathing,
balance and counterweight. The Avarin is stationary in this study. The breathing
period is six seconds; balance and counterweight use twelve seconds. Small
rest-space offsets retain planted feet and rigid civic equipment.

Generated paths are `Assets/Ninefold/ArtReview/<Unit>02_<fingerprint>/` with
`<Unit>Study02.prefab`, `<Unit>Review02.unity`, `LivingIdle02.anim` and
`LivingIdle02.controller`. Additional scene builds preserve previous folders.
Existing metadata GUIDs are preserved. Source OBJ, MTL, texture and manifest paths
remain under `SourceArt/Burdenbeast/` and `SourceArt/Riftwalker/`.

## Integration and checks

This review controller only demonstrates idle motion. It does not implement
the battle event Animator contract or register a model in the battle/Collection
catalog. Coordinate those changes with the gameplay chat after art acceptance;
walking, attacks, hit reactions and defeat remain separate work. CPU-generated
blend shapes are baked during scene construction, not recalculated every frame.
Native Unity import, animation playback, shading and phone performance need a
manual review; repository CI does not compile these Unity editor windows.

Source validation checks closed outward geometry, scale, limb/contact counts,
partner clearance and byte-for-byte reproducibility including texture files.
C# import checks validate actual textured OBJ sources and conservative idle
displacement bounds over every vertex, including fixed pads/partners, rigid
payload offsets and counterweight clearance. These checks do not certify all
intersections or physical mass balance. Rendered multiview sheets accompany CI.

Next: review the idle, refine silhouettes and civic architecture further, then
author grounded walking cycles. Apex construction remains a subsequent task.
