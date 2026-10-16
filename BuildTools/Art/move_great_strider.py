#!/usr/bin/env python3
"""Offline provisional crawl study on the authored mesh; no Unity dependency."""
import argparse
import base64
from concurrent.futures import ProcessPoolExecutor
import io
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from build_great_strider import make_model, MATERIALS, FINISH, OPACITY
from render_great_strider import render, font
from strider_materials import attach_uvs, tiles

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / 'SourceArt/GreatStrider/motion-study.json'


def unit(v):
    return v / np.linalg.norm(v)


def rotation(a, b):
    """Shortest arc; this study rejects antiparallel bone configurations."""
    a, b = unit(a), unit(b)
    v = np.cross(a, b)
    c = float(a @ b)
    if c < -.999999:
        raise ValueError('Antiparallel bone rotation')
    k = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + k + k @ k / (1 + c)


def smooth(v):
    """Quintic easing with zero first/second derivatives at both ends."""
    v = np.clip(v, 0, 1)
    return v*v*v*(v*(v*6-15)+10)


def pitch(points, pivot, angle):
    """Pitch with either one angle or a separate angle per vertex."""
    q = points-pivot
    c, s = np.cos(angle), np.sin(angle)
    return np.column_stack((q[:,0], c*q[:,1]-s*q[:,2], s*q[:,1]+c*q[:,2]))+pivot


def quaternion(r):
    """Stable matrix conversion, including rotations near 180 degrees (xyzw)."""
    k = np.array([
        [r[0,0]-r[1,1]-r[2,2], r[1,0]+r[0,1], r[2,0]+r[0,2], r[2,1]-r[1,2]],
        [r[1,0]+r[0,1], r[1,1]-r[0,0]-r[2,2], r[2,1]+r[1,2], r[0,2]-r[2,0]],
        [r[2,0]+r[0,2], r[2,1]+r[1,2], r[2,2]-r[0,0]-r[1,1], r[1,0]-r[0,1]],
        [r[2,1]-r[1,2], r[0,2]-r[2,0], r[1,0]-r[0,1], np.trace(r)]])
    return np.linalg.eigh(k)[1][:,-1]


def joint_blend(points, rest_pivot, new_pivot, first, second, weight):
    """Normalized quaternion blend about a shared pivot; preserves joint radius.

    This is not a full dual-quaternion rig or a volume-preserving tissue model.
    Both bone transforms must map rest_pivot to new_pivot.
    """
    a, b = quaternion(first), quaternion(second)
    if a@b < 0:
        b = -b
    w = np.asarray(weight)[:,None]
    q = (1-w)*a+w*b
    q /= np.linalg.norm(q,axis=1)[:,None]
    v = points-rest_pivot
    cross = 2*np.cross(q[:,:3],v)
    return v+q[:,3,None]*cross+np.cross(q[:,:3],cross)+new_pivot


class Study:
    def __init__(self):
        self.config = json.loads(CONFIG.read_text())
        self.mesh = make_model()
        low = min(v[1] for p in self.mesh.parts for v in p['vertices'])
        self.scale = 6 / (max(v[1] for p in self.mesh.parts for v in p['vertices']) - low)
        for p in self.mesh.parts:
            p['vertices'] = (np.array(p['vertices']) - [0, low, 0]) * self.scale
        attach_uvs(self.mesh.parts)
        self.bones = {}
        names = {p['name'] for p in self.mesh.parts}
        assert all('Ilyth_Limb_'+key in names for key in self.config['phase_offsets'])
        for name in self.config['phase_offsets']:
            side = -1 if name.startswith('L') else 1
            fore = name.endswith('Fore')
            self.bones[name] = np.array([(side*.27, 5.16, .67 if fore else -.65),
                                        (side*1.11, 2.66, .72 if fore else -1.18),
                                        (side*1.47, .25, 1.43 if fore else -1.56)]) * self.scale

    def joints(self, t):
        c = self.config
        # Art-directed load shift away from the currently lifting side.
        root = np.array([c['body_sway_m']*math.sin(4*math.pi*(t-.025)),
                         -c['body_settle_m']+c['body_bob_m']*math.cos(8*math.pi*t),
                         t*c['stride_m']])
        result = {}
        for name, rest in self.bones.items():
            offset = c['phase_offsets'][name]
            cycle = math.floor(t+offset)
            phase = (t+offset)-cycle
            swing = phase >= c['stance_fraction']
            u = (phase-c['stance_fraction'])/(1-c['stance_fraction']) if swing else 0
            travel = float(smooth(u))
            delta = np.array([0., c['lift_m']*math.sin(math.pi*u)**4,
                              (cycle-offset+travel)*c['stride_m']])
            hip = rest[0]+root
            foot = rest[2]+delta
            a, b = np.linalg.norm(rest[1]-rest[0]), np.linalg.norm(rest[2]-rest[1])
            d = np.linalg.norm(foot-hip)
            if not abs(a-b)+1e-7 < d < a+b-1e-7:
                raise ValueError(f'{name}: target outside joint reach at {t}')
            axis = unit(foot-hip)
            pole = rest[1]-rest[0]
            bend = unit(pole-axis*(pole@axis))
            along = (a*a-b*b+d*d)/(2*d)
            knee = hip+axis*along+bend*math.sqrt(max(0, a*a-along*along))
            result[name] = dict(points=np.array([hip,knee,foot]), delta=delta, swing=swing)
        return root, result

    def pose(self, t):
        root, joints = self.joints(t)
        return self.deform(t, root, joints)

    def deform(self, phase, root, joints, body_rotation=None, world=False):
        """Shared skinning: world-space IK, independent planted-foot orientation."""
        t = phase
        body_rotation = np.eye(3) if body_rotation is None else body_rotation
        parts = []
        for part in self.mesh.parts:
            name = part['name']
            v = part['vertices']
            limb = next((key for key in self.bones if key in name), None)
            if limb:
                rest = self.bones[limb]
                new = joints[limb]['points']
                upper = rotation(body_rotation@(rest[1]-rest[0]), new[1]-new[0])@body_rotation
                lower = rotation(body_rotation@(rest[2]-rest[1]), new[2]-new[1])@body_rotation
                w = smooth((v[:,1]-rest[1,1]+.38)/.76)
                posed = joint_blend(v, rest[1], new[1], lower, upper, w)
                # Anchor the limb root to the torso before transitioning to the upper bone.
                hip = smooth((rest[0,1]-v[:,1])/.60)
                mask = hip < 1
                posed[mask] = joint_blend(v[mask],rest[0],new[0],body_rotation,upper,hip[mask])
                foot_rotation = joints[limb].get('rotation', np.eye(3))
                contact = (v-rest[2])@foot_rotation.T+new[2]
                ankle = smooth((v[:,1]-rest[2,1]-.05)/.45)
                mask = ankle < 1
                posed[mask] = joint_blend(v[mask],rest[2],new[2],foot_rotation,lower,ankle[mask])
                if 'Contact' in name:
                    posed = contact
                elif 'Ankle' in name:
                    posed = contact
            else:
                posed = v.copy()
                sail = any(k in name for k in ('SensorySail','SailVein','SailChannel','SailRoot'))
                head = name.startswith('Ilyth_') and any(k in name for k in ('CompactHead','CranialMineral','FeedingSeam','Eye_','Brow_'))
                pivot = np.array([0,5.25,.85])*self.scale
                angle = self.config['head_pitch_rad']*math.sin(2*math.pi*t-.4)
                if head or sail or 'ShortNeck' in name:
                    weight = smooth((v[:,1]-5.15*self.scale)/(.35*self.scale)) if 'ShortNeck' in name else 1
                    posed = pitch(posed,pivot,angle*weight)
                if sail:
                    # Separate small response, inherited from the head transform.
                    sign = -1 if '_L' in name else 1
                    sail_pivot = pitch(np.array([[sign*.17,5.75,1.16]])*self.scale,pivot,angle)[0]
                    weight = smooth((np.linalg.norm(posed-sail_pivot,axis=1)-.08)/.45)
                    posed = pitch(posed,sail_pivot,weight*self.config['sail_pitch_rad']*math.sin(2*math.pi*t-.8))
                if name.startswith(('Cradle_','Avarin_','Waycaster_')):
                    pivot = np.array([0,4.8,0])*self.scale
                    moved = pitch(posed,pivot,self.config['cradle_pitch_rad']*math.sin(4*math.pi*t-.5))
                    # Keep upper attachment pads fixed; flex the connecting load frame.
                    weight = smooth((4.85*self.scale-v[:,1])/(.65*self.scale))[:,None] if 'LoadFrame' in name else 1
                    if 'Attachment' in name:
                        weight = 0
                    posed = posed+(moved-posed)*weight
                posed = posed@body_rotation.T+root
            parts.append(dict(**{k:part[k] for k in ('uvs','uv_faces') if k in part},name=name, material=part['material'], faces=part['faces'],
                              vertices=(posed if world else posed-[0,0,root[2]]).tolist()))
        return dict(parts=parts, materials=MATERIALS, finish=FINISH, opacity=OPACITY, textures=tiles())

    def validate(self):
        max_slide = max_length = 0.
        previous = None
        for t in np.linspace(-1, 2, 601):
            root, joints = self.joints(float(t))
            assert sum(not j['swing'] for j in joints.values()) >= 3
            for key, j in joints.items():
                lengths = np.linalg.norm(np.diff(j['points'],axis=0),axis=1)
                expected = np.linalg.norm(np.diff(self.bones[key],axis=0),axis=1)
                max_length = max(max_length, float(np.max(abs(lengths-expected))))
                assert j['delta'][1] >= -1e-10
                if previous and not j['swing'] and not previous[key]['swing']:
                    max_slide = max(max_slide, float(np.linalg.norm(j['points'][2]-previous[key]['points'][2])))
            previous = joints
        assert max_slide < 1e-8 and max_length < 1e-8
        # Verify the visible contact geometry too, not only the IK targets.
        for t in np.linspace(0, 1, 17):
            posed = self.pose(float(t))
            _, joints = self.joints(float(t))
            for key, j in joints.items():
                pads = [p for p in posed['parts'] if 'Contact' in p['name'] and key in p['name']]
                assert len(pads) == 4
                minimum = min(v[1] for p in pads for v in p['vertices'])
                assert abs(minimum-j['delta'][1]) < 1e-8
            assert all(np.isfinite(p['vertices']).all() for p in posed['parts'])
        # Check lift/plant boundaries explicitly; uniform samples can miss seams.
        max_boundary_speed = 0.
        eps = 1e-5
        for offset in self.config['phase_offsets'].values():
            for boundary in (self.config['stance_fraction']-offset, 1-offset):
                for direction in (-1,1):
                    _, at = self.joints(boundary)
                    _, near = self.joints(boundary+direction*eps)
                    key = next(k for k,v in self.config['phase_offsets'].items() if v == offset)
                    speed = np.linalg.norm(near[key]['points'][2]-at[key]['points'][2])/eps
                    max_boundary_speed = max(max_boundary_speed,float(speed))
        assert max_boundary_speed < 1e-5
        # Operator and seat share one rigid transform; attachment pads stay on the body.
        source = {p['name']:p['vertices'] for p in self.mesh.parts}
        assembly = [k for k in source if k.startswith(('Avarin_','Waycaster_')) or k == 'Cradle_Seat']
        reference = np.array([source[k][0] for k in assembly])
        distances = np.linalg.norm(reference-reference[0],axis=1)
        max_assembly_error = 0.
        for t in (.125,.375,.625,.875):
            root,_ = self.joints(t)
            posed = {p['name']:np.array(p['vertices']) for p in self.pose(t)['parts']}
            points = np.array([posed[k][0] for k in assembly])
            max_assembly_error = max(max_assembly_error,float(np.max(abs(np.linalg.norm(points-points[0],axis=1)-distances))))
            for key in source:
                if key.startswith('Cradle_Attachment'):
                    assert np.max(abs(posed[key]-(source[key]+[root[0],root[1],0]))) < 1e-8
        assert max_assembly_error < 1e-8
        p0, p1 = self.pose(0), self.pose(1)
        closure = max(np.max(abs(np.array(a['vertices'])-np.array(b['vertices'])))
                      for a,b in zip(p0['parts'],p1['parts']))
        assert closure < 1e-8
        assert all(np.isfinite(p['vertices']).all() for p in p0['parts'])
        return dict(samples=601, cycles=3, max_stance_slide_m=max_slide,
                    max_bone_length_error_m=max_length, loop_error_m=float(closure),
                    minimum_support_feet=3, max_lift_plant_boundary_speed_m_per_cycle=max_boundary_speed,
                    max_operator_seat_rigidity_error_m=max_assembly_error,
                    scope='Kinematic checks only; collisions, mass balance and final skinning unverified')


def ground(data, t, stride):
    """World-fixed review grid; repeats once per stride so the movie loops."""
    parts = list(data['parts'])
    def quad(name, vertices, material):
        parts.append(dict(name=name, material=material, vertices=vertices, faces=[[0,2,1],[0,3,2]]))
    quad('ReviewGround',[[-4,-.018,-4],[4,-.018,-4],[4,-.018,4],[-4,-.018,4]],'review_ground')
    for i in range(-10,11):
        z=i*stride-t*stride
        quad('GridZ'+str(i),[[-4,-.012,z-.008],[4,-.012,z-.008],[4,-.012,z+.008],[-4,-.012,z+.008]],'review_grid')
    for i in range(-4,5):
        x=i*.75
        quad('GridX'+str(i),[[x-.008,-.012,-4],[x+.008,-.012,-4],[x+.008,-.012,4],[x-.008,-.012,4]],'review_grid')
    return dict(textures=data.get('textures',{}),parts=parts,materials={**data['materials'],'review_ground':(.64,.66,.64),'review_grid':(.37,.40,.40)},finish=data['finish'],opacity=data.get('opacity',{}))


def frame(args):
    index, count = args
    study = Study()
    t = index/count
    data = ground(study.pose(t),t,study.config['stride_m'])
    im = Image.new('RGB',(1080,610),(224,224,216))
    d = ImageDraw.Draw(im)
    d.text((16,12), 'GREAT STRIDER / MOTION STUDY 02 / ACTUAL MESH', font=font(21), fill='#252c32')
    for j,(label,eye) in enumerate([('THREE-QUARTER',(9,6.5,12)),('FRONT',(0,5,15)),('SIDE',(15,5,0))]):
        tile = render(data,eye,(360,500),8.2,fit=False)
        im.paste(tile,(j*360,60))
        d.text((j*360+12,42),label,font=font(14),fill='#252c32')
    _, joints = study.joints(t)
    d.text((16,565), 'Swing: '+(', '.join(k for k,j in joints.items() if j['swing']) or 'none')+'  |  Frame '+str(index+1),font=font(16),fill='#252c32')
    d.text((16,590), 'World-fixed grid / tracking camera / provisional motion / physical balance unverified',font=font(14),fill='#444c52')
    return im


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--validate-only', action='store_true')
    p.add_argument('--frames', type=int, default=32)
    p.add_argument('--workers', type=int, default=2)
    p.add_argument('--output', type=Path, default=ROOT/'SourceArt/GreatStrider/MotionReview')
    args = p.parse_args()
    if args.frames < 8 or args.workers < 1:
        p.error('Use at least 8 frames and 1 worker')
    study = Study()
    report = study.validate()
    print(json.dumps(report, indent=2), flush=True)
    if args.validate_only:
        return
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output/'checks.json').write_text(json.dumps(report,indent=2)+'\n')
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        frames = list(pool.map(frame, [(i,args.frames) for i in range(args.frames)]))
    duration = study.config['cycle_seconds']*1000/args.frames
    frames[0].save(args.output/'walk.gif',save_all=True,append_images=frames[1:],duration=duration,loop=0)
    frames[0].save(args.output/'preview.png')
    encoded = []
    for im in frames:
        stream = io.BytesIO(); im.save(stream,format='PNG')
        encoded.append('data:image/png;base64,'+base64.b64encode(stream.getvalue()).decode())
    html = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>Great Strider motion review</title><style>body{background:#20262b;color:#eee;font:16px system-ui;max-width:1120px;margin:24px auto;padding:16px}img{width:100%;max-width:1080px}button,select,input{margin:10px;padding:10px}p{line-height:1.5}</style>
<h1>Great Strider — motion study 02</h1><p>Actual study 04 mesh. Provisional slow crawl; not a locked gait or game-ready animation.</p>
<img id="view" alt="Great Strider walking study"><br><button id="play">Pause</button><label>Speed <select id="speed"><option value="0.25">0.25×</option><option value="0.5">0.5×</option><option selected value="1">1×</option></select></label><br>
<label>Frame <input id="scrub" type="range" min="0" value="0"><output id="counter"></output></label>
<p>Camera follows forward travel. Three or four feet remain planted. The world-fixed grid repeats every stride; it is a review aid, not a battlefield. Body load shifts, head/sail response and shared cradle/operator pitch are art-directed. Preliminary two-bone legs, fixed lower bends and rigid contacts require later deformation work. Collision clearance and physical balance are unverified.</p>
<script>const frames=FRAMES,seconds=SECONDS;let i=0,playing=true,last=0,elapsed=0;const view=document.getElementById('view'),scrub=document.getElementById('scrub'),counter=document.getElementById('counter'),button=document.getElementById('play');scrub.max=frames.length-1;function show(){view.src=frames[i];scrub.value=i;counter.textContent=`${i+1} / ${frames.length}`;}button.onclick=()=>{playing=!playing;button.textContent=playing?'Pause':'Play';};scrub.oninput=()=>{playing=false;button.textContent='Play';i=Number(scrub.value);elapsed=0;show();};function tick(now){if(playing&&last){elapsed+=Math.min(now-last,250)*Number(document.getElementById('speed').value);const step=seconds*1000/frames.length;while(elapsed>=step){i=(i+1)%frames.length;elapsed-=step;show();}}last=now;requestAnimationFrame(tick);}show();requestAnimationFrame(tick);</script></html>'''
    (args.output/'index.html').write_text(html.replace('FRAMES',json.dumps(encoded)).replace('SECONDS',str(study.config['cycle_seconds'])))
    print(args.output/'index.html')

if __name__ == '__main__':
    main()
