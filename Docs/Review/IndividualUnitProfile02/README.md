# Individual Unit Profile 02 — 3D Viewer Controls Contract

Added an optional `IFrontiersInteractivePreviewSource` to the existing model-preview registry. A model provider can render an actual in-game prefab in its own isolated scene/RenderTexture, then implement `RotateUnitPreview(unitId, degrees)` and `ZoomUnitPreview(unitId, delta)`. The Collection profile shows touch-friendly rotation/zoom buttons only when that capability is registered. Static/placeholder providers continue showing the existing noninteractive preview and an accurate explanation.

**No 3D model or rendering implementation was supplied by this PR.** The separate model development stream must implement the provider; the UI never creates cameras, prefabs, or RenderTextures. The profile reuses `GetUnitPreview` for image content. Button commands are not wired to physical transforms until a registered implementation exists.

No canonical unit equipment or lore content added. No combat or persistence changes. Follow-up: add actual interactive provider from approved model assets and pointer-drag/pinch behavior after verifying Unity touch events and model performance.
