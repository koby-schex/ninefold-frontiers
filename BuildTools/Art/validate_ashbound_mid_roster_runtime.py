#!/usr/bin/env python3
"""Preserve the two differentiated mid-roster wearable designs when skinned."""
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
    "Castigator field-shawl","Castigator high right shoulder brace",
    "Castigator right reflector ","Castigator right forearm cage","Castigator right cage-rib ",
    "Castigator rear capture spool","Castigator spool external winding ",
    "Castigator left balancing pack","Castigator routed back lead ",
    "Wrecker left load shoulder","Wrecker right counter shoulder","Wrecker overlapping body lamina ",
    "Wrecker leg load-brace ","Wrecker leg brace-rivet ","Wrecker right contact brace",
    "Wrecker right anchor contact-pad","Wrecker right pad-reinforcement ",
    "Wrecker left ballast cassette","Wrecker rear containment cassette"):
    demand(phrase in plan,"Missing existing mid-roster design: "+phrase)
demand("if(kind==2 || kind==3)" in plan and "if(kind==2)Castigator" in plan and "else Wrecker" in plan,
      "Keep real exact archetype selection")
demand("Starts(n,\"Castigator right forearm \")" in plan and
      "Starts(n,\"Wrecker leg load-brace \")" in plan and
      "Starts(n,\"Wrecker right anchor \")" in plan and
      "AshboundWearablePlan.JointFor(piece.Name,kind)" in art,
      "Equipment must follow lower arm/shin")
demand("if(ashboundType>=0 && ashboundType<=5)" in battle and
      "float cadence=wrecker?13f:castigator?16f:19f;" in battle and
      "token.OrganicArmRight.localRotation" in battle and "token.OrganicLegLeft.localRotation" in battle,
      "Do not regress distinctive live motion")
demand("if(hasApprovedPrefab)" in battle and "living crescent sensory mantle" in canon and
      "No bonded creatures fight alongside" in canon and "Fracture Wake" in canon,
      "Canon and mapped approved prefabs retain precedence")
print("PASS exact mid-roster wearable silhouettes, articulated skin and preserved equipment mounts")
