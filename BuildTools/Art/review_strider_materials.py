#!/usr/bin/env python3
"""PR 53 geometry-preserving material comparison and export checks."""
import json
import tempfile
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from build_great_strider import ROOT, write, make_model
from review_strider_surface import load_meshes
from render_great_strider import render, font, mipmaps, sample_tile
from strider_materials import tiles, SURFACES, albedo_pixels
from move_great_strider import Study

BASELINE='906f8a15572466d7ec4560da77ff8bf09aed67ce'  # PR 53 merge; fixed historical source, never mutable main.
OUTPUT=ROOT/'SourceArt/GreatStrider/MaterialReview'


def checks(before,after):
    assert len(before['parts'])==len(after['parts'])
    for a,b in zip(before['parts'],after['parts']):
        for key in ('name','material','vertices','faces'):
            assert a[key]==b[key],('Geometry changed',a['name'],key)
    assert before['opacity']==after['opacity'], 'No membrane opacity revision in this pass'
    textured=0
    for p in after['parts']:
        if p['material'] not in SURFACES: continue
        uv=np.asarray(p['uvs']); ids=np.asarray(p['uv_faces'])
        assert ids.shape==np.asarray(p['faces']).shape and np.isfinite(uv).all()
        assert ids.min()>=0 and ids.max()<len(uv)
        textured+=1
    source=ROOT/'SourceArt/GreatStrider'
    with tempfile.TemporaryDirectory() as temp:
        write(make_model(),Path(temp))
        for name,values in tiles().items():
            file=Path('Textures')/(name+'.png')
            assert (Path(temp)/file).read_bytes()==(source/file).read_bytes()
            assert np.array_equal(np.array(Image.open(source/file)),albedo_pixels(values,after['materials'][name]))
            amplitude=SURFACES[name][0]
            assert min(map(min,values))>=255*(1-amplitude)-1 and max(map(max,values))<=255
            # Distant filtering must converge to the mean, independent of UV or camera.
            samples=sample_tile(mipmaps(values),np.array([[0,0],[.37,.83],[-3.5,9.2]]),2)
            assert np.allclose(samples,np.mean(values)/255)
    vertices=uvs=faces=0
    for line in (source/'GreatStrider.obj').read_text().splitlines():
        f=line.split()
        if not f: continue
        if f[0]=='v': vertices+=1
        elif f[0]=='vt': uvs+=1
        elif f[0]=='f':
            faces+=1
            for corner in f[1:]:
                ids=corner.split('/')
                assert 1<=int(ids[0])<=vertices
                if len(ids)>1: assert 1<=int(ids[1])<=uvs
    study=Study()
    for time in (0,.7,1.6):
        pose=study.pose(time)
        for rest,posed in zip(study.mesh.parts,pose['parts']):
            assert rest.get('uvs')==posed.get('uvs') and rest.get('uv_faces')==posed.get('uv_faces')
        assert pose['textures']==tiles()
    return dict(baseline=BASELINE,parts=len(after['parts']),vertices=vertices,triangles=faces,
                textured_parts=textured,uv_coordinates=uvs,textures=len(tiles()),
                geometry_identical=True,opacity_unchanged=True,texture_bytes_reproducible=True,
                rest_uvs_retained_in_sampled_motion=True,mip_filter_converges_to_mean=True,
                scope='Source texture/UV review, not production unwrap or Unity/device certification')


def main():
    before,after=load_meshes(BASELINE,None)
    report=checks(before,after);OUTPUT.mkdir(parents=True,exist_ok=True)
    (OUTPUT/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2),flush=True)
    rows=[('Whole unit',(9,6.5,12),(0,3,0),7.2,False),
          ('Skin and grown reinforcement',(6,7,10),(0,5.35,.8),2.4,False),
          ('Rider / cradle cutaway',(6,4,8),(0,4.3,.12),1.8,True)]
    sheet=Image.new('RGB',(1200,1740),'#eeede5');d=ImageDraw.Draw(sheet)
    d.text((24,18),'GREAT STRIDER / MATERIAL STUDY 08',font=font(27),fill='#262c32')
    d.text((24,57),'Actual geometry and texture maps / matched lighting / provisional surfaces',font=font(17),fill='#596069')
    for row,(label,eye,target,extent,cutaway) in enumerate(rows):
        for col,(title,data) in enumerate((('BEFORE / PR 53',before),('AFTER / STUDY 08',after))):
            if cutaway:data={**data,'parts':[p for p in data['parts'] if p['name'].startswith(('Avarin_','Cradle_','Waycaster_')) or p['name']=='Ilyth_Torso']}
            sheet.paste(render(data,eye,(600,500),extent,target,False),(col*600,130+row*535))
            d.text((col*600+14,100+row*535),title+' - '+label,font=font(15),fill='#262c32')
    sheet.save(OUTPUT/'comparison.png')
    mobile=Image.new('RGB',(780,844),'#eeede5');d=ImageDraw.Draw(mobile)
    for col,(title,data) in enumerate((('BEFORE',before),('AFTER',after))):
        mobile.paste(render(data,(8,12,10),(390,620),9.5),(col*390,100))
        d.text((col*390+16,30),title+' / 390 px portrait',font=font(18),fill='#262c32')
    d.text((16,775),'Proposed tactical camera / not a Unity screenshot',font=font(17),fill='#596069')
    mobile.save(OUTPUT/'mobile.png')

if __name__=='__main__': main()
