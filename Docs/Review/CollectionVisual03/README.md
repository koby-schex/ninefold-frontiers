# Collection Visual 03 — PT-056

## Why this PR

The game is becoming substantially easier to navigate, but the real Unity screenshots still show **too much persistent text** and abstract unit-ID circles. Visual Foundation 02 introduced an optional texture source for unit details. This pass extends that same source into **Collection roster thumbnails** so the unit is the primary point of recognition.

### What changes in Unity

- Each Collection row now has a large, portrait-shaped visual slot. If a producer supplies a texture for a matching **gameplay UnitId**, it appears automatically; otherwise a restrained abstract test-ID fallback stays visible. Standard and Apex roles have different border/caption treatment but no fictional emblem.
- Owned/rank, resource quantity, and a thin fragment fill are easier to skim. When an existing core offer reports `CanConfirm`, the row shows **READY** without automatically starting an upgrade.
- Keep All / Owned / Locked filters, sorted authored roster groups, the full-row touch target, existing ownership and fragment counts, the advance/unlock confirmation, and the full ability kit inside unit detail.
- Unit detail now uses the large preview panel as its visual identity rather than repeating a second prototype badge beneath it. Its progression actions and inspection panel are otherwise unchanged.
- **Preview framing fix:** unit images now use `ScaleMode.ScaleToFit` to preserve a complete model. The optional campaign artwork continues to use `ScaleMode.ScaleAndCrop` inside wide cards. The World Five Olyra protective pose is approximately 5.6m wide by 3.4m high, so center-cropping would cut away meaningful lobes and partner anatomy.
- The new `Home/CollectionVisual03.uss` styles only Collection. The source label advances to **PT-056**, and the launcher explicitly validates the stylesheet.

### Work chat separation

As of this review, PR #67 (Olyra model) and #68 (Visual Interface Foundation 02) are merged. `Docs/Production/ModelPreviewHandoff.md` establishes that Great Strider and Olyra source art/review prefabs are **not production runtime prefabs**. They have distinct art identifiers. The UI must **not** map `fixture-a-* / fixture-b-*` gameplay IDs directly to those models.

**This PR does NOT:**

- Touch any `SourceArt`, `BuildTools/Art`, `Assets/Ninefold/ArtReview`, Work model/editor import, animation, material or preview-camera files.
- Create stable prefab paths or asset GUID mappings, load OBJ directly at runtime, create cameras/RenderTextures or instantiate models.
- Change any unit schema, core gameplay, save data, progression, balance, roster membership, or campaign content.
- Claim that the abstract temporary IDs represent the actual World Five species, Olyra or Great Strider.

The separate Work chat will eventually deliver a stable model/prefab and rendering adapter, and its owner will decide when a gameplay UnitId corresponds to a canonical asset. The existing `FrontiersPreviewRegistry.Source` contract remains unchanged. A producer may return `Texture2D` or `RenderTexture`; it **owns texture lifetime**. The UI reads the provider on an ordinary collection rebuild. This presentation PR has no side effects on the provider or renderer.

### Test in Unity 6.3 LTS 6000.3.21f1

1. After CI passes and the PR is merged, stop Play, pull `main`, finish import. Launcher should show **PT-056**. Check Console for `CollectionVisual03` missing/USS warnings.
2. Open Units at fixed 540×960 (fit Game view zoom). Verify the **All / Owned / Locked** filters each show appropriate portrait rows, never fictional unit art. Confirm all counters and fragment fills match the same core offer data as before.
3. Inspect a standard owned unit, a locked unit and an Apex. On opening detail, the hero preview should be visible and **no second redundant ID badge** should appear below it. Return via All units; no changes to save state.
4. View stats and the four ability slots. Test Preview→Cancel and Preview→Confirm upgrade, unlock, customization and max-rank behavior. No progress changes merely by opening a thumbnail.
5. During a saved battle, Units remains browsable for inspection but unlock, advance, customize and campaign launch are still core-guarded.
6. Check narrow 320×568 and 390×844 portrait, tablet and short landscape. All rows scroll; arrow and full-row touch targets stay visible and unit text does not clip.
7. **When and only when the Work chat adds a production source**, test a wide non-humanoid creature render in both thumbnail and hero; verify complete silhouette is contained with no crop, no blank/black RenderTexture, no camera interference, no asset lifetime leaks, and stable FPS/memory on device. Missing model mapping should show the abstract fallback.
8. Confirm Home, Campaigns, Mission List, Battle, Settings and Results are unchanged. Capture a Units roster and unit-detail screenshot for the next art-direction review.

### Limitations

No production unit portrait images or 3D runtime models are supplied here. A PR can improve visual balance and the preview interface, but **cannot prove high-fidelity visuals** without runtime assets and actual Unity screenshots. GitHub automated checks are not a replacement for compiling/rendering the project in Unity Editor or testing on the iPhone.
