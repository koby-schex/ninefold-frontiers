#!/usr/bin/env python3
"""Multi-angle actual-mesh study renders (not concept art or canon approval)."""
import argparse
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
MODELS = ("RiftForager", "PartedStriker", "SeamCastigator",
          "AnchorWrecker", "AfterechoSeer", "CampWitness")
FRAMES = (("Front", 0), ("Three-quarter", math.pi/4),
          ("Right", math.pi/2), ("Back", math.pi))

def parse_materials(path):
    result = {}; current = None
    for line in path.read_text(encoding="utf8").splitlines():
        tokens = line.split()
        if tokens[:1] == ["newmtl"]:
            current = tokens[1]
        elif tokens[:1] == ["Kd"] and current:
            result[current] = np.array([float(v) for v in tokens[1:4]],
                                       dtype=np.float64)
    return result

def parse_obj(path):
    positions = []; triangles = []; materials = []; current = None
    for line in path.read_text(encoding="utf8").splitlines():
        if line.startswith("v "):
            positions.append([float(n) for n in line.split()[1:4]])
        elif line.startswith("usemtl "):
            current = line.split()[1]
        elif line.startswith("f "):
            corners = [int(token.split("/")[0])-1 for token in line.split()[1:]]
            if len(corners) != 3 or current is None:
                raise ValueError("Source requires material-assigned triangles")
            triangles.append(corners); materials.append(current)
    if not triangles: raise ValueError("Empty source mesh")
    return (np.array(positions, np.float64),
            np.array(triangles, np.int32), materials)

def render(vertices, triangles, names, palette, angle, model, view,
           size=(330,600)):
    w,h=size
    c,s=math.cos(angle),math.sin(angle)
    x=vertices[:,0]*c-vertices[:,2]*s
    z=vertices[:,0]*s+vertices[:,2]*c
    pitch=math.radians(9)
    y=(vertices[:,1]-.91)*math.cos(pitch)-z*math.sin(pitch)
    depth=(vertices[:,1]-.91)*math.sin(pitch)+z*math.cos(pitch)
    scale=min((h-96)/1.95,(w-64)/max(1.,np.ptp(x)))
    xy=np.column_stack((w*.5+x*scale,(h-24)*.5-y*scale))
    p=vertices[triangles]
    normals=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0])
    lengths=np.linalg.norm(normals,axis=1)
    valid=lengths>1e-10
    normals[valid]/=lengths[valid,None]
    lamp=np.array([-.5,.8,.7]);lamp/=np.linalg.norm(lamp)
    light=np.clip(.44+.47*np.abs(normals@lamp),.25,.96)
    rgb={key:np.clip(color*255,0,255) for key,color in palette.items()}
    colors=np.array([rgb[key] for key in names],dtype=np.float64)
    shades=np.clip(colors*light[:,None],0,255).astype(np.uint8)
    order=np.argsort(depth[triangles].mean(axis=1))
    im=Image.new("RGB",(w,h),(22,26,33)); draw=ImageDraw.Draw(im)
    draw.ellipse((w*.5-85,h-62,w*.5+85,h-30),
                 fill=(32,39,47),outline=(62,70,77),width=2)
    for idx in order:
        if not valid[idx]: continue
        poly=xy[triangles[idx]]
        if not np.isfinite(poly).all(): raise ValueError("Nonfinite face")
        draw.polygon([tuple(map(float,v)) for v in poly],
                     fill=tuple(int(value) for value in shades[idx]))
    draw.text((16,13),model.upper(),fill=(233,225,208))
    draw.text((16,34),view+" / ACTUAL MESH STUDY 01",fill=(181,187,188))
    draw.text((16,h-24),"UNAPPROVED | UNRIGGED | NO GAMEPLAY",fill=(180,148,122))
    return im

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--source",type=Path,default=ROOT/"SourceArt/Ashbound")
    parser.add_argument("--output",type=Path,default=ROOT/"SourceArt/Ashbound/Review")
    options=parser.parse_args()
    options.output.mkdir(parents=True,exist_ok=True)
    for name in MODELS:
        folder=options.source/name
        palette=parse_materials(folder/(name+".mtl"))
        vertices,faces,materials=parse_obj(folder/(name+".obj"))
        frames=[render(vertices,faces,materials,palette,angle,name,label)
                for label,angle in FRAMES]
        sheet=Image.new("RGB",(frames[0].width*len(frames),frames[0].height))
        for i,frame in enumerate(frames):
            sheet.paste(frame,(i*frame.width,0))
        target=options.output/(name+"-FourAngles.png")
        sheet.save(target,optimize=True)
        print(name,len(vertices),"vertices,",len(faces),
              "triangles; four-angle render:",target)

if __name__=="__main__": main()
