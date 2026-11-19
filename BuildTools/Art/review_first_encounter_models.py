"""Render exact source-linked tactical REST meshes; no native Unity or canon claim."""
import argparse
import math
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from mesh_depth_raster import draw_mesh_with_depth

PALETTES = {
    "Seamstalker": {"body": (.49,.35,.58), "accent": (.43,.39,.46), "dark": (.13,.19,.23)},
    "Sillrunner": {"body": (.43,.62,.50), "accent": (.64,.60,.58), "dark": (.13,.19,.23)},
    "RiftForager": {"body": (.43,.48,.50), "accent": (.23,.24,.28), "dark": (.13,.19,.23)},
}

def load(path, cloth=False):
    v, n, f, colors = [], [], [], []
    material = None
    for row in path.read_text(encoding="utf-8").splitlines():
        tok = row.split()
        if not tok:
            continue
        if tok[0] == "v":
            v.append([float(x) for x in tok[1:4]])
        elif tok[0] == "vn":
            n.append([float(x) for x in tok[1:4]])
        elif tok[0] == "usemtl":
            material = tok[1]
        elif tok[0] == "f":
            f.append([int(x.split("/")[0])-1 for x in tok[1:]])
            colors.append(material)
    v, n, f = np.asarray(v), np.asarray(n), np.asarray(f, dtype=np.int32)
    if len(v) != len(n) or len(f) < (1900 if cloth else 3000) or f.shape[1] != 3:
        raise ValueError("Invalid real body mesh")
    if set(colors) != ({"accent", "dark"} if cloth else {"body", "accent", "dark"}):
        raise ValueError("All three true material regions required")
    if not np.isfinite(v).all() or not np.isfinite(n).all():
        raise ValueError("Invalid rest coordinates")
    return v, n, f, colors

def frame(model, kind, angle, tilt, width, height, zoom):
    v, n, f, mats = model
    yaw, pitch = math.radians(angle), math.radians(tilt)
    c, s = math.cos(yaw), math.sin(yaw)
    x = v[:,0]*c-v[:,2]*s
    z = v[:,0]*s+v[:,2]*c
    y = v[:,1]*math.cos(pitch)-z*math.sin(pitch)
    depth = v[:,1]*math.sin(pitch)+z*math.cos(pitch)
    scale = height/(2*zoom)
    xy = np.column_stack((width/2+x*scale,height*.54-y*scale))
    # Vertex-gradient normals from the same generated runtime mesh;
    # triangle-mean lighting is an inspection approximation, not Unity PBR.
    norm = n[f].mean(axis=1)
    norm /= np.maximum(1e-8,np.linalg.norm(norm,axis=1))[:,None]
    light = np.asarray([-.35,.88,.55])
    light /= np.linalg.norm(light)
    shade = np.clip(.40+.58*np.maximum(0,norm@light),.32,1)
    palette = np.asarray([PALETTES[kind][m] for m in mats])
    shades = (np.clip(palette*shade[:,None],0,1)*255).astype(np.uint8)
    image = draw_mesh_with_depth(Image.new("RGB",(width,height),(22,27,35)),
                                 xy,depth,f,shades)
    d=ImageDraw.Draw(image)
    d.rectangle((0,0,width,43), fill=(15,19,26))
    d.text((10,9),kind+" | "+str(angle)+" DEG", fill=(231,223,202))
    d.text((10,27),"ACTUAL REST BODY - NOT UNITY",fill=(157,173,188))
    d.rectangle((0,height-27,width,height),fill=(15,19,26))
    d.text((10,height-20),"FORAGER: SEPARATE GEAR NOT SHOWN" if kind=="RiftForager"
           else "PROVISIONAL WILDLIFE ART",fill=(201,174,146))
    return image

def main():
    p=argparse.ArgumentParser()
    p.add_argument("directory",type=Path)
    args=p.parse_args()
    out=args.directory
    models={k:load(out/(k+"-RuntimeBody.obj")) for k in PALETTES}
    body=models["RiftForager"]
    cloth=load(out/"RiftForager-RuntimeCloth.obj",cloth=True)
    models["RiftForager"]=(np.concatenate((body[0],cloth[0])),
        np.concatenate((body[1],cloth[1])),
        np.concatenate((body[2],cloth[2]+len(body[0]))),body[3]+cloth[3])
    for mode, angles, tilt, size, zoom in [
        ("Detail", (0,45,90,180), 15, (400,500), 1.70),
        ("Tactical", (0,45,90), 58, (390,844), 4.0)]:
        w,h=size
        sheet=Image.new("RGB",(len(angles)*w,len(models)*h))
        for r,(kind,model) in enumerate(models.items()):
            for i,angle in enumerate(angles):
                sheet.paste(frame(model,kind,angle,tilt,w,h,zoom),(i*w,r*h))
        dest=out/("FirstEncounter-"+mode+".png")
        sheet.save(dest)
        with Image.open(dest) as check:
            check.verify()
    metrics={"status":"actual tactical rest geometry; not native Unity, canon or phone performance",
             "Forager":"body plus two stitched cloth panels; separate attached runtime mantle and equipment not included",
             "Forager_body_triangles":len(body[2]),"Forager_cloth_triangles":len(cloth[2]),
             "meshes":{k:{"vertices":len(v),"triangles":len(f),
                          "bounds":[v.min(axis=0).tolist(),v.max(axis=0).tolist()]}
                       for k,(v,n,f,m) in models.items()}}
    (out/"runtime-rest-metrics.json").write_text(json.dumps(metrics,indent=2)+"\n")
    print(json.dumps(metrics,indent=2))
if __name__=="__main__":
    main()
