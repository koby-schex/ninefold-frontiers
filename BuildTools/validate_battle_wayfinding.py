#!/usr/bin/env python3
"""Regression gate: visible/correct mission capture zones and player instructions.

This is a *structural* source check, not a Unity visual/device compile.
Core mission 4/8 playthrough tests separately validate actual secure mechanics.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def source(path):
    return (ROOT / path).read_text(encoding="utf-8")

def require(yes, what):
    if not yes:
        raise AssertionError(what)

core = source("Assets/Ninefold/Runtime/Core/Content/WorldFivePlayableSlice.cs")
battle = source("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
ui = source("Assets/Ninefold/Runtime/Presentation/FrontiersPlaytest.cs")
guide = source("Assets/Ninefold/Runtime/Presentation/BattleObjectiveGuidance.cs")
style = source("Assets/Ninefold/Runtime/Presentation/Playtest.uss")
radar_style = source("Assets/Ninefold/Resources/Home/BattleObjectiveGuidance01.uss")

# The real core mission remains a 2-round consecutive Secure zone; no
# inappropriate "press to interact" action is introduced.
require('ObjectiveDefinition.Secure("primary",B(2,2,5,6,3)' in core,
        "Mission 4 Secure area changed without updating acceptance tests")
require('new[] { "$squad" },new[] { "w5-zone-test-threat" },2,true' in core,
        "Mission 4 rounds/contest rule was unexpectedly changed")
require("o.Kind==ObjectiveKind.Secure || o.Kind==ObjectiveKind.RescueAndExtract" in battle,
        "On-field renderer must outline secure and extraction areas")
require('OutlineZone(o.Zone,' in battle and
        'Shape(title+" · visible corner beacon"' in battle,
        "Area boundary and corner beacons missing")
require('objectiveLabels.Add(o.Id,label)' in battle and
        '"HOLD " + state.Progress + "/" + state.Required' in battle,
        "Active zone needs a visible hold status label")
require("o.Kind==ObjectiveKind.Protect" in battle and
        '"GUARD " + state.Progress + "/" + state.Required' in battle,
        "Protect actor requires followable status feedback")
require("BattleObjectiveGuidance.NextStep(controls,catalog.Missions[b.MissionId],b)" in ui,
        "Player should not need to open an opaque help page to understand the task")
require('objective.Kind == ObjectiveKind.Secure' in ui and
        '"map-secure-zone"' in ui and
        '"map-extraction-zone"' in ui,
        "Prebattle map must show both capture and extraction footprints")
require(".map-secure-zone" in style and ".map-extraction-zone" in style,
        "Capture/extraction areas must be distinguishable in briefing map")
require('case ObjectiveKind.Secure:' in guide and
        'consecutive rounds.' in guide and
        "KEEP HOLDING:" in guide,
        "On-screen secure instructions must explain entry, contest and time")
require('Area(authored.Zone,"btg-goal-zone")' in guide and
        "GOLD = HOLD ZONE" in guide,
        "Radar must display the whole secure footprint, not just an unexplained dot")
require(".btg-next-step" in radar_style and ".btg-goal-zone" in radar_style,
        "Instructions and highlighted radar area require styles")
require("PickingMode.Ignore" in guide and
        "battleRadar.worldBound.Contains(e.position)" in ui,
        "Read-only radar must remain excluded from movement taps")
print("PASS Mission 4 / Mission 8: capture zone is visually represented, legible and safe")
print("PASS Rescue extraction: visible physical destination in field and briefing")
print("PASS Mission-specific actionable guidance and read-only radar")
print("NOTE Unity 6000.3 native compile, actual Game view and device playtest still required.")
