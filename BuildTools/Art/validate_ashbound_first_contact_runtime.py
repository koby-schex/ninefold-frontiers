#!/usr/bin/env python3
"""Regression assertions for *live battle* first-contact Ashbound visual study.

Structural only: does not replace Unity compilation, native GPU profiling,
3D view inspection, canon master comparison or skinning/animation review.
"""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
read = lambda x: (root / x).read_text(encoding="utf-8")
art = read("Assets/Ninefold/Runtime/Presentation/WorldFiveBattleOrganics.cs")
battle = read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
canon = read("Docs/Canon/WorldFiveAshboundCanonAddendum01.md")

def required(condition, message):
    if not condition:
        raise AssertionError(message)

required("if(kind==0 || kind==1)" in art and
         "AshboundFirstContactDetails(root,kind,skin,mantle," in art,
         "Only Forager and Striker must receive the initial detailed sculpt pass")
required("GroupAshboundMotion(root,owned);" in art and
         "private static void GroupAshboundMotion(GameObject root,List<Mesh> owned)" in art,
         "The first pair must have explicit articulated child hierarchy")
for anatomy in [
    "Temple brow bridge", "Temple mouth relief", "Temple eye ",
    "Temple cheek structure ", "Mantle filament ", "Clavicle contour ",
    "Avarin forearm relief ", "Field wrap ", "Shin reinforcement ",
    "Heel geometry ", "Upper field wrap", "Cross body harness ",
    "External Wake conductor ", "External Wake support loom"
]:
    required(anatomy in art, "Missing first-contact surface detailing: "+anatomy)
for forager in [
    "Forager shoulder capture relay", "Forager hooked route reader",
    "Forager reader tip", "Forager repair satchel", "Forager trailing tag"
]:
    required(forager in art, "Forager's lighter asymmetric silhouette missing "+forager)
for striker in [
    "Striker layered right shoulder", "Striker right arm impact housing",
    "Striker blunt contact ridge", "Striker side ballast",
    "Striker layered chest plate "
]:
    required(striker in art, "Striker's weighted close-pressure silhouette missing "+striker)
for pivot in [
    "Ashbound motion core", "Ashbound motion head",
    "Ashbound motion arm Left", "Ashbound motion arm Right",
    "Ashbound motion leg Left", "Ashbound motion leg Right"
]:
    required(pivot in art, "Avarin articulated pivot missing: "+pivot)
required("part.SetParent(target,true);" in art and
         "else if(n.StartsWith(\"Avarin temple face\"" in art and
         "else if(n.StartsWith(\"Avarin arm \"" in art and
         "if(n.StartsWith(\"Avarin leg \"" in art,
         "The face, moving equipment and feet must attach to their real limbs")
required("BatchPivotSurfaces(pivot,owned)" in art and
         "mesh.CombineMeshes(combines,true,true,false);" in art and
         "owned.Remove(original.sharedMesh);" in art,
         "Geometric detail must consolidate same-material surfaces per articulated section")
required("GetComponent<Collider>" not in art and
         "AddComponent<Collider>" not in art and
         "AddComponent<Rigidbody>" not in art and
         "AddComponent<Animator>" not in art,
         "A presentation-only study must not create new physics, hit testing or combat control")
required("if(hasApprovedPrefab)" in battle and
         "else if(isSillrunner||isSeamstalker||" in battle and
         "isAshbound)" in battle and
         "Array.IndexOf(WorldFiveActOnePrototype.EnemyTemplateIds,actor.UnitId)>=0" in battle,
         "Visual candidates must not override accepted models or misidentify tutorial enemies")
required("if(ashboundType==0 || ashboundType==1)" in battle and
         'Find("Ashbound motion core")' in battle and
         'Find("Ashbound motion head")' in battle,
         "Battle render must cache articulated first-contact actor pivots")
required("if(token.OrganicCore!=null)" in battle and
         "token.OrganicLegLeft.localRotation" in battle and
         "token.OrganicLegRight.localRotation" in battle and
         "token.OrganicHead.localRotation" in battle,
         "Actual battle movement must animate the new anatomy")
required("if(!isActionPlayback)" in battle and
         "isActionPlayback=true;" in battle and
         "isActionPlayback=false;" in battle,
         "Idle should not overwrite action playback or leave a stuck state")
required("pulse?.TargetId == before.Id && token.OrganicCore!=null" in battle and
         "Mathf.Sin(strike*Mathf.PI)" in battle,
         "A Core-authorized impact needs a bounded visual flinch")
required("GroupAshboundMotion" not in canon and
         "The Ashbound" in canon and
         "living crescent sensory mantle" in canon and
         "No bonded creatures fight alongside" in canon,
         "Canon control file must remain the source of species constraints")
required(re.search(r"if\(!striker\)[\s\S]*?Forager hooked route reader",art) and
         re.search(r"else\s*\{[\s\S]*?Striker layered right shoulder",art),
         "Rift Forager and Parted Striker must not share the same gear topology")

print("PASS first-contact runtime detail: eyes, facial structure, living mantle and attached moving gear")
print("PASS differentiated Forager/Striker silhouettes with bounded, renderer-only motion")
print("PASS approved-prefab priority, no physics/gameplay, per-pivot material batch disposal")
print("NOTE native Unity compile and locked visual canon review are still required.")
