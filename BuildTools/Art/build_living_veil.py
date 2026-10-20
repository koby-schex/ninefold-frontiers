#!/usr/bin/env python3
"""Living Veil canopy study 02; actual editable geometry, meters/Y-up/+Z.
Uses shared loft/sheet primitives and the established Avarin face study.
Two static poses; no production rig, animation, or final canon acceptance implied.
"""
import argparse, copy, json, math, struct, zlib
from pathlib import Path
from build_great_strider import Mesh, make_model as strider_model, MATERIALS, FINISH, add, sub, cross, unit, mul
ROOT=Path(__file__).resolve().parents[2]
COLORS={**MATERIALS,'olyra_core':(.16,.125,.18),'olyra_membrane':(.53,.54,.55)}
ALPHA={'olyra_membrane':.55,'sail_membrane':.68}
POSES=('resting','protective')

def membrane_alpha(t,v):
    # Continuous distal and lateral clarity; no detached opacity-band shells.
    distal=t*t*(3-2*t)
    lateral=math.sin(math.pi*v)**.45
    return (.53-.32*distal)*(.42+.58*lateral)

def membrane(m,name,surface):
    rows,cols=33,25
    m.panel(name,'olyra_membrane',surface,rows,cols,.003)
    p=m.parts.pop();vertices=[];uv=[]
    for side in (-1,1):
        for i in range(rows):
            for j in range(cols):
                t=i/(rows-1);v=j/(cols-1);e=.0001
                dt=sub(surface(min(1,t+e),v),surface(max(0,t-e),v))
                dv=sub(surface(t,min(1,v+e)),surface(t,max(0,v-e)))
                n=unit(cross(dt,dv))
                vertices.append(add(surface(t,v),mul(n,side*.003)))
                uv.append((v,t))
    m.part(name,'olyra_membrane',vertices,p['faces'])
    p=m.parts[-1];p['uvs']=uv;p['uv_faces']=p['faces']
    p['vertex_opacity']=[membrane_alpha(t,v) for v,t in uv]

def texture_bytes():
    # RGBA sRGB tint with authored smooth alpha, generated without dependencies.
    size=256;rgb=[round((1.055*c**(1/2.4)-.055)*255) for c in COLORS['olyra_membrane']]
    raw=b''.join(b'\x00'+bytes(c for x in range(size) for c in (*rgb,round(255*membrane_alpha(1-y/(size-1),x/(size-1))))) for y in range(size))
    def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
    return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b'')

def make_model(pose='resting'):
    if pose not in POSES:raise ValueError(pose)
    m=Mesh()
    m.oval('Olyra_Core','olyra_core',(0,2.72,0),(.40,.29,.57))
    m.loft('Olyra_SensoryCrest','grown_mineral',[
        ((0,2.84,.12),.18,.045),((0,2.89,.36),.23,.055),((0,2.81,.62),.08,.025)],24)
    for i,x in enumerate((-.12,0,.12)):
        m.oval('Olyra_RecessedOrgan_'+str(i),'eye',(x,2.79,.535),(.012,.013,.009))
    # Five homologous lobes in both static poses; no new anatomy on deployment.
    for i,(angle,length,width) in enumerate(((5,1.45,.82),(76,1.68,.94),(151,1.48,.87),(222,1.72,.98),(294,1.58,.89))):
        a=math.radians(angle)
        def surface(t,v):
            theta=a+(v-.5)*width*(.12+1.9*math.sin(math.pi*t)**.65)+.12*t
            if pose=='resting':
                radial=.18+1.10*math.sin(t*math.pi*.82)
                y=2.83-length*t+.20*math.sin(math.pi*t)-.09*(2*v-1)**2*math.sin(math.pi*t)
            else:
                reach=(2.45,2.65,2.5,2.55,2.35)[i]
                drop=(.90,1.55,1.75,1.65,1.05)[i]
                radial=.18+reach*math.sin(t*math.pi*.65)
                y=3.23+.30*math.sin(math.pi*t)-drop*t*t-.12*(2*v-1)**2*math.sin(math.pi*t)
            return (radial*math.sin(theta),y,radial*math.cos(theta))
        membrane(m,'Olyra_Lobe_'+str(i),surface)
        for j,v in enumerate((.12,.5,.88)):
            m.strand('Olyra_Rib_'+str(i)+'_'+str(j),'grown_mineral',
                [add(surface(k/24,v),(0,.009,0)) for k in range(25)],.008 if j!=1 else .014)
        # Fine branching follows the lobe rather than reading as a mechanical frame.
        for side in (-1,1):
            for k,t0 in enumerate((.28,.49,.68)):
                m.strand('Olyra_Vein_'+str(i)+'_'+str(side)+'_'+str(k),'grown_mineral',
                    [add(surface(t0+.18*u/8,.5+side*.34*u/8),(0,.005,0)) for u in range(9)],.004)
    if pose=='protective':
        for p in m.parts:
            if p['name'].startswith(('Olyra_Core','Olyra_SensoryCrest','Olyra_RecessedOrgan')):
                p['vertices']=[(x,y+.4,z) for x,y,z in p['vertices']]
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
    if pose=='protective':
        for p in m.parts:
            if p['name'].startswith(('Avarin_','VeilHarness_')):
                p['vertices']=[(x-1.70,y,z-.35) for x,y,z in p['vertices']]
    return m

def write(m,out,pose='resting'):
    out.mkdir(parents=True,exist_ok=True);stem='LivingVeil' if pose=='resting' else 'LivingVeilProtective';obj=['# Living Veil canopy study 02; meters; Y up; +Z forward','mtllib LivingVeil.mtl'];offset=1;uv_offset=1
    used=sorted({p['material'] for p in m.parts})
    for p in m.parts:
        obj+=['o '+p['name'],'usemtl '+p['material'],'s 1']
        obj+=['v '+' '.join(f'{v:.6f}' for v in pnt) for pnt in p['vertices']]
        obj+=['vt '+' '.join(f'{v:.7f}' for v in uv) for uv in p.get('uvs',[])]
        obj+=['f '+' '.join(str(i+offset)+('/'+str(p['uv_faces'][fi][j]+uv_offset) if 'uvs' in p else '') for j,i in enumerate(f)) for fi,f in enumerate(p['faces'])]
        offset+=len(p['vertices']);uv_offset+=len(p.get('uvs',[]))
    (out/(stem+'.obj')).write_text('\n'.join(obj)+'\n')
    (out/'Textures').mkdir(exist_ok=True)
    (out/'Textures/OlyraMembrane.png').write_bytes(texture_bytes())
    (out/'LivingVeil.mtl').write_text('\n'.join('newmtl '+n+'\nKd '+' '.join(map(str,(1,1,1) if n=='olyra_membrane' else COLORS[n]))+'\nNs '+str(FINISH.get(n,(32,.08))[0])+'\nd '+str(ALPHA.get(n,1))+('\nmap_Kd Textures/OlyraMembrane.png' if n=='olyra_membrane' else '')+'\n' for n in used))
    vertices=[v for p in m.parts for v in p['vertices']]
    bounds=[[min(v[k] for v in vertices) for k in range(3)],[max(v[k] for v in vertices) for k in range(3)]]
    report=dict(status='canopy study 02; not production ready',units='meters',up='Y',forward='Z',parts=len(m.parts),vertices=len(vertices),triangles=sum(len(p['faces']) for p in m.parts),height=bounds[1][1],bounds=bounds,lobes=5,materials=used,
        pose=pose,model_id='world05.living-veil.olyra',membrane_alpha='RGBA texture; MTL d is transparency classification, importer uses alpha multiplier 1',limitations=['Provisional static pose and surfaces; not locked visual acceptance','No gliding pose, production rig, animation or LODs','Layered transparency sorting needs native review','Exact curvature, harness detail and three tiny visual organs are implementation proposals'])
    (out/('manifest.json' if pose=='resting' else 'protective-manifest.json')).write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=ROOT/'SourceArt/LivingVeil');args=p.parse_args()
    for pose in POSES:write(make_model(pose),args.output,pose)
