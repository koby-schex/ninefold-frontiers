#!/usr/bin/env python3
"""Structural guard for Rift Forager integrated-sculpt and texture seam pass.

The executable Windows/Ubuntu tests validate actual generated surfaces,
UV triangle spans, closed base topology and continuous weight fields.
Unity compilation, images and canonical art acceptance remain separate.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
def read(name):return (root/name).read_text(encoding="utf-8")
s=read("Assets/Ninefold/Editor/RiftForagerAnatomyReconstruction.cs")
rig=read("Assets/Ninefold/Editor/AshboundRigReview.cs")
tests=read("Tests/Ninefold.Core.Tests/RiftForagerAnatomyReconstructionTests.cs")
old=read("BuildTools/Art/validate_rift_forager_anatomy_reconstruction.py")
manifest=read("SourceArt/Ashbound/RiftForager/manifest.json")
workflow=read(".github/workflows/repository-checks.yml")
guide=read("Docs/Review/RiftForagerSculptSurface02/README.md")
def check(ok,msg):
    if not ok:raise AssertionError(msg)
check('rift-forager-continuous-anatomy-surface-03' in s and
      'private static float Union(' in s and
      'internal static float Field(' in s,
      "Second pass must alter the original continuous anatomical surface, not overlay props")
for term in (
    '1.641f,.064f',
    '1.710f,.025f',
    '1.365f,.063f',
    '1.294f,-.106f',
    '1.045f,.075f',
    '.485f,.079f',
    '.061f,.212f'
):
    check(term in s,"Missing smoothly integrated body landmark "+term)
check('internal static Surface MakeTextureReady(Surface raw)' in s and
      'var shifted=new Dictionary<int,int>()' in s and
      'result.UVs.Add(new[]{uv[0]+1f,uv[1]});' in s and
      'bool crosses=max-min>.5f;' in s and
      'Rewrite(raw.GarmentIndices,result.GarmentIndices)' in s and
      'Rewrite(raw.SkinIndices,result.SkinIndices)' in s,
      "Cylindrical seam requires position-identical duplicated UV render vertices")
check('var sculpt=RiftForagerAnatomyReconstruction.Generate();' in rig and
      'RiftForagerAnatomyReconstruction.MakeTextureReady(sculpt);' in rig and
      'unified.RecalculateTangents();' in rig and
      'AshboundSkeleton.SkinOrganic(rebuilt.Vertices[i])' in rig,
      "Actual skinned Unity review prefab must use the seam-safe sculpt/UV output")
for method in ("RebuildAnatomicalPlanes","RebuildSourceFeatureClearance", "RebuildUvSeams","RebuildWeightContinuity"):
    check(method in tests,"Executable real model quality test missing: "+method)
check('RebuildTopology' in tests and 'Seamless anatomy has' in tests and
      'RebuildSurface' in tests and 'RebuildWeights' in tests,
      "Existing manifold, volume and bone checks must stay enabled")
check('validate_rift_forager_sculpt_surface.py' in workflow and
      'validate_rift_forager_anatomy_reconstruction.py' in workflow and
      'RiftForagerAnatomyReconstructionTests()' in read("Tests/Ninefold.Core.Tests/Program.cs"),
      "Must run both new and original safeguards in default CI")
check('"rigged": false' in manifest and
      '"mobile_certified": false' in manifest,
      "Source model cannot be silently approved by a geometry pass")
check('BattleModelCatalogSetup' not in s and
      'WorldFiveBattleOrganics' not in s and
      'GetComponent' not in s,
      "No live battle, gameplay or Unity component modifications allowed in pure sculpt generator")
check('canon' in guide.lower() and 'ArtReview' in guide and
      'Unity' in guide and 'iPhone 14' in guide,
      "Manual review of locked visual reference and native performance is required")
check('MakeTextureReady' in s and
      'RiftForagerAnatomyReconstruction.Generate()' in old,
      "First body reconstruction remains a separate prior verified gate")
print("PASS: distinct low-frequency natural anatomy surface features, not new components")
print("PASS: UV seam-safe real skinned review mesh, preserved source character details")
print("PASS: Windows/Ubuntu actual geometry tests and untouched live battle catalogs")
print("NOTE: not a finished sculpt, canon approval or mobile-certified model")
