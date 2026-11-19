"""Exact shared battlefield meshes, not native Unity screenshots.

Renders the actual C# triangle batches with per-face material swatches,
directional lighting, and depth. No unit art, shader, PBR or frame-time claim.
"""
import argparse
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from mesh_depth_raster import draw_mesh_with_depth

def frame(scene,w,h,tactical=True):
    a=np.asarray(scene["bounds"]["min"]);b=np.asarray(scene["bounds"]["max"])
    center=(a+b)/2;center[1]=.6
    tilt=math.radians(58 if tactical else 38)
    yaw=0 if tactical else math.radians(20)
    scale=min((w-32)/(b[0]-a[0]),(h-84)/((b[2]-a[2])*math.sin(tilt)+2.5))
    verts=[];faces=[];colors=[];count=0
    light=np.asarray([-.47,.84,.38]);light/=np.linalg.norm(light)
    for mesh in scene["meshes"]:
        v=np.asarray(mesh["vertices"]);f=np.arange(len(v)).reshape(-1,3)
        p=v[f]
        normal=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0])
        normal/=np.maximum(np.linalg.norm(normal,axis=1,keepdims=True),1e-7)
        shade=.50+.50*np.maximum(0,normal@light)
        color=np.asarray(scene["palette"][mesh["material"]])
        colors.extend((np.clip(color*shade[:,None],0,1)*255).astype(np.uint8))
        verts.extend(v);faces.extend(f+count);count+=len(v)
    v=np.asarray(verts)-center
    x=v[:,0]*math.cos(yaw)-v[:,2]*math.sin(yaw)
    z=v[:,0]*math.sin(yaw)+v[:,2]*math.cos(yaw)
    # Camera at the near/south edge looking down: y positive is closer,
    # far/north (+z) farther. Far scenery cannot cover the walkable plane
    # except from its actual perspective projection.
    vertical=v[:,1]*math.cos(tilt)+z*math.sin(tilt)
    depth=v[:,1]*math.sin(tilt)-z*math.cos(tilt)
    xy=np.column_stack((w*.5+x*scale,h*.56-vertical*scale))
    bg=Image.new("RGB",(w,h),tuple(int(c*255) for c in scene["sky"]))
    img=draw_mesh_with_depth(bg,xy,depth,np.asarray(faces),np.asarray(colors))
    d=ImageDraw.Draw(img)
    d.rectangle((0,0,w,40),fill=(18,20,29))
    d.text((8,7),scene["label"]+" | "+scene["mood"],fill=(238,230,218))
    d.text((8,23),"ACTUAL TERRAIN GEOMETRY / SOFTWARE SHADE",fill=(151,163,178))
    return img

def main():
    p=argparse.ArgumentParser();p.add_argument("directory",type=Path);args=p.parse_args()
    manifest=json.loads((args.directory/"manifest.json").read_text())
    scenes=[json.loads((args.directory/m["file"]).read_text()) for m in manifest]
    # Separate opening and continuation sheets keep both reviews readable.
    w,h=350,440
    story_scenes=[s for s in scenes if not s["label"].startswith("quick-")]
    for label,group in [("World-Five-14-Battlefield-Roster",story_scenes[:14]),
                        ("World-Five-15-34-Battlefield-Roster",story_scenes[14:])]:
        if not group:continue
        story=Image.new("RGB",(w*4,h*((len(group)+3)//4)),(18,20,29))
        for i,s in enumerate(group):story.paste(frame(s,w,h),(i%4*w,i//4*h))
        story.save(args.directory/(label+".png"))
    quick=Image.new("RGB",(w*4,h),(18,20,29))
    for i,s in enumerate(s for s in scenes if s["label"].startswith("quick-")):quick.paste(frame(s,w,h),(i*w,0))
    quick.save(args.directory/"Quick-Battle-4-Seeded-Arenas.png")
    moods=Image.new("RGB",(500*3,580*2),(18,20,29))
    seen=set();i=0
    for s in scenes:
        if s["mood"] in seen:continue
        seen.add(s["mood"]);moods.paste(frame(s,500,580,False),(i%3*500,i//3*580));i+=1
    moods.save(args.directory/"Battlefield-Environment-Moods.png")
    print("Rendered",len(scenes),"actual resolved map surfaces and",len(seen),"moods.")
if __name__=="__main__":main()
