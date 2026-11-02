#!/usr/bin/env python3
"""Verify last two Ashbound enemy archetypes are real live visual candidates.

Structural source contract only: Unity 6000.3 native compilation, visual
canon comparison, rig deformation and mobile benchmarks are separate gates.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[2]
def src(path):
    return (root/path).read_text(encoding="utf-8")
art=src("Assets/Ninefold/Runtime/Presentation/WorldFiveBattleOrganics.cs")
details=src("Assets/Ninefold/Runtime/Presentation/WorldFiveAshboundFinalPairDetails.cs")
battle=src("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
canon=src("Docs/Canon/WorldFiveAshboundCanonAddendum01.md")
first=src("BuildTools/Art/validate_ashbound_first_contact_runtime.py")
middle=src("BuildTools/Art/validate_ashbound_mid_roster_runtime.py")

def check(value,message):
    if not value:
        raise AssertionError(message)

check("if(kind==4 || kind==5)" in art and
      "WorldFiveAshboundFinalPairDetails.Build(root,kind,skin,mantle," in art and
      "GroupAshboundMotion(root,owned);" in art,
      "Seer and Witness must use authored real battlefield meshes and pivots")
check("if(kind==2 || kind==3)" in art and
      "WorldFiveAshboundMidRosterDetails.Build" in art and
      "if(kind==0 || kind==1)" in art and
      "AshboundFirstContactDetails" in art,
      "Previous four enemy model refinements must remain present")
check("AvarinDetailedAnatomy(root,false,skin,mantle,cloth," in details and
      "internal static void AvarinDetailedAnatomy" in art and
      "AvarinDetailedAnatomy(root,wrecker,skin,mantle,fabric,gear," in
       src("Assets/Ninefold/Runtime/Presentation/WorldFiveAshboundMidRosterDetails.cs"),
      "All six use the same natural detailed Avarin anatomy, not mixed species")
check("if(kind!=4 && kind!=5)" in details and
      "if(kind==4) Seer(" in details and "else Witness(" in details,
      "The final pair must have separate explicit artwork by exact template index")
for phrase in (
    "Seer damaged left shoulder shroud",
    "Seer right external capture hinge",
    "Seer external signal petal ",
    "Seer petal exposed conduit ",
    "Seer offset Wake capture cassette",
    "Seer cassette instability readout ",
    "Seer left forearm brace",
    "Seer right forearm brace",
    "Seer trailing damaged garment",
    "Seer rear Wake stabilizer reel",
    "Seer rear cassette wiring ",
):
    check(phrase in details or
          (phrase.startswith("Seer left forearm") and '"Seer "+half+" forearm brace"' in details) or
          (phrase.startswith("Seer right forearm") and '"Seer "+half+" forearm brace"' in details),
          "Seer's natural face and unstable external sensing design incomplete: "+phrase)
for phrase in (
    "Witness layered field overcoat",
    "Witness reinforced left shoulder wrap",
    "Witness lighter right shoulder wrap",
    "Witness repair strap ",
    "Witness right camp registry satchel",
    "Witness registry reader face",
    "Witness registry marking ",
    "Witness left arm signal support",
    "Witness left signal slat ",
    "Witness left signal hinge ",
    "Witness rear camp relay cassette",
    "Witness field relay channel ",
    "Witness field utility pouch ",
):
    check(phrase in details,
          "Witness logistical/support silhouette incomplete: "+phrase)
check("Part(root,owned" in details and
      "using static Ninefold.Presentation.WorldFiveBattleOrganics;" in details and
      "internal static GameObject Part(" in art,
      "Actual 3D lofted meshes must be in the live renderer, not documentation")
for expr in (
    'n.StartsWith("Seer left forearm ",StringComparison.Ordinal)',
    'n.StartsWith("Seer right brace channel ",StringComparison.Ordinal)',
    'n.StartsWith("Witness left arm ",StringComparison.Ordinal)',
    'n.StartsWith("Witness left signal ",StringComparison.Ordinal)',
    'n.StartsWith("Witness lighter right shoulder ",StringComparison.Ordinal)'
):
    check(expr in art, "Seer/Witness gear is not attached to an animated limb: "+expr)
check("part.SetParent(target,true);" in art and
      "BatchPivotSurfaces(pivot,owned)" in art and
      "mesh.CombineMeshes(combines,true,true,false);" in art and
      "owned.Remove(original.sharedMesh);" in art,
      "6 model anatomy pivots and per-material batching must be preserved")
check("if(ashboundType>=0 && ashboundType<=5)" in battle and
      "t.AshboundKind=ashboundType;" in battle and
      "bool seer=token.AshboundKind==4;" in battle and
      "bool witness=token.AshboundKind==5;" in battle and
      "if(seer) cadence=17f;" in battle and
      "if(witness) cadence=12f;" in battle,
      "Real battle renderer needs correct six archetypes and distinct pacing")
check("float uneasy=pair.Value.AshboundKind==4" in battle and
      "float surveying=pair.Value.AshboundKind==5" in battle and
      "if(!isActionPlayback)" in battle and
      "pulse?.TargetId == before.Id && token.OrganicCore!=null" in battle,
      "Anatomical idle cues cannot override an authoritative hit/attack")
check("TryCreateApprovedVisual(h.UnitId,out var modelBody)" in battle and
      "if(hasApprovedPrefab)" in battle and
      "else if(isSillrunner||isSeamstalker||" in battle and
      "Array.IndexOf(WorldFiveActOnePrototype.EnemyTemplateIds,actor.UnitId)>=0" in battle,
      "Approved prefabs and exact enemy identity must remain authoritative")
for validator in (first,middle):
    check("if(ashboundType>=0 && ashboundType<=5)" in validator,
          "Earlier art gates must cover all six without losing old model checks")
for name in ("Rift Forager","Parted Striker","Seam Castigator",
             "Anchor Wrecker","Afterecho Seer","Camp Witness"):
    check(name in canon,"Only six ordinary Ashbound classes are authorized: "+name)
check("living crescent sensory mantle" in canon and
      "No bonded creatures fight alongside" in canon and
      "Fracture Wake" in canon and "Afterecho" in canon,
      "Natural anatomy, engineered equipment and genuine strain remain controlling")
for forbidden in ("AddComponent<Rigidbody>", "AddComponent<Collider>",
                  "AddComponent<Animator>", "AddComponent<MonoBehaviour>"):
    check(forbidden not in details,
          "Enemy geometry cannot sneak in physics, hitboxes or AI: "+forbidden)

print("PASS all six real in-battle Ashbound archetypes have detailed distinct runtime meshes")
print("PASS Seer instability hardware and Witness logistical support with shared Avarin anatomy")
print("PASS live articulated gear, no authoritative gameplay or approved-prefab overrides")
print("NOTE final Unity compile, locked canon visual comparison and iPhone profiling remain open")
