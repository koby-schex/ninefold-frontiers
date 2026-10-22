# World Five story structure — approved direction, implementation stage 01

## Story presentation
**Tutorial = Prologue (missions 001–010).** They should not feature, name, reveal, or fight the Ashbound. Existing hostile `fixture-npc` figures must be framed as test stand-ins for route hazards or non-Ashbound opposition until those authored encounters are revised; merely omitting the name while dressing them as Ashbound is not permitted. A story mystery about incorrect routes, unstable crossings, vanished travelers and rescue obligations accumulates without identifying a villain or disclosing the protected route-crisis reveal.

**First post-tutorial mission = main story opening** (`w5-story-011`, RESERVED story ID, not yet present in playable ContentCatalog). The Continuance investigates deliberate damage to a crossing after a prologue rescue. A splinter Avarin group is encountered for the first time; dialogue and visuals establish **The Ashbound** and distinguish intentional sabotage from natural fracture conditions. Start with ordinary anonymous NPC variants: Rift Forager and Parted Striker; later add Camp Witness and Anchor Wrecker. The Parting is initially referenced through an abandoned companion harness and a survivor's testimony rather than shown gratuitously. The Afterecho presents a readable vulnerability once the player meets initiated opponents. The player's immediate objective is to protect a stranded party and hold an escape lane; it is not to exterminate a settlement.

**Act One onward** should develop the Ashbound's ideology, power-and-drawback mechanics, camp society and a meaningful story arc, with later opportunities for non-Ashbound creatures and route hazards. Named bosses require a dedicated narrative milestone and approval.

## Reusable narrative model for all nine world campaigns
Each campaign gets **a distinct story**, tailored to its world rather than a reskin of the World Five cult:
1. **Playable prologue/tutorial context**: movement/abilities are taught while a problem emerges; onboarding objectives matter to characters.
2. **Inciting incident & antagonist reveal**: the first non-tutorial mission shows the defining conflict and stakes.
3. **Escalation and choices**: terrain, relationships, mission objectives, and consequences progressively change.
4. **Midpoint reversal**: learning alters the player's interpretation and creates new goals.
5. **Climax & resolution**: major objectives culminate in a real story outcome, with hooks for later content.
6. **Mission narrative fields** (to implement later): pre-mission briefing, in-mission triggers, objective-specific dialogue, success/failure debrief, optional scenes and unlock consequences. Use short skippable dialogue and replayable mission summaries in portrait UI.

## Scope & save safety
This PR introduces only the **runtime story boundary and its automated guard test**. It does not retcon or renumber existing V4 missions, change save revisions, implement a new mission, introduce battle assets, modify 3D, or assign enemies to missions. Before making `w5-story-011` playable, extend mission content, campaign chain, rewards and battle-save version in a deliberately coordinated migration, then add the enemy AI/ability profiles and a full progression playthrough.
