"""Compare exact shared runtime source geometry and software-posed instances."""
import argparse
import math
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from mesh_depth_raster import draw_mesh_with_depth

MODELS=["GreatStrider","LivingVeil","Riftwalker","Burdenbeast"]
LABELS=["Great Strider / Ilyth + Avarin","Living Veil / Olyra + Avarin",
        "Riftwalker / legless Vaskel + Avarin","Burdenbeast / Tervan + Avarin"]

def palette(root,kind):
    folder=root/kind
    data={}
    material=None
    for line in (folder/(kind+".mtl.bytes")).read_text().splitlines():
        t=line.split()
        if not t:continue
        if t[0]=="newmtl":
            material=t[1]; data[material]={"color":np.ones(3),"alpha":1.}
        elif t[0]=="Kd":data[material]["color"]=np.asarray([float(v) for v in t[1:4]])
        elif t[0]=="d":data[material]["alpha"]=float(t[1])
        elif t[0]=="map_Kd":
            with Image.open(folder/(t[1]+".bytes")) as im:
                data[material]["texture"]=np.asarray(im.convert("RGBA"))/255.
    return data

def load(path,pal):
    vertices,normals,uvs,faces,mats=[],[],[],[],[]
    mat=None
    for line in path.read_text().splitlines():
        t=line.split()
        if not t:continue
        if t[0]=="v":vertices.append([float(x) for x in t[1:4]])
        elif t[0]=="vn":normals.append([float(x) for x in t[1:4]])
        elif t[0]=="vt":uvs.append([float(x) for x in t[1:3]])
        elif t[0]=="usemtl":mat=t[1]
        elif t[0]=="f":
            faces.append([int(i.split("/")[0])-1 for i in t[1:]])
            mats.append(mat)
    v=np.asarray(vertices);n=np.asarray(normals);f=np.asarray(faces,dtype=np.int32)
    if len(v)!=len(n) or len(v)!=len(uvs) or f.shape[1]!=3 or len(f)<50000:
        raise ValueError("Incomplete actual source geometry")
    if not np.isfinite(v).all() or not np.isfinite(n).all():raise ValueError("Non-finite geometry")
    coords=np.asarray(uvs)[f].mean(axis=1)
    mat=np.asarray(mats);rgb=np.zeros((len(f),3));alpha=np.ones(len(f))
    for name,data in pal.items():
        mask=mat==name
        if not mask.any():continue
        rgb[mask]=data["color"];alpha[mask]=data["alpha"]
        if "texture" in data:
            tex=data["texture"]
            x=(coords[mask,0]%1*tex.shape[1]).astype(int)
            y=((1-coords[mask,1])%1*tex.shape[0]).astype(int)
            rgba=tex[y,x]
            rgb[mask]*=rgba[:,:3]
            # Only the Olyra RGBA tile overrides the alpha scalar.
            if name=="olyra_membrane":alpha[mask]=rgba[:,3]
    n=n[f].mean(axis=1)
    n/=np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-9)
    light=np.asarray([-.45,.86,.52]);light/=np.linalg.norm(light)
    shade=.40+.60*np.maximum(0,n@light)
    color=(np.clip(rgb*shade[:,None]+.025,0,1)**.75*255).astype(np.uint8)
    return v,f,color,alpha

def frame(m,angle,tilt,w,h,extent,title):
    v,f,rgb,alpha=m
    y=v[:,1]-.58
    yaw,pitch=map(math.radians,(angle,tilt))
    x=v[:,0]*math.cos(yaw)-v[:,2]*math.sin(yaw)
    z=v[:,0]*math.sin(yaw)+v[:,2]*math.cos(yaw)
    vert=y*math.cos(pitch)-z*math.sin(pitch)
    depth=y*math.sin(pitch)+z*math.cos(pitch)
    scale=h/extent
    xy=np.column_stack((w*.5+x*scale,h*.54-vert*scale))
    # The reference inspection uses exact UV centroid colors and the nearest
    # translucent surface approximation. Explicitly not native URP shading.
    opaque=alpha>=.999
    bg=Image.new("RGB",(w,h),(25,30,39))
    base,zbase=draw_mesh_with_depth(bg,xy,depth,f[opaque],rgb[opaque],return_depth=True)
    glass,zglass=draw_mesh_with_depth(Image.new("RGB",(w,h)),xy,depth,
        f[~opaque],rgb[~opaque],return_depth=True)
    a=(np.clip(alpha[~opaque],0,1)*255).astype(np.uint8)
    amap=draw_mesh_with_depth(Image.new("RGB",(w,h)),xy,depth,
        f[~opaque],np.repeat(a[:,None],3,axis=1))
    blend=np.asarray(amap,dtype=float)[:,:,0]/255
    blend*=zglass>zbase
    img=Image.fromarray((np.asarray(base)*(1-blend[:,:,None])+
        np.asarray(glass)*blend[:,:,None]).astype(np.uint8))
    draw=ImageDraw.Draw(img)
    draw.rectangle((0,0,w,38),fill=(15,19,26))
    draw.text((10,8),title,fill=(232,225,210))
    draw.text((10,23),"EXACT PACKED GEOMETRY - SOFTWARE REVIEW",fill=(150,166,182))
    return img

def main():
    p=argparse.ArgumentParser()
    p.add_argument("source",type=Path);p.add_argument("output",type=Path)
    args=p.parse_args()
    w,h=430,450
    sheet=Image.new("RGB",(3*w,4*h),(25,30,39))
    tactical=Image.new("RGB",(2*390,4*600),(25,30,39))
    for row,(kind,label) in enumerate(zip(MODELS,LABELS)):
        pal=palette(args.source,kind)
        rest=load(args.output/(kind+"-rest.obj"),pal)
        motion=load(args.output/(kind+"-motion.obj"),pal)
        state=load(args.output/(kind+"-protective.obj"),pal) if kind=="LivingVeil" else motion
        for col,(m,angle,label2) in enumerate([(rest,35,"REST"),(rest,145,"BACK"),(state,35,
                 "PROTECTIVE" if kind=="LivingVeil" else "MOVEMENT")]):
            sheet.paste(frame(m,angle,13,w,h,1.95,label+" / "+label2),(col*w,row*h))
        tactical.paste(frame(rest,35,58,390,600,4.7,label+" / REST"),(0,row*600))
        tactical.paste(frame(state,35,58,390,600,4.7,label+" / "+("PROTECTIVE" if kind=="LivingVeil" else "MOTION")),(390,row*600))
    sheet.save(args.output/"Playable-Bond-Roster-Detail.png")
    tactical.save(args.output/"Playable-Bond-Roster-Tactical.png")
    print((args.output/"playable-bond-metrics.json").read_text())
if __name__=="__main__":main()
