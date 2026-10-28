#!/usr/bin/env python3
"""Tervan/Vaskel detail studies. Canon constraints, proposed contours.

No game registration, rig, hidden lore, or final visual approval is implied.
"""
import argparse, copy, json
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
    from worldfive_anatomy import tervan
    m=tervan();partner(m,(2.55,1.05))
    return finish_study(m)

def riftwalker():
    from worldfive_anatomy import vaskel
    m=vaskel();partner(m,(1.45,-.12))
    m.oval('Avarin_ContinuityAnchor','ceramic',(1.62,1.01,.04),(.035,.05,.022))
    return finish_study(m)

BUILDERS={'Burdenbeast':burdenbeast,'Riftwalker':riftwalker}

def write(name,folder):
    m=BUILDERS[name]();folder.mkdir(parents=True,exist_ok=True)
    lines=['# '+name+' silhouette rebuild 03; meters Y-up +Z','mtllib '+name+'.mtl'];offset=1;uvoffset=1
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
    manifest=dict(study='silhouette rebuild 03',model=name,units='meters',parts=len(m.parts),vertices=offset-1,
        triangles=sum(len(p['faces']) for p in m.parts),canon=['v26.0 section 26.6','v25.3 bonded-tradition entries','locked World Five visual master'],
        limitations=['Proposed rebuilt anatomy; user visual acceptance pending','Static geometry review; no new animation, production rig, LODs or game registration','Projected study UVs; production unwrap outstanding','Unseen anatomy and civic arrangements are proposals'])
    (folder/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    return m

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--unit',choices=BUILDERS);args=parser.parse_args()
    for name in ([args.unit] if args.unit else BUILDERS):write(name,ROOT/'SourceArt'/name)
