"""Scope, save integration, preview honesty and shared surface guards."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
s=lambda p:(root/p).read_text(encoding="utf-8")
core="Assets/Ninefold/Runtime/Core/Content/"
pres="Assets/Ninefold/Runtime/Presentation/"
variation=s(core+"BattlefieldVariation.cs")
catalog=s(core+"ContentCatalog.cs")
plan=s(core+"BattlefieldArtPlan.cs")
defs=s(core+"ContentDefinitions.cs")
fixture=s(core+"PlaytestContentPackage.cs")
battle=s(pres+"PlaytestBattlefield.cs")
scenery=s(pres+"BattlefieldScenery.cs")
ui=s(pres+"FrontiersPlaytest.cs")
radar=s(pres+"BattleObjectiveGuidance.cs")
save=s("Assets/Ninefold/Runtime/Core/Combat/BattlefieldSave.cs")
export=s("BuildTools/Art/BattlefieldTerrainExport/Program.cs")
assert "proceduralCover:expandedLayouts && m.Id==\"fixture-mixed\"" in fixture
assert "Guid.TryParseExact(attempt,\"N\",out _)" in variation
assert "BattlefieldVariation.IsLaterOptional(id)" in defs
assert "definition.Objectives.Any(o=>o.Kind!=ObjectiveKind.Defeat)" in defs
assert "BattlefieldVariation.Resolve(m,attempt,eligible)" in catalog
assert "map.Ground.Any" in variation and "Separated(o.Bounds,box)" in variation
assert "Map.Obstacles.Count" in save and "Map.Ground.Count" in save
assert "BuildMap(view.Map,objectives,view.MissionId,environmentKey,view.AttemptId,proceduralCover)" in battle
assert "BattlefieldArtPlan.Build(map,missionId,environmentKey,attempt,proceduralCover)" in battle
assert "scenery.Build(terrain.transform,scene)" in battle
assert 'Shape("Bounded floor"' not in battle and 'Shape("Arena plinth"' not in battle
assert "battle.Map.Obstacles" in radar and "previewObstacles" in ui
assert "new Plane(Vector3.up, Vector3.zero).Raycast" in battle
assert "mesh.UploadMeshData(true)" in scenery and "MeshCount=>meshes.Count" in scenery
assert "TileNoise" in scenery and "CreateMineralMap(true)" in scenery
assert "BattlefieldArtPlan.Build(map,mission.Id,mission.EnvironmentKey,attempt,mission.ProceduralCover)" in export
for mood in ("PaleSurvey","VioletShelves","MineralBasin","BrokenTerrace","DeepFracture","Neutral"):
    assert mood in plan
for bad in ("AddComponent<Collider>","AddComponent<Rigidbody>","AddComponent<Light>","AddComponent<MonoBehaviour>"):
    assert bad not in scenery
print("PASS authored mission protection, saved exact terrain, explicitly varied Quick Battles, real mesh source, mobile batching and correct map preview.")
