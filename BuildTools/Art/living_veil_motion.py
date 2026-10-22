#!/usr/bin/env python3
"""Motion-study reference; mirrored by tested editor-only LivingVeilMotion.cs."""
import copy, math
from functools import lru_cache
from build_living_veil import make_model
CLIPS=('RestingIdle','Deploy','ProtectiveIdle','Retract')

def ease(t):
    t=max(0,min(1,t));return t*t*t*(t*(t*6-15)+10)

def sample(clip,t):
    if clip not in CLIPS or not math.isfinite(t):raise ValueError((clip,t))
    t=max(0,min(1,t))
    if clip=='RestingIdle':return 0,0,0,math.sin(t*math.tau)
    if clip=='ProtectiveIdle':return 1,0,1,math.sin(t*math.tau)
    if clip=='Retract':
        t=1-t
        t+=.12*math.sin(math.pi*t)**2
    if t<=0:return 0,0,0,0
    if t>=1:return 1,0,1,0
    travel=ease(t)
    arch=16*travel*travel*(1-travel)*(1-travel)
    return ease((t-.22)/.70),1.0*ease(t/.42)*(1-ease((t-.32)/.68)),travel,.25*math.sin(math.tau*t)*arch

def vertex(name,a,b,frame):
    if not name.startswith('Olyra_'):return a
    blend,lift,shift,wave=frame
    p=[x+(y-x)*blend for x,y in zip(a,b)]
    swell=1+.006*wave if name.startswith(('Olyra_Lobe_','Olyra_Rib_','Olyra_Vein_')) else 1
    return p[0]*swell+1.70*shift,p[1]+lift+.025*wave,p[2]*swell+.35*shift

@lru_cache(maxsize=1)
def sources():
    r,p=make_model('resting'),make_model('protective')
    assert len(r.parts)==len(p.parts)
    for a,b in zip(r.parts,p.parts):
        assert a['name']==b['name'] and a['faces']==b['faces']
        assert len(a['vertices'])==len(b['vertices'])
    return r,p

def model(clip,t):
    r,p=sources();m=copy.deepcopy(r);frame=sample(clip,t)
    for part,a,b in zip(m.parts,r.parts,p.parts):
        part['vertices']=[vertex(a['name'],x,y,frame) for x,y in zip(a['vertices'],b['vertices'])]
    return m
