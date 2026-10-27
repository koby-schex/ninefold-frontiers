#!/usr/bin/env python3
"""Real mesh inspections for the two new standard-unit proportion studies."""
import argparse,collections,json,tempfile
from pathlib import Path
import numpy as np
from build_worldfive_roster import BUILDERS,COLORS,FINISH,ROOT,write

OUT=ROOT/'SourceArt/WorldFiveRoster/Review'

def validate(name):
    m=BUILDERS[name]();prefix='Tervan_' if name=='Burdenbeast' else 'Vaskel_'
    expected=8 if name=='Burdenbeast' else 2
    assert len({p['name'] for p in m.parts})==len(m.parts)
    assert sum(p['name'].startswith(prefix+'Limb_') for p in m.parts)==expected
    assert sum(p['name'].startswith(prefix+'Contact_') for p in m.parts)==expected
    for p in m.parts:
        v=np.asarray(p['vertices']);f=np.asarray(p['faces']);tri=v[f]
        assert np.isfinite(v).all()
        area=np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)
        assert (area>1e-8).all(),(name,p['name'],'degenerate')
        assert np.einsum('ij,ij->i',tri[:,0],np.cross(tri[:,1],tri[:,2])).sum()>0,(name,p['name'],'inward')
        edges=collections.Counter(tuple(sorted((a,b))) for face in p['faces'] for a,b in zip(face,face[1:]+face[:1]))
        assert all(c==2 for c in edges.values()),(name,p['name'],'open')
        if p['name'].startswith(prefix+'Contact_'):assert abs(v[:,1].min())<1e-8
    animal=np.array([v for p in m.parts if p['name'].startswith(prefix) for v in p['vertices']])
    avarin=np.array([v for p in m.parts if p['name'].startswith('Avarin_') for v in p['vertices']])
    assert avarin[:,0].min()>animal[:,0].max(),(name,'partner clearance')
    if name=='Burdenbeast':assert 5<np.ptp(animal[:,2])<7
    else:
        assert 1.2<np.ptp(animal[:,1])<1.8
        counter=np.array(next(p['vertices'] for p in m.parts if p['name']=='Vaskel_Counterweight'))
        assert counter[:,1].min()>.4,'Counterweight must not become a third ground contact'
    with tempfile.TemporaryDirectory() as d:
        write(name,Path(d))
        for f in Path(d).iterdir():assert f.read_bytes()==(ROOT/'SourceArt'/name/f.name).read_bytes(),('stale',f.name)
    return dict(unit=name,limbs=expected,closed_parts=len(m.parts),grounded_contacts=True,partner_clearance=True,
        bounds=[animal.min(0).tolist(),animal.max(0).tolist()],limitations='Initial proposed contours, not final art, rig or collision certification')

def main(render_images):
    reports=[validate(name) for name in BUILDERS];OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'checks.json').write_text(json.dumps(reports,indent=2)+'\n');print(json.dumps(reports,indent=2))
    if not render_images:return
    from render_great_strider import render,font
    from PIL import Image,ImageDraw
    sheet=Image.new('RGB',(1440,1160),'#eeede5');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),'WORLD FIVE / NEW STANDARD-UNIT PROPORTION STUDIES 01',font=font(24),fill='#262c32')
    draw.text((20,48),'Actual source geometry. Proposed contours and equipment; no final master fidelity or game integration.',font=font(16),fill='#596069')
    for row,name in enumerate(BUILDERS):
        data=dict(parts=BUILDERS[name]().parts,materials=COLORS,finish=FINISH,opacity={'sail_membrane':.68})
        target=(.4,1.1,0) if name=='Burdenbeast' else (.3,.9,0);extent=8 if name=='Burdenbeast' else 3.5
        for col,(label,eye) in enumerate((('Front',(0,2,10)),('Three-quarter',(7,4,10)),('Tactical',(6,9,9)))):
            sheet.paste(render(data,eye,(480,480),extent,target,False),(col*480,120+row*520))
            draw.text((col*480+12,95+row*520),name+' / '+label,font=font(17),fill='#262c32')
    sheet.save(OUT/'Roster-Studies01.png')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--render',action='store_true');main(parser.parse_args().render)
