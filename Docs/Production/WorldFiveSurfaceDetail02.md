# World Five — surface detail 02

## Feedback and scope

The player found Great Strider's color change hard to see, liked Living Veil's
base colors and revised deployment, and requested much more detail and life.
This is the first bounded response: stronger surface relief and reliable preview
materials. It does not claim the requested anatomical sculpt or living behavior
is finished. The locked World Five visual master remains the target.

Great Strider's palette was deliberately close to gray. No missing albedo binding
was found in its review importer; an older review scene or raw OBJ import can also
hide the current material work. The window now warns explicitly when its scene's
revision/fingerprint differs from current source. Flexible limb tissue is more
clearly smoky plum beneath the existing pearl-gray and silver-white surfaces.
Eight tangent normal maps and eight smoothness maps add lighting-dependent tissue,
mineral and fabric relief. Eight albedo maps have stronger bounded variation.

Living Veil retains its approved base palette, canopy and transition curves. Core,
skin, fabric and living mantle maps have stronger relief and finish variation.
The motion viewport previously omitted normal/finish maps, clamped every albedo
map and forced textured mantle opacity to one. It now loads linear detail maps,
repeats tiled surfaces, preserves mantle opacity and recalculates mesh tangents
when deforming. The canopy alone uses clamped UVs and its authored RGBA alpha.
No runtime or UI integration changes are included.

## Review assets

Under `Assets/Ninefold/ArtReview/`, generate new copies from the existing Art menus:

| Source identifier | Folder and prefab | Scene |
| --- | --- | --- |
| GreatStrider | `GreatStrider13_<fingerprint>/GreatStriderStudy13.prefab` | `GreatStriderReview13.unity` |
| LivingVeil | `LivingVeil10_<fingerprint>/LivingVeilStudy10.prefab` | `LivingVeilReview10.unity` |
| LivingVeilProtective | `LivingVeilProtective10_<fingerprint>/LivingVeilProtectiveStudy10.prefab` | `LivingVeilReview10.unity` |

The scenes are inside their fingerprint folders. Old scene assets and existing
metadata remain intact. Reload source in the Living Veil Motion Review window.
Production unit-ID mapping and preview-provider registration remain pending.

## Evidence and limitations

`review_surface_detail.py` verifies the sixteen new Strider maps are reproducible,
all fifteen detailed materials have unit-length normals, bounded relief and
nonuniform finish, and tissue/mineral normals contain measurable surface relief.
It exports literal albedo/normal/smoothness inspection panels. Existing geometry,
UV, source reproduction and motion checks remain active. Source geometry is
unchanged. Normal maps affect shading, not silhouettes or collision geometry.

CI renders all four Olyra clips independently instead of serially, retaining the
same frames and all 308-frame motion validations. Software mesh renders do not
reproduce native URP normal shading; Unity compilation, shader appearance,
transparency sorting and phone readability/performance remain unverified here.
Box-projected textures and these procedural surfaces remain provisional.

## Next model work

1. Larger forms: layered grown reinforcement and soft-tissue transitions on Ilyth;
   core-to-lobe roots and organic membrane folds on Olyra, compared against canon.
2. Secondary life: breathing with fixed contacts, soft tissue lag, and independent
   mantle/ribbon response. Preserve the accepted Olyra deployment path and scale.
3. Native review of both at close-up and actual battlefield size before finalizing
   material strength, followed by rigging/LOD and game-preview integration.

These steps require actual model work; stronger texture contrast alone cannot
make the current simplified studies match the locked master.
