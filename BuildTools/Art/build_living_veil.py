#!/usr/bin/env python3
"""Living Veil resting-form study 01; actual editable geometry, meters/Y-up/+Z.
Uses shared loft/sheet primitives and the established Avarin face study.
No protective or gliding pose, production rig, or final canon acceptance implied.
"""
import argparse, copy, json, math
from pathlib import Path
from build_great_strider import Mesh, make_model as strider_model, MATERIALS, FINISH, add
ROOT=Path(__file__).resolve().parents[2]
COLORS={**MATERIALS,'olyra_core':(.16,.125,.18),'olyra_membrane':(.53,.54,.55),
        'olyra_edge':(.57,.58,.59),'olyra_tip':(.59,.60,.61)}
ALPHA={'olyra_membrane':.55,'olyra_edge':.34,'olyra_tip':.20,'sail_membrane':.68}

def make_model():
    m=Mesh()
    m.oval('Olyra_Core','olyra_core',(0,2.72,0),(.40,.29,.57))
    m.loft('Olyra_SensoryCrest','grown_mineral',[
        ((0,2.84,.12),.18,.045),((0,2.89,.36),.23,.055),((0,2.81,.62),.08,.025)],24)
    for i,x in enumerate((-.12,0,.12)):
        m.oval('Olyra_RecessedOrgan_'+str(i),'eye',(x,2.79,.535),(.012,.013,.009))
    # Five asymmetric, overlapping, down-folded lobes. Broad middle and
    # narrowed ends distinguish a canopy fold from flight wings or tentacles.
    for i,(angle,length,width) in enumerate(((5,1.45,.82),(76,1.68,.94),(151,1.48,.87),(222,1.72,.98),(294,1.58,.89))):
        a=math.radians(angle)
        def surface(t,v):
            radial=.18+1.10*math.sin(t*math.pi*.82)
            theta=a+(v-.5)*width*(.12+1.9*math.sin(math.pi*t)**.65)+.12*t
            return (radial*math.sin(theta),2.83-length*t+.20*math.sin(math.pi*t)-.09*(2*v-1)**2*math.sin(math.pi*t),radial*math.cos(theta))
        # Three joined strips approximate clearer distal edges without new anatomy.
        for band,(lo,hi,mat) in enumerate(((0,.64,'olyra_membrane'),(.64,.88,'olyra_edge'),(.88,1,'olyra_tip'))):
            m.panel('Olyra_Lobe_'+str(i)+'_Band_'+str(band),mat,
                lambda t,v,lo=lo,hi=hi:surface(lo+(hi-lo)*t,v),20,17,.004)
        for j,v in enumerate((.12,.5,.88)):
            m.strand('Olyra_Rib_'+str(i)+'_'+str(j),'grown_mineral',
                [add(surface(k/18,v),(0,.006,0)) for k in range(19)],.009 if j!=1 else .016)
    # Preserve established face and mantle anatomy, repositioned on a standing body.
    include=('Avarin_Head','Avarin_Neck','Avarin_Torso','Avarin_Forehead','Avarin_Chest',
             'Avarin_Crescent','Avarin_Mantle','Avarin_Mouth','Avarin_Eye','Avarin_Temple',
             'Avarin_UpperLid','Avarin_LowerLid','Avarin_SensoryRibbon','Avarin_TunicSeam')
    for p in strider_model().parts:
        if p['name'].startswith(include):
            p=copy.deepcopy(p);p['vertices']=[(x+1.95,y-3.01,z+.60) for x,y,z in p['vertices']];m.parts.append(p)
    for side in (-1,1):
        s='L' if side<0 else 'R';x=1.95
        m.loft('Avarin_Leg_'+s,'cloth',[((x+side*.10,.99,.44),.10,.105),((x+side*.14,.53,.45),.075,.08),((x+side*.16,.13,.48),.05,.065)],20)
        m.oval('Avarin_Foot_'+s,'soft_charcoal',(x+side*.16,.065,.55),(.065,.065,.15))
        m.loft('Avarin_Arm_'+s,'cloth',[((x+side*.21,1.35,.40),.09,.08),((x+side*.29,1.10,.43),.065,.06),((x+side*.32,.90,.51),.044,.04)],20)
        m.oval('Avarin_Hand_'+s,'avar_skin',(x+side*.32,.84,.52),(.043,.08,.035))
        m.strand('VeilHarness_Shoulder_'+s,'continuance_frame',[(x+side*.12,1.43,.37),(x+side*.17,1.28,.57),(x+side*.14,1.02,.58)],.016)
        m.oval('VeilHarness_Clip_'+s,'ceramic',(x+side*.15,1.06,.60),(.028,.045,.024))
        m.oval('VeilHarness_LineHousing_'+s,'continuance_frame',(x+side*.21,.96,.39),(.055,.07,.055))
        m.strand('VeilHarness_StowedLine_'+s,'soft_charcoal',[(x+side*.22,.96,.44),(x+side*.24,.84,.44),(x+side*.20,.80,.44)],.009)
    m.oval('VeilHarness_Regulator','ceramic',(1.95,1.14,.61),(.065,.07,.025))
    m.oval('VeilHarness_Monitor','survey_light',(1.95,1.15,.639),(.025,.03,.005))
    # Ground the partner feet only, never rescale the organism to rider height.
    for p in m.parts:
        if p['name'].startswith('Avarin_Foot_'):
            bottom=min(v[1] for v in p['vertices']);p['vertices']=[(x,y-bottom,z) for x,y,z in p['vertices']]
    return m

def write(m,out):
    out.mkdir(parents=True,exist_ok=True);obj=['# Living Veil resting study 01; meters; Y up; +Z forward','mtllib LivingVeil.mtl'];offset=1
    used=sorted({p['material'] for p in m.parts})
    for p in m.parts:
        obj+=['o '+p['name'],'usemtl '+p['material'],'s 1']
        obj+=['v '+' '.join(f'{v:.6f}' for v in pnt) for pnt in p['vertices']]
        obj+=['f '+' '.join(str(i+offset) for i in f) for f in p['faces']];offset+=len(p['vertices'])
    (out/'LivingVeil.obj').write_text('\n'.join(obj)+'\n')
    (out/'LivingVeil.mtl').write_text('\n'.join('newmtl '+n+'\nKd '+' '.join(map(str,COLORS[n]))+'\nNs '+str(FINISH.get(n,(32,.08))[0])+'\nd '+str(ALPHA.get(n,1))+'\n' for n in used))
    vertices=[v for p in m.parts for v in p['vertices']]
    bounds=[[min(v[k] for v in vertices) for k in range(3)],[max(v[k] for v in vertices) for k in range(3)]]
    report=dict(status='resting form study 01; not production ready',units='meters',up='Y',forward='Z',parts=len(m.parts),vertices=len(vertices),triangles=sum(len(p['faces']) for p in m.parts),height=bounds[1][1],bounds=bounds,lobes=5,materials=used,
        pose='resting folded lobes; Avarin standing alongside',limitations=['Provisional anatomy/surface study, not locked visual acceptance','No deployed/protective poses, rig, animation, UV unwrap or LODs','Separate closed membrane bands approximate edge clarity; transparency sorting needs native review','Harness line housings and three tiny visual organs are implementation proposals, not new locked canon'])
    (out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    (out/'review_mesh.json').write_text(json.dumps(dict(parts=m.parts,materials={n:COLORS[n] for n in used},opacity=ALPHA,finish={n:FINISH.get(n,(32,.08)) for n in used})))
    print(json.dumps(report,indent=2))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=ROOT/'SourceArt/LivingVeil');write(make_model(),p.parse_args().output)
