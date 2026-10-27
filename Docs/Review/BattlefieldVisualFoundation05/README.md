# Ninefold: Frontiers — Battlefield visual foundation 05

## Implemented
- **Open-center ground halos:** a shared 48-segment annular mesh replaces the filled cylinder/cube selection, target, inspection and movement-destination markers. Targets and movement previews retain the same world positions and interaction semantics, but terrain and feet remain visible through the center. Subtle halo breathing applies only to the active selection and movement preview.
- **Grounded unit contact:** every rendered combat actor shares a 64px antialiased radial alpha texture and transparent material. Contact silhouettes follow legitimate movement positions and disappear when the rescued civilian is carried or an actor is defeated. The renderer reuses the same texture and material; no per-unit texture creation. When transparent shaders are stripped, contact shadows are *disabled*, not rendered as opaque polygons.
- **Environment-directed lighting:** a single persistent shadowless directional light is adjusted by the existing BattlefieldSceneDirection mood. It does not modify global RenderSettings, navigable terrain, mission content, or any model assets. A single shadowless pooled point light adds a short glow at the target during real combat effects. No runtime light spawning per strike.
- **Battle camera response:** a tiny bounded recoil uses the existing battle-view camera only while the authoritative action animation plays. Disabled automatically on reduced-motion/instant animation, and reset after playback and when exiting a battle.
- **Resource lifecycle:** rings use one pooled mesh; each contact shadow is cleaned up with its unit, and the shared mesh/texture/material and lights are released with the battlefield renderer.

## Manual Unity / iPhone portrait verification
1. In Missions 1–3, inspect the active/selected actor ring and movement halo. Ensure the center is transparent, no obscured model feet, and taps/selecting behave identically to before.
2. Check World Five survey/fracture scenes on an actual portrait phone: key lighting must show terrain without flattening model textures. Switch scenes and ensure color/mood updates correctly.
3. Test normal attack, healing, and status effects: impact light follows the actual target, occurs only during the ability event, and disappears after playback. No light persists when returning Home or canceling.
4. Toggle **reduced motion**. Ensure camera recoil is absent and no flicker or stale impact glow remains.
5. Re-test Mission 3 and Mission 9 rescue: NPC no longer casts a shadow at pickup after being carried; the designated player carrier still reaches the gold extraction zone.
6. Defeat an opponent then restart a mission: old contact shadows, target rings and lighting must not leak between attempts.
7. Review iPhone 14 frame rate, shader compatibility, overdraw and scene lighting while the highest player/enemy unit count is active.

## Boundaries
This is a **rendering-foundation and tactical readability upgrade**, *not* a claim of final production graphics. The battlefield still contains abstract placeholder unit bodies where real gameplay prefabs have not been integrated. The parallel model workstream owns Great Strider / Living Veil source meshes, anatomy, texturing and rigs; this PR does not edit their assets. Next major art step is production-model integration into playable battles and environment asset replacement.

The repository's Windows/Ubuntu Core tests and structure check **cannot execute the Unity renderer or GPU shaders**. Unity editor and iPhone testing remain required before certifying visual quality.
