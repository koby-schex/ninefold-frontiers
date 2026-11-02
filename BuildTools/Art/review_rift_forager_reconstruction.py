#!/usr/bin/env python3
"""Rift Forager: truthful BEFORE/AFTER static mesh review in CI.

BEFORE = checked-in unrigged SourceArt OBJ.
AFTER = exact current continuous anatomy exported by .NET, plus unchanged
        SourceArt head/mantle/face/gear parts; previous separate body omitted.

This is NOT a Unity Game-view render and does NOT simulate skeletal animation,
URP PBR shading, baked normals, canon approval, or iPhone performance.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_ashbound import parse_materials, parse_obj
from mesh_depth_raster import draw_mesh_with_depth

ORIGINAL = ROOT / "SourceArt/Ashbound/RiftForager/RiftForager.obj"
MATERIALS = ROOT / "SourceArt/Ashbound/RiftForager/RiftForager.mtl"
VIEWS = (("front", 0.0), ("three-quarter", math.pi/4),
         ("right", math.pi/2), ("back", math.pi))
BODY_NAMES = {"Avarin_Torso", "Avarin_Pelvis",
              "Avarin_Neck", "Avarin_TempleFace"}
BODY_PREFIXES = ("Avarin_Leg_", "Avarin_Foot_",
                 "Avarin_Arm_", "Avarin_Hand_")
REQUIRED_ACCESSORIES = {
    "Avarin_LivingCrescentMantle",
    "Avarin_CrescentRim",
    "Avarin_Eye_L",
    "Avarin_Eye_R",
    "Salvage_SplitFieldApron",
}


def anatomical_old_part(name):
    return name in BODY_NAMES or any(name.startswith(s) for s in BODY_PREFIXES)


def without_source_body(path):
    """Parse the exact authored o/usemtl grouping, retain every accessory face.

    OBJ vertex indices are absolute. Source geometry is compacted after
    face-level body removal to avoid falsely inflated mesh bounds.
    """
    positions, faces, mats, parts = [], [], [], []
    name = None
    material = None
    retained_names = set()
    excluded_names = set()
    with path.open(encoding="utf-8") as stream:
        for row in stream:
            row = row.strip()
            if row.startswith("o "):
                name = row[2:].strip()
            elif row.startswith("usemtl "):
                material = row.split(maxsplit=1)[1]
            elif row.startswith("v "):
                positions.append([float(v) for v in row.split()[1:4]])
            elif row.startswith("f "):
                if name is None or material is None:
                    raise ValueError("Original OBJ face lacks named part/material")
                corners = [int(part.split("/")[0])-1
                           for part in row.split()[1:]]
                if len(corners) != 3:
                    raise ValueError("Triangulated source required")
                if anatomical_old_part(name):
                    excluded_names.add(name)
                else:
                    faces.append(corners)
                    mats.append(material)
                    parts.append(name)
                    retained_names.add(name)
    if not REQUIRED_ACCESSORIES.issubset(retained_names):
        raise ValueError("Source canonical mantle/eyes or field gear missing: " +
                         str(sorted(REQUIRED_ACCESSORIES-retained_names)))
    if not BODY_NAMES.issubset(excluded_names) or len(excluded_names) < 12:
        raise ValueError("Expected twelve distinct disconnected body sections")
    arr = np.asarray(faces, dtype=np.int32)
    used, inverse = np.unique(arr.reshape(-1), return_inverse=True)
    verts = np.asarray(positions, dtype=np.float64)[used]
    faces = inverse.reshape(-1, 3).astype(np.int32)
    if not np.isfinite(verts).all():
        raise ValueError("Nonfinite original source geometry")
    return verts, faces, mats, retained_names, excluded_names


def combine_with_body(body_path, old_path):
    accessories, faces, material_names, preserved, removed = without_source_body(old_path)
    body_vertices, body_faces, body_materials = parse_obj(body_path)
    if len(body_vertices) < 4000 or len(body_faces) < 8000:
        raise ValueError("Rebuilt connected anatomy is unexpectedly small")
    if set(body_materials) != {"skin", "garment"}:
        raise ValueError("Connected body lost its two approved study regions")
    vertices = np.concatenate([accessories, body_vertices], axis=0)
    triangles = np.concatenate([faces, body_faces+len(accessories)], axis=0)
    mats = material_names + body_materials
    if not np.isfinite(vertices).all():
        raise ValueError("Nonfinite composite vertices")
    if len(triangles) != len(faces)+len(body_faces):
        raise ValueError("Composite mesh dropped triangles")
    return (vertices, triangles, mats,
            {"retained_parts": len(preserved),
             "replaced_body_parts": sorted(removed),
             "retained_accessory_triangles": len(faces),
             "new_connected_body_triangles": len(body_faces),
             "new_connected_body_vertices": len(body_vertices)})


def write_mesh(path, vertices, faces, materials):
    """Inspector-ready static OBJ; not a Unity prefab, rig or final asset."""
    with path.open("w", encoding="utf-8") as output:
        output.write("# PROVISIONAL review-only combined mesh. No rig, UV or materials baked.\n")
        output.write("mtllib RiftForager.mtl\n")
        output.write("o RiftForager_Review_Composite\n")
        for pt in vertices:
            output.write("v "+ " ".join(f"{float(v):.7f}" for v in pt)+"\n")
        last = None
        for tri, mat in zip(faces, materials):
            if mat != last:
                output.write("usemtl "+mat+"\n")
                last = mat
            output.write("f "+ " ".join(str(int(v)+1) for v in tri)+"\n")


def raster(vertices, triangles, material_names, palette,
           yaw, tilt, width, height, half_height, label, view, center_y=.93):
    """Orthographic face raster of true static OBJ triangles.

    Model colors are source MTL swatches. No synthesized new anatomy or
    glossy fake photorealistic detail, and no claim to match Unity shaders.
    """
    cos_y, sin_y = math.cos(yaw), math.sin(yaw)
    rotated_x = vertices[:,0]*cos_y - vertices[:,2]*sin_y
    rotated_z = vertices[:,0]*sin_y + vertices[:,2]*cos_y
    pitch = math.radians(tilt)
    upward = (vertices[:,1]-center_y)*math.cos(pitch) - rotated_z*math.sin(pitch)
    distance = (vertices[:,1]-center_y)*math.sin(pitch) + rotated_z*math.cos(pitch)
    scale = height / (2*half_height)
    screen = np.column_stack((width*.5+rotated_x*scale,
                              height*.50-upward*scale))
    tris = vertices[triangles]
    norm = np.cross(tris[:,1]-tris[:,0], tris[:,2]-tris[:,0])
    lengths = np.linalg.norm(norm, axis=1)
    valid = lengths > 1e-12
    norm[valid] /= lengths[valid,None]
    light = np.array([-.48,.84,.72])
    light /= np.linalg.norm(light)
    intensity = np.clip(.41+.53*np.abs(norm@light), .2,.99)
    colors = np.asarray([np.clip(palette[key],0,1)
                         for key in material_names], dtype=np.float64)
    shaded = (np.clip(colors*intensity[:,None],0,1)*255).astype(np.uint8)
    img=Image.new("RGB", (width,height), (22,27,34))
    drawer=ImageDraw.Draw(img)
    # Neutral scale cue; do not imply any canonical World Five location.
    foot=height*.50+(center_y*math.cos(pitch))*scale
    drawer.ellipse((width*.5-48,foot-8,width*.5+48,foot+8),
                   fill=(39,45,53),outline=(59,66,72),width=1)
    # Triangle-centroid painter ordering misrepresented intersections and
    # sometimes concealed original eyes/gear. Depth is now per-pixel across
    # every real triangle and is independent of triangle submission order.
    img=draw_mesh_with_depth(img,screen,distance,triangles[valid],shaded[valid])
    drawer=ImageDraw.Draw(img)
    drawer.rectangle((0,0,width,69),fill=(17,22,29))
    drawer.text((12,9), label,fill=(236,223,199))
    drawer.text((12,28), f"{view} | {tilt} DEG | ORTHO {half_height:g}",
                fill=(178,198,206))
    drawer.text((12,49), "STATIC MESH - NOT UNITY",fill=(205,159,121))
    drawer.rectangle((0,height-33,width,height),fill=(17,22,29))
    drawer.text((12,height-24),"UNAPPROVED | NO RIG / PBR / FPS",
                fill=(184,166,149))
    return img


def sheet(name, options, models, palette, views, width, height,
          tilt, half_height, center_y=.93):
    source, replacement=models
    result=Image.new("RGB",(width*len(views)*2,height))
    for row, (variant, data) in enumerate((
        ("BEFORE / SOURCE OBJ",source),
        ("AFTER / COMPOSITE STUDY",replacement),
    )):
        vertices, triangles, materials=data
        for i,(label,yaw) in enumerate(views):
            frame=raster(vertices,triangles,materials,palette,
                         yaw,tilt,width,height,half_height,variant,label,center_y=center_y)
            x=(i*2+row)*width
            result.paste(frame,(x,0))
    output=options.output/name
    result.save(output,optimize=True)
    print(f"PNG {output} [{result.width}x{result.height}]")
    return output


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--body", type=Path, required=True,
                        help="OBJ written by the exact .NET anatomy generator")
    parser.add_argument("--output", type=Path, required=True)
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    palette=parse_materials(MATERIALS)
    before=parse_obj(ORIGINAL)
    after_vertices,after_triangles,after_mats,details=combine_with_body(
        args.body,ORIGINAL)
    after=(after_vertices,after_triangles,after_mats)
    if set(before[2])!=set(after_mats):
        raise ValueError("Original eight source material groups must survive")

    from shutil import copyfile
    copyfile(MATERIALS,args.output/"RiftForager.mtl")
    write_mesh(args.output/"RiftForager-AfterComposite-REVIEW.obj",
               *after)
    # The screen-relative actor scale is fixed for both variants.
    # These are NOT native Unity renders. The orthographic tilt/size matches
    # the code's target battle-camera parameters for geometry inspection only.
    images=[
        sheet("RiftForager-BeforeAfter-Tactical-390x844.png",
              args,(before,after),palette,VIEWS,390,844,58,4.0),
        sheet("RiftForager-BeforeAfter-Close-390x844.png",
              args,(before,after),palette,VIEWS,390,844,18,1.26),
        sheet("RiftForager-BeforeAfter-Narrow-320x568.png",
              args,(before,after),palette,VIEWS[:2],320,568,58,4.0),
        # Head / shoulder enlargement is inspection-only, NOT gameplay scale.
        sheet("RiftForager-BeforeAfter-Face-390x844.png",
              args,(before,after),palette,VIEWS[:2],390,844,18,.56,1.53),
    ]
    report={
        "status":"PROVISIONAL STATIC MESH COMPARISON — NOT CANON APPROVAL",
        "before":"original unrigged SourceArt/RiftForager OBJ",
        "after":"current connected C# anatomical export + unchanged source accessories",
        "important_limitations":[
            "NOT Unity Game-view or gameplay lighting",
            "NOT skinned, animated, rig pose, URP/PBR or normal-map render",
            "NOT mobile performance or iPhone frame-rate measurement",
            "NOT official or locked Ashbound / Avarin character design",
        ],
        "angles_degrees":[0,45,90,180],
        "camera_approximation":{
            "tactical_tilt_degrees":58,
            "tactical_orthographic_half_height":4.0,
            "close_tilt_degrees":18,
            "close_orthographic_half_height":1.26,
            "face_orthographic_half_height":.56,
            "face_center_y":1.53,
            "visibility":"per-pixel barycentric z-buffer, not centroid painter",
        },
        "before":{"vertices":int(len(before[0])),
                  "triangles":int(len(before[1]))},
        "after":{"vertices":int(len(after[0])),
                 "triangles":int(len(after[1]))},
        "preserved_details":details,
        "images":[image.name for image in images],
    }
    (args.output/"comparison-metrics.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__=="__main__":
    main()
