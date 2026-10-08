# Home screen — approved sanctuary direction

Approved by Koby on 7 October 2026 as the visual target for Ninefold: Frontiers. The subsequent background-only derivative was also approved. This approval covers the Home composition, atmosphere, palette and interface treatment; it does not revise setting canon or replace a locked visual master.

## Controlling sources

- Master Lore Archive v26.0, section 26.6, incorporating unchanged v25.3 canon.
- v25.3 World Five sanctuary decisions 18.2–18.5 and Production Foundations 1–8.
- Locked World Five Avarin / Continuance showcase.
- Approved portrait Home concept: campaign centerpiece, prominent light primary action, secondary Quick Battle / Your Units, four-tab navigation.

The artwork depicts World Five sanctuary architecture, the Anchor-Spire Heart, ash-silver distributed Sanctuary Rings, fractured-dusk lighting and restrained silver-violet illumination. Rings are stabilization infrastructure, not casual portals. Incidental building placements and terrain are illustrative, not a new geographic map or canonical settlement plan. Foreground figures, banners and emblems were removed to avoid introducing unapproved anatomy or heraldry. No creature or Apex design is added.

## Implementation

`Assets/Ninefold/Resources/Home` contains a background-only JPEG, a scoped USS theme, licensed DejaVu Serif font, and abstract navigation glyphs. Text and buttons are live UI Toolkit controls, not pixels in the background. The approved campaign name **The Fractured Passage** remains the eventual story target; the integration build honestly shows its existing Test campaign A/B and mission labels until production content is authored.

Home automatically loads its resources on existing scenes. No manual Inspector wiring or scene rebuild is required. The current safe-area calculation applies to controls; the artwork extends behind device cutouts. Long/short windows use a scrollable center with fixed branding and navigation. No parallax, flashing or new animation is added. Missing artwork leaves a dark backdrop; normal UI controls still work.

Primary-action priority remains: saved battle → pending completion reward → next available unplayed campaign mission → browse/replay. Quick Battle and progression changes stay blocked during a saved battle. Errors restore the visible diagnostic header. Saves, rewards, unit balance and canon rules are unchanged.

The 853×1844 background is encoded as a quality-92 JPEG for the first implementation. It is not a 3D environment, and does not establish the final game's 3D quality. Texture imports disable mipmaps and CPU-readable copies; max import dimension is 2048. Small UI assets are explicitly stored as ordinary Git blobs so clones do not need LFS downloads for this screen. Larger production art retains the repository's existing LFS policy. The font license is included in FontLicense.txt.

## Art provenance

Generated with the built-in image tool from the approved Home concept and locked World Five showcase. The original generation remains in the conversation; the runtime JPEG is the derived, optimized asset. Original generated PNG SHA-256 is recorded in ArtManifest.json. Prompt is retained in ArtPrompt.txt. The locked source masters themselves are not republished in this repository.

## Review and validation

Open Preview.html locally to review a browser approximation built from the actual Home theme and background. `?state=resume`, `?state=claim` and `?state=browse` show alternative primary states. It is not a Unity screenshot or playable game; controls in this preview do not write saves or start battles.

Repository CI checks structure and core rules on Windows/Linux. Unity compilation and real device rendering are still required later. During that check: verify 320×568, 390×844, tablet and landscape sizes; all actions remain reachable, safe areas clear, long titles wrap, Resume restores the saved battle, claims remain one-time, navigation leaves the Home styling behind, and returning from battle/settings works. The compact breakpoint is measured in panel units, not physical pixels.
