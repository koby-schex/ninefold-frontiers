#!/usr/bin/env python3
"""Reproducibility, topology and silhouette review of the actual Olyra study."""
import argparse, collections, json, math, tempfile
from pathlib import Path
from build_living_veil import make_model,write,ROOT,ALPHA,POSES,membrane_alpha,texture_bytes
from build_great_strider import dot,cross,sub

def validate(pose):
    m=make_model(pose);parts={p['name']:p for p in m.parts}
    assert len(parts)==len(m.parts)==97
    assert sum(len(p['faces']) for p in m.parts)==66948
    assert sum(len(p['vertices']) for p in m.parts)==33668
    for i in range(5):
        p=parts[f'Olyra_Lobe_{i}']
        assert len(p['uvs'])==len(p['vertices'])
        assert all(0<=u<=1 and 0<=v<=1 for u,v in p['uvs'])
        assert all(tuple(f)==tuple(uv) for f,uv in zip(p['faces'],p['uv_faces']))
        assert len(p['vertex_opacity'])==len(p['vertices'])
    assert sum(n.startswith('Olyra_Lobe_') for n in parts)==5
    assert sum(n.startswith('Avarin_Arm_') for n in parts)==2
    assert sum(n.startswith('Avarin_Leg_') for n in parts)==2
    assert sum(n.startswith('Avarin_SensoryRibbon_') for n in parts)==6
    assert membrane_alpha(1,.5)<membrane_alpha(.5,.5)<membrane_alpha(0,.5)<1
    assert membrane_alpha(.5,0)<membrane_alpha(.5,.5)
    assert max(abs(membrane_alpha(k/255,.5)-membrane_alpha((k+1)/255,.5)) for k in range(255))<.002
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
    if pose=='resting':
        assert min(v[0] for v in av)>max(v[0] for v in creature)
    else:
        assert 4<max(v[0] for v in creature)-min(v[0] for v in creature)<6
        # Conservative triangle/AABB exclusion around the entire partner/harness.
        partner=[v for p in m.parts if p['name'].startswith(('Avarin_','VeilHarness_')) for v in p['vertices']]
        lo=[min(v[k] for v in partner)-.03 for k in range(3)]
        hi=[max(v[k] for v in partner)+.03 for k in range(3)]
        for p in m.parts:
            if not p['name'].startswith('Olyra_'):continue
            for f in p['faces']:
                triangle=[p['vertices'][i] for i in f]
                assert any(max(v[k] for v in triangle)<lo[k] or min(v[k] for v in triangle)>hi[k] for k in range(3)),('partner clearance',p['name'])
    folder=ROOT/'SourceArt/LivingVeil'
    with tempfile.TemporaryDirectory() as d:
        write(make_model(pose),Path(d),pose)
        names=('LivingVeil.obj','manifest.json') if pose=='resting' else ('LivingVeilProtective.obj','protective-manifest.json')
        for n in (*names,'LivingVeil.mtl','Textures/OlyraMembrane.png'):
            assert (Path(d)/n).read_bytes()==(folder/n).read_bytes(),('stale',n)
    return dict(pose=pose,parts=97,vertices=33668,triangles=66948,lobes=5,closed_parts=True,
        reproducible_export=True,grounded_partner=True,partner_clearance=True,
        limitations='Not a lobe self-intersection, rigging, mobile performance or native transparency certificate')

def main(render_images=False):
    report=[validate(pose) for pose in POSES];print(json.dumps(report,indent=2));out=ROOT/'SourceArt/LivingVeil/Review';out.mkdir(parents=True,exist_ok=True)
    (out/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    if not render_images:return
    from render_great_strider import render,font
    from PIL import Image,ImageDraw
    from build_living_veil import COLORS,ALPHA,FINISH
    sheet=Image.new('RGB',(1400,1200),'#eeede5');draw=ImageDraw.Draw(sheet)
    draw.text((20,16),'LIVING VEIL / OLYRA — CANOPY STUDY 02',font=font(26),fill='#262c32')
    draw.text((20,52),'Actual source geometry • two static poses • provisional surfaces',font=font(18),fill='#596069')
    for row,pose in enumerate(POSES):
        m=make_model(pose);data=dict(parts=m.parts,materials=COLORS,opacity=ALPHA,finish=FINISH)
        for i,(label,eye) in enumerate((('Front',(0,3,10)),('Three-quarter',(7,4,10)),('Tactical',(6,9,9)))):
            # Shared framing across poses makes scale comparisons meaningful.
            sheet.paste(render(data,eye,(460,490),7.1,(.3,1.7,0),False),(i*466,125+row*540))
            draw.text((i*466+15,95+row*540),pose.title()+' / '+label,font=font(18),fill='#262c32')
    sheet.save(out/'LivingVeil-Review.png')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--render',action='store_true');main(p.parse_args().render)
