#!/usr/bin/env python3
"""Preserve the two late Ashbound wearable identities in the full roster."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
read=lambda p:(root/p).read_text(encoding="utf-8")
plan=read("Assets/Ninefold/Runtime/Core/Content/AshboundWearablePlan.cs")
art=read("Assets/Ninefold/Runtime/Presentation/WorldFiveBattleOrganics.cs")
battle=read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
canon=read("Docs/Canon/WorldFiveAshboundCanonAddendum01.md")
def demand(ok,msg):
    if not ok:raise AssertionError(msg)
for phrase in (
    "Seer damaged left shoulder shroud","Seer right external capture hinge",
    "Seer external signal petal ","Seer petal exposed conduit ",
    "Seer offset Wake capture cassette","Seer cassette instability readout ",
    "Seer trailing damaged garment","Seer rear Wake stabilizer reel","Seer rear cassette wiring ",
    "Witness layered field overcoat","Witness reinforced left shoulder wrap","Witness lighter right shoulder wrap",
    "Witness repair strap ","Witness right camp registry satchel","Witness registry reader face",
    "Witness registry marking ","Witness left arm signal support","Witness left signal slat ",
    "Witness left signal hinge ","Witness rear camp relay cassette","Witness field relay channel ",
    "Witness field utility pouch "):
    demand(phrase in plan,"Missing existing late-roster identity: "+phrase)
demand("if(kind==4 || kind==5)" in plan and "if(kind==4)Seer" in plan and "else Witness" in plan,
      "Keep exact late-role dispatch")
demand("Starts(n,\"Seer left forearm \")" in plan and "Starts(n,\"Seer right brace channel \")" in plan and
      "Starts(n,\"Witness left arm \")" in plan and "Starts(n,\"Witness left signal \")" in plan and
      "AshboundWearablePlan.JointFor(piece.Name,kind)" in art,
      "Wearable hardware must follow actual rig joint")
demand("if(seer) cadence=17f;" in battle and "if(witness) cadence=12f;" in battle and
       "float uneasy=pair.Value.AshboundKind==4" in battle and
       "float surveying=pair.Value.AshboundKind==5" in battle and
       "pulse?.TargetId == before.Id && token.OrganicCore!=null" in battle,
       "Existing sensory strain and late role motion cannot change gameplay")
for name in ("Rift Forager","Parted Striker","Seam Castigator","Anchor Wrecker","Afterecho Seer","Camp Witness"):
    demand(name in canon,"Unapproved new roster")
demand("living crescent sensory mantle" in canon and "No bonded creatures fight alongside" in canon and
       "Fracture Wake" in canon and "Afterecho" in canon,
       "Preserve canon constraints")
print("PASS late-role natural anatomy, external instrumentation, actual gear mapping and preserved motion")
