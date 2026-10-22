#!/usr/bin/env python3
"""Living Veil silhouette study 07; actual editable geometry, meters/Y-up/+Z.
Uses shared loft/sheet primitives and the established Avarin face study.
Two static poses; no production rig, animation, or final canon acceptance implied.
"""
import argparse, copy, json, math, struct, zlib
from functools import lru_cache
from refine_avar_partner import refine_partner
from living_veil_materials import SURFACES, attach_uvs, write_maps
from pathlib import Path
from build_great_strider import Mesh, make_model as strider_model, MATERIALS, FINISH as STRIDER_FINISH, add, sub, cross, unit, mul
ROOT=Path(__file__).resolve().parents[2]
COLORS={**MATERIALS,'olyra_core':(.16,.125,.18),'olyra_membrane':(.53,.54,.55)}
ALPHA={'olyra_membrane':.55,'sail_membrane':.68}
FINISH={**STRIDER_FINISH,'olyra_core':(42,.10),'olyra_membrane':(95,.14)}
FINISH.update({name:(2/(1-values[3])**4-2, STRIDER_FINISH[name][1]) for name,values in SURFACES.items()})
POSES=('resting','protective')

def membrane_variation(t,v):
    # Broad cloudy tissue and softly wandering microfibres. No emissive markings.
    fade=math.sin(math.pi*t)**2*math.sin(math.pi*v)**2
    cloud=math.sin(13*t+2*math.sin(7*v))*math.sin(17*v-3*t)
    fibre=math.sin(105*v+4*math.sin(11*t))**6
    return 1+fade*(.10*cloud+.065*fibre-.035)

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

@lru_cache(maxsize=1)
def texture_bytes():
    # RGBA sRGB tint with authored smooth alpha, generated without dependencies.
    size=512
    def pixel(t,v):
        rgb=[round((1.055*(c*membrane_variation(t,v))**(1/2.4)-.055)*255) for c in COLORS['olyra_membrane']]
        return (*rgb,round(255*membrane_alpha(t,v)))
    raw=b''.join(b'\x00'+bytes(c for x in range(size) for c in pixel(1-y/(size-1),x/(size-1))) for y in range(size))
    def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
    return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b'')

def make_model(pose='resting'):
    if pose not in POSES:raise ValueError(pose)
    m=Mesh()
    m.oval('Olyra_Core','olyra_core',(0,2.72,0),(.40,.29,.57))
    # Shape the existing continuous core, keeping its crest and organs attached.
    core=m.parts[-1]
    core['vertices']=[(x*(1-.15*(z/.57)**2),
        y+.022*math.sin(3*math.atan2(x,z))*(x/.4)**2*(1-((y-2.72)/.29)**2),z)
        for x,y,z in core['vertices']]
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
            # Soft tissue scalloping fades at the root and at both side margins.
            y+=.025*math.sin(5*math.pi*v+.6*i)*math.sin(math.pi*t)**2*math.sin(math.pi*v)
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
    refine_surfaces(m)
    refine_partner(m)
    attach_uvs([p for p in m.parts if not p['name'].startswith('Olyra_')])
    # Ground the partner feet only, never rescale the organism to rider height.
    for p in m.parts:
        if p['name'].startswith('Avarin_Foot_'):
            bottom=min(v[1] for v in p['vertices']);p['vertices']=[(x,y-bottom,z) for x,y,z in p['vertices']]
    if pose=='protective':
        for p in m.parts:
            if p['name'].startswith(('Avarin_','VeilHarness_')):
                p['vertices']=[(x-1.70,y,z-.35) for x,y,z in p['vertices']]
    if pose=='protective':
        # Cyclic ring reindexing preserves each strand surface but aligns its
        # cross-section correspondence for deformation (avoids half-turn collapse).
        rest={p['name']:p for p in make_model('resting').parts}
        for p in m.parts:
            if not p['name'].startswith(('Olyra_Rib_','Olyra_Vein_')):continue
            a=rest[p['name']]['vertices'];b=p['vertices'];sides=p['loft_sides'];rings=(len(b)-2)//sides
            def score(shift):
                total=0
                for ring in range(rings):
                    av=a[ring*sides:(ring+1)*sides];bv=b[ring*sides:(ring+1)*sides]
                    ac=tuple(sum(v[k] for v in av)/sides for k in range(3));bc=tuple(sum(v[k] for v in bv)/sides for k in range(3))
                    for j in range(sides):
                        x=sub(av[j],ac);y=sub(bv[(j+shift)%sides],bc)
                        total+=sum((v-w)**2 for v,w in zip(x,y))
                return total
            shift=min(range(sides),key=score)
            p['vertices']=[b[ring*sides+(j+shift)%sides] for ring in range(rings) for j in range(sides)]+b[-2:]
    return m

def refine_surfaces(m):
    """Continuous relief on existing topology; same vertex ordering in both poses."""
    for p in m.parts:
        name=p['name'];v=p['vertices'];sides=p.get('loft_sides')
        if name.startswith(('Olyra_Rib_','Olyra_Vein_')):
            rings=(len(v)-2)//sides
            for ring in range(rings):
                ids=range(ring*sides,(ring+1)*sides)
                center=tuple(sum(v[j][k] for j in ids)/sides for k in range(3))
                t=ring/(rings-1);scale=1.32-.94*t
                for j in ids:v[j]=add(center,mul(sub(v[j],center),scale))
        elif p['material']=='cloth' and sides:
            # Subtle axial folds, narrower wrists/ankles and a softly draped tunic.
            rings=(len(v)-2)//sides
            for ring in range(rings):
                ids=range(ring*sides,(ring+1)*sides)
                center=tuple(sum(v[j][k] for j in ids)/sides for k in range(3))
                t=ring/(rings-1);fade=math.sin(math.pi*t)**2
                for j in ids:
                    a=math.tau*(j%sides)/sides
                    scale=1+.065*fade*math.cos(5*a+1.3*t)
                    point=add(center,mul(sub(v[j],center),scale))
                    if name=='Avarin_Torso':
                        hem=max(0,min(1,(1.08-point[1])/.25))
                        point=(1.95+(point[0]-1.95)*(1+.10*hem),point[1]-.14*hem,point[2])
                    v[j]=point
            if name=='Avarin_Torso':
                # Move cap centres with the same deformation; keep the shell closed.
                for j in (-2,-1):
                    x,y,z=v[j];hem=max(0,min(1,(1.08-y)/.25));v[j]=(1.95+(x-1.95)*(1+.10*hem),y-.14*hem,z)
        elif name.startswith(('VeilHarness_Clip_','VeilHarness_LineHousing_')):
            # Flatten the oval housings into softly shouldered ceramic/frame fittings.
            center=tuple((min(q[k] for q in v)+max(q[k] for q in v))/2 for k in range(3))
            radius=tuple(max(abs(q[k]-center[k]) for q in v) for k in range(3))
            p['vertices']=[tuple(center[k]+radius[k]*math.copysign(abs((q[k]-center[k])/radius[k])**.82,q[k]-center[k]) for k in range(3)) for q in v]

def write(m,out,pose='resting'):
    out.mkdir(parents=True,exist_ok=True);stem='LivingVeil' if pose=='resting' else 'LivingVeilProtective';obj=['# Living Veil silhouette study 07; meters; Y up; +Z forward','mtllib LivingVeil.mtl'];offset=1;uv_offset=1
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
    write_maps(out/'Textures', COLORS)
    def material(n):
        texture='Textures/OlyraMembrane.png' if n=='olyra_membrane' else ('Textures/'+n+'.png' if n in SURFACES else None)
        return 'newmtl '+n+'\nKd '+' '.join(map(str,(1,1,1) if texture else COLORS[n]))+'\nNs '+str(FINISH.get(n,(32,.08))[0])+'\nd '+str(ALPHA.get(n,1))+(('\nmap_Kd '+texture) if texture else '')+'\n'
    (out/'LivingVeil.mtl').write_text('\n'.join(material(n) for n in used))

    vertices=[v for p in m.parts for v in p['vertices']]
    bounds=[[min(v[k] for v in vertices) for k in range(3)],[max(v[k] for v in vertices) for k in range(3)]]
    report=dict(status='silhouette study 07; not production ready',units='meters',up='Y',forward='Z',parts=len(m.parts),vertices=len(vertices),triangles=sum(len(p['faces']) for p in m.parts),height=bounds[1][1],bounds=bounds,lobes=5,materials=used,
        pose=pose,model_id='world05.living-veil.olyra',membrane_alpha='RGBA texture; MTL d is transparency classification, importer uses alpha multiplier 1',limitations=['Provisional static pose and surfaces; not locked visual acceptance','No gliding pose, production rig, animation or LODs','Layered transparency sorting needs native review','Exact curvature, harness detail and three tiny visual organs are implementation proposals'])
    (out/('manifest.json' if pose=='resting' else 'protective-manifest.json')).write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=ROOT/'SourceArt/LivingVeil');args=p.parse_args()
    for pose in POSES:write(make_model(pose),args.output,pose)
