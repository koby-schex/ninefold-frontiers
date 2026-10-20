# World Five Campaign Development 01 — Opening Ten Missions

**Internal production candidate · Draft 01 · World Five: The Fractured Meridian · Canon source: Master Lore Archive v26.0**

This document is a **game-production outline**, not locked fiction, a Unity scene list, a released mission catalog or public-facing promotional lore. The opening mission premise is canon-grounded. The names, events, enemy/hazard particulars and level treatments below are additive *proposals* requiring story, visual and gameplay approval before being made playable.

## Canon anchors we must preserve

- World Five's canonical title is **The Fractured Meridian**. The Avarin are the featured species; **The Continuance** is the featured institutional identity. There is no unified Ninefold capital or universal central government.
- The first game's opening, near Year 0, follows a **small Continuance route-preservation field team**. A once dependable route has begun arriving somewhere other than its expected destination. The story begins locally; the explanation must not be prematurely stated, inferred or marketed as known.
- Initial emotional arc: **Continuity Under Pressure**. Start with preserving a route, not teaching cosmology through paragraphs. Nearby rescue, closure, stabilization and survival expose the consequences of instability.
- Approved World Five regions we use in this proposal: **Pale Corridors**, **Anchor Basins**, and a limited **Driftlands** edge. Nareth Sanctuary is a representative Continuance sanctuary, not an all-Ninefold capital. Broader Fractured Rim and Lost Marches content can wait.
- Bonded life is always **trust-first and consensual**; the Avarin mantle is an environmental sensing organ, **not** a creature-control mechanism. Canon bonds/traditions: Great Strider/Ilyth, Living Veil/Olyra, Riftwalker/Vaskel, Burdenbeast/Tervan. Serai Venn/Nethra are the World Five Apex identity; Nethra's origin and Serai's fate remain protected mysteries.
- The starter World Five campaign is one of many campaigns and modes planned for the game. Only **one Apex per battle**; ordinary campaign missions never silently allow more than five player units. **World Collision** (6–8 current authored positions) is a separately signposted event type, not a field-size choice. **No World Collision is proposed for these ten tutorial missions.**
- Campaign artwork/3D assets must come from approved source masters; the Work chat owns production unit models, prefabs, animation and rendering. No fictional enemy identity or missing canonical unit kit is locked here.

## Learning arc: teach without text walls

The player should learn in an actual combat encounter, with tap-to-act, one clearly marked objective, a relevant visual cue, and a *single contextual hint only when necessary*. Every following mission deliberately changes the objective, field rhythm or topography. Internal footprints below are **designer-only**; users never see Small/Medium/Large labels or choose a map size.

| # | Candidate mission | Region and internal footprint | Primary objective | Play mechanic / visual lesson |
| --- | --- | --- | --- | --- |
| 001 | The Wrong Arrival | Pale Corridors · close | Stabilize | One short legal movement approach; interactive gold anchor beyond entry |
| 002 | Hold the Passage | Pale Corridors · close, alternate lanes | Stabilize | Terrain/cover readability; two meaningful ways to reach the task |
| 003 | Return the Stranded | Pale Corridors · standard | Rescue & Extract | Distinguish rescue point from subsequent extraction destination |
| 004 | The Safe Boundary | Anchor Basins · standard | Secure | Hold a visible zone for required turns; no false instant-win cue |
| 005 | Shelter First | Anchor Basins · close | Protect | Keep a clearly highlighted nonplayer target safe |
| 006 | Wait for the Window | Pale Corridors · standard | Survive | Endure a short interval; no invented objective waypoint or false route |
| 007 | Clear the Approach | Pale Corridors · standard | Defeat | First deliberate normal attack, target preview and line-of-sight lesson; enemy identity *not* approved |
| 008 | The Divided Route | Driftlands · extended but bounded | Secure | Squad spacing around two corridors and terrain tradeoffs |
| 009 | Keep the Bond | Anchor Basins · standard | Rescue & Extract | Timed main ability vs movement/action economy; protect consensual bonded-life rules |
| 010 | A Line That Holds | Pale Corridors · extended but bounded | Stabilize | Integrates previous mechanics; optional goals without overwhelming the primary destination |

### Scenario-by-scenario intent and entry criteria

**001 — The Wrong Arrival.** A routine field check shows a familiar route's endpoint is unreliable. Introduce one unit/action/movement sequence, then stabilize a nearby interaction point. The player must *move* to the objective. Final environment key is pending approval; candidate `w5-pale-entry`.

**002 — Hold the Passage.** The problem is contained locally, but an alternate covered approach clarifies that map geometry matters. An obvious marked destination, one short route and an alternative through cover. Candidate `w5-pale-side-route`.

**003 — Return the Stranded.** Shift the player's focus away from defeating everything. A survivor/rescue target and extraction zone must be visible and distinct. No real-world/person name or explanation of disappearance is invented. Candidate `w5-pale-rescue`.

**004 — The Safe Boundary.** Introduce a hold-zone mechanic near a route-stability anchor; the goal is *occupy and hold*, not sprint across the map. The HUD shows remaining turns. Candidate `w5-basin-perimeter`.

**005 — Shelter First.** The playable team protects a clearly identifiable nonplayer party. A canonical Olyra/Living Veil can be introduced later if the Work chat delivers the corresponding finished model and the story role is approved. Do not fake the model or assign an invented ability. Candidate `w5-basin-shelter`.

**006 — Wait for the Window.** A localized closure requires surviving until the team can continue. Visual countdown and safe-lane staging matter; there is no waypoint when the objective has no position. Candidate `w5-pale-hazard-window`.

**007 — Clear the Approach.** Introduce a threat explicitly from an eventual approved adversary class, not an unnamed faction magically invented for World Five. Until then, only a test proxy is permitted. Teach attack-preview consequences, range and cover. Candidate `w5-pale-blocked-access`.

**008 — The Divided Route.** A limited Driftlands excursion broadens environmental identity without revealing the underlying world mystery. Internally a somewhat wider field, but *not* a massive war map and not World Collision. Two corridors and a single visible control zone provide spatial choice without confusion. Candidate `w5-drift-branch`.

**009 — Keep the Bond.** Return to a safer anchor-basin route to recover someone under pressure. A main ability is useful but not mandatory, and consent-first bonded-life canon is respected. The exact unit and kit cannot be chosen until approved. Candidate `w5-basin-return-lane`.

**010 — A Line That Holds.** Finish the first tutorial act with a tougher field stabilization and at most two optional goals; the crisis' explanation and its later consequences stay open. Give a meaningful conclusion to this *local* emergency, without resolving the campaign's protected main mystery. Candidate `w5-pale-stability-line`.

## Gameplay-authoring constraints before these become playable

- **Each mission needs an actual `MissionContent`**: valid map volumes/cover/costly terrain; three or more standard-unit-capable faction eligibility; stable, separate deployment points; valid objective targets/interactions; actual enemy brain setup when hostiles exist; intentional priority/optional goals; per-mission rewards; and sequenced `CampaignMission` prerequisite edges.
- The current production package still contains only **abstract `fixture-*` missions** and their progression. The proposed IDs `w5-draft-001` through `w5-draft-010` are NOT inserted into `ContentCatalog`, the Playtest Launcher or the phone UI. No saved-game revision changes.
- Teaching an ability cannot invent its name/mechanical effect; use an existing approved kit or wait for canonical kit/content pass. A main ability should not become compulsory if all available starter units do not have a compatible ability.
- One distinct visual environment treatment per opening beat is an *authoring intent*. A distinct key does **not** imply a finished scene exists. Camera, resource budget, terrain silhouette and canon review are required before deployment.
- The first three missions should feel learnable by touch, with short duration. Later missions remain suited to portrait mobile; wider fields stay intentionally bounded and navigable. The screen never displays internal footprint terminology.
- Any draft mentioning a rescue target, zone, adversary or bonded-life participant needs explicit human narrative and lore approval before it becomes a public story beat.

## How to use the code

The new `WorldFiveOpeningCampaignDraft.cs` is a **read-only blueprint**, not a runtime mission source. It exposes ten provisional titles/IDs, confirmed-region labels, draft art keys, one real `ObjectiveKind` per beat, one learning focus, internal footprint preference, authoring intent and a one-sentence objective-readability goal.

```csharp
WorldFiveOpeningCampaignDraft.Validate();
foreach (var beat in WorldFiveOpeningCampaignDraft.Missions)
    System.Console.WriteLine(beat.DraftId + " / " + beat.PrimaryGoal + " / " + beat.Region);
```

The production `CampaignProductionAudit` from PR #75 remains the separate *live catalog* quality gate. These ten draft beats should only enter that pipeline when each has a real, canon-reviewed mission definition. `Tests/Ninefold.Core.Tests/WorldFiveOpeningTests.cs` asserts the blueprint's 10 ordered IDs, varied real objective types, distinct scene-key intentions, canonical region vocabulary, non-playable isolation and unchanged existing save revision.

## Unity tests and coordination

No Unity screenshots are needed to merge this doc/authoring-only PR. The user can later test **PT-060 / W5-ENV-01**; those results should guide the next visual branch, not be overwritten speculatively. The Work chat's source art, model prefabs, materials, animation and renderer adapters remain outside the scope of this PR.

**Next after this:** Convert candidate missions 001–003 into *real validated playtest missions* on their own versioned content branch, once the World Five launch roster/unit names, objective behavior, environment assignment and localized opening story are approved. Preserve existing fixture saves or isolate the new production package entirely; do not replace checkpoint geometry in place. Expand toward 100+ missions in reviewed batches rather than dumping undifferentiated levels.
