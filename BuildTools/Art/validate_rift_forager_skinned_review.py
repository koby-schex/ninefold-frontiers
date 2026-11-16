#!/usr/bin/env python3
"""Structural guard for the *actual skinned review candidate* pipeline.

Deliberately does not claim Unity compilation, visual or lore approval,
production retopology/texture painting, on-device performance, or promotion.
The .NET Core runner separately executes actual SourceArt skin-weight math.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[2]
def text(path):
    return (root/path).read_text(encoding="utf-8")

rig=text("Assets/Ninefold/Editor/AshboundSkeleton.cs")
build=text("Assets/Ninefold/Editor/AshboundRigReview.cs")
pbr=text("Assets/Ninefold/Editor/AshboundReviewPbr.cs")
tests=text("Tests/Ninefold.Core.Tests/AshboundSkeletonTests.cs")
source=text("SourceArt/Ashbound/RiftForager/manifest.json")
play=text("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
catalog=text("Assets/Ninefold/Editor/BattleModelCatalogSetup.cs")

def require(condition,message):
    if not condition:
        raise AssertionError(message)

require('new Joint("Root"' in rig and
        'new Joint("LivingCrescentMantle"' in rig and
        'new Joint("RightAnkle"' in rig and
        'internal static Weight Skin(' in rig and
        'internal static Pose Sample(' in rig,
        "19-joint named Avarin anatomy, continuous skinning and animation sampler required")
require('case AshboundMotion.Clip.Idle:' in rig and
        'case AshboundMotion.Clip.Advance:' in rig and
        'case AshboundMotion.Clip.Attack:' in rig and
        'case AshboundMotion.Clip.Hit:' in rig and
        'case AshboundMotion.Clip.Afterecho:' in rig and
        'case AshboundMotion.Clip.Defeat:' in rig,
        "Skinned model must actually animate six already documented states")
require('GetWindow<AshboundRigReview>' in build and
        'Build Rift Forager Skinned Benchmark' in build and
        'SourceArt/Ashbound/' in build and
        'if(name!="RiftForager")' in build,
        "Builder must be a directly accessible Rift Forager-only Unity command")
require('GreatStriderSource.ReadMaterials' in build and
        'GreatStriderSource.ReadMesh' in build and
        'compactParts.Add(section.SourceParts[oldIndex]);' in build and
        'mesh.boneWeights=weights;' in build and
        'mesh.bindposes=bindposes;' in build and
        'mesh.SetTriangles(compactTriangles,0);' in build and
        'new BoneWeight' in build and
        'new SkinnedMeshRenderer' not in build and
        'node.AddComponent<SkinnedMeshRenderer>()' in build,
        "Real original OBJ triangles must be skinned and bound, not a primitive rig diagram")
require('mesh.RecalculateTangents();' in build and
        'mesh.SetUVs(0,' in build and
        'renderer.bones=bones;' in build and
        'renderer.rootBone=bones[0];' in build,
        "Tangent-space normal maps need real skinned mesh data")
require('private const int Size=512;' in pbr and
        'private static float TiledNoise(' in pbr and
        'private static float HashNoise(' in pbr and
        'TiledNoise(x,y,7,seed)' in pbr and
        'TiledNoise(x,y,61,seed+137)' in pbr and
        'importer.maxTextureSize=512;' in pbr and
        'surface.Color[0],surface.Color[1],surface.Color[2]' in pbr,
        "Review materials must retain original swatches with tileable 512px microdetail")
require('WritePackedMask(' in pbr and
        'material.SetTexture("_MetallicGlossMap",packed);' in pbr and
        'material.EnableKeyword("_METALLICSPECGLOSSMAP");' in pbr and
        'importer.sRGBTexture=false;' in pbr and
        'metallicSmoothness.png' in pbr and
        'new Texture2D(Size,Size,TextureFormat.RGBA32,' in pbr,
        "Actual imported, linear packed URP metallic/smoothness maps required")
require('var lodSteps=new[]{.036f,.052f};' in build and
        'actor.AddComponent<LODGroup>()' in build and
        'anatomyLodGroup.SetLODs(' in build and
        'lodMesh.boneWeights=weightsLod;' in build and
        'lodRenderer.bones=bones;' in build and
        'RebuildMultiResolutionAnatomy' in
        text("Tests/Ninefold.Core.Tests/RiftForagerAnatomyReconstructionTests.cs"),
        "High/medium/distant physical 19-bone anatomy LODs required")
require('SkeletonMajorMotionPass' in tests and
        'case AshboundMotion.Clip.Defeat:' in rig and
        'if(bone==15)return new Pose(-8f*beat+3f*doubleBeat' in rig,
        "All six clips require pose detail and actual regression tests")
require('AshboundReviewPbr.Build(folder,shader,materials)' in build and
        'material.SetTexture("_BaseMap",albedo);' in pbr and
        'material.SetTexture("_BumpMap",normal);' in pbr and
        'material.EnableKeyword("_NORMALMAP");' in pbr and
        'TextureImporterType.NormalMap' in pbr and
        'texture.SetPixels32' not in pbr and
        'staging.SetPixels32(pixels)' in pbr,
        "Actual imported albedo and tangent-space normal PNGs required")
for m in ('"skin"','"mantle"','"garment"','"salvage"','"hardware"','"seam"','"dark"','"eye"'):
    require(m in pbr,"Original eight material groups must be handled: "+m)
require('new AnimationClip' in build and
        'AnimationUtility.SetEditorCurve' in build and
        'AnimatorController.CreateAnimatorControllerAtPath' in build and
        'PrefabUtility.SaveAsPrefabAsset' in build and
        'EditorSceneManager.SaveScene' in build,
        "Create physical AnimationClip assets, controller, skinned prefab and review scene")
require('Assets/Ninefold/ArtReview' in build and
        'BattleModelCatalogSetup' not in build and
        'BattleUnitVisuals' not in build and
        'WorldFiveBattleOrganics' not in build and
        'FrontiersPlaytest' not in build,
        "Review must not silently register unfinished model in live battles")
require('if(i>1 && type!=AshboundMotion.Clip.Defeat)' in build,
        "Defeat must be terminal, not accidentally blend back to idle")
require('Unity 6000.3.21f1' in build and
        'SaveCurrentModifiedScenesIfUserWantsTo' in build and
        'beforeAssemblyReload+=ReleasePreview' in build,
        "Pinned version and safe editor lifecycle required")
require('SkeletonOriginalMeshes' in tests and
        'SkeletonWeights' in tests and
        'SkeletonClipBoundaries' in tests and
        'SkeletonMotionDetail' in tests,
        "Actual source geometry, normalized skinning and motion must have executable tests")
require('"rigged": false' in source and
        '"mobile_certified": false' in source,
        "Locked provisional OBJ must not be rewritten as allegedly finished")
require('TryCreateApprovedVisual' in play and 'BattleModelCatalogSetup' in catalog,
        "Approved live model ownership remains separate from ArtReview")
print("PASS source OBJ -> 19-bone skinned prefab, six animation clips and normal-mapped textures")
print("PASS ArtReview isolation, no combat-ID promotion and preserved source-master status")
print("NOTE source structure alone does not prove Unity compilation, visual quality or canon accuracy")
