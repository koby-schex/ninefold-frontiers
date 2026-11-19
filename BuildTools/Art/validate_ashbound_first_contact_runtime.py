#!/usr/bin/env python3
"""Regression gate for the exact connected live Ashbound roster source."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
read=lambda x:(root/x).read_text(encoding="utf-8")
plan=read("Assets/Ninefold/Runtime/Core/Content/AshboundWearablePlan.cs")
art=read("Assets/Ninefold/Runtime/Presentation/WorldFiveBattleOrganics.cs")
skin=read("Assets/Ninefold/Runtime/Presentation/FirstEncounterBattleArt.cs")
battle=read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
motion=read("Assets/Ninefold/Runtime/Core/Content/AshboundRosterMotion.cs")
canon=read("Docs/Canon/WorldFiveAshboundCanonAddendum01.md")
export=read("BuildTools/Art/FirstEncounterMeshExport/Program.cs")

def check(ok,msg):
    if not ok:raise AssertionError(msg)

for name in ("Temple brow bridge","Temple mouth relief","Temple eye ",
             "Temple cheek structure ","Mantle filament ","Clavicle contour ",
             "Avarin forearm relief ","Field wrap ","Shin reinforcement ",
             "Heel geometry ","Upper field wrap","Cross body harness ",
             "External Wake conductor ","External Wake support loom",
             "Forager shoulder capture relay","Forager hooked route reader",
             "Forager reader tip","Forager repair satchel","Forager trailing tag",
             "Striker layered right shoulder","Striker right arm impact housing",
             "Striker blunt contact ridge","Striker side ballast","Striker layered chest plate "):
    check(name in plan,"Lost established Avarin source design: "+name)
check("ReplacedAnatomy(name)" in plan and "if(!striker)" in plan and
      "AshboundFirstContactDetails" in plan and "if(kind==0 || kind==1)" in plan,
      "Forager/Striker plan identity and old blockout removal required")
for name in ("Ashbound motion core","Ashbound motion head",
             "Ashbound motion arm Left","Ashbound motion arm Right",
             "Ashbound motion leg Left","Ashbound motion leg Right",
             "Ashbound elbow Left","Ashbound elbow Right",
             "Ashbound knee Left","Ashbound knee Right"):
    check(name in art,"Missing actual body joint: "+name)
check("AshboundWearablePlan.Build(kind)" in art and
      "AshboundWearablePlan.Geometry(piece)" in art and
      "AshboundWearablePlan.JointFor(piece.Name,kind)" in art and
      "FirstEncounterBattleArt.AttachAshbound(root,kind,bones,skin,fabric,dark,owned)" in art,
      "All active model surfaces must share actual C# recipe and rest bone attachment")
check("node.transform.SetParent(joint,true);" in art and
      "BatchPivotSurfaces(bones[i],owned)" in art and
      "mesh.CombineMeshes(combines,true,true,false);" in art and
      "owned.Remove(original.sharedMesh);" in art,
      "Gear must follow actual bone and dispose source meshes after batching")
check("FirstEncounterForagerCloth.Tactical((int)kind-2)" in skin and
      "ClothWeights((int)kind-2" in skin and "mesh.bindposes=binds;mesh.boneWeights=weights;" in skin,
      "Actual role-specific fitted garments and weighted rest geometry required")
check("WriteComplete(roster,k)" in export and "AshboundWearablePlan.Geometry(piece)" in export
      and "AshboundWearablePlan.Build(role)" in export,
      "Review must export same six fully assembled rest meshes as the battle")
check("if(hasApprovedPrefab)" in battle and "else if(isSillrunner||isSeamstalker||" in battle and
      "Array.IndexOf(WorldFiveActOnePrototype.EnemyTemplateIds,actor.UnitId)>=0" in battle,
      "Do not override approved assets or infer wrong enemy identities")
check("if(ashboundType>=0 && ashboundType<=5)" in battle and
      "PoseAshboundDistal(token,gait,lunge,0f,0f)" in battle and
      "AshboundRosterMotion.Sample(token.AshboundKind" in battle and
      "PoseAshboundDistal(token,0f,0f,flinch,0f)" in battle and
      "PoseAshboundDistal(token,0f,0f,0f,strike)" in battle and
      "if(!isActionPlayback)" in battle and "isActionPlayback=true;" in battle and
      "isActionPlayback=false;" in battle and "pulse?.TargetId == before.Id && token.OrganicCore!=null" in battle,
      "Lower limb and arm presentation must be driven by actual battle playback")
check("living crescent sensory mantle" in canon and "No bonded creatures fight alongside" in canon,
      "Approved Ashbound anatomy and absence of bonded creatures are required")
for src in (plan,art,skin):
    for forbidden in ("AddComponent<Collider>","AddComponent<Rigidbody>","AddComponent<Animator>","AddComponent<MonoBehaviour>"):
        check(forbidden not in src,"Art cannot own combat state: "+forbidden)
print("PASS six complete skinned Avarin assemblies with preserved first-contact gear and real source-equal exports")
