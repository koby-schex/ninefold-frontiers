# Rift Forager — Integrated Art Quality Batch 09

**Status:** provisional Ashbound / Avarin `ArtReview` asset. These are real source edits across geometry, fabric, shading, motion, LOD and QA. **Not** production-certified, canon-approved, Unity-playtested, iPhone-profiled or registered in live battle.

## Goal: one meaningful review rather than many micro-PRs

This integrated batch resolves the pattern of submitting one face radius or one texture parameter at a time. It combines coordinated changes and an automated visual review in a **single merge candidate**. The controlling Master Lore & World-Building Archive v26.0, locked World Five Avarin artwork and approved Ashbound addendum remain authoritative; no biology, abilities or species rules are changed.

## Included improvements

| Domain | Real delivered work |
| --- | --- |
| Overall silhouette | Narrowed connected pelvis/torso and upper shoulder geometry; reshaped actual existing tissue surfaces rather than layering extra primitives |
| Arms | Refined upper-arm transition, forearm longitudinal plane and elbow-to-wrist proportions |
| Legs | Refined thigh/knee silhouette, ankle taper and local leg anatomical contours |
| Back | Subtle integrated dorsal structural contour, no additional spine anatomy or new organ |
| Head preservation | Keeps prior source eyes, mouth, brow, temple-face and living crescent; regional body changes fall off far from facial features |
| Clothing silhouette | Broader natural flare and gathered asymmetric rear fieldcloth folds; physically modeled thickness preserved |
| Fabric shading | Recomputed area-weighted normals from real cloth and trim triangles instead of uniform `±Z` normals |
| Idle | Added restrained arm/wrist/neck micro-movement |
| Advance | Added actual pelvis vertical weight shift, differential foot roll, richer wrist/elbow follow-through |
| Attack | Added neck/hand/wrist secondary articulation around the existing main attack |
| Hit | Added elbow/knee recoil that returns to rest |
| Afterecho | Propagated existing instability to arms/hands/pelvis, preserving the Arcana consequence rather than adding a new power |
| Defeat | Added ankle, elbow and hand settle to terminal fall |
| Surface materials | Eight original swatches preserved; newly generated **linear URP packed metallic–smoothness** masks provide distinct fabric, skin, natural mantle, and worn hardware response |
| LOD preparation | Three real connected-anatomy resolutions: high `.023`, medium `.036`, distant `.052`; fully bound to the same 19-joint rig and staged in a Unity `LODGroup`. Source mantle/gear and tailored cloth stay separately visible |
| Real-mesh automated review | Exact C# exporter writes high + two additional anatomy-only OBJ tiers; CI rasterizes a matched three-tier 390×844 comparison, alongside original before/after face, close, tactical, and narrow views |
| Native Unity review coverage | All **six** existing animated states captured at 390×844 tactical angles (24 views), 320×568 portrait angles (12 views), and 390×844 close angles (12 views) in contact sheets. The review validator reports **active high-detail** triangles, not a misleading sum of all LOD tiers |
| Tests/quality gates | New .NET tests for actual full-body regional sculpt, three geometry tiers, normal validity, secondary motion and existing source facial clearance; new source/artifact checks for LOD exports and contact sheets |

## Review checklist

1. In GitHub Actions for this PR, inspect **Ashbound model source checks → Rift Forager connected anatomy comparison** and the artifact `rift-forager-connected-anatomy-before-after`.
2. Compare the newest **AFTER** views against #186 AFTER at the **same camera/scale**. Inspect front, three-quarter, back and mobile tactical views. Use `RiftForager-ThreeLOD-Tactical-390x844.png` to judge the three actual anatomy mesh tiers.
3. The static CI renderer uses the **original MTL swatches**, not Unity URP materials. To assess actual mask shading, rig motion, cloth clipping, 48 native review captures or mobile FPS, a separately run pinned **Unity 6000.3.21f1** ArtReview session is still necessary. This workflow cannot run Unity locally or automatically certify iPhone performance.
4. Reject shape/canon mismatches even if numerical checks pass. Especially examine the mantle, facial silhouette, dangling fieldcloth and six-clip leg/arm interactions. No live enemy-catalog promotion until separately approved.

## Protected boundaries

No change to active battle visuals, campaign missions, unit stats, AI or attack rules; no alteration of the original `SourceArt/Ashbound` OBJ, MTL or manifests; no new biological additions; no bonded Ashbound companions; no edits to Riftwalker, any other World Five creature or any playable faction. The existing live unit size, tags and Apex restrictions are unchanged.

This batch is an integrated **candidate-quality improvement**, not a claim that a procedural mesh is now a finished professional hand-sculpt. Further approval should be based on a single comprehensive visual acceptance, not a succession of tiny radius changes.
