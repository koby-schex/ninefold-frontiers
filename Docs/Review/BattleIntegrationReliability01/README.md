# Battle Integration & Reliability 01 — PR 94

## Core regression scope
Add four integration tests using the existing actual battle flow, checkpoint persistence, enemy execution and mission outcomes. Assert that winning and losing mission results persist through reload, spent abilities cannot be replayed after reload, enemy turns do not deal double damage after reloading, and terminal battles cannot schedule further opponent actions.

This augments existing coverage for stale input sessions, interrupted writes, control ownership and campaign rewards. It does **not** certify every World Five mission as end-to-end human-playable or prove all Unity UI interactions work.

## Project-wide Ninefold experience standard (for future UI work)
Consistent from first launch through home, collection, campaigns, battle and results:
- Use the approved Ninefold visual/canon references as the source of truth, not generic fantasy/sci-fi styling.
- Consistent design tokens: palette, typography hierarchy, layout grid, corner shapes, icon stroke, spacing, focus/pressed/disabled/selected states and touch target sizing.
- A single shared navigation and bottom-menu geometry; no Home-only sizing deviation.
- Shared button components and interaction vocabulary; double-tap enemy attack and single-tap information/selection remain consistent with PR 85.
- Clear information hierarchy and accessible contrast/touch feedback; avoid floating bars and gratuitous HUD clutter.
- Smooth state transitions and predictable back/resume behavior. Users must understand objectives, progress, consequences and rewards.
- Do not substitute unapproved placeholder artwork for locked canon; coordinate any 3D model work with the separate Work development chat.
- Measure polish through real on-device testing, not source-level assertions alone. Replayability requires satisfying combat, pacing and progression in addition to consistent styling.

This document is a **design requirement**, not a claim that the current Unity UI already meets each criterion.

## Ownership / compatibility
Tests, test registration and documentation only. No runtime behavior, save schema, UI prefabs, materials, meshes, animations, scenes or World Five mission content changed.

## Verification
Run the Windows/Linux core and repository workflows. Verify on-device later; no Unity editor or mobile visual QA is claimed by this PR.
