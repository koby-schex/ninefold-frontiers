# World Five — Ashbound Mid-Roster Live Visuals 01

**Status: provisional real in-battle 3D refinement, not production-art approval.**

## Purpose

Continue after the merged Rift Forager / Parted Striker live model refinement (PR #173) by improving two more **ordinary, nonplayable Ashbound** enemy types directly in the actual battle view. This is not another independent SourceArt study, lore change, combat kit, or speculative story. It leaves all playable models (including Riftwalker) to the other workstream.

The models targeted here are:
- **Seam Castigator**, appears in the opt-in Encounter Variety study's Missions **12 and 14** (World Five).
- **Anchor Wrecker**, appears in Mission **13**, alongside a Striker, Forager, and Camp Witness.

## Visible changes

### Seam Castigator (technical ID `ashbound-seam-castigator`)

- Avarin temple face, proper living paired crescent sensory mantle with filaments, and narrow field wraps, **sharing the same underlying natural anatomy** as the first-contact enemies.
- Tall but visibly **external** asymmetrical field hardware behind the right shoulder. Four offset reflector elements provide a recognizably different profile at mobile tactical distance.
- A right forearm capture cage with six delicate rings, a rear capture spool with winding, routed back leads, and a counterbalancing left field pack.
- Slightly shorter, alert movement cadence with right arm raised when an authoritative battle ability resolves. Gear visibly follows shoulder and forearm motion; the biologically living mantle is unaffected by any fictional new bond powers.

### Anchor Wrecker (technical ID `ashbound-anchor-wrecker`)

- Same real Avarin biology, but much heavier, low and wide salvage construction. Right/left shoulders differ deliberately.
- Five layered body-armour surfaces, paired reinforced leg bracing, visible brace rivets, broad blunt right contact pad and large inert containment cassettes. All equipment is external and removable, not a mutation or new creature species.
- Slower, weighted step cadence, restrained torso lean, short close-range equipment swing and a visible flinch on existing battle-hit events. No gameplay statistics are invented.

Both silhouettes should be readable as different **equipment roles**, not different character species. **Fracture Wake** remains engineered unstable Arcana capture with genuinely damaging **Afterecho**; the natural Avarin mantle perceives environmental Arcana, *not* bonds. No bonded creatures, trophies, firearms, souls, special leaders, bosses or Apex units are introduced.

## Runtime and performance implementation

- `WorldFiveAshboundMidRosterDetails.cs` creates actual mesh geometry via the same `WorldFiveBattleOrganics.Part` and `Loft` mesh construction methods used by live first-contact enemies.
- `WorldFiveBattleOrganics.cs` shares *one* head/face/mantle/limb anatomical detail source across the four types (avoiding incompatible anatomy), while the Seer and Witness remain the earlier provisional treatment.
- Both types are added to `GroupAshboundMotion`, which attaches armour, forearm equipment and leg braces to six independent torso/head/limb pivots and merges same-material geometry **within**, not across, those pivots. This reuses existing scene-level mesh ownership/disposal.
- The battlefield selects them only through the existing exact authorized six Ashbound technical unit IDs, retains already approved model prefab precedence, and caches the pivots. No new Collider, Rigidbody, MonoBehaviour, combat ability, status effect or animator registry is introduced. No world-space movement/health/defeat or save mechanics are changed.
- Movement and strike amplitudes differ: lighter/seeking **Castigator** versus slower heavily loaded **Wrecker**. Idle breathing, environmental attention, and reaction to *existing authoritative* impact events remain cosmetic.

**Limitations:** These procedural loft studies are *not* a production sculpt, true weighted skeletal mesh, authored UV/PBR texture set, IK locomotion, animation controller, or approved canon character turnaround. Their silhouettes, technical gear and colors are visual proposals. Proper high-quality assets must ultimately replace procedural fallbacks, and real mobile GPU cost is not yet measured.

## Native acceptance in Unity 6000.3.21f1

1. Open the existing opt-in World Five Encounter Variety content. Confirm a clean Unity Console. Play **Mission 12** and inspect the Seam Castigator, then **Mission 13** for the Anchor Wrecker; Mission 14 repeats the Castigator.
2. Contrast with Mission 11: Forager remains slight with probe/satchel and Striker remains right-arm-heavy; neither lost its facial anatomy or original movement. The Castigator must have a uniquely elevated field array and narrow arm lattice; Wrecker must have a different broad, low defensive silhouette with heavy bracing. Evaluate **actual Unity Game-view screenshots at the portrait camera**, not solely isolated review renders.
3. Move/attack and watch the head/mantle, limb cloth, chest plates and engineered equipment. Look for intersecting/reorienting parts and any visual disconnect between legs and feet.
4. Pause between turns; natural breath and head sensing should remain visible without overpowering the battlefield. Attacks should *not* cause the HUD or entity root to jump; real damage, action legality, ability effects and turn order remain unchanged.
5. Validate tap-once inspect, double-tap enemy attack, legal move-to-range, obstacles, rescue, Mission 4 secure zone, and mission objective readability in a smoke test. No new hitboxes, target markers or movement restrictions are expected.
6. Confirm an explicitly approved battle prefab still takes priority when registered; temporary procedural art should never replace it.
7. Compare the biped, temple-face and **natural living crescent mantle** with controlling *Master Lore & World-Building Archive v26.0*, locked World Five reference art and approved Ashbound canon addendum. **Reject** any anatomical deviation or implied new bond power, even if visually impressive.
8. Test 320×568 and 390×844 portrait Game views; profile on iPhone 14 (draw calls, triangles, GC spikes, render time, animation cost, textures, thermal load). No mobile performance metrics are certified by this PR.

## Automated checks

- Existing first-contact and general runtime organic-art validators continue to run.
- New `BuildTools/Art/validate_ashbound_mid_roster_runtime.py` checks that the two real runtime builders are present, their gear is unique, their components are attached to limbs, low-scope animated positions are keyed by the correct exact IDs, source geometry is batched, no gameplay physics is added, and the canon file remains unchanged.
- Windows/Ubuntu Core tests plus repository checks must pass. These tests **do not** compile or visually evaluate Unity-specific scripts.

**After this PR:** Refine the **Afterecho Seer and Camp Witness** in the actual battlefield, then prioritize replacing the entire six-enemy procedural fallback family with truly polished approved, textured, rigged, mobile-tested assets rather than accumulating endless provisional models.
