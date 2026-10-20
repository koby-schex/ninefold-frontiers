# World Five Campaign Production Foundation 01

**Status:** content-authoring infrastructure. No commercial missions or canon changes are approved by this document.

## Production priorities

The first release of **Ninefold: Frontiers** launches with the **World Five / Avarin campaign**, intended to teach the player the tactics loop while still feeling like a full-length, visually distinctive journey. Each world eventually has its own campaign. The game will expand to additional factions, playable units and game modes over time. The launch campaign target remains **at least 100 authored missions**, not 100 clones of a demonstration map.

The prototype still has two abstract faction-A campaign missions and a separate faction-B engineering fixture. **Those fixtures are not the canon World Five campaign.** The current World Five-inspired material, scene names and geometry are *studies*, not approved named locations. Do not relabel temporary units as canonical Avarin, or invent names/abilities to fill the campaign count.

The other Work conversation owns 3D unit model production. This PR does not touch model assets, prefab identifiers, animations, rigging, materials or generated source. It also leaves the recently merged mobile UI (PT-060) and world scenery kit (W5-ENV-01) untouched pending the next Unity screenshots.

## Completed in this PR

A new pure-.NET, Unity-independent content tool:

`Assets/Ninefold/Runtime/Core/Content/CampaignProductionAudit.cs`

It accepts an existing, validated `ContentCatalog`, an authored campaign ID and a review stage. It returns deterministic, read-only `CampaignProductionReport` data with:

- Number of missions, unique terrain layouts, distinct scene keys and distinct primary objective kinds
- Issue codes, affected mission IDs and advisory/blocking severity
- A simple `PassesGate` result for the authoring workflow, **not** a claim of lore or visual approval

The original `ContentCatalog` already validates body/obstacle collision, deployable factions, object references and safe objective bounds. The new layer complements, rather than replaces, those rules.

### Draft review

`CampaignProductionAudit.Review(catalog, campaignId)` returns nonblocking advisories for:

- Back-to-back missions with *identical* battlefield bounds, cover geometry and difficult ground (even if an author merely moves spawns or renames the mission)
- A nonadjacent revisit to the same layout: a narrative return can be intentional, so this is only advisory
- A Stabilize objective reachable by a unit immediately on deployment
- Missing or unexpected world-specific `EnvironmentKey`, using an explicitly supplied prefix

The audit never alters enemy AI, map geometry, camera rules, progression, saves or squad constraints.

### Release review

`CampaignProductionAudit.Review(catalog, campaignId, CampaignReviewStage.Release, environmentPrefix:"w5-")`

Release mode blocks:

- Shipping `AbstractFixture` content
- Fewer than **100 authored campaign missions** (default target; override only for test scaffolding and explicitly different products)
- Consecutive identical battlefield geometry
- Missing/wrong-world scene treatments when the caller supplies its approved world-specific prefix
- Stabilization objectives instantly in reach at deployment
- Less than three distinct arena layouts, less than three distinct scene keys, or less than three primary objective kinds

The three-variety minimums are **basic quality floors, not good enough by themselves** for a full 100-mission commercial campaign. Authors should aim for far greater world, encounter and narrative diversity. The audit cannot certify fun, camera comfort, route clarity, mission difficulty or the locked visual master, and passing it never constitutes product-release approval.

The optional `environmentPrefix` is an **authoring-side world separation check**, not a player-facing label. For the current prototype, `study-w5-` is suitable; a production key taxonomy (and exact region identities) should be approved before real content ingestion.

## Planned campaign production pipeline

1. **Canonical story/timeline:** Confirm the World Five mission arc, factions, regions and key events against Master Lore v26.0 or the latest approved archive. No historical anchors rejected by the user may be reinstated.
2. **First ten missions:** Author a cohesive tutorial sequence with progressively introduced movement, normal attacks, terrain cover, initiative, objectives, ability timing, and squad assembly, but avoid permanent explanatory text overlays. Tutorials must also work with touch-first combat.
3. **Encounter authoring:** Every mission has its own objective, actual map bounds, spawn/deployment, appropriate enemy AI, reward, optional challenges and precise gameplay routing. Existing ContentCatalog construction must validate each mission before it can appear in the game.
4. **Map-scale diversity:** Use different compact, intermediate and wider internal field footprints for purposeful encounter design, without exposing a size picker or tier to the player. Larger does not mean massive. Only specially authored **World Collision** battles are labelled as such.
5. **World Five environment identity:** Author scene keys for appropriate World Five regions, distinct but canon-compatible material/terrain kits, changing terrain silhouettes, botanical signatures and collision events only where supported by locked lore. No generic World Five art pasted into other worlds' campaigns.
6. **Enemy and unit content:** Once production-ready models, stable IDs and names arrive from the Work chat and canon roster, bind them explicitly to the relevant world/faction and finalize abilities, passive descriptions, 3D previews and animation cues.
7. **Reward/progression QA:** Test each mission chain, unlock eligibility, beginner affordability, campaign one-time rewards, Apex access, defeat recovery, checkpoint reopening and free-to-play fairness.
8. **Release gates:** Run existing core integration tests, this new production audit, Unity smoke/device performance, manual lore approval and accessibility checks. No gate should be bypassed merely to hit a mission number.

## World Collision design boundary

World Collision is a separately authored high-stakes battle/war event that may support **more than five player units** with special camera and squad handling. The current engine can author 6–8 deployment slots for that encounter kind. Neither this review nor this plan adds World Collision to the launch campaign or exposes a user-selected field size. Actual performance, visual scale, path clarity, multiple unit previews and larger-player-squad AI integration still require further development and testing.

## Review workflow example

```csharp
var draft = CampaignProductionAudit.Review(
    contentCatalog, campaignId, CampaignReviewStage.Draft,
    environmentPrefix: "study-w5-");

// A release review is deliberately expected to fail on current fixtures:
var release = CampaignProductionAudit.Review(
    contentCatalog, campaignId, CampaignReviewStage.Release,
    environmentPrefix: "w5-");

foreach (var finding in draft.Findings)
    System.Console.WriteLine(finding.Code + ": " +
        (finding.MissionId ?? "campaign") + " / " + finding.Detail);
```

These method calls do not change content, checkpoint bytes, flags, player visibility or any other application state.

## Tests

`Tests/Ninefold.Core.Tests/CampaignProductionAuditTests.cs` covers draft fixture warnings, differing V3 layouts, world-specific environment prefixes, explicit release blockers, argument validation, and unchanged catalog revision.

The broader GitHub suite runs on Windows and Linux. As always, **GitHub tests are not Unity UI compilation and do not establish on-phone visual quality**.

## Next best production action

Once the pending PT-060/W5-ENV-01 Unity playtest is complete, capture screenshots of Home/Campaign/Unit Detail and both World Five battlefields. In parallel, begin **World Five Campaign Arc 01: mission design and tutorial beats 001–010** with canonical world location and faction context before committing named assets and actual playable mission definitions.
