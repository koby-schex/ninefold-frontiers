# Visual Interface Foundation 02 — PT-055

## Scope and user feedback

The latest Unity portrait screenshots showed that the underlying battle loop, preview/confirm interaction, campaigns and collection are functional, but the screens still look like text-heavy web UI. This pass introduces **visual-first presentation primitives** and a dedicated 3D-preview connection point. It is not a claim of finished production artwork.

The other active Work conversation **owns actual 3D unit models, prefabs, animations, model preview cameras, textures, render targets, and the rendering pipeline**. This PR does not change any of those.

## Implemented

### Campaign art surface

Every existing Campaigns overview card now has a compact image surface above its textual state. The art surface takes a future `Texture` from the preview bridge and otherwise displays an understated, **noncanonical abstract atlas placeholder**. It shows real completed/total counts and a lock/available marker; no false artwork or map information is invented. The existing card title, mission progress, primary action, locked preview and saved battle restrictions remain authoritative.

### Unit preview portal

Opening a unit detail now places a large, portrait-oriented preview frame before the unit's existing identity and progression controls. When no renderer is available, a neutral geometrical placeholder displays the temporary unit ID and **3D PREVIEW PENDING**. This is not a finished unit silhouette or faction emblem.

`Assets/Ninefold/Runtime/Presentation/FrontiersVisualPrimitives.cs` defines the separate, opt-in contract:

```csharp
using UnityEngine;
using Ninefold.Presentation;

public sealed class ModelPreviewAdapter : IFrontiersPreviewSource
{
    public Texture GetUnitPreview(string unitId) { /* actual render source */ return null; }
    public Texture GetCampaignPreview(string campaignId) { /* approved illustration */ return null; }
}

// Register/unregister within the 3D presentation adapter's own lifecycle:
FrontiersPreviewRegistry.Source = modelPreviewAdapter;
```

The interface accepts `Texture`, so both ordinary `Texture2D` illustrations and a separately owned `RenderTexture` can be supplied. **The adapter must own and clean up its camera, render textures, prefabs, and GPU memory**. It should use existing authored unit IDs; **no mapping** from a fixture ID to a canonical 3D model is asserted by this PR. The bridge is optional, read-only and does not touch unit game state or saves. It does not automatically rebind UI when a texture appears; the next normal screen refresh will read the provider. Don't connect models until the model owner signs off on a stable ID/asset/preview contract. If the provider is absent, the abstract fallback remains safe.

This PR does **not** set `FrontiersPreviewRegistry.Source`, create model prefabs or modify Work-owned files.

### Compact tactical HUD

- Replace one long active-unit status line with three compact data chips: active unit, remaining movement and action availability, all read from the existing `BattleInteraction` view.
- Shorten repeated guidance and the Main ability button label, but leave actionable confirm/cancel, per-target damage preview, move, end turn, attack and signature controls untouched.
- A small-scoped `visual-hud-mode` stylesheet reduces large header buttons and the fixed 224-unit battle dock to a 190-unit scrollable dock, returning more space to the actual battlefield. The existing scroll, fixed footprint and confirmation interaction are preserved.
- Never hide health or damage previews, change attack range/hit rules, reduce target eligibility safeguards, remove one Apex restrictions or alter battlefield calculations.

### Shared visual system

New `Assets/Ninefold/Resources/Home/VisualFoundation02.uss` (and .meta) provides preview surfaces, neutral atlas geometry, small faction-agnostic status capsules and HUD data chips. Existing palette references (deep green ink, warm ivory, muted gold and the approved silver-violet World Five Home accent) are preserved. No new lore or emblems are created.

Loaded last through the existing Playtest host and listed in the launcher resource preflight; source milestone is **PT-055**.

## Coordination / merge safety

- Branch `ui/visual-interface-foundation-02` is based on the latest `main` when created; no direct commits to main.
- The only existing code touched is UI composition in `FrontiersPlaytest.cs` and the launcher/revision resource wiring; new components live in isolated new `Presentation` and `Resources/Home` files.
- **Do not overlap** Work-owned model asset directories, Blender scripts, model-generation pipelines, prefab references, texture materials or animation code.
- If a subsequent Work PR changes `FrontiersPlaytest.cs`, compare and resolve explicitly before merging this PR. Don't silently overwrite either version.
- Keep new C# and USS `.meta` assets tracked with their authored GUIDs to prevent Unity reference churn.
- Art providers should implement the interface in **their own files** and register during scene/render adapter lifetime. They must not edit this stylesheet or change the authored core unit schema merely to display a preview.

## Full Unity verification checklist

1. Pull merged main with Unity stopped; wait for import and C# compilation. Confirm launcher **PT-055** and no missing `Home/VisualFoundation02` warning.
2. At 540 × 960 (Game tab fitted), open Campaigns: both fixture campaigns should show the abstract art-slot surfaces with actual progress, correct locked states and exactly the prior available actions.
3. Open Units → unit detail: check the preview area shows the abstract fallback with the correct test unit ID and a clear role/owned state. Check both Standard and Apex, owned and locked.
4. Try all owned/unowned unlock, advance, customize, confirmation/cancel paths; balances/progress remain unchanged until actual Confirm. Preview is strictly read-only.
5. During a saved battle, browse Campaigns and Units: inspect works, while starting a second battle, claiming rewards or spending fragments remains blocked. Resume after inspection without losing the pending turn.
6. In a battle inspect 540 × 960, 390 × 844 and 320 × 568. The battlefield should take more vertical space. All player actions, confirm/cancel buttons and damage/health previews must remain visible/reachable with scrolling where required.
7. Verify the HUD chips always show the current activation's actor, movement and primary-action state, including enemy turns. Inspect tapped target and marked movement behavior for no accidental orders.
8. Inspect keyboard focus, touch target sizes, safe-area inset and short landscape fallback. Verify no USS warnings, clipping, overlap or accidental live-map pointer interception.
9. Test result claim and mission navigation. None of the above should change progression or 3D scene world objects.
10. When the Work chat eventually provides a preview source, check it is registered/unregistered safely; missing/failed textures use the fallback and the UI re-reads preview data on normal refresh. Evaluate memory/thermal impact of RenderTextures on the actual phone.

## Known limitations

A primitive image slot and better screen hierarchy are **not** the same as actual 3D unit art. Current fixture IDs and the approved sanctuary JPEG are still placeholders in their own contexts. Core repository checks do not run Unity C#/USS compilation or device rendering; the user still needs one Unity playtest with the merged project. The new interface must not be interpreted as authorization to assign canonical world art to fictional test campaign IDs.
