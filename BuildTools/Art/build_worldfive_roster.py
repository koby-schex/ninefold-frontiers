#!/usr/bin/env python3
"""Tervan/Vaskel detail studies. Canon constraints, proposed contours.

No game registration, rig, hidden lore, or final visual approval is implied.
"""
import argparse, copy, json, math
from pathlib import Path
from build_great_strider import Mesh, MATERIALS, FINISH, ROOT
from build_living_veil import make_model as veil_model
from strider_materials import attach_uvs, tiles, png_albedo

COLORS={**MATERIALS,'tervan_hide':(.19,.17,.16),'tervan_joint':(.075,.07,.08),
        'vaskel_tissue':(.09,.055,.11),'anchor_channel':(.38,.30,.48)}
SURFACE_TYPES={'tervan_hide':'living_pearl','tervan_joint':'limb_tissue',
    'vaskel_tissue':'living_pearl','grown_mineral':'grown_mineral',
    'ceramic':'ceramic','continuance_frame':'continuance_frame','cloth':'cloth','avar_skin':'avar_skin'}

def finish_study(m):
    # Rest-space projected UVs preserve source geometry and smooth seam normals.
    for p in m.parts:
        material=p['material']
        if material in SURFACE_TYPES:
            p['material']=SURFACE_TYPES[material];attach_uvs([p]);p['material']=material
    return m

def partner(m,position):
    """Reuse the existing standing Avarin study, without Olyra's Veil Harness."""
    for part in veil_model().parts:
        if not part['name'].startswith('Avarin_'):continue
        p=copy.deepcopy(part)
        p['vertices']=[(x-1.95+position[0],y,z-.45+position[1]) for x,y,z in p['vertices']]
        # Rebuild projected study UVs after placing the independent partner.
        p.pop('uvs',None);p.pop('uv_faces',None);m.parts.append(p)

def burdenbeast():
    m=Mesh()
    m.loft('Tervan_Torso','tervan_hide',[
        ((0,1.12,-2.65),.44,.36),((0,1.16,-2.20),1.13,.56),
        ((0,1.15,-1.1),1.43,.56),((0,1.14,.50),1.46,.58),
        ((0,1.10,1.75),1.22,.53),((0,1.08,2.52),.70,.37)],32)
    # Flatten the load surface and keep the central back gently concave.
    body=m.parts[-1]
    body['vertices']=[(x,1.48+.10*(x/1.5)**2+.18*(y-1.48) if y>1.48 else y,z) for x,y,z in body['vertices']]
    m.oval('Tervan_SensoryHood','tervan_hide',(0,1.10,2.49),(.78,.38,.52))
    for side in (-1,1):
        s='L' if side<0 else 'R'
        m.oval('Tervan_RecessedEye_'+s,'eye',(side*.52,1.22,2.79),(.026,.023,.025))
        m.strand('Tervan_SensoryFold_'+s,'limb_tissue',[(side*.60,1.11,2.77),(side*.66,1.03,2.66),(side*.60,.97,2.60)],.018)
        for k,z in enumerate((-1.95,-.70,.60,1.78)):
            x=side*(1.10 if k in (0,3) else 1.33)
            m.loft('Tervan_Limb_'+s+str(k),'tervan_hide',[
                ((x*.85,1.15,z),.32,.34),((x*.98,.91,z+.01),.31,.32),((x,.60,z+.04),.22,.24),
                ((x*1.10,.23,z+.06),.27,.29)],20)
            limb=m.parts[-1]
            # Flexure relief belongs to the skin; no detached decorative strands.
            shaped=[]
            for a,b,c in limb['vertices']:
                relief=.045*math.sin(65*b)*math.exp(-((b-.55)/.19)**2)
                shaped.append((a+(a-x*1.04)*relief,b,c+(c-z-.04)*relief))
            limb['vertices']=shaped
            m.oval('Tervan_Contact_'+s+str(k),'tervan_joint',(x*1.10,.13,z+.07),(.35,.13,.35))
            pad=m.parts[-1];bottom=min(v[1] for v in pad['vertices'])
            pad['vertices']=[(a,b-bottom,c) for a,b,c in pad['vertices']]
            m.loft('Tervan_SupportRidge_'+s+str(k),'grown_mineral',[
                ((side*.38,1.52,z-.38),.06,.035),((side*.78,1.65,z-.12),.23,.055),
                ((side*1.13,1.47,z+.30),.15,.04)],20)
        m.strand('CivicFrame_Rail_'+s,'continuance_frame',[(side*.78,1.70,z) for z in (-2.0,-.8,.5,1.9)],.055)
    m.loft('Tervan_FeedingFold','tervan_joint',[
        ((-.46,.87,2.76),.04,.04),((0,.81,2.85),.065,.055),((.46,.87,2.76),.04,.04)],16)
    # Independent stowed civic modules; no biological cannon or passenger saddle.
    for k,z in enumerate((-1.5,-.25,1.0)):
        m.strand('CivicFrame_Crossmember_'+str(k),'continuance_frame',[(-.80,1.71,z),(0,1.76,z),(.80,1.71,z)],.045)
        for side in (-1,1):
            label=('Archive','Care','Shelter')[k]+('_L' if side<0 else '_R')
            m.loft('CivicModule_'+label,'ceramic',[
                ((side*.58,1.76,z),.36,.43),((side*.58,2.04,z),.40,.46),
                ((side*.58,2.28,z),.31,.38)],20)
            module=m.parts[-1];verts=module['vertices'];sides=module['loft_sides']
            for ring in range((len(verts)-2)//sides):
                ids=range(ring*sides,(ring+1)*sides)
                rx=max(abs(verts[j][0]-side*.58) for j in ids)
                rz=max(abs(verts[j][2]-z) for j in ids)
                for j in ids:
                    a,b,c=verts[j];u=(a-side*.58)/rx;v=(c-z)/rz
                    verts[j]=(side*.58+rx*math.copysign(abs(u)**.42,u),b,z+rz*math.copysign(abs(v)**.42,v))
            m.strand('CivicModule_Retainer_'+label,'continuance_frame',[(side*.58,1.79,z-.44),(side*.58,2.30,z),(side*.58,1.79,z+.44)],.023)
            # Fitted edging and recessed panel ribs clarify modular civic equipment.
            for edge in (-1,1):
                m.strand('CivicModule_Edge_'+label+str(edge),'continuance_frame',
                    [(side*.58+edge*.31,1.86,z-.43),(side*.58+edge*.36,2.08,z-.44),(side*.58+edge*.29,2.25,z-.37)],.016)
            for rib in range(3):
                m.strand('CivicModule_Panel_'+label+str(rib),'continuance_frame',
                    [(side*.58-.22,1.91+rib*.07,z-.457),(side*.58+.22,1.91+rib*.07,z-.457)],.009)
    m.loft('Convergence_StowedHousing','continuance_frame',[
        ((0,1.80,.15),.20,.39),((0,2.08,.15),.22,.42),((0,2.31,.15),.14,.32)],20)
    for side in (-1,1):
        m.strand('Convergence_FoldedSupport_'+str(side),'ceramic',[(side*.17,1.80,-.15),(side*.26,2.15,.02),(side*.15,2.31,.35)],.033)
    partner(m,(2.55,1.05))
    return finish_study(m)

def riftwalker():
    m=Mesh()
    m.loft('Vaskel_CrescentTorso','vaskel_tissue',[
        ((-.42,1.10,-.08),.10,.11),((-.25,1.43,.03),.19,.16),
        ((.12,1.61,.12),.21,.17),((.44,1.48,.21),.16,.14),
        ((.57,1.25,.27),.07,.08)],24)
    for name,points in (
        ('L',[((-.23,1.22,.02),.16,.15),((-.32,.91,-.10),.145,.135),((-.36,.61,-.20),.085,.10),((-.25,.13,-.26),.08,.10)]),
        ('R',[((.30,1.39,.14),.19,.17),((.45,1.06,.14),.17,.15),((.54,.64,.13),.09,.10),((.36,.13,.32),.09,.11)])):
        m.loft('Vaskel_Limb_'+name,'vaskel_tissue',points,24)
        x,_,z=points[-1][0]
        m.oval('Vaskel_Contact_'+name,'soft_charcoal',(x,.075,z),(.14,.075,.17))
        pad=m.parts[-1];bottom=min(v[1] for v in pad['vertices'])
        pad['vertices']=[(a,b-bottom,c) for a,b,c in pad['vertices']]
    m.loft('Vaskel_Counterweight','vaskel_tissue',[
        ((-.30,1.22,-.09),.14,.13),((-.62,1.13,-.20),.15,.12),
        ((-.92,1.30,-.38),.14,.10),((-.96,1.02,-.60),.055,.045)],24)
    crest=[(-.44,1.14,.01),(-.28,1.51,.12),(.10,1.75,.19),(.45,1.58,.29),(.61,1.29,.31)]
    m.loft('Vaskel_SensoryCrescent','grown_mineral',[(p,w,.034) for p,w in zip(crest,(.03,.075,.09,.07,.025))],20)
    # Nested organic lips emphasize the sensory crescent without adding a halo.
    for side in (-1,1):
        m.strand('Vaskel_CrescentLip_'+str(side),'grown_mineral',
            [(x,y-.035,z+side*.055) for x,y,z in crest[1:4]],.014)
    for k in range(7):
        t=(k+.5)/7;angle=math.pi*(.08+.84*t)
        x=.10+.46*math.cos(angle);y=1.20+.49*math.sin(angle);z=.31-.15*t
        m.oval('Vaskel_SensoryPit_'+str(k),'eye',(x,y,z),(.009,.009,.008))
    # Separated, non-emissive study inlays. No continuous glowing ring.
    for k in (0,2):
        a,b=crest[k],crest[k+1]
        pts=[tuple(a[j]+(b[j]-a[j])*u for j in range(3)) for u in (.22,.40,.58)]
        m.strand('Vaskel_ChannelSegment_'+str(k),'anchor_channel',[(x,y+.017,z+.034) for x,y,z in pts],.009)
    for side in (-1,1):
        m.oval('Vaskel_SecondaryEye_'+str(side),'eye',(.47+side*.035,1.39,.345),(.012,.012,.009))
    m.oval('Vaskel_ContinuityAnchor','ceramic',(-.37,1.23,.12),(.035,.045,.024))
    # Keep the whole compact creature within the selected 1.2–1.8 m study band;
    # the independent Avarin retains the established 1.82 m source height.
    for p in m.parts:p['vertices']=[tuple(q*.94 for q in v) for v in p['vertices']]
    partner(m,(1.45,-.12))
    m.oval('Avarin_ContinuityAnchor','ceramic',(1.62,1.01,.04),(.035,.05,.022))
    return finish_study(m)

BUILDERS={'Burdenbeast':burdenbeast,'Riftwalker':riftwalker}

def write(name,folder):
    m=BUILDERS[name]();folder.mkdir(parents=True,exist_ok=True)
    lines=['# '+name+' detail and idle study 02; meters Y-up +Z','mtllib '+name+'.mtl'];offset=1;uvoffset=1
    for p in m.parts:
        lines+=['o '+p['name'],'usemtl '+p['material'],'s 1']
        lines+=['v '+' '.join(f'{q:.6f}' for q in v) for v in p['vertices']]
        uvs=p.get('uvs',[]);lines+=['vt '+' '.join(f'{q:.6f}' for q in uv) for uv in uvs]
        for k,f in enumerate(p['faces']):
            lines.append('f '+' '.join(str(i+offset)+('/'+str(p['uv_faces'][k][j]+uvoffset) if uvs else '') for j,i in enumerate(f)))
        offset+=len(p['vertices']);uvoffset+=len(uvs)
    (folder/(name+'.obj')).write_text('\n'.join(lines)+'\n')
    used=sorted({p['material'] for p in m.parts})
    textures=folder/'Textures';textures.mkdir(exist_ok=True)
    for n in used:
        if n in SURFACE_TYPES:png_albedo(textures/(n+'.png'),tiles()[SURFACE_TYPES[n]],COLORS[n])
    (folder/(name+'.mtl')).write_text('\n'.join('newmtl '+n+'\nKd '+' '.join(map(str,(1,1,1) if n in SURFACE_TYPES else COLORS[n]))+'\nNs '+str(FINISH.get(n,(24,.08))[0])+'\nd '+str(.68 if n=='sail_membrane' else 1)+'\n'+('map_Kd Textures/'+n+'.png\n' if n in SURFACE_TYPES else '') for n in used))
    manifest=dict(study='detail and idle 02',model=name,units='meters',parts=len(m.parts),vertices=offset-1,
        triangles=sum(len(p['faces']) for p in m.parts),canon=['v26.0 section 26.6','v25.3 bonded-tradition entries','locked World Five visual master'],
        limitations=['Developing silhouette; not final master fidelity','Review idle blend shapes only; no walking, production rig, LODs or game registration','Projected study UVs; production unwrap outstanding','Unseen anatomy and module arrangements are proposals'])
    (folder/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    return m

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--unit',choices=BUILDERS);args=parser.parse_args()
    for name in ([args.unit] if args.unit else BUILDERS):write(name,ROOT/'SourceArt'/name)
