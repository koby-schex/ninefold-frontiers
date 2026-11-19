"""Inspect all SIX actual assembled runtime rest assets, including every wearable.

The exporter invokes the same pure C# geometry and attachment definitions as
the Unity runtime. This software raster is not a native animated/PBR render.
"""
import argparse
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from mesh_depth_raster import draw_mesh_with_depth

ROSTER = {
    "RiftForager":"Route reader",
    "PartedStriker":"Right contact housing",
    "SeamCastigator":"Reflector and cage",
    "AnchorWrecker":"Load braces and contact pad",
    "AfterechoSeer":"External capture assembly",
    "CampWitness":"Registry and left relay",
}
PALETTE = {
    "skin":(.43,.48,.50), "mantle":(.32,.42,.46),
    "fabric":(.23,.24,.28), "gear":(.52,.45,.42),
    "wake":(.66,.48,.39), "dark":(.13,.19,.23),
}
def load(path):
    v,n,f,roles,objects=[],[],[],[],[]
    material=None
    for row in path.read_text(encoding="utf-8").splitlines():
        t=row.split()
        if not t:continue
        if t[0]=="v":v.append([float(s) for s in t[1:4]])
        elif t[0]=="vn":n.append([float(s) for s in t[1:4]])
        elif t[0]=="usemtl":material=t[1]
        elif t[0]=="o":objects.append(t[1])
        elif t[0]=="f":
            f.append([int(s.split("/")[0])-1 for s in t[1:]])
            roles.append(material)
    v=np.asarray(v);n=np.asarray(n);f=np.asarray(f,dtype=np.int32)
    if len(v)!=len(n) or len(f)<18000 or len(objects)<30 or f.shape[1]!=3:
        raise ValueError("Incomplete assembled runtime geometry: "+str(path))
    if set(roles)!=set(PALETTE) or not np.isfinite(v).all() or not np.isfinite(n).all():
        raise ValueError("Missing real material/vertex data: "+str(path))
    if not any(s.startswith("Living_crescent_") for s in objects):
        raise ValueError("Missing natural crescent")
    color=np.asarray([PALETTE[m] for m in roles])
    norm=n[f].mean(axis=1)
    norm/=np.maximum(1e-8,np.linalg.norm(norm,axis=1))[:,None]
    light=np.asarray([-.42,.82,.56]);light/=np.linalg.norm(light)
    shade=np.clip(.36+.63*np.maximum(0,norm@light),.30,1)
    rgb=(np.clip(color*shade[:,None],0,1)*255).astype(np.uint8)
    return v,f,rgb,objects,roles

def frame(model,kind,degrees,pitch_degrees,width,height,meters):
    v,f,rgb,_,_=model
    yaw,pitch=math.radians(degrees),math.radians(pitch_degrees)
    c,s=math.cos(yaw),math.sin(yaw)
    x=v[:,0]*c-v[:,2]*s;z=v[:,0]*s+v[:,2]*c
    y=v[:,1]*math.cos(pitch)-z*math.sin(pitch)
    depth=v[:,1]*math.sin(pitch)+z*math.cos(pitch)
    scale=height/(2*meters)
    xy=np.column_stack((width*.5+x*scale,height*.54-y*scale))
    img=draw_mesh_with_depth(Image.new("RGB",(width,height),(22,27,35)),xy,depth,f,rgb)
    d=ImageDraw.Draw(img)
    d.rectangle((0,0,width,41),fill=(14,18,24))
    d.text((9,7),kind+" | "+str(degrees)+"°",fill=(232,222,202))
    d.text((9,25),ROSTER[kind],fill=(157,173,187))
    d.rectangle((0,height-22,width,height),fill=(14,18,24))
    d.text((9,height-16),"ACTUAL ASSEMBLED REST GEOMETRY",fill=(196,173,146))
    return img

def main():
    p=argparse.ArgumentParser();p.add_argument("directory",type=Path)
    out=p.parse_args().directory
    models={k:load(out/(k+"-CompleteRuntime.obj")) for k in ROSTER}
    # Six rows, three matched angles, allowing a single comprehensive art
    # review rather than six isolated PRs. Retain separately scaled tactical.
    metrics={"status":"Actual body, cloth, natural mantle, face and equipment; no Unity motion/PBR or mobile validation",
             "models":{}}
    for kind,(v,f,rgb,objects,roles) in models.items():
        metrics["models"][kind]={"vertices":len(v),"triangles":len(f),
            "actual_parts":len(objects),"bounds":[v.min(axis=0).tolist(),v.max(axis=0).tolist()]}
    for title,angles,pitch,w,h,zoom in [
        ("Detail",(0,45,180),12,390,480,1.14),
        ("Tactical",(45,135),58,390,844,4.0)]:
        sheet=Image.new("RGB",(len(angles)*w,len(models)*h))
        for row,(kind,m) in enumerate(models.items()):
            for col,a in enumerate(angles):
                sheet.paste(frame(m,kind,a,pitch,w,h,zoom),(col*w,row*h))
        sheet.save(out/("Ashbound-Full-Roster-"+title+".png"))
    (out/"full-roster-model-metrics.json").write_text(json.dumps(metrics,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(metrics,indent=2))

if __name__=="__main__":
    main()
