# Unit Combat Identity 01 — Support Kit Candidate

This opt-in, **noncanonical** engineering candidate gives `fixture-a-2` a distinct support-oriented four-slot kit: normal attack (300 damage), main ability (540 healing), signature (420 damage), and an explicitly unimplemented passive. Other prototype units keep their original fixture kits.

Prototype health now varies by unit (for example 1220 for fixture-a-1 and 1320 for fixture-a-2, with NPC 1080). The original generic damage and healing values scale accordingly (480 damage normal, 360 healing main, 720 signature), and armor is scaled tenfold. These are preliminary playtest numbers, not canonical or final balance. The unique V2 revision and profile isolate them from older saves.

The ability system is reused rather than bypassed. The candidate defines its own catalog package/revision and profile ID, so it must only be used with **independent battle and progress storage**. It is not wired into the existing V4 Home campaign entry and does not modify any saved World Five attempt.

## Follow-up

Review approved Ninefold lore before assigning abilities/names to actual species. After Unity and CI validation, introduce an explicit playable opt-in selector and refine targeting feedback, ability descriptions, animations, balance and passive mechanics. The model team owns 3D visual assets.

## Checks

Core tests assert distinct values, unchanged V4 missions, and support-unit checkpoint resume. Test from Unity 6.3 LTS before any future main-campaign integration. This candidate does not assert release or canon approval.
