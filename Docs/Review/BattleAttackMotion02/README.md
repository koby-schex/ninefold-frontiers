# Battle Attack Motion 02

Adds a deterministic, renderer-only animation pass to the existing abstract battlefield. Ability users turn toward their target, briefly lean back during windup, lunge at strike and recover; targets react with a short scale-and-height pulse. This replaces the previous flat Z-axis wobble and moves the visuals toward spatially coherent combat.

All motion occurs after authoritative combat outcomes; hit accuracy, HP, status effects, AI and saved state are unchanged. This is animation choreography for the placeholder battlefield bodies, not skeletal animation or completed creature rigs. The model workstream can later use the same attack/impact event timing without this branch touching its assets.

Playtest: observe attack direction and impact, healing feedback, rapid consecutive turns, reduced-motion mode and defeat scale restoration. Further tasks: animation clips on actual rigs, per-ability VFX/telegraphs, and scene quality.
