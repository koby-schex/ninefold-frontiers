#!/usr/bin/env python3
"""Reproducibility, topology and silhouette review of the actual Olyra study."""
import argparse, collections, json, math, tempfile
from pathlib import Path
from build_living_veil import make_model,write,ROOT,ALPHA
from build_great_strider import dot,cross,sub

def validate():
    m=make_model();parts={p['name']:p for p in m.parts}
    assert len(parts)==len(m.parts)==77
    assert sum(len(p['faces']) for p in m.parts)==54488
    assert sum(len(p['vertices']) for p in m.parts)==27398
    for i in range(5):
        assert all(f'Olyra_Lobe_{i}_Band_{j}' in parts for j in range(3))
    assert sum(n.startswith('Olyra_Lobe_') for n in parts)==15
    assert sum(n.startswith('Avarin_Arm_') for n in parts)==2
    assert sum(n.startswith('Avarin_Leg_') for n in parts)==2
    assert sum(n.startswith('Avarin_SensoryRibbon_') for n in parts)==6
    assert ALPHA['olyra_tip']<ALPHA['olyra_edge']<ALPHA['olyra_membrane']<1
    for name,p in parts.items():
        v=p['vertices'];edges=collections.Counter();directions=collections.Counter();volume=0
        assert all(math.isfinite(x) for row in v for x in row),name
        for f in p['faces']:
            assert len(set(f))==3 and all(0<=i<len(v) for i in f),name
            a,b,c=[v[i] for i in f];n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-16,(name,'degenerate')
            volume+=dot(a,cross(b,c))/6
            for a,b in zip(f,f[1:]+f[:1]):edges[tuple(sorted((a,b)))]+=1;directions[a,b]+=1
        assert volume>0,(name,'inward')
        assert all(c==2 for c in edges.values()),(name,'open edge')
        assert all(directions[a,b]==directions[b,a] for a,b in edges),(name,'winding')
    av=[v for p in m.parts if p['name'].startswith('Avarin_') for v in p['vertices']]
    assert abs(min(v[1] for v in av))<1e-8
    assert 1.80<max(v[1] for v in av)<1.85
    # The standing partner stays clear of the resting organism's bounding box.
    creature=[v for p in m.parts if p['name'].startswith('Olyra_') for v in p['vertices']]
    assert min(v[0] for v in av)>max(v[0] for v in creature)
    folder=ROOT/'SourceArt/LivingVeil'
    with tempfile.TemporaryDirectory() as d:
        write(make_model(),Path(d))
        for n in ('LivingVeil.obj','LivingVeil.mtl','manifest.json'):
            assert (Path(d)/n).read_bytes()==(folder/n).read_bytes(),('stale',n)
    return dict(parts=77,vertices=27398,triangles=54488,lobes=5,closed_parts=True,
        reproducible_export=True,grounded_partner=True,resting_partner_clearance=True,
        limitations='Not a self-intersection, rigging, mobile performance or native transparency certificate')

def main(render_images=False):
    report=validate();print(json.dumps(report,indent=2));out=ROOT/'SourceArt/LivingVeil/Review';out.mkdir(parents=True,exist_ok=True)
    (out/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    if not render_images:return
    from render_great_strider import render,font
    from PIL import Image,ImageDraw
    from build_living_veil import COLORS,ALPHA,FINISH
    m=make_model();data=dict(parts=m.parts,materials=COLORS,opacity=ALPHA,finish=FINISH)
    sheet=Image.new('RGB',(1400,900),'#eeede5');draw=ImageDraw.Draw(sheet)
    draw.text((20,16),'LIVING VEIL / OLYRA — RESTING FORM STUDY 01',font=font(26),fill='#262c32')
    draw.text((20,52),'Actual source mesh • folded pose • provisional materials • not final art',font=font(18),fill='#596069')
    for i,(label,eye,target,extent) in enumerate((('Front / partner',(0,2.5,10),(.57,1.5,0),4.5),('Three-quarter',(7,4,10),(.57,1.5,0),4.8),('Tactical silhouette',(6,9,9),(.57,1.5,0),5.2))):
        sheet.paste(render(data,eye,(460,750),extent,target,True),(i*466,115))
        draw.text((i*466+15,88),label,font=font(18),fill='#262c32')
    sheet.save(out/'LivingVeil-Review.png')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--render',action='store_true');main(p.parse_args().render)
