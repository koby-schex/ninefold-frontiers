#!/usr/bin/env python3
"""Bounded study 11 refinement: actual source before/after, not concept art."""
import argparse
import json
import numpy as np
from PIL import Image, ImageDraw
from build_great_strider import ROOT
from review_strider_surface import load_meshes
from review_strider_connections import inside, cap_points
from render_great_strider import render, font

BASELINE='85525b330055ff03cf35f8d977140eb4a7b9706f'  # merged review 10; source material study 08
OUTPUT=ROOT/'SourceArt/GreatStrider/DetailReview'


def checks(before,after):
    old={p['name']:p for p in before['parts']}; new={p['name']:p for p in after['parts']}
    assert old.keys()==new.keys(), 'No new anatomical or equipment parts'
    changed={}
    allowed=('Ilyth_Limb_', 'Ilyth_GrownSheath_', 'Avarin_Arm_', 'Avarin_Leg_', 'Cradle_SidePanel_', 'Cradle_PanelSeam_')
    for name,a in old.items():
        b=new[name]
        assert a['faces']==b['faces'] and a['material']==b['material'], name
        delta=float(np.max(np.linalg.norm(np.array(a['vertices'])-np.array(b['vertices']),axis=1)))
        if delta>1e-12:
            assert name.startswith(allowed) or name in ('Ilyth_ShortNeck','Avarin_Torso'),name
            assert delta<.025,(name,delta)
            changed[name]=delta
        if 'uvs' in b:
            assert np.isfinite(b['uvs']).all()
            assert len(b['uv_faces'])==len(b['faces'])
    assert len(changed)>=10, 'Expected actual surface changes'
    a=np.concatenate([p['vertices'] for p in before['parts']]); b=np.concatenate([p['vertices'] for p in after['parts']])
    assert np.allclose([a.min(0),a.max(0)],[b.min(0),b.max(0)],atol=1e-12)
    for key in ('L_Fore','R_Fore','L_Rear','R_Rear'):
        name='Ilyth_Limb_'+key
        assert all(inside(v,new['Ilyth_Torso']) for v in cap_points(new[name])),name
        assert old[name]['vertices'][6*32:]==new[name]['vertices'][6*32:],name
    for end,host in ((False,'Ilyth_Torso'),(True,'Ilyth_CompactHead')):
        assert all(inside(v,new[host]) for v in cap_points(new['Ilyth_ShortNeck'],end)),host
    assert before['materials']==after['materials'] and before['opacity']==after['opacity']
    assert before['textures']==after['textures'], 'Palette/texture pattern not revised'
    assert after['finish']['cloth'][0]<after['finish']['living_pearl'][0]<after['finish']['grown_mineral'][0]
    return dict(baseline=BASELINE,changed_parts=changed,parts=len(new),triangles=sum(len(p['faces']) for p in new.values()),
        unchanged_envelope=True,unchanged_topology=True,buried_root_caps=True,
        unchanged_palette_and_membrane_opacity=True,
        limitations='Separate overlapping source meshes; not welded topology, final sculpt, native Unity verification or canon approval')


def main(check_only=False):
    before,after=load_meshes(BASELINE,None)
    report=checks(before,after); OUTPUT.mkdir(parents=True,exist_ok=True)
    (OUTPUT/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2),flush=True)
    if check_only:return
    rows=[('Organic transition',(7,6,10),(0,5.05,.6),2.5,'body'),
          ('Cradle / operator cutaway',(6,4,8),(0,4.25,.12),1.8,'rider'),
          ('Mobile silhouette',(8,12,10),(0,3,0),9.5,'all')]
    sheet=Image.new('RGB',(1000,1540),'#eeede5');draw=ImageDraw.Draw(sheet)
    draw.text((20,16),'GREAT STRIDER / DETAIL STUDY 11',font=font(26),fill='#262c32')
    draw.text((20,54),'Actual mesh / matched lighting / surface proposals, not locked canon',font=font(16),fill='#596069')
    for row,(label,eye,target,extent,scope) in enumerate(rows):
        for col,(title,data) in enumerate((('BEFORE / STUDY 08',before),('AFTER / STUDY 11',after))):
            if scope=='rider':data={**data,'parts':[p for p in data['parts'] if p['name'].startswith(('Avarin_','Cradle_','Waycaster_'))]}
            if scope=='body':data={**data,'parts':[p for p in data['parts'] if p['name'].startswith('Ilyth_')]}
            sheet.paste(render(data,eye,(500,440),extent,target,False),(col*500,120+row*470))
            draw.text((col*500+12,92+row*470),title+' / '+label,font=font(14),fill='#262c32')
    sheet.save(OUTPUT/'comparison.png')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--check-only',action='store_true')
    main(parser.parse_args().check_only)
