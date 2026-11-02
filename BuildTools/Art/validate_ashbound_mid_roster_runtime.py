#!/usr/bin/env python3
"""Run-level source contract for the mid-roster live Ashbound visual work.

Source-only. Not a Unity compile, canonical visual acceptance, authored
skeletal animation, performance test, or mobile GPU certification.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[2]
def read(path):
    return (root/path).read_text(encoding="utf-8")
art=read("Assets/Ninefold/Runtime/Presentation/WorldFiveBattleOrganics.cs")
details=read("Assets/Ninefold/Runtime/Presentation/WorldFiveAshboundMidRosterDetails.cs")
battle=read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
canon=read("Docs/Canon/WorldFiveAshboundCanonAddendum01.md")
original=read("BuildTools/Art/validate_ashbound_first_contact_runtime.py")

def check(value, message):
    if not value:
        raise AssertionError(message)

check("if(kind==2 || kind==3)" in art and
      "WorldFiveAshboundMidRosterDetails.Build(root,kind,skin,mantle," in art and
      "GroupAshboundMotion(root,owned);" in art,
      "Both named mid-roster enemies need real battlefield geometry and motion")
check("if(kind==0 || kind==1)" in art and
      "AshboundFirstContactDetails(root,kind,skin,mantle," in art,
      "The Rift Forager/Parted Striker quality pass must not be replaced")
check("internal static void AvarinDetailedAnatomy" in art and
      "AvarinDetailedAnatomy(root,wrecker,skin,mantle,fabric,gear," in details and
      "AvarinDetailedAnatomy(root,striker,skin,mantle,fabric," in art,
      "The second pair should share actual natural Avarin face/mantle structure")
check("if(kind!=2 && kind!=3)" in details and
      "if(wrecker) Wrecker" in details and "else Castigator" in details,
      "Visual specialization must be limited to the two intended NPC types")
for phrase in (
    "Castigator high right shoulder brace",
    "Castigator right reflector ",
    "Castigator right forearm cage",
    "Castigator right cage-rib ",
    "Castigator rear capture spool",
    "Castigator spool external winding ",
    "Castigator left balancing pack",
    "Castigator routed back lead ",
):
    check(phrase in details, "Ranged Castigator silhouette incomplete: "+phrase)
for phrase in (
    "Wrecker left load shoulder",
    "Wrecker right counter shoulder",
    "Wrecker overlapping body lamina ",
    "Wrecker leg load-brace ",
    "Wrecker leg brace-rivet ",
    "Wrecker right contact brace",
    "Wrecker right anchor contact-pad",
    "Wrecker right pad-reinforcement ",
    "Wrecker left ballast cassette",
    "Wrecker rear containment cassette",
):
    check(phrase in details, "Heavy Wrecker silhouette incomplete: "+phrase)
check("Part(root,owned" in details and
      "using static Ninefold.Presentation.WorldFiveBattleOrganics;" in details and
      "internal static GameObject Part(" in art,
      "Role-specific art must create actual owned triangular source meshes")
check('n.StartsWith("Castigator right ",StringComparison.Ordinal)' in art and
      'n.StartsWith("Wrecker right ",StringComparison.Ordinal)' in art and
      'n.StartsWith("Wrecker leg load-brace ",StringComparison.Ordinal)' in art and
      'n.StartsWith("Wrecker leg brace-rivet ",StringComparison.Ordinal)' in art,
      "Right arm and braced legs must carry all attached equipment during movement")
check("part.SetParent(target,true);" in art and
      "BatchPivotSurfaces(pivot,owned)" in art and
      "mesh.CombineMeshes(combines,true,true,false);" in art,
      "Actual body part motion and per-limb material batching are mandatory")
check("if(ashboundType>=0 && ashboundType<=5)" in battle and
      "t.AshboundKind=ashboundType;" in battle and
      "float cadence=wrecker?13f:castigator?16f:19f;" in battle and
      "token.OrganicArmRight.localRotation" in battle and
      "token.OrganicLegLeft.localRotation" in battle,
      "New enemy battle pacing must be distinct and tied to correct archetypes")
check("TryCreateApprovedVisual(h.UnitId,out var modelBody)" in battle and
      "if(hasApprovedPrefab)" in battle and
      "else if(isSillrunner||isSeamstalker||" in battle,
      "An approved model must always override provisional runtime details")
check("Array.IndexOf(WorldFiveActOnePrototype.EnemyTemplateIds,actor.UnitId)>=0" in battle and
      "if(ashboundType>=0 && ashboundType<=5)" in battle,
      "Only exact approved Ashbound NPC identity may get detailed mesh candidates")
for forbidden in ("AddComponent<Rigidbody>", "AddComponent<Collider>",
                  "AddComponent<Animator>", "AddComponent<MonoBehaviour>"):
    check(forbidden not in details, "Visual geometry is not a gameplay implementation: "+forbidden)
check("living crescent sensory mantle" in canon and
      "NOT a playable faction" in canon and
      "No bonded creatures fight alongside" in canon and
      "Fracture Wake" in canon and
      "Afterecho" in canon,
      "Species and Ashbound status must still be controlled by the approved addendum")
check("if(!striker)" in art and "Forager hooked route reader" in art and
      "Striker blunt contact ridge" in art and
      "AshboundFirstContactDetails" in original,
      "The earlier Forager and Striker detail pass must remain distinct")

print("PASS four distinct in-battle Ashbound geometries: Forager / Striker / Castigator / Wrecker")
print("PASS shared Avarin face/mantle, distinct external gear, attached motion and renderer batching")
print("PASS no Core rules, new physics, companion unit, or approved-model override")
print("NOTE: Unity 6000.3.21f1 compile, Game-view comparison and mobile profiling still required")
