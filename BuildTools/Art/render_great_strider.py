#!/usr/bin/env python3
"""Deterministic software review renderer for the actual authored mesh.

Requires numpy and Pillow. No image generation, hidden model or network calls.
"""
import argparse
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT=Path(__file__).resolve().parents[2]

def normalize(v): return v/np.maximum(np.linalg.norm(v,axis=-1,keepdims=True),1e-12)

def render(data, eye, size=(880,1000), extent=7.0, target=(0,3,0), fit=True):
    w,h=size
    target=np.array(target,dtype=float); eye=np.array(eye,dtype=float)
    forward=normalize(target-eye); right=normalize(np.cross(forward,[0,1,0])); up=np.cross(right,forward)
    vertices=np.array([v for p in data['parts'] for v in p['vertices']])
    horizontal=(vertices-target)@right
    scale=min(h/extent,(w-36)/max(np.ptp(horizontal),1)) if fit else h/extent
    image=Image.new('RGB',size,(224,224,216))
    # Neutral studio ground and soft contact shadow; geometry uses a depth buffer.
    shadow=Image.new('RGBA',size); dr=ImageDraw.Draw(shadow)
    ground=int(h/2+target[1]*scale*up[1])
    dr.ellipse((w*.18,ground-27,w*.82,ground+35),fill=(35,38,41,85))
    image=Image.alpha_composite(image.convert('RGBA'),shadow.filter(ImageFilter.GaussianBlur(22)))
    pixels=np.array(image)[:,:,:3].copy(); depth=np.full((h,w),np.inf)
    light=normalize(np.array([-.6,1,.7])); half=normalize(light-forward)
    for part in data['parts']:
        v=np.array(part['vertices']); f=np.array(part['faces'],dtype=int)
        normals=np.zeros_like(v)
        raw=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]])
        for corner in range(3): np.add.at(normals,f[:,corner],raw)
        normals=normalize(normals)
        rel=v-target
        points=np.column_stack((w/2+rel@right*scale,h/2-rel@up*scale,(v-eye)@forward))
        rgb=np.array(data['materials'][part['material']])
        for ids in f:
            p=points[ids]; n=normals[ids]
            xmin=max(0,int(np.floor(p[:,0].min()))); xmax=min(w-1,int(np.ceil(p[:,0].max())))
            ymin=max(0,int(np.floor(p[:,1].min()))); ymax=min(h-1,int(np.ceil(p[:,1].max())))
            if xmin>xmax or ymin>ymax: continue
            x,y=np.meshgrid(np.arange(xmin,xmax+1)+.5,np.arange(ymin,ymax+1)+.5)
            den=(p[1,1]-p[2,1])*(p[0,0]-p[2,0])+(p[2,0]-p[1,0])*(p[0,1]-p[2,1])
            if abs(den)<1e-9: continue
            a=((p[1,1]-p[2,1])*(x-p[2,0])+(p[2,0]-p[1,0])*(y-p[2,1]))/den
            b=((p[2,1]-p[0,1])*(x-p[2,0])+(p[0,0]-p[2,0])*(y-p[2,1]))/den
            c=1-a-b; z=a*p[0,2]+b*p[1,2]+c*p[2,2]
            region=depth[ymin:ymax+1,xmin:xmax+1]
            mask=(a>=0)&(b>=0)&(c>=0)&(z<region)
            if not mask.any(): continue
            normal=normalize(a[...,None]*n[0]+b[...,None]*n[1]+c[...,None]*n[2])
            # Orient thin surfaces toward the viewer for two-sided review shading.
            facing=normal@(-forward)
            normal=np.where((facing<0)[...,None],-normal,normal)
            diffuse=np.maximum(0,normal@light)
            spec=np.maximum(0,normal@half)**36
            shade=(.32+.66*diffuse)[...,None]*rgb+spec[...,None]*.17
            shade+=np.maximum(0,normal[...,1,None])*.045
            value=(np.clip(shade,0,1)**(1/1.6)*255).astype(np.uint8)
            pixels[ymin:ymax+1,xmin:xmax+1][mask]=value[mask]
            region[mask]=z[mask]
    return Image.fromarray(pixels)

def font(size):
    path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    return ImageFont.truetype(path,size) if Path(path).exists() else ImageFont.load_default()

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--source',type=Path,default=ROOT/'SourceArt/GreatStrider')
    args=p.parse_args(); data=json.loads((args.source/'review_mesh.json').read_text())
    sheet=Image.new('RGB',(1600,1120),(238,237,229)); draw=ImageDraw.Draw(sheet)
    draw.text((42,25),'NINEFOLD: FRONTIERS  /  GREAT STRIDER',font=font(30),fill='#262c32')
    draw.text((43,70),'ACTUAL MESH • FORM STUDY 02 • 6.0 m • NOT FINAL GAME ART',font=font(17),fill='#596069')
    views=[((9,6.5,12),(30,125),(840,885),'THREE-QUARTER'),((0,4,15),(886,125),(340,885),'FRONT'),((15,4,0),(1230,125),(340,885),'SIDE')]
    for eye,pos,size,label in views:
        sheet.paste(render(data,eye,size,7.2),pos)
        draw.text((pos[0]+15,1024),label,font=font(18),fill='#333b43')
    draw.text((42,1074),'Editable anatomy, sensory sails, recessed cradle and Avarin operator. Rig / textures / animation pending.',font=font(17),fill='#596069')
    sheet.save(args.source/'GreatStrider-Review.png')
    detail=render(data,(0,4.4,8),(1100,900),2.35,(0,4.45,.20),False)
    detail.save(args.source/'GreatStrider-Detail.png')
    mobile=Image.new('RGB',(390,844),(238,237,229))
    mobile.paste(render(data,(8,12,10),(390,620),9.5),(0,100))
    md=ImageDraw.Draw(mobile)
    md.text((15,24),'TACTICAL READABILITY STUDY',font=font(19),fill='#262c32')
    md.text((15,55),'390 px portrait / proposed camera',font=font(15),fill='#596069')
    md.text((15,750),'Actual mesh - no Unity/device claim',font=font(14),fill='#596069')
    mobile.save(args.source/'GreatStrider-Mobile.png')
    print(args.source/'GreatStrider-Review.png')
