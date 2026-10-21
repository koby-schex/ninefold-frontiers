#!/usr/bin/env python3
"""Reproducibility, topology and silhouette review of the actual Olyra study."""
import argparse, collections, json, math, tempfile
from functools import lru_cache
from pathlib import Path
from build_living_veil import make_model,write,ROOT,ALPHA,POSES,membrane_alpha,texture_bytes
from build_great_strider import dot,cross,sub

def validate(pose):
    m=make_model(pose);parts={p['name']:p for p in m.parts}
    assert len(parts)==len(m.parts)==122
    assert sum(len(p['faces']) for p in m.parts)==89208
    assert sum(len(p['vertices']) for p in m.parts)==44848
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
    assert sum(n.startswith('Avarin_FrontDrape_') for n in parts)==2
    assert sum(n.startswith('Avarin_OuterDrape_') for n in parts)==2
    assert sum(n.startswith('VeilHarness_ClipLoop_') for n in parts)==2
    assert sum(n.startswith('VeilHarness_LineCoil_') for n in parts)==2
    # Cloth never reaches the ground or masks the split between the feet.
    for name,p in parts.items():
        if name.startswith(('Avarin_FrontDrape_','Avarin_OuterDrape_')):
            assert min(v[1] for v in p['vertices'])>.18,(name,'ground clearance')
        if name.startswith('Avarin_FrontDrape_'):
            center=1.95 if pose=='resting' else .25
            assert all(abs(v[0]-center)>.025 for v in p['vertices']),(name,'split hem clearance')
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
    return dict(pose=pose,parts=122,vertices=44848,triangles=89208,lobes=5,closed_parts=True,
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
    draw.text((20,16),'LIVING VEIL / OLYRA — PARTNER STUDY 05',font=font(26),fill='#262c32')
    draw.text((20,52),'Actual source geometry • two static poses • provisional surfaces',font=font(18),fill='#596069')
    for row,pose in enumerate(POSES):
        m=make_model(pose);data=review_data(m)
        for i,(label,eye) in enumerate((('Front',(0,3,10)),('Three-quarter',(7,4,10)),('Tactical',(6,9,9)))):
            # Shared framing across poses makes scale comparisons meaningful.
            sheet.paste(render(data,eye,(460,490),7.1,(.3,1.7,0),False),(i*466,125+row*540))
            draw.text((i*466+15,95+row*540),pose.title()+' / '+label,font=font(18),fill='#262c32')
    sheet.save(out/'LivingVeil-Review.png')
    details=Image.new('RGB',(1400,640),'#eeede5');labels=ImageDraw.Draw(details)
    labels.text((20,12),'PARTNER 05 / ACTUAL MESH — DETAIL AND TACTICAL READ',font=font(23),fill='#262c32')
    m=make_model('resting')
    for i,(label,prefix,eye,target,extent) in enumerate((
        ('Core and membrane roots','Olyra_',(3,3.6,6),(0,2.7,0),1.7),
        ('Avarin and safety harness',('Avarin_','VeilHarness_'),(3,2,8),(1.95,.95,.45),2.3),
        ('Protective / tactical scale',None,(6,9,9),(.3,1.7,0),7.1))):
        import copy
        chosen=copy.deepcopy(m if prefix else make_model('protective'))
        if prefix:chosen.parts=[p for p in chosen.parts if p['name'].startswith(prefix)]
        details.paste(render(review_data(chosen),eye,(460,540),extent,target,False),(i*466,85))
        labels.text((i*466+12,55),label,font=font(18),fill='#262c32')
    details.save(out/'LivingVeil-Detail05.png')
    render_partner(out)

def render_partner(out):
    from render_great_strider import render,font
    from PIL import Image,ImageDraw
    m=make_model('resting');m.parts=[p for p in m.parts if not p['name'].startswith('Olyra_')]
    sheet=Image.new('RGB',(1440,560),'#eeede5');labels=ImageDraw.Draw(sheet)
    labels.text((16,10),'AVARIN PARTNER 05 / ACTUAL GEOMETRY / PROVISIONAL ART',font=font(20),fill='#262c32')
    views=(('Front',(1.95,1.1,8),(1.95,.95,.43),2.2),
           ('Side',(8,1.1,.43),(1.95,.95,.43),2.2),
           ('Back',(1.95,1.1,-8),(1.95,.95,.43),2.2),
           ('Face and living mantle',(2.3,1.75,6),(1.95,1.62,.43),.52))
    for i,(label,eye,target,extent) in enumerate(views):
        sheet.paste(render(review_data(m),eye,(360,490),extent,target,False),(360*i,65))
        labels.text((360*i+12,40),label,font=font(16),fill='#262c32')
    sheet.save(out/'Avarin-Partner05.png')

@lru_cache(maxsize=1)
def membrane_review_tint():
    import io
    import numpy as np
    from PIL import Image
    from build_living_veil import COLORS
    tile=np.asarray(Image.open(io.BytesIO(texture_bytes())),dtype=float)[:,:,0]/255
    linear=np.where(tile<=.04045,tile/12.92,((tile+.055)/1.055)**2.4)
    return linear/COLORS['olyra_membrane'][0]*255

def review_data(m):
    """Read the authored PNG; renderer receives its decoded linear tint variation.

    The shared software renderer supports scalar tint and vertex alpha, not URP
    multilayer compositing. Alpha is sampled at mesh vertices by the generator.
    """
    from build_living_veil import COLORS,FINISH
    parts=[{**p,'uv_faces':[list(f) for f in p['uv_faces']]} if 'uv_faces' in p else p for p in m.parts]
    return dict(parts=parts,materials=COLORS,opacity=ALPHA,finish=FINISH,
        textures={'olyra_membrane':membrane_review_tint()})
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--render',action='store_true');main(p.parse_args().render)
