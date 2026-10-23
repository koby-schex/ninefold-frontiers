#!/usr/bin/env python3
"""Sampled motion safety and actual-mesh playback evidence; no Unity required."""
import argparse, json, math
from pathlib import Path
import numpy as np
from living_veil_motion import CLIPS, sources, sample, vertex, model
from build_living_veil import ROOT,COLORS,ALPHA,FINISH
OUT=ROOT/'SourceArt/LivingVeil/Review'
FIXTURE=ROOT/'SourceArt/LivingVeil/motion-study.json'

def fixture():
    r,p=sources();probes=[]
    for a,b in zip(r.parts,p.parts):
        if a['name'] in ('Olyra_Core','Olyra_Lobe_0','Olyra_Rib_2_1','Avarin_Foot_L','VeilHarness_Regulator'):
            index=len(a['vertices'])//2
            probes.append(dict(part=a['name'],rest=a['vertices'][index],shelter=b['vertices'][index]))
    samples=[]
    for clip in CLIPS:
        for t in (0,.1,.2,.425,.65,.75,.85,.925,1):
            frame=sample(clip,t)
            samples.append(dict(clip=clip,time=t,frame=frame,vertices=[vertex(q['part'],q['rest'],q['shelter'],frame) for q in probes]))
    return dict(revision='glide study 08',model_id='world05.living-veil.olyra',
        note='Provisional editor motion, not production rig or gameplay timings. Avarin stays fixed in resting location.',
        duration_seconds=dict(RestingIdle=4,Deploy=3.6,ProtectiveIdle=4,Retract=4.1),probes=probes,samples=samples)

def validate():
    assert json.loads(FIXTURE.read_text())==json.loads(json.dumps(fixture())), 'Stale motion reference'
    r,p=sources();prepared=[]
    partner=np.array([v for a in r.parts if not a['name'].startswith('Olyra_') for v in a['vertices']])
    lo=partner.min(axis=0)-.03;hi=partner.max(axis=0)+.03
    envelope_lo=np.full(3,np.inf);envelope_hi=np.full(3,-np.inf)
    for a,b in zip(r.parts,p.parts):
        av=np.asarray(a['vertices']);bv=np.asarray(b['vertices']);faces=np.asarray(a['faces'])
        base=np.cross(av[faces[:,1]]-av[faces[:,0]],av[faces[:,2]]-av[faces[:,0]])
        prepared.append((a['name'],av,bv,faces,np.linalg.norm(base,axis=1)))
    min_ratio=math.inf
    # Sample both timing profiles; retraction follows the same route at a different pace.
    for clip,count in (('Deploy',121),('Retract',121),('RestingIdle',33),('ProtectiveIdle',33)):
        previous=None
        for t in np.linspace(0,1,count):
            blend,lift,shift,wave=sample(clip,float(t));points=[]
            for name,a,b,f,base in prepared:
                organism=name.startswith('Olyra_')
                if organism:
                    v=a+(b-a)*blend
                    if name.startswith(('Olyra_Lobe_','Olyra_Rib_','Olyra_Vein_')):v[:,(0,2)]*=1+.006*wave
                    v+=np.array([1.70*shift,lift+.025*wave,.35*shift])
                else:v=a
                assert np.isfinite(v).all()
                if not organism:assert np.array_equal(v,a), 'Partner moved'
                else:
                    tri=v[f]
                    separated=((tri.max(axis=1)<lo)|(tri.min(axis=1)>hi)).any(axis=1)
                    assert separated.all(), (clip,float(t),name,'partner clearance')
                    cross=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
                    ratio=np.linalg.norm(cross,axis=1)/base
                    min_ratio=min(min_ratio,float(ratio.min()))
                    assert (ratio>.02).all(),(clip,float(t),name,'collapsed face')
                    volume=np.einsum('ij,ij->i',tri[:,0],np.cross(tri[:,1],tri[:,2])).sum()/6
                    assert volume>0,(clip,float(t),name,'inward volume')
                points.append(v)
            points=np.concatenate(points);envelope_lo=np.minimum(envelope_lo,points.min(axis=0));envelope_hi=np.maximum(envelope_hi,points.max(axis=0))
            if previous is not None:assert np.linalg.norm(points-previous,axis=1).max()<.16,(clip,'discontinuous step')
            previous=points
    for t in np.linspace(0,1,101):
        u=1-float(t);u+=.12*math.sin(math.pi*u)**2
        assert np.allclose(sample('Deploy',u),sample('Retract',float(t)),atol=1e-12)
    for t in (.1,.25,.5,.75,.9):
        blend,lift,shift,wave=sample('Deploy',t)
        assert 0<shift<1 and 0<=lift<=1.01, 'Staged drift or excessive lift'
    for clip in ('RestingIdle','ProtectiveIdle'):assert np.allclose(sample(clip,0),sample(clip,1),atol=1e-12)
    assert sample('Deploy',0)==(0,0,0,0) and sample('Deploy',1)==(1,0,1,0)
    return dict(revision='glide study 08',sampled_frames=308,reverse_path_verified=True,grounded_fixed_partner=True,
        expanded_partner_clearance_m=.03,min_triangle_area_ratio=min_ratio,bounds=[envelope_lo.tolist(),envelope_hi.tolist()],
        limits='Sampled partner clearance and area/volume checks; not exhaustive lobe self-intersection, continuous collision, Unity or device certification.')

def render(clip_filter=None):
    from render_great_strider import render as draw,font
    from review_living_veil import review_data
    from PIL import Image,ImageDraw
    for clip in CLIPS:
        if clip_filter is not None and clip != clip_filter:continue
        frames=[]
        count=24
        # Include both transition endpoints; omit duplicate final frame in idle loops.
        times=np.linspace(0,1,count,endpoint='Idle' not in clip)
        for t in times:
            m=model(clip,float(t));data=review_data(m)
            img=draw(data,(7,5,12),(320,400),10,(.9,2.4,.15),False)
            ImageDraw.Draw(img).text((10,10),clip+'  '+f'{t:.2f}',font=font(16),fill='#22282d')
            frames.append(img)
        ms=round((4000 if 'Idle' in clip else 4100 if clip=='Retract' else 3600)/count)
        options=dict(save_all=True,append_images=frames[1:],duration=ms,disposal=2)
        if 'Idle' in clip:options['loop']=0
        frames[0].save(OUT/(clip+'.gif'),**options)
        sheet=Image.new('RGB',(1280,400),'#eeede5')
        for i,k in enumerate((0,8,16,23)):sheet.paste(frames[k],(i*320,0))
        sheet.save(OUT/(clip+'-contact.png'))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--write-reference',action='store_true');parser.add_argument('--render',action='store_true');parser.add_argument('--clip',choices=CLIPS);args=parser.parse_args()
    if args.write_reference:FIXTURE.write_text(json.dumps(fixture(),indent=2)+'\n')
    result=validate();OUT.mkdir(exist_ok=True,parents=True);(OUT/'motion-checks.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
    if args.render:render(args.clip)
