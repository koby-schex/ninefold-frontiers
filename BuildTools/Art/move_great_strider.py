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
from PIL import ImageDraw
from build_great_strider import make_model, MATERIALS, FINISH
from render_great_strider import render, font

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


class Study:
    def __init__(self):
        self.config = json.loads(CONFIG.read_text())
        self.mesh = make_model()
        low = min(v[1] for p in self.mesh.parts for v in p['vertices'])
        self.scale = 6 / (max(v[1] for p in self.mesh.parts for v in p['vertices']) - low)
        for p in self.mesh.parts:
            p['vertices'] = (np.array(p['vertices']) - [0, low, 0]) * self.scale
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
        root = np.array([0., -c['body_settle_m'], t*c['stride_m']])
        result = {}
        for name, rest in self.bones.items():
            offset = c['phase_offsets'][name]
            cycle = math.floor(t+offset)
            phase = (t+offset)-cycle
            swing = phase >= c['stance_fraction']
            u = (phase-c['stance_fraction'])/(1-c['stance_fraction']) if swing else 0
            travel = u*u*(3-2*u)
            delta = np.array([0., c['lift_m']*math.sin(math.pi*u)**2,
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
        parts = []
        for part in self.mesh.parts:
            name = part['name']
            v = part['vertices']
            limb = next((key for key in self.bones if key in name), None)
            if limb:
                rest = self.bones[limb]
                new = joints[limb]['points']
                upper = (v-rest[0]) @ rotation(rest[1]-rest[0], new[1]-new[0]).T + new[0]
                lower = (v-rest[1]) @ rotation(rest[2]-rest[1], new[2]-new[1]).T + new[1]
                # Explicit deterministic preliminary weights, blended only near the knee.
                w = np.clip((v[:,1]-rest[1,1]+.28)/.56, 0, 1)[:,None]
                posed = w*upper+(1-w)*lower
                if 'Contact' in name:
                    posed = v+joints[limb]['delta']
                elif 'Ankle' in name:
                    posed = v+joints[limb]['delta']
            else:
                posed = v+root
                # Small rigid sail response only; head, cradle and operator follow the body.
                if any(s in name for s in ('SensorySail', 'SailVein', 'SailChannel', 'SailRoot')):
                    sign = -1 if '_L' in name else 1
                    angle = sign*.012*math.sin(2*math.pi*t)
                    r = np.array([[math.cos(angle),-math.sin(angle),0],
                                  [math.sin(angle),math.cos(angle),0],[0,0,1]])
                    pivot = np.array([sign*.26, 4.9, .55])*self.scale+root
                    posed = (posed-pivot)@r.T+pivot
            parts.append(dict(name=name, material=part['material'], faces=part['faces'],
                              vertices=(posed-[0,0,root[2]]).tolist()))
        return dict(parts=parts, materials=MATERIALS, finish=FINISH)

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
        p0, p1 = self.pose(0), self.pose(1)
        closure = max(np.max(abs(np.array(a['vertices'])-np.array(b['vertices'])))
                      for a,b in zip(p0['parts'],p1['parts']))
        assert closure < 1e-8
        assert all(np.isfinite(p['vertices']).all() for p in p0['parts'])
        return dict(samples=601, cycles=3, max_stance_slide_m=max_slide,
                    max_bone_length_error_m=max_length, loop_error_m=float(closure),
                    minimum_support_feet=3,
                    scope='Kinematic checks only; collisions, mass balance and final skinning unverified')


def frame(args):
    index, count = args
    study = Study()
    t = index/count
    data = study.pose(t)
    im = render(data, (9,6.5,12), (480,620), 7.8, fit=False)
    d = ImageDraw.Draw(im)
    d.text((12,12), 'GREAT STRIDER / MOTION STUDY 01', font=font(17), fill='#252c32')
    d.text((12,38), 'Provisional crawl - actual mesh', font=font(14), fill='#444c52')
    _, joints = study.joints(t)
    d.text((12,570), 'Swing: '+(', '.join(k for k,j in joints.items() if j['swing']) or 'none'),
           font=font(15), fill='#252c32')
    d.text((12,595), 'Tracking camera / no physical balance claim', font=font(12), fill='#444c52')
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
<title>Great Strider motion review</title><style>body{background:#20262b;color:#eee;font:16px system-ui;max-width:650px;margin:24px auto;padding:16px}img{width:100%;max-width:480px}button,select,input{margin:10px;padding:10px}p{line-height:1.5}</style>
<h1>Great Strider — motion study 01</h1><p>Actual study 04 mesh. Provisional slow crawl; not a locked gait or game-ready animation.</p>
<img id="view" alt="Great Strider walking study"><br><button id="play">Pause</button><label>Speed <select id="speed"><option value="0.25">0.25×</option><option value="0.5">0.5×</option><option selected value="1">1×</option></select></label><br>
<label>Frame <input id="scrub" type="range" min="0" value="0"><output id="counter"></output></label>
<p>Camera follows forward travel. Three or four feet remain planted. Cradle and operator follow the body rigidly. Preliminary two-bone legs, fixed lower bends and rigid contacts require later deformation work. Collision clearance and physical balance are unverified.</p>
<script>const frames=FRAMES,seconds=SECONDS;let i=0,playing=true,last=0,elapsed=0;const view=document.getElementById('view'),scrub=document.getElementById('scrub'),counter=document.getElementById('counter'),button=document.getElementById('play');scrub.max=frames.length-1;function show(){view.src=frames[i];scrub.value=i;counter.textContent=`${i+1} / ${frames.length}`;}button.onclick=()=>{playing=!playing;button.textContent=playing?'Pause':'Play';};scrub.oninput=()=>{playing=false;button.textContent='Play';i=Number(scrub.value);elapsed=0;show();};function tick(now){if(playing&&last){elapsed+=Math.min(now-last,250)*Number(document.getElementById('speed').value);const step=seconds*1000/frames.length;while(elapsed>=step){i=(i+1)%frames.length;elapsed-=step;show();}}last=now;requestAnimationFrame(tick);}show();requestAnimationFrame(tick);</script></html>'''
    (args.output/'index.html').write_text(html.replace('FRAMES',json.dumps(encoded)).replace('SECONDS',str(study.config['cycle_seconds'])))
    print(args.output/'index.html')

if __name__ == '__main__':
    main()
