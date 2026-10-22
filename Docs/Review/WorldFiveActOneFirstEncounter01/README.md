# World Five Act One — The First Ashbound Encounter
**Implementation candidate**: new `WorldFiveActOnePrototype.Create()` content catalog with separate save revision. Existing V4 Unity entry and saved battles remain unchanged.

## Story
**Prologue 001–010** — the Continuance stabilizes routes, protects stranded travelers and investigates route anomalies without encountering the Ashbound.

**Mission 011 — The Cut Crossing** (working title) — on the first deployment after tutorial completion, the team responds to deliberate damage on a route. A scavenger and an initiated skirmisher of the **Ashbound** confront the party. The opposing side uses no creature partners. After victory, the recovered evidence shows an intentionally emptied creature harness and a fracture-tuned device.

- **Briefing:** “The crossing failed from the wrong side. It wasn't a collapse. Find the travelers, and keep the approach open.”
- **First contact:** “Those aren't Continuance markings. Their harnesses are empty.”
- **Debrief:** “The damage was deliberate. Someone is turning the routes against us.”
- **Narrative caution:** no premature explanation of the larger route crisis, Nethra, or Serai Venn. No gratuitous depiction of the Parting.

## Implemented now
- Dedicated isolated 11-mission content candidate (existing ten tutorial missions followed by 011).
- Two NPC-only Ashbound unit templates (Rift Forager, Parted Striker) with distinct health/initiative/movement values and repeatable anonymous actor IDs.
- First hostiles at mission 011, a real combat defeat objective, associated reward and post-010 prerequisite.
- Regression tests for exact story ordering, absent Ashbound in tutorial, noncollectibility and distinct save revision.

## Not yet implemented
- This is a **prototype catalog**, not yet hooked to the Unity menu (avoids breaking V4 save progression).
- Temporary `fixture-kit` attacks remain on Ashbound templates; bespoke Fracture Wake, Afterecho, abilities/telegraphs and final AI behavior remain outstanding.
- The written briefing / midbattle / debrief lines above are not wired into UI.
- The 001–010 existing hostile fixtures still require lore-friendly replacement as non-Ashbound hazards or alternative opposition.
- Full live 011 victory/reward/next-act playthrough remains to be verified.

## Later integration
Prepare explicit V4 progress upgrade / opt-in fresh V5 profile, add dedicated narrative presentation, then integrate verified NPC kits and final 3D assets from the separate model workstream. No early player-facing reveal in UI.
