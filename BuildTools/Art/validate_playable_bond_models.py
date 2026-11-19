#!/usr/bin/env python3
"""Check real source bytes, source mesh pipeline and protected live registration.

Structural only; native Unity compile, art approval and phone profiling remain required.
"""
from pathlib import Path
import hashlib
root=Path(__file__).resolve().parents[2]
read=lambda p:(root/p).read_text(encoding="utf-8")
models=("GreatStrider","LivingVeil","Riftwalker","Burdenbeast")
packed=root/"Assets/Ninefold/Resources/WorldFiveBondModels"
count=0
for name in models:
    source=root/"SourceArt"/name
    files=list(source.glob("*.obj"))+list(source.glob("*.mtl"))+list((source/"Textures").glob("*.png"))
    for path in files:
        dest=packed/name/(str(path.relative_to(source))+".bytes")
        assert dest.is_file() and dest.with_name(dest.name+".meta").is_file(),dest
        assert hashlib.sha256(path.read_bytes()).digest()==hashlib.sha256(dest.read_bytes()).digest(),dest
        count+=1
rig=read("Assets/Ninefold/Runtime/Core/Content/PlayableBondRig.cs")
parser=read("Assets/Ninefold/Runtime/Core/Content/PlayableBondSource.cs")
art=read("Assets/Ninefold/Runtime/Presentation/PlayableBondBattleArt.cs")
battle=read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
ui=read("Assets/Ninefold/Runtime/Presentation/FrontiersPlaytest.cs")
exporter=read("BuildTools/Art/PlayableBondMeshExport/Program.cs")
tests=read("Tests/Ninefold.Core.Tests/PlayableBondVisualTests.cs")
assert count>=70
assert "WorldFivePlayableAbilityStudy.StandardIds" in rig and "if(!squad" in rig and "WorldFiveStorySequence.IsTutorial" in rig
assert battle.index("if(hasApprovedPrefab)")<battle.index("else if(PlayableBondRig.TrySelect")
assert "h.IsPlayerSquad" in battle and "nextBondScope==playableBondScope" in battle
assert "playableBonds.TryBuild" in battle and "token.BondVisual.Pose(" in battle and "playableBonds.Dispose();" in battle
assert "S.ReadMesh" in art and "R.Skin(plan" in art and "mesh.bindposes=bindposes;mesh.boneWeights=weights;" in art
assert "S.ValidateCorrespondence(model,protective)" in art and "AddBlendShapeFrame" in art
assert "SetBlendShapeWeight" in art and "R.ProtectiveVertex" in art and "Resources.UnloadAsset" in art
assert "return false;" in art and "preserving the playable fixture fallback" in art
assert "SourceIndices" in parser and "a.SourceIndices.SequenceEqual(b.SourceIndices)" in parser
assert "R.Skin(plan" in exporter and "R.Pose(plan" in exporter and "R.ProtectiveVertex" in exporter
assert "PlayableStarterMapping" in tests and "PlayableRigCoverage" in tests and "PlayableShelterMorph" in tests
assert "worldFiveSlice && PlayableBondRig.TryName" in ui
for forbidden in ("AddComponent<Collider>","AddComponent<Rigidbody>","AddComponent<MonoBehaviour>","AddComponent<Animator>"):
    assert forbidden not in art
print(f"PASS {count} byte-identical packed source assets; player squad only, existing approved prefab precedence")
print("PASS actual source skin, five-lobed protective morph, visual-only playback, shared meshes and cleanup")
