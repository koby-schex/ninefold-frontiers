#!/usr/bin/env python3
"""Review-source safety gate for the actual in-battle World Five art fallback.

This checks source intent and non-regression. It is NOT a Unity compilation,
visual comparison with locked masters, performance benchmark or lore approval.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
read = lambda name: (root / name).read_text(encoding="utf-8")
art = read("Assets/Ninefold/Runtime/Presentation/WorldFiveBattleOrganics.cs")
battle = read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
scenery = read("Assets/Ninefold/Runtime/Presentation/BattlefieldScenery.cs")
creatures = read("Assets/Ninefold/Runtime/Core/Content/WorldFiveTutorialWildlifeV1.cs")
enemy = read("Assets/Ninefold/Runtime/Core/Content/WorldFiveActOnePrototype.cs")

def check(ok, message):
    if not ok:
        raise AssertionError(message)

check("Mesh Loft(" in art and "mesh.SetVertices(vertices);" in art and
      "mesh.SetTriangles(triangles,0);" in art and "mesh.RecalculateNormals();" in art,
      "Procedural art must build actual multi-ring triangulated 3D surfaces")
check("internal static void Wildlife" in art and "internal static void Ashbound" in art,
      "Separate wildlife / hostile-Avarin art builders are required")
for name in ["Dorsal shell", "Head", "Walking limb", "Sensory ridge",
             "Tapered tail", "Balance fan", "Lateral eye",
             "Avarin torso", "Avarin temple face", "Living crescent ",
             "Avarin arm ", "Avarin leg ", "Fracture Wake equipment"]:
    check(name in art, "Missing named organic anatomy or safe hardware: " + name)
check("foot.transform.SetParent(walkingLeg.transform,true)" in art and
      "eye.transform.SetParent(head.transform,true)" in art,
      "Wildlife claws and eyes must follow actual moving anatomy")
check("new Mesh" in art and "new GameObject(name)" in art and
      "new GameObject(h.UnitId" in battle,
      "No fallback may be only a decorative cube on top of a game token")
check("TryCreateApprovedVisual(h.UnitId,out var modelBody)" in battle and
      "if(hasApprovedPrefab)" in battle and
      "else if(isSillrunner||isSeamstalker||" in battle and
      "isAshbound)" in battle,
      "Approved prefabs must retain precedence over every provisional mesh")
check("Array.IndexOf(WorldFiveActOnePrototype.EnemyTemplateIds,actor.UnitId)>=0" in battle,
      "Only exact authored Ashbound IDs may use the Avarin visual study")
check("actor.UnitId == WorldFiveTutorialWildlifeV1.SillrunnerId" in battle and
      "actor.UnitId == WorldFiveTutorialWildlifeV1.SeamstalkerId" in battle,
      "Specific wildlife visuals must never leak into anonymous unit IDs")
check("ConfigureWildlifeActors" in battle and
      "ashboundActors.Clear()" in battle and "organicMeshes.Clear()" in battle,
      "Battle scene transitions must clear old art / instance-specific meshes")
check("PoseWildlife" in battle and "PoseAshbound" in battle and
      "if(!pair.Value.Organic || !pair.Value.Body.activeSelf) continue;" in battle,
      "Motion and live idle animation must actually execute inside the battlefield")
check("CreateSurfaceTexture" in art and
      "organicTextures.Clear()" in battle and
      "new Texture2D(size,size,TextureFormat.RGBA32,true)" in art,
      "Texture variation must have deterministic allocation and disposal")
check("private static void BuildCover(Batch faces,Batch caps" in scenery and
      "var outline=new[]" in scenery and "new Vector3[8]" in scenery and
      "caps.Face" in scenery,
      "Obstacles should be layered/chamfered inside existing authored bounds")
check("public void Animate(float time)" in scenery and
      "scenery.Animate(Time.unscaledTime);" in battle,
      "Subtle in-game environmental response must be wired to a real update path")
check("new FieldObstacle" in creatures or "ObjectiveDefinition" in creatures,
      "Native wildlife authored Core content should be present/untouched by art change")
check("public static readonly string[] EnemyTemplateIds" in enemy,
      "Approved ordinary Ashbound roster IDs are required")
check("AddComponent<Collider>" not in art and
      "AddComponent<MonoBehaviour>" not in art and
      "AddComponent<Rigidbody>" not in art,
      "Procedural art must not create physical gameplay or behavior components")
print("PASS actual mesh-based World Five wildlife & Ashbound visual fallbacks")
print("PASS identity gating, approved-prefab precedence, organic motion and scene disposal")
print("PASS noninteractive layered cover, material grain and scene response")
print("NOTE requires Unity native compile, visual master comparison and iPhone profiling")
