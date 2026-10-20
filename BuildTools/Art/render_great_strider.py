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

def mipmaps(values):
    levels=[np.asarray(values,dtype=float)/255]
    while levels[-1].shape[0]>1:
        a=levels[-1]
        levels.append((a[::2,::2]+a[1::2,::2]+a[::2,1::2]+a[1::2,1::2])*.25)
    return levels


def sample_tile(levels, uv, footprint):
    # Trilinear mip filtering keeps fine variation quiet at tactical distance.
    lod=np.clip(np.log2(max(footprint*len(levels[0]),1)),0,len(levels)-1)
    lo=int(lod); hi=min(lo+1,len(levels)-1)
    def bilinear(tile):
        size=len(tile); q=(uv%1)*size
        ix=np.floor(q[...,0]).astype(int); iy=np.floor(q[...,1]).astype(int)
        tx=q[...,0]-ix; ty=q[...,1]-iy
        # PNG rows run down; OBJ V runs up.
        iy=(-iy)%size; ny=(iy-1)%size; nx=(ix+1)%size
        return ((tile[iy,ix]*(1-tx)+tile[iy,nx]*tx)*(1-ty)+
                (tile[ny,ix]*(1-tx)+tile[ny,nx]*tx)*ty)
    return bilinear(levels[lo])*(1-(lod-lo))+bilinear(levels[hi])*(lod-lo)

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
    # Opaque pass, then nearest transparent shell. This avoids counting both
    # sides of a closed thin membrane twice; overlapping transparent layers
    # and refraction require a production renderer. Opaque depth stays authoritative.
    transparent=np.zeros((h,w,3),dtype=np.uint8)
    alpha=np.zeros((h,w)); transparent_depth=np.full((h,w),np.inf)
    texture_levels={name:mipmaps(values) for name,values in data.get('textures',{}).items()}
    ordered=sorted(data['parts'],key=lambda p:data.get('opacity',{}).get(p['material'],1)<1)
    for part in ordered:
        opacity=data.get('opacity',{}).get(part['material'],1)
        v=np.array(part['vertices']); f=np.array(part['faces'],dtype=int)
        normals=np.zeros_like(v)
        raw=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]])
        for corner in range(3): np.add.at(normals,f[:,corner],raw)
        normals=normalize(normals)
        rel=v-target
        points=np.column_stack((w/2+rel@right*scale,h/2-rel@up*scale,(v-eye)@forward))
        rgb=np.array(data['materials'][part['material']])
        exponent, strength = data.get('finish',{}).get(part['material'],(36,.17))
        tex=texture_levels.get(part['material']) if 'uvs' in part else None
        uv_coords=np.asarray(part.get('uvs',[]))
        for face_index,ids in enumerate(f):
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
            if opacity < 1:
                glass_depth=transparent_depth[ymin:ymax+1,xmin:xmax+1]
                mask &= z < glass_depth
            if not mask.any(): continue
            normal=normalize(a[...,None]*n[0]+b[...,None]*n[1]+c[...,None]*n[2])
            # Orient thin surfaces toward the viewer for two-sided review shading.
            facing=normal@(-forward)
            normal=np.where((facing<0)[...,None],-normal,normal)
            diffuse=np.maximum(0,normal@light)
            spec=np.maximum(0,normal@half)**exponent
            color=rgb
            if tex is not None:
                triangle_uv=uv_coords[part['uv_faces'][face_index]]
                uv=a[...,None]*triangle_uv[0]+b[...,None]*triangle_uv[1]+c[...,None]*triangle_uv[2]
                # Derivatives of the affine UV projection, in texture repeats/pixel.
                dax=(p[1,1]-p[2,1])/den; dbx=(p[2,1]-p[0,1])/den
                day=(p[2,0]-p[1,0])/den; dby=(p[0,0]-p[2,0])/den
                du=triangle_uv[0]-triangle_uv[2]; dv=triangle_uv[1]-triangle_uv[2]
                footprint=max(np.linalg.norm(dax*du+dbx*dv),np.linalg.norm(day*du+dby*dv))
                color=rgb*sample_tile(tex,uv,footprint)[...,None]
            shade=(.32+.66*diffuse)[...,None]*color+spec[...,None]*strength
            shade+=np.maximum(0,normal[...,1,None])*.045
            value=(np.clip(shade,0,1)**(1/1.6)*255).astype(np.uint8)
            if opacity < 1:
                transparent[ymin:ymax+1,xmin:xmax+1][mask]=value[mask]
                if 'vertex_opacity' in part:
                    weights=np.asarray(part['vertex_opacity'])[ids]
                    pixel_alpha=a*weights[0]+b*weights[1]+c*weights[2]
                    alpha[ymin:ymax+1,xmin:xmax+1][mask]=pixel_alpha[mask]
                else:
                    alpha[ymin:ymax+1,xmin:xmax+1][mask]=opacity
                glass_depth[mask]=z[mask]
            else:
                pixels[ymin:ymax+1,xmin:xmax+1][mask]=value[mask]
                region[mask]=z[mask]
    pixels=np.rint(pixels*(1-alpha[...,None])+transparent*alpha[...,None]).astype(np.uint8)
    return Image.fromarray(pixels)

def font(size):
    path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    return ImageFont.truetype(path,size) if Path(path).exists() else ImageFont.load_default()

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--source',type=Path,default=ROOT/'SourceArt/GreatStrider')
    args=p.parse_args(); data=json.loads((args.source/'review_mesh.json').read_text())
    sheet=Image.new('RGB',(1600,1120),(238,237,229)); draw=ImageDraw.Draw(sheet)
    draw.text((42,25),'NINEFOLD: FRONTIERS  /  GREAT STRIDER',font=font(30),fill='#262c32')
    draw.text((43,70),'ACTUAL MESH • MATERIAL STUDY 08 • 6.0 m • NOT FINAL GAME ART',font=font(17),fill='#596069')
    views=[((9,6.5,12),(30,125),(840,885),'THREE-QUARTER'),((0,4,15),(886,125),(340,885),'FRONT'),((15,4,0),(1230,125),(340,885),'SIDE')]
    for eye,pos,size,label in views:
        sheet.paste(render(data,eye,size,7.2),pos)
        draw.text((pos[0]+15,1024),label,font=font(18),fill='#333b43')
    draw.text((42,1074),'Editable anatomy, sensory sails, recessed cradle and Avarin operator. Production rig / textures pending. Offline motion studies available.',font=font(17),fill='#596069')
    sheet.save(args.source/'GreatStrider-Review.png')
    detail=render(data,(0,3.6,8),(1100,900),2.35,(0,4.45,.20),False)
    detail.save(args.source/'GreatStrider-Detail.png')
    mobile=Image.new('RGB',(390,844),(238,237,229))
    mobile.paste(render(data,(8,12,10),(390,620),9.5),(0,100))
    md=ImageDraw.Draw(mobile)
    md.text((15,24),'TACTICAL READABILITY STUDY',font=font(19),fill='#262c32')
    md.text((15,55),'390 px portrait / proposed camera',font=font(15),fill='#596069')
    md.text((15,750),'Actual mesh - no Unity/device claim',font=font(14),fill='#596069')
    mobile.save(args.source/'GreatStrider-Mobile.png')
    print(args.source/'GreatStrider-Review.png')

    inspection=Image.new('RGB',(1600,1800),(238,237,229)); d=ImageDraw.Draw(inspection)
    d.text((30,20),'GREAT STRIDER / MATERIAL STUDY 08 / INSPECTION',font=font(27),fill='#262c32')
    d.text((30,60),'Actual mesh. Neutral pose only; deformation and full collision clearance are unverified.',font=font(18),fill='#596069')
    for eye,pos,label in [((0,3,15),(20,110),'FRONT'),((15,3,0),(810,110),'SIDE'),
                          ((0,3,-15),(20,940),'BACK'),((6,-3,7),(810,940),'UNDERSIDE / OBLIQUE')]:
        inspection.paste(render(data,eye,(770,760),9.5 if label.startswith('UNDERSIDE') else 7.5),pos)
        d.text((pos[0]+15,pos[1]+775),label,font=font(21),fill='#262c32')
    inspection.save(args.source/'GreatStrider-Inspection.png')
