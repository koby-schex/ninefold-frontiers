#!/usr/bin/env python3
"""Offline motion study 03: idle, controlled travel and a foot-planted stepping turn."""
import argparse
import base64
from concurrent.futures import ProcessPoolExecutor
import io
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from move_great_strider import Study, ROOT, smooth, unit, ground
from render_great_strider import render, font

CONFIG = ROOT/'SourceArt/GreatStrider/motion-sequences.json'
OUTPUT = ROOT/'SourceArt/GreatStrider/MotionSequences'


def yaw(angle):
    c, s = math.cos(angle), math.sin(angle)
    return np.array([[c,0,s],[0,1,0],[-s,0,c]])


class Sequences:
    def __init__(self):
        self.rig = Study()
        self.config = json.loads(CONFIG.read_text())
        assert len(self.config['step_order']) == 4
        assert set(self.config['step_order']) == set(self.rig.bones)
        for key in ('start_stop','turn'):
            c = self.config[key]
            assert 0 < c['swing_seconds'] <= c['step_slot_seconds']
            assert 0 < c['start_seconds'] < self.events(key)[-1][2] < c['seconds']

    def events(self, clip):
        if clip in ('idle','walk'):
            return []
        c = self.config[clip]
        return [(key,c['start_seconds']+i*c['step_slot_seconds'],
                 c['start_seconds']+i*c['step_slot_seconds']+c['swing_seconds'])
                for i,key in enumerate(self.config['step_order'])]

    def state(self, clip, seconds):
        c = self.config[clip]
        seconds = float(np.clip(seconds,0,c['seconds']))
        phase = seconds/c['seconds']
        if clip == 'walk':
            root, joints = self.rig.joints(phase)
            return root,joints,np.eye(3),phase,'walking'
        events = self.events(clip)
        progress = float(smooth((seconds-c['start_seconds'])/
                               (events[-1][2]-c['start_seconds']))) if events else 0.
        heading = math.radians(c.get('turn_degrees',0))*progress
        travel = np.array([0.,0.,c.get('distance_m',0)*progress])
        root = travel+np.array([0.,-self.rig.config['body_settle_m']+
                               self.config['breath_m']*math.sin(2*math.pi*phase),0.])
        rotation = yaw(heading)
        result = {}
        active = next(((key,start,end) for key,start,end in events if start<seconds<end),None)
        if active:
            key,start,end = active
            u = (seconds-start)/(end-start)
            side = -1 if key.startswith('L') else 1
            root[0] -= side*self.rig.config['body_sway_m']*math.sin(math.pi*u)**4
        for key,rest in self.rig.bones.items():
            event = next((e for e in events if e[0]==key),None)
            u = float(np.clip((seconds-event[1])/(event[2]-event[1]),0,1)) if event else 0.
            eased = float(smooth(u))
            foot_heading = math.radians(c.get('turn_degrees',0))*eased
            foot_rotation = yaw(foot_heading)
            lift = self.rig.config['lift_m']*math.sin(math.pi*u)**4
            # Foot position and orientation both stop while the foot is planted.
            foot = foot_rotation@rest[2]+np.array([0.,lift,c.get('distance_m',0)*eased])
            hip = rotation@rest[0]+root
            a,b = np.linalg.norm(np.diff(rest,axis=0),axis=1)
            distance = np.linalg.norm(foot-hip)
            if not abs(a-b)+1e-7 < distance < a+b-1e-7:
                raise ValueError(f'{clip} {key}: unreachable foot at {seconds:.5f}s')
            axis = unit(foot-hip)
            pole = rotation@(rest[1]-rest[0])
            bend = unit(pole-axis*(pole@axis))
            along = (a*a-b*b+distance*distance)/(2*distance)
            knee = hip+axis*along+bend*math.sqrt(max(0,a*a-along*along))
            result[key] = dict(points=np.array([hip,knee,foot]),rotation=foot_rotation,
                               swing=bool(event and event[1]<seconds<event[2]),lift=lift)
        label = 'idle' if not events else ('preparing' if seconds<events[0][1] else
                'settled' if seconds>=events[-1][2] else 'stepping turn' if clip=='turn' else 'moving')
        return root,result,rotation,phase,label

    def pose(self, clip, seconds, world=False):
        root,joints,rotation,phase,_ = self.state(clip,seconds)
        return self.rig.deform(phase,root,joints,rotation,world=world)

    def validate(self, clip):
        c = self.config[clip]
        max_slide = max_rotation_slide = max_length = 0.
        previous = None
        minimum_support = 4
        for seconds in np.linspace(0,c['seconds'],601):
            _,joints,_,_,_ = self.state(clip,seconds)
            minimum_support = min(minimum_support,sum(not j['swing'] for j in joints.values()))
            for key,j in joints.items():
                expected = np.linalg.norm(np.diff(self.rig.bones[key],axis=0),axis=1)
                max_length = max(max_length,float(np.max(abs(np.linalg.norm(np.diff(j['points'],axis=0),axis=1)-expected))))
                if previous and not j['swing'] and not previous[key]['swing']:
                    max_slide = max(max_slide,float(np.linalg.norm(j['points'][2]-previous[key]['points'][2])))
                    max_rotation_slide = max(max_rotation_slide,float(np.max(abs(j.get('rotation',np.eye(3))-previous[key].get('rotation',np.eye(3))))))
            previous = joints
        assert minimum_support >= 3
        assert max(max_slide,max_rotation_slide,max_length) < 1e-8
        max_floor_error = max_visible_slide = 0.
        previous_pads = None
        previous_joints = None
        for seconds in np.linspace(0,c['seconds'],33):
            data = self.pose(clip,seconds,world=True)
            _,joints,_,_,_ = self.state(clip,seconds)
            pads = {}
            for key,j in joints.items():
                pads[key] = np.array([v for p in data['parts'] if 'Contact' in p['name'] and key in p['name'] for v in p['vertices']])
                expected = j.get('lift',j.get('delta',np.zeros(3))[1])
                max_floor_error = max(max_floor_error,abs(float(pads[key][:,1].min())-expected))
                if previous_pads is not None and not j['swing'] and not previous_joints[key]['swing']:
                    max_visible_slide = max(max_visible_slide,float(np.max(abs(pads[key]-previous_pads[key]))))
            assert all(np.isfinite(p['vertices']).all() for p in data['parts'])
            previous_pads,previous_joints = pads,joints
        assert max(max_floor_error,max_visible_slide) < 1e-8
        max_boundary_speed = 0.
        for key,start,end in self.events(clip):
            for boundary in (start,end):
                at = self.state(clip,boundary)[1][key]
                for sign in (-1,1):
                    nearby = self.state(clip,boundary+sign*1e-5)[1][key]
                    speed = np.linalg.norm(nearby['points'][2]-at['points'][2])/1e-5
                    max_boundary_speed = max(max_boundary_speed,float(speed))
        assert max_boundary_speed < 1e-5
        if clip in ('idle','walk'):
            a,b = self.pose(clip,0),self.pose(clip,c['seconds'])
            assert max(np.max(abs(np.array(p['vertices'])-np.array(q['vertices']))) for p,q in zip(a['parts'],b['parts'])) < 1e-8
        else:
            # All four feet finish in the new settled stance, not mid-swing.
            root,joints,rotation,_,_ = self.state(clip,c['seconds'])
            for key,j in joints.items():
                assert not j['swing']
                target = rotation@self.rig.bones[key][2]+[0,0,root[2]]
                assert np.linalg.norm(j['points'][2]-target) < 1e-8
            # The settled mesh matches idle after undoing only destination travel/yaw.
            neutral = self.pose('idle',0,world=True)
            for boundary in (0,c['seconds']):
                position,_,orientation,_,_ = self.state(clip,boundary)
                pose = self.pose(clip,boundary,world=True)
                for rest,part in zip(neutral['parts'],pose['parts']):
                    restored = (np.array(part['vertices'])-[0,0,position[2]])@orientation
                    assert np.max(abs(restored-np.array(rest['vertices']))) < 1e-8
            for boundary in (0,c['seconds']):
                a = self.state(clip,boundary)[0]
                b = self.state(clip,boundary+(1e-5 if boundary==0 else -1e-5))[0]
                assert np.linalg.norm((a-b)[[0,2]])/1e-5 < 1e-5
        return dict(clip=clip,skeleton_samples=601,mesh_samples=33,
                    minimum_support_feet=minimum_support,max_stance_slide_m=max_slide,
                    max_stance_rotation_error=max_rotation_slide,max_bone_length_error_m=max_length,
                    max_visible_pad_slide_m=max_visible_slide,max_floor_error_m=max_floor_error,
                    max_boundary_speed_m_per_second=max_boundary_speed,
                    scope='Kinematic prototype; physical balance and full collisions unverified')


def frame(args):
    clip,index,count = args
    seq = Sequences()
    c = seq.config[clip]
    t = c['seconds']*index/(count if c['loop'] else count-1)
    root,joints,rotation,phase,label = seq.state(clip,t)
    data = ground(seq.pose(clip,t),root[2]/seq.rig.config['stride_m'],seq.rig.config['stride_m'])
    im = Image.new('RGB',(1080,610),(224,224,216)); d = ImageDraw.Draw(im)
    d.text((16,12),'GREAT STRIDER / MOTION STUDY 03 / '+c['label'].upper(),font=font(21),fill='#252c32')
    for j,(name,eye) in enumerate([('THREE-QUARTER',(9,6.5,12)),('FRONT',(0,5,15)),('SIDE',(15,5,0))]):
        im.paste(render(data,eye,(360,500),8.2,fit=False),(j*360,60))
        d.text((j*360+12,42),name,font=font(14),fill='#252c32')
    swing=', '.join(k for k,v in joints.items() if v['swing']) or 'none'
    d.text((16,565),f'{t:.2f}s / {label} / swing: {swing}',font=font(16),fill='#252c32')
    d.text((16,590),'Actual mesh / world-fixed ground references / provisional movement',font=font(14),fill='#444c52')
    return im


def render_clip(clip,count,workers,output):
    seq=Sequences(); c=seq.config[clip]
    report=seq.validate(clip)
    print(json.dumps(report,indent=2),flush=True)
    output.mkdir(parents=True,exist_ok=True)
    with ProcessPoolExecutor(max_workers=workers) as pool:
        images=list(pool.map(frame,[(clip,i,count) for i in range(count)]))
    interval=c['seconds']*1000/(count if c['loop'] else count-1)
    options={'loop':0} if c['loop'] else {}
    images[0].save(output/(clip+'.gif'),save_all=True,append_images=images[1:],duration=interval,**options)
    images[0].save(output/(clip+'-preview.png'))
    frames=[]
    for im in images:
        stream=io.BytesIO(); im.save(stream,format='PNG')
        frames.append('data:image/png;base64,'+base64.b64encode(stream.getvalue()).decode())
    (output/(clip+'.json')).write_text(json.dumps(dict(**c,frames=frames)),encoding='utf-8')
    (output/(clip+'-checks.json')).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(f'Rendered {clip}: {count} frames',flush=True)


def pack(output):
    clips={key:json.loads((output/(key+'.json')).read_text(encoding='utf-8')) for key in ('idle','start_stop','turn','walk')}
    template=(Path(__file__).with_name('strider_sequence_viewer.html')).read_text(encoding='utf-8')
    (output/'index.html').write_text(template.replace('/*CLIP_DATA*/',json.dumps(clips)),encoding='utf-8')
    print(output/'index.html')


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--clip',choices=['idle','start_stop','turn','walk'])
    p.add_argument('--validate-only',action='store_true')
    p.add_argument('--pack',action='store_true')
    p.add_argument('--frames',type=int,default=32)
    p.add_argument('--workers',type=int,default=2)
    p.add_argument('--output',type=Path,default=OUTPUT)
    args=p.parse_args()
    if args.frames<16 or args.workers<1:
        p.error('Use at least 16 frames and one worker')
    if args.validate_only:
        seq=Sequences()
        for clip in ([args.clip] if args.clip else ('idle','start_stop','turn','walk')):
            print(json.dumps(seq.validate(clip),indent=2),flush=True)
    elif args.pack:
        pack(args.output)
    elif args.clip:
        render_clip(args.clip,args.frames,args.workers,args.output)
    else:
        for clip in ('idle','start_stop','turn','walk'):
            render_clip(clip,args.frames,args.workers,args.output)
        pack(args.output)

if __name__=='__main__':
    main()
