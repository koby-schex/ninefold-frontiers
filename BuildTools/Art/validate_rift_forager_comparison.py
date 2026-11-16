#!/usr/bin/env python3
"""Structural and delivered-artifact validation for actual Rift Forager mesh study.

The --mode sources gate runs in normal Repository checks without generation.
The --mode artifacts gate runs after the source geometry has been exported
and rasterized in the dedicated Ashbound model review workflow.
"""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageChops

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"SourceArt/Ashbound/Review/Generated"
EXPORT="BuildTools/Art/RiftForagerMeshExport/Program.cs"
RENDER="BuildTools/Art/review_rift_forager_reconstruction.py"
WORKFLOW=".github/workflows/ashbound-model-review.yml"
SHEETS={
    "RiftForager-BeforeAfter-Tactical-390x844.png":(3120,844),
    "RiftForager-BeforeAfter-Close-390x844.png":(3120,844),
    "RiftForager-BeforeAfter-Narrow-320x568.png":(1280,568),
    "RiftForager-BeforeAfter-Face-390x844.png":(1560,844),
    "RiftForager-ThreeLOD-Tactical-390x844.png":(1170,844),
}


def demand(ok,description):
    if not ok:
        raise AssertionError(description)


def source_check():
    surface=(ROOT/"Assets/Ninefold/Editor/RiftForagerAnatomyReconstruction.cs").read_text()
    tailor=(ROOT/"Assets/Ninefold/Editor/RiftForagerTailoredGarment.cs").read_text()
    rig=(ROOT/"Assets/Ninefold/Editor/AshboundRigReview.cs").read_text()
    core=(ROOT/"Tests/Ninefold.Core.Tests/Ninefold.Core.Tests.csproj").read_text()
    export=(ROOT/EXPORT).read_text()
    render=(ROOT/RENDER).read_text()
    wf=(ROOT/WORKFLOW).read_text()
    tests=(ROOT/"Tests/Ninefold.Core.Tests/RiftForagerAnatomyReconstructionTests.cs").read_text()
    source=(ROOT/"SourceArt/Ashbound/RiftForager/manifest.json").read_text()
    for phrase in (
        "RiftForagerAnatomyReconstruction.Generate()",
        "RiftForagerAnatomyReconstruction.MakeTextureReady(raw)",
        "WriteFaces(writer,\"garment\"",
        "WriteFaces(writer,\"skin\"",
        "NO animations, skin weights or Unity shader are baked",
    ):
        demand(phrase in export,"Standalone .NET OBJ must be the exact pure C# Unity geometry: "+phrase)
    demand("WriteBodyLod(" in export and
           'RiftForager-AnatomyLOD1-REVIEW.obj' in export and
           'RiftForager-AnatomyLOD2-REVIEW.obj' in export and
           "render_body_lod_sheet(args,palette)" in render and
           "body_only_anatomy(path,body_name)" in render and
           "RebuildMultiResolutionAnatomy" in tests,
           "Three exact C# anatomy LODs and honest 3-way sheet must be in CI")
    demand("RiftForagerTailoredGarment.Generate()" in export and
           "RiftForagerTailoredGarment.Generate()" in rig and
           "RiftForagerTailoredGarment.cs" in core and
           "RiftForagerTailoredGarment.cs" in (ROOT/"BuildTools/Art/RiftForagerMeshExport/RiftForagerMeshExport.csproj").read_text() and
           "RiftForagerTailoredGarment.cs" in wf and
           "RebuildTailoredGarment" in tests and
           "PanelPoint(int panel,int column,int row,bool inside)" in tailor,
           "Real fieldcloth source must be shared by rig, OBJ export, tests and art workflow")
    demand("internal static Surface Generate(" in surface and
           "internal static Surface MakeTextureReady(Surface raw)" in surface,
           "Actual source mesh generator must still be shared with Unity")
    for phrase in (
        'BODY_NAMES = {"Avarin_Torso", "Avarin_Pelvis"',
        '"Avarin_LivingCrescentMantle"',
        '"Avarin_Eye_L"',
        '"Avarin_Eye_R"',
        'without_source_body(path)',
        'combine_with_body(',
        'from render_ashbound import parse_materials, parse_obj',
        'tactical_tilt_degrees',
        'STATIC MESH - NOT UNITY',
        'NOT skinned, animated, rig pose, URP/PBR or normal-map render',
        'new_connected_body_triangles',
    ):
        demand(phrase in render,"Comparison must use authentic geometry with truthful review labels: "+phrase)
    demand("from mesh_depth_raster import draw_mesh_with_depth" in render and
           "img=draw_mesh_with_depth(img,screen,distance,triangles[valid],shaded[valid])" in render and
           "np.argsort(distance[triangles].mean(axis=1))" not in render,
           "Review previews must use pixel-accurate occlusion, not centroid sorting")
    demand("test_rift_forager_depth_raster.py" in wf and
           "test_rift_forager_depth_raster.py" in (ROOT/".github/workflows/repository-checks.yml").read_text(),
           "Depth raster regression tests must run in both art and default repository checks")
    demand("RebuildTopology" in tests and "RebuildUvSeams" in tests,
           "Topology/UV reconstruction checks must remain independently executable")
    demand((ROOT/EXPORT).is_file(),
           "Concrete .NET export program must exist")
    for fragment in (
        RENDER,
        "BuildTools/Art/RiftForagerMeshExport/RiftForagerMeshExport.csproj",
        "actions/upload-artifact@v4",
        "rift-forager-connected-anatomy-before-after",
        "dotnet run --project BuildTools/Art/RiftForagerMeshExport",
    ):
        demand(fragment in wf,
               "CI workflow must generate and upload actual comparison: "+fragment)
    demand('"rigged": false' in source and '"mobile_certified": false' in source,
           "Unapproved original static source must remain unaltered")
    demand("BattleModelCatalogSetup" not in render and
           "BattleUnitVisuals.asset" not in render and
           "UnityEditor" not in export,
           "Automated model preview must not register unfinished models in gameplay")
    print("PASS correct connected anatomy C# source -> static OBJ export -> source-accurate comparison")
    print("PASS real retained mantle/eyes/gear, review-artifact and no gameplay promotion")
    print("NOTE: static render only, not Unity, animation, URP, canon or phone FPS")


def artifact_check():
    report_path=OUT/"comparison-metrics.json"
    demand(report_path.is_file(),"Actual metrics JSON not created")
    data=json.loads(report_path.read_text(encoding="utf-8"))
    demand("PROVISIONAL STATIC" in data["status"],
           "Status must explicitly reject canonical approval")
    limitations=" ".join(data["important_limitations"])
    for phrase in ("NOT Unity", "NOT skinned", "NOT mobile",
                   "NOT official"):
        demand(phrase in limitations,"Important factual caveat omitted: "+phrase)
    demand("per-pixel barycentric" in data["camera_approximation"]["visibility"],
           "Generated mesh evidence must explicitly record correct visibility algorithm")
    before=data["before"]
    after=data["after"]
    preserved=data["preserved_details"]
    lod=data["anatomy_lods"]
    demand(32000<lod["high_triangles"]<90000 and
           6000<lod["medium_triangles"]<lod["high_triangles"] and
           3000<lod["distant_triangles"]<lod["medium_triangles"],
           "Source-generated anatomy LOD triangle budgets or ordering invalid")
    for f in ("RiftForager-AnatomyLOD1-REVIEW.obj",
              "RiftForager-AnatomyLOD2-REVIEW.obj"):
        demand((OUT/f).is_file() and (OUT/f).stat().st_size>100,
               "Generated source-linked anatomical LOD OBJ missing: "+f)
    demand(before["vertices"]>15000 and before["triangles"]>30000,
           "Original source geometry too small")
    demand(after["vertices"]>4000 and after["triangles"]>8000,
           "Combined reconstructed model geometry too small")
    demand(2200<=preserved["new_tailored_fieldcloth_triangles"]<=6000 and
           preserved["new_anatomy_and_cloth_triangles"]==
           preserved["new_connected_body_triangles"]+preserved["new_tailored_fieldcloth_triangles"],
           "Physically draped garment is missing from exported geometry")
    demand(preserved["new_connected_body_triangles"]>8000 and
           preserved["retained_accessory_triangles"]>5000,
           "Connected body OR existing equipment was lost")
    demand(len(preserved["replaced_body_parts"])>=12,
           "The comparison must remove original detached anatomical pieces")
    for f in ("RiftForager-ConnectedBody-REVIEW.obj",
              "RiftForager-AfterComposite-REVIEW.obj",
              "RiftForager.mtl"):
        demand((OUT/f).is_file() and (OUT/f).stat().st_size>100,
               "Generated real inspectable model/material missing: "+f)
    for name,size in SHEETS.items():
        path=OUT/name
        demand(path.is_file(),"Required mesh comparison PNG not produced: "+name)
        with Image.open(path) as img:
            img.verify()
        with Image.open(path) as img:
            demand(img.size==size,"Incorrect actual image dimensions: "+name)
            width_per_view=390 if "390x844" in name else 320
            left=img.crop((0,0,width_per_view,img.height))
            right=img.crop((width_per_view,0,2*width_per_view,img.height))
            if "ThreeLOD" not in name:
                differences=ImageChops.difference(left,right)
                demand(differences.getbbox() is not None,
                       "Before/after pair is a duplicated image instead of a distinct model render")
    print("PASS real source/connected anatomy OBJ files, 2 additional LOD OBJs, original .MTL and five review sheets")
    print("PASS nonidentical before/after images, correct native pixel sizes and intact assets")
    print("NOTE all visual models remain provisional. Do not claim Unity quality or canon acceptance.")


def main():
    cli=argparse.ArgumentParser()
    cli.add_argument("--mode",choices=("sources","artifacts"),required=True)
    opts=cli.parse_args()
    source_check()
    if opts.mode=="artifacts":
        artifact_check()


if __name__=="__main__":
    main()
