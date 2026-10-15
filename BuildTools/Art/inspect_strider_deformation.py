#!/usr/bin/env python3
"""Pinned before/after mesh inspection; no Unity or Blender required."""
import argparse
import importlib.util
import json
import subprocess
import tempfile
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from move_great_strider import ROOT, CONFIG, joint_blend, pitch
from route_great_strider import Route
from sequence_great_strider import yaw
from render_great_strider import render, font

BASELINE = 'dcb5edc7fed199ff4a0a47e72b744484af51ba99'
OUTPUT = ROOT/'SourceArt/GreatStrider/DeformationReview'


def baseline():
    source = subprocess.check_output(['git','show',BASELINE+':BuildTools/Art/move_great_strider.py'],cwd=ROOT)
    # Keep sufficient path depth for the imported module's ROOT calculation.
    with tempfile.TemporaryDirectory() as tmp:
        path = Path(tmp)/'art'/'baseline.py'
        path.parent.mkdir(); path.write_bytes(source)
        spec = importlib.util.spec_from_file_location('baseline_skinning',path)
        module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        module.CONFIG = CONFIG
        route = Route(); route.rig = module.Study()
    return route


def checks(route, old):
    # Nontrivial pivots, large angles, endpoints, and 180-degree conversion.
    rng = np.random.default_rng(52)
    points = rng.normal(size=(101,3)); rest = np.array([.4,1.2,-.7]); new = np.array([2.,-.3,4.])
    maximum = 0.
    for angle in (.01,.9,np.pi):
        a, b = yaw(-.4), yaw(angle)
        posed = joint_blend(points,rest,new,a,b,np.linspace(0,1,101))
        maximum = max(maximum,float(np.max(abs(np.linalg.norm(posed-new,axis=1)-np.linalg.norm(points-rest,axis=1)))))
        for w,r in ((0,a),(1,b)):
            assert np.allclose(joint_blend(points,rest,new,a,b,np.full(101,w)),(points-rest)@r.T+new,atol=1e-12)
    assert maximum < 1e-12
    assert np.allclose(pitch(points,rest,np.zeros(101)),points)
    assert all(np.array_equal(a['vertices'],b['vertices']) and a['faces']==b['faces']
               for a,b in zip(route.rig.mesh.parts,old.rig.mesh.parts))
    # Identity deformation must reproduce neutral geometry exactly.
    rig = route.rig
    saved = rig.config.copy()
    for key in ('head_pitch_rad','sail_pitch_rad','cradle_pitch_rad'):
        rig.config[key] = 0
    neutral = rig.deform(0,np.zeros(3),{k:dict(points=v,rotation=np.eye(3)) for k,v in rig.bones.items()},world=True)
    error = max(float(np.max(abs(np.array(p['vertices'])-q['vertices']))) for p,q in zip(neutral['parts'],rig.mesh.parts))
    rig.config = saved
    assert error < 1e-12
    # Root vertices follow the torso, while planted contact geometry remains rigid
    # (the route validator separately checks every foot and assembly).
    hip_error = 0.; anchored_vertices = 0; assembly_error = 0.
    for t in np.linspace(0,route.seconds,33):
        state = route.state(float(t)); posed = {p['name']:np.array(p['vertices']) for p in route.pose(float(t))['parts']}
        before = {p['name']:np.array(p['vertices']) for p in old.pose(float(t))['parts']}
        for name in posed:
            if name.startswith(('Avarin_','Cradle_','Waycaster_')):
                assembly_error=max(assembly_error,float(np.max(abs(posed[name]-before[name]))))
        for part in rig.mesh.parts:
            key = next((k for k in rig.bones if part['name']=='Ilyth_Limb_'+k),None)
            if key:
                v=part['vertices']; mask=v[:,1]>=rig.bones[key][0,1]
                if mask.any():
                    anchored_vertices += int(mask.sum())
                    expected=v[mask]@state['rotation'].T+state['root']
                    hip_error=max(hip_error,float(np.max(abs(posed[part['name']][mask]-expected))))
    assert hip_error < 1e-12 and anchored_vertices > 0
    assert assembly_error < 1e-12
    return dict(baseline=BASELINE,max_joint_radius_error_m=maximum,neutral_identity_error_m=error,
                hip_root_anchor_error_m=hip_error,anchored_vertex_samples=anchored_vertices,
                rider_cradle_equipment_change_m=assembly_error,neutral_mesh_unchanged=True,
                scope='Joint-radius and attachment checks, not tissue-volume or full collision certification')


def main():
    parser=argparse.ArgumentParser(); parser.add_argument('--validate-only',action='store_true'); args=parser.parse_args()
    route,old=Route(),baseline()
    report=checks(route,old)
    # Find the greatest relative upper/lower bend across the actual route.
    best=(-1,None,None)
    for t in np.linspace(0,route.seconds,241):
        state=route.state(float(t))
        for key,j in state['joints'].items():
            p=j['points']; u=p[0]-p[1]; v=p[2]-p[1]
            bend=np.pi-np.arccos(np.clip(u@v/np.linalg.norm(u)/np.linalg.norm(v),-1,1))
            if bend>best[0]: best=(bend,float(t),key)
    _,t,key=best
    rest=route.rig.bones[key][1]; knee=route.state(t)['joints'][key]['points'][1]
    source=next(p['vertices'] for p in route.rig.mesh.parts if p['name']=='Ilyth_Limb_'+key)
    mask=abs(source[:,1]-rest[1])<.38
    errors={}
    for label,r in (('before',old),('after',route)):
        posed=np.array(next(p['vertices'] for p in r.pose(t)['parts'] if p['name']=='Ilyth_Limb_'+key))
        errors[label]=float(np.max(abs(np.linalg.norm(posed[mask]-knee,axis=1)-np.linalg.norm(source[mask]-rest,axis=1))))
    assert errors['after']<1e-12 and errors['before']>errors['after']
    report['sampled_knee_radius_error_m']=errors
    report['inspection_pose']=dict(seconds=t,limb=key,knee_bend_degrees=float(np.degrees(best[0])))
    print(json.dumps(report,indent=2),flush=True)
    if args.validate_only:return
    OUTPUT.mkdir(parents=True,exist_ok=True)
    (OUTPUT/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    state=route.state(t); p=state['joints'][key]['points']
    head=np.array([0,5.65,1.15])*route.rig.scale@state['rotation'].T+state['root']
    rider=np.array([0,4.25,.05])*route.rig.scale@state['rotation'].T+state['root']
    views=[('Knee / isolated limb',p[1],1.35,'limb'),('Ankle / isolated limb',p[2]+[0,.18,0],1.1,'limb'),
           ('Hip / body connection',p[0],1.6,'all'),('Neck and sensory-sail roots',head,1.7,'all'),
           ('Rider / limbs hidden for inspection',rider,1.85,'rider')]
    sheet=Image.new('RGB',(960,len(views)*370+100),'#e0e0d8');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),'GREAT STRIDER / DEFORMATION STUDY 05',font=font(23),fill='#252c32')
    draw.text((20,44),f'Actual mesh / route {t:.3f}s / {key} / identical cameras and pose',font=font(16),fill='#252c32')
    for row,(label,target,extent,scope) in enumerate(views):
        for col,(title,r) in enumerate((('BEFORE / PR 50',old),('AFTER / STUDY 05',route))):
            data=r.pose(t)
            if scope=='limb':data={**data,'parts':[part for part in data['parts'] if key in part['name']]}
            if scope=='rider':
                data={**data,'parts':[part for part in data['parts'] if part['name'].startswith(('Avarin_','Cradle_','Waycaster_')) or part['name']=='Ilyth_Torso']}
            eye_offset=[8,0,-4] if scope=='rider' else [6,2,8]
            tile=render(data,np.array(target)+eye_offset,size=(480,330),extent=extent,target=target,fit=False)
            sheet.paste(tile,(col*480,100+row*370))
            draw.text((col*480+12,80+row*370),title+' — '+label,font=font(13),fill='#252c32')
    sheet.save(OUTPUT/'comparison.png')
    print(OUTPUT/'comparison.png')

if __name__=='__main__':main()
