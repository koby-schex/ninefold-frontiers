#!/usr/bin/env python3
"""Surface study compared with PR 51; actual geometry, fixed cameras."""
import importlib.util
import json
import subprocess
import tempfile
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from build_great_strider import make_model, write, ROOT
from render_great_strider import render, font

BASELINE='28a1b51cdf87ad7305447f4bbe1169118b4dd573'
OUTPUT=ROOT/'SourceArt/GreatStrider/SurfaceReview'


def load_meshes():
    with tempfile.TemporaryDirectory() as directory:
        temp=Path(directory); script=temp/'BuildTools/Art/baseline.py'; script.parent.mkdir(parents=True)
        script.write_bytes(subprocess.check_output(['git','show',BASELINE+':BuildTools/Art/build_great_strider.py'],cwd=ROOT))
        spec=importlib.util.spec_from_file_location('surface_baseline',script)
        module=importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        module.write(module.make_model(),temp/'before'); write(make_model(),temp/'after')
        return [json.loads((temp/name/'review_mesh.json').read_text()) for name in ('before','after')]


def checks(before,after):
    assert [(p['name'],p['material']) for p in before['parts']]==[(p['name'],p['material']) for p in after['parts']]
    bounds=[]
    for data in (before,after):
        v=np.concatenate([p['vertices'] for p in data['parts']]);bounds.append(np.array([v.min(axis=0),v.max(axis=0)]))
    error=float(np.max(abs(bounds[0]-bounds[1])))
    assert error<.025,('Unexpected envelope change',error)
    # Refining a loft must retain its ring centers, endpoints and profile radii.
    old={p['name']:p for p in before['parts']}
    center_error=0.; frame_centroid_error=0.; refined=0
    for p in after['parts']:
        if 'loft_sides' not in p:continue
        a=np.array(old[p['name']]['vertices']); b=np.array(p['vertices']); sides=p['loft_sides']
        if len(a)==len(b):
            assert np.allclose(a,b,atol=1e-12)
        else:
            rows=(len(b)-2)//sides; old_sides=(len(a)-2)//rows
            ca=a[:-2].reshape(rows,old_sides,3).mean(axis=1); cb=b[:-2].reshape(rows,sides,3).mean(axis=1)
            if p['name'].startswith('Cradle_LoadFrame_'):
                # The existing piecewise vertical frame stretch is nonlinear:
                # denser radial sampling changes its vertex centroid slightly.
                # Endpoints remain exact; limit this sampling effect to 0.1 mm.
                frame_centroid_error=max(frame_centroid_error,float(abs(ca-cb).max()))
            else:
                center_error=max(center_error,float(abs(ca-cb).max()))
            assert np.allclose(a[-2:],b[-2:],atol=1e-12)
            refined+=1
    assert center_error<1e-12 and frame_centroid_error<.0001 and refined>0
    return dict(baseline=BASELINE,parts=len(after['parts']),refined_lofts=refined,
                max_envelope_change_m=error,max_loft_center_change_m=center_error,max_frame_sample_centroid_change_m=frame_centroid_error,
                triangles_before=sum(len(p['faces']) for p in before['parts']),
                triangles_after=sum(len(p['faces']) for p in after['parts']),
                opacity=after['opacity'],scope='Neutral profile and envelope checks; not final art or device performance certification')


def main():
    before,after=load_meshes();report=checks(before,after)
    OUTPUT.mkdir(parents=True,exist_ok=True);(OUTPUT/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2),flush=True)
    # Each comparison uses exactly the same camera, light and framing.
    rows=[('Whole unit',(9,6.5,12),(0,3,0),7.2,'all'),
          ('Head / neck / sails',(6,7,10),(0,5.55,.95),2.3,'all'),
          ('Rider and cradle / cutaway',(6,4,8),(0,4.3,.12),1.8,'rider')]
    sheet=Image.new('RGB',(1200,1740),'#eeede5');d=ImageDraw.Draw(sheet)
    d.text((24,18),'GREAT STRIDER / SURFACE STUDY 06',font=font(27),fill='#262c32')
    d.text((24,57),'Actual mesh / matched cameras / provisional materials / not final game art',font=font(17),fill='#596069')
    for row,(label,eye,target,extent,scope) in enumerate(rows):
        for col,(title,data) in enumerate((('BEFORE / PR 51',before),('AFTER / STUDY 06',after))):
            if scope=='rider':data={**data,'parts':[p for p in data['parts'] if p['name'].startswith(('Avarin_','Cradle_','Waycaster_')) or p['name']=='Ilyth_Torso']}
            tile=render(data,eye,(600,500),extent,target,False)
            sheet.paste(tile,(col*600,130+row*535));d.text((col*600+14,100+row*535),title+' — '+label,font=font(16),fill='#262c32')
    sheet.save(OUTPUT/'comparison.png')
    mobile=Image.new('RGB',(780,844),'#eeede5');d=ImageDraw.Draw(mobile)
    for col,(title,data) in enumerate((('BEFORE',before),('AFTER',after))):
        mobile.paste(render(data,(8,12,10),(390,620),9.5),(col*390,100))
        d.text((col*390+16,30),title+' / 390 px portrait',font=font(18),fill='#262c32')
    d.text((16,775),'Proposed camera / no Unity or device-performance claim',font=font(17),fill='#596069')
    mobile.save(OUTPUT/'mobile.png')

if __name__=='__main__':main()
