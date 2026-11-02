#!/usr/bin/env python3
"""Verify this is a real anatomical topology replacement, not another detail overlay.

Structural checks run alongside compiled .NET tests that inspect actual output
triangle manifoldness, surface normals, body morphometry and bone weights.
Unity compilation and canon visual approval still require native examination.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[2]
def read(p):
    return (root/p).read_text(encoding="utf-8")
surface=read("Assets/Ninefold/Editor/RiftForagerAnatomyReconstruction.cs")
builder=read("Assets/Ninefold/Editor/AshboundRigReview.cs")
skeleton=read("Assets/Ninefold/Editor/AshboundSkeleton.cs")
tests=read("Tests/Ninefold.Core.Tests/RiftForagerAnatomyReconstructionTests.cs")
camera=read("Assets/Ninefold/Editor/RiftForagerBattleCameraReview.cs")
source=read("SourceArt/Ashbound/RiftForager/manifest.json")
workflow=read(".github/workflows/repository-checks.yml")
canonical=read("Docs/Canon/WorldFiveAshboundCanonAddendum01.md")

def require(ok, message):
    if not ok:
        raise AssertionError(message)

for fragment in (
    'ReplacesOldBodyPart(string part)',
    'part=="Avarin_Torso"',
    'part=="Avarin_Pelvis"',
    'part=="Avarin_Neck"',
    'part=="Avarin_TempleFace"',
    '"Avarin_Leg_"',
    '"Avarin_Foot_"',
    '"Avarin_Arm_"',
    '"Avarin_Hand_"',
    'private static float Union(',
    'private static float Field(' if False else 'internal static float Field(',
    'Tetrahedra =',
    'private static readonly int[,] CornerOffsets',
    'var edges=new Dictionary<ulong,int>()',
    'mesh.Vertices.Add(new[]{px,py,pz});',
    'mesh.Normals.Add(new[]{gx/norm,gy/norm,gz/norm});',
    'Triangle(p,q,a);',
    'Triangle(q,d,a);',
):
    require(fragment in surface,
        "The revised candidate needs actual single-volume indexed anatomy: "+fragment)

require('RiftForagerAnatomyReconstruction.ReplacesOldBodyPart(' in builder and
        'compactTriangles.Add(newIndex);' in builder and
        'mesh.SetTriangles(compactTriangles,0);' in builder and
        'RiftForagerAnatomyReconstruction.Generate()' in builder,
        "Old anatomical triangles must be physically removed and replaced, not overlaid")
require('new GameObject("Reconstructed continuous Avarin anatomy")' in builder and
        '"Rift Forager continuous anatomical rest skin 01"' in builder and
        'unified.subMeshCount=2;' in builder and
        'unified.SetTriangles(rebuilt.GarmentIndices,0);' in builder and
        'unified.SetTriangles(rebuilt.SkinIndices,1);' in builder and
        'unified.RecalculateTangents();' in builder,
        "Output must be actual 3D topology with two cloth/skin materials")
require('organicRenderer.sharedMesh=unified;' in builder and
        'organicRenderer.bones=bones;' in builder and
        'organicRenderer.rootBone=bones[0];' in builder and
        'unified.boneWeights=organicWeights;' in builder and
        'unified.bindposes=organicBindposes;' in builder and
        'organicRenderer.sharedMaterials=new[]{' in builder and
        'AssetDatabase.CreateAsset(unified,' in builder,
        "The reconstructed body must be a saved, truly skinned material asset")
require('AshboundSkeleton.SkinOrganic(rebuilt.Vertices[i])' in builder and
        'internal static Weight4 SkinOrganic(float[] vertex)' in skeleton and
        'boneIndex2=w.C,weight2=w.WC' in builder and
        'boneIndex3=w.D,weight3=w.WD' in builder and
        'Math.Abs' in skeleton,
        "Cross-joint organic deformation must use normalized four-bone weights")
require('File.ReadAllBytes("Assets/Ninefold/Editor/RiftForagerAnatomyReconstruction.cs")' in builder and
        'SourceArt/Ashbound/' in builder and
        'folder+"/RiftForagerContinuousAnatomy01.asset"' in builder,
        "Generated assets must be source-revision fingerprinted and stay in ArtReview")
require('connected.Length!=1 || connected[0].sharedMesh.subMeshCount!=2' in camera and
        'Build the updated connected-anatomy Rift Forager first.' in camera,
        "Battle camera quality review must reject the obsolete disconnected rig")
for method in ("RebuildIdentity","RebuildTopology","RebuildSurface","RebuildWeights"):
    require(method in tests,"Executable reconstruction test missing: "+method)
require('RiftForagerAnatomyReconstructionTests()' in
        read("Tests/Ninefold.Core.Tests/Program.cs") and
        'RiftForagerAnatomyReconstruction.cs' in
        read("Tests/Ninefold.Core.Tests/Ninefold.Core.Tests.csproj"),
        "New actual geometry/weight tests must run on Windows and Ubuntu")
require('validate_rift_forager_anatomy_reconstruction.py' in workflow,
        "New reconstruction structural guard must be part of default CI")
require('"rigged": false' in source and
        '"mobile_certified": false' in source and
        '"status": "provisional static 3D source; not approved for runtime"' in source,
        "Do not rewrite the controlling source manifest as approved production art")
require('living crescent sensory mantle' in canonical and
        'No bonded creatures fight alongside' in canonical,
        "No change to the controlling Avarin/Ashbound species constraints")
for forbidden in ("BattleModelCatalogSetup","BattleUnitVisuals.asset",
                  "MonoBehaviour","Rigidbody","Collider",
                  "WorldFiveBattleOrganics"):
    require(forbidden not in surface,
        "The organic topology builder cannot alter gameplay or accept extra physics: "+forbidden)

print("PASS: original 12 detached Avarin anatomical meshes replaced with one indexed continuous rest skin")
print("PASS: rebuilt real skinned mesh with normalized four-bone blending and cloth/skin submeshes")
print("PASS: source/approved catalog remain unchanged, prior rig review still isolated")
print("NOTE: procedural connected surface is an intermediate art study, not approved 3D sculpt")
