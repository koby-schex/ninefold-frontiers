# World Five — Ashbound Combat Batch 02

## Scope implemented
Six **anonymous nonplayable** Ashbound NPC templates now exist in the isolated Act One catalog: Rift Forager, Parted Striker, Seam Castigator, Anchor Wrecker, Afterecho Seer, and Camp Witness. Each has its own initiative, movement, health/armor, attack power/range and four-slot combat kit. The first post-tutorial encounter continues to use the Forager and Striker. Existing ten tutorial missions remain Ashbound-free. This uses a new **isolated content revision**, leaving V4 saves untouched.

## Numeric role baseline (prototype balance, not canon)
- Forager: fast, fragile, short range
- Striker: high close-range damage
- Castigator: ranged pressure
- Wrecker: tougher, slower
- Seer: mobile long-range glass cannon
- Witness: low-damage support-themed profile

All six use deterministic single-target damage kits as **temporary baseline abilities**; the dedicated Fracture Wake/Afterecho feedback, movement powers, sabotage, support targeting and telegraph animations **are not implemented by this PR**. No enemy is playable, named, special or Apex.

## Future major work (separate art/engine boundaries)
1. Build engine-supported, telegraphed Fracture Wake abilities and permanent Afterecho counterplay using status systems.
2. Integrate story sequencing, dialogue and mission 011 into Unity via opt-in migration without losing old checkpoints.
3. Extend missions 012 onward with numbered UI labels `Mission 11`, `Mission 12`, etc. **No individual mission titles on the player interface.**
4. Connect actual enemy models and attack animations when the separate 3D branch is ready; keep animation controllers and rig files out of this branch.
5. Test tutorial-to-Act One victory, progression, balance and in-device portrait readability before release.

## Save safety
New Act One catalog revision `w5-act-one-candidate-02`. No V4 profile rewrite. This remains an isolated content candidate, not a shipped Unity replacement.
