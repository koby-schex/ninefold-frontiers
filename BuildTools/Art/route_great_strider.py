#!/usr/bin/env python3
"""One continuous world-space route; no clip-local pose or secondary-motion resets."""
import argparse
import base64
from concurrent.futures import ProcessPoolExecutor
import io
import json
import math
from pathlib import Path
import re
import numpy as np
from PIL import Image, ImageDraw
from move_great_strider import Study, ROOT, smooth, unit
from sequence_great_strider import yaw
from render_great_strider import render, font

OUTPUT = ROOT/'SourceArt/GreatStrider/MotionRoute'


def travel_progress(t, duration):
    """Smooth acceleration, constant-speed middle, smooth deceleration."""
    ramp=duration*.2
    t=float(np.clip(t,0,duration))
    integral=lambda u:u**6-3*u**5+2.5*u**4
    speed=1/(duration-ramp)
    if t<ramp:
        return speed*ramp*integral(t/ramp)
    if t>duration-ramp:
        return 1-speed*ramp*integral((duration-t)/ramp)
    return speed*(t-ramp*.5)


class Route:
    def __init__(self):
        self.rig=Study()
        self.config=json.loads((ROOT/'SourceArt/GreatStrider/motion-route.json').read_text())
        self.order=self.config['step_order']
        assert len(self.order)==4 and set(self.order)==set(self.rig.bones)
        self.segments=[]
        time=0.; position=np.zeros(3); heading=0.
        for entry in self.config['segments']:
            segment=dict(entry,start=time,position=position.copy(),heading=heading)
            kind=entry['kind']
            assert kind in ('idle','walk','turn')
            if kind=='idle':
                duration=entry['seconds']; events=[]
            else:
                cycles=entry.get('cycles',1)
                assert isinstance(cycles,int) and cycles>=1
                slot=entry['step_slot']; swing=entry['swing_seconds']
                assert 0<swing<=slot
                events=[dict(key=key,start=time+i*slot,end=time+i*slot+swing,cycle=i//4)
                        for i,key in enumerate(self.order*cycles)]
                duration=events[-1]['end']-time
            assert duration>0
            segment.update(end=time+duration,events=events)
            self.segments.append(segment)
            if kind=='walk':
                position=position+yaw(heading)@np.array([0,0,entry['cycles']*self.rig.config['stride_m']])
            elif kind=='turn':
                heading+=math.radians(entry['degrees'])
            time+=duration
        self.seconds=time
        self.destination=position
        self.final_heading=heading

    def state(self, seconds):
        t=float(np.clip(seconds,0,self.seconds))
        s=next((s for s in self.segments if t<s['end']),self.segments[-1])
        local=t-s['start']; duration=s['end']-s['start']; kind=s['kind']
        progress=travel_progress(local,duration) if kind=='walk' else float(smooth(local/duration))
        heading=s['heading']+math.radians(s.get('degrees',0))*progress
        rotation=yaw(heading)
        center=s['position'].copy()
        if kind=='walk':
            center+=yaw(s['heading'])@np.array([0,0,s['cycles']*self.rig.config['stride_m']*progress])
        phase=t/self.config['secondary_cycle_seconds']
        root=center+[0,-self.rig.config['body_settle_m']+self.config['breath_m']*math.sin(2*math.pi*phase),0]
        active=next((e for e in s['events'] if e['start']<t<e['end']),None)
        if active:
            u=(t-active['start'])/(active['end']-active['start'])
            side=-1 if active['key'].startswith('L') else 1
            root+=rotation@np.array([-side*self.rig.config['body_sway_m']*math.sin(math.pi*u)**4,0,0])
        joints={}
        for key,rest in self.rig.bones.items():
            events=[e for e in s['events'] if e['key']==key and t>=e['start']]
            event=events[-1] if events else None
            u=float(np.clip((t-event['start'])/(event['end']-event['start']),0,1)) if event else 0.
            eased=float(smooth(u)); lift=self.rig.config['lift_m']*math.sin(math.pi*u)**4
            foot_heading=s['heading']+math.radians(s.get('degrees',0))*eased
            foot_rotation=yaw(foot_heading)
            foot=foot_rotation@rest[2]+s['position']+[0,lift,0]
            if kind=='walk' and event:
                foot+=yaw(s['heading'])@np.array([0,0,(event['cycle']+eased)*self.rig.config['stride_m']])
            hip=rotation@rest[0]+root
            a,b=np.linalg.norm(np.diff(rest,axis=0),axis=1)
            distance=np.linalg.norm(foot-hip)
            if not abs(a-b)+1e-7<distance<a+b-1e-7:
                raise ValueError(f'Unreachable {key} at {t:.5f}s ({kind})')
            axis=unit(foot-hip); pole=rotation@(rest[1]-rest[0]); bend=unit(pole-axis*(pole@axis))
            along=(a*a-b*b+distance*distance)/(2*distance)
            knee=hip+axis*along+bend*math.sqrt(max(0,a*a-along*along))
            joints[key]=dict(points=np.array([hip,knee,foot]),rotation=foot_rotation,
                             swing=bool(event and event['start']<t<event['end']),lift=lift)
        label=s['label']
        if kind=='walk':
            label+=' / '+('starting' if local<duration*.2 else 'stopping' if local>duration*.8 else 'walking')
        return dict(root=root,center=center,joints=joints,rotation=rotation,phase=phase,label=label)

    def pose(self, seconds):
        s=self.state(seconds)
        return self.rig.deform(s['phase'],s['root'],s['joints'],s['rotation'],world=True)

    def validate(self):
        slide=twist=length=0.; minimum_support=4; previous=None
        samples=math.ceil(self.seconds*60)+1
        for t in np.linspace(0,self.seconds,samples):
            state=self.state(t); joints=state['joints']
            minimum_support=min(minimum_support,sum(not j['swing'] for j in joints.values()))
            for key,j in joints.items():
                expected=np.linalg.norm(np.diff(self.rig.bones[key],axis=0),axis=1)
                length=max(length,float(np.max(abs(np.linalg.norm(np.diff(j['points'],axis=0),axis=1)-expected))))
                if previous and not j['swing'] and not previous[key]['swing']:
                    slide=max(slide,float(np.max(abs(j['points'][2]-previous[key]['points'][2]))))
                    twist=max(twist,float(np.max(abs(j['rotation']-previous[key]['rotation']))))
            previous=joints
        assert minimum_support>=3 and max(slide,twist,length)<1e-8
        # Check the actual mesh on both sides of every join, not only root transforms.
        join_jump=velocity_jump=0.; eps=1e-5
        for segment in self.segments[:-1]:
            t=segment['end']
            before=self.pose(t-eps)['parts']; at=self.pose(t)['parts']; after=self.pose(t+eps)['parts']
            for a,b,c in zip(before,at,after):
                a,b,c=(np.array(p['vertices']) for p in (a,b,c))
                join_jump=max(join_jump,float(np.max(abs(c-a))))
                velocity_jump=max(velocity_jump,float(np.max(abs((c-b)/eps-(b-a)/eps))))
        assert join_jump<1e-4 and velocity_jump<.01
        floor_error=0.; boundary_speed=0.
        times=set(np.linspace(0,self.seconds,49))
        for segment in self.segments:
            for event in segment['events']:
                for t in (event['start'],event['end']):
                    times.add(t)
                    at=self.state(t)['joints'][event['key']]
                    for sign in (-1,1):
                        near=self.state(t+sign*eps)['joints'][event['key']]
                        boundary_speed=max(boundary_speed,float(np.linalg.norm(near['points'][2]-at['points'][2])/eps))
        assert boundary_speed<1e-5
        # Visible pads, attachment pads and operator-seat spacing on the curved route.
        source={p['name']:p['vertices'] for p in self.rig.mesh.parts}
        assembly=[k for k in source if k.startswith(('Avarin_','Waycaster_')) or k=='Cradle_Seat']
        reference=np.array([source[k][0] for k in assembly]); distances=np.linalg.norm(reference-reference[0],axis=1)
        rigidity=0.
        for t in sorted(times):
            state=self.state(t); parts=self.pose(t)['parts']; data={p['name']:np.array(p['vertices']) for p in parts}
            assert all(np.isfinite(v).all() for v in data.values())
            for key,j in state['joints'].items():
                pads=np.array([v for name,vertices in data.items() if 'Contact' in name and key in name for v in vertices])
                floor_error=max(floor_error,abs(float(pads[:,1].min())-j['lift']))
                # The complete visible pad must share the IK foot transform.
                for name in data:
                    if 'Contact' in name and key in name:
                        expected=(source[name]-self.rig.bones[key][2])@j['rotation'].T+j['points'][2]
                        assert np.max(abs(expected-data[name]))<1e-8
            points=np.array([data[k][0] for k in assembly])
            rigidity=max(rigidity,float(np.max(abs(np.linalg.norm(points-points[0],axis=1)-distances))))
            for name in data:
                if name.startswith('Cradle_Attachment'):
                    assert np.max(abs(data[name]-(source[name]@state['rotation'].T+state['root'])))<1e-8
        assert max(floor_error,rigidity)<1e-8
        # Walks after turns advance along the new heading; the two-cycle middle never stops.
        for segment in self.segments:
            if segment['kind']=='walk':
                mid=(segment['start']+segment['end'])/2
                velocity=(self.state(mid+eps)['center']-self.state(mid-eps)['center'])/(2*eps)
                direction=yaw(segment['heading'])@np.array([0,0,1])
                assert velocity@direction>.01 and np.linalg.norm(np.cross(velocity,direction))<1e-8
        final=self.state(self.seconds)
        assert np.linalg.norm(final['center']-self.destination)<1e-8
        assert all(not j['swing'] for j in final['joints'].values())
        return dict(seconds=self.seconds,skeleton_samples=samples,mesh_samples=len(times),minimum_support_feet=minimum_support,
                    max_stance_slide_m=slide,max_stance_rotation_error=twist,max_bone_length_error_m=length,
                    max_join_displacement_over_20_microseconds_m=join_jump,max_join_velocity_difference_m_per_second=velocity_jump,
                    max_pad_floor_error_m=floor_error,max_foot_boundary_speed_m_per_second=boundary_speed,
                    max_operator_seat_rigidity_error_m=rigidity,destination_m=self.destination.tolist(),
                    scope='Scripted continuous route; physical balance, full collisions and runtime integration unverified')


def review_ground(data, camera):
    parts=list(data['parts'])
    def quad(name,vertices,material):
        parts.append(dict(name=name,material=material,vertices=(np.array(vertices)-camera).tolist(),faces=[[0,2,1],[0,3,2]]))
    quad('ground',[[-5,-.02,-5],[5,-.02,-5],[5,-.02,7],[-5,-.02,7]],'ground')
    for i in range(-10,15):
        z=i*.5
        quad('z'+str(i),[[-5,-.01,z-.012],[5,-.01,z-.012],[5,-.01,z+.012],[-5,-.01,z+.012]],'grid')
    for i in range(-10,11):
        x=i*.5
        quad('x'+str(i),[[x-.012,-.01,-5],[x+.012,-.01,-5],[x+.012,-.01,7],[x-.012,-.01,7]],'grid')
    return dict(parts=parts,materials={**data['materials'],'ground':(.64,.66,.64),'grid':(.37,.40,.40)},finish=data['finish'])


def frame(args):
    index,count=args; route=Route(); t=route.seconds*index/(count-1); state=route.state(t)
    data=route.pose(t); camera=state['center'].copy(); camera[1]=0
    for p in data['parts']:
        p['vertices']=(np.array(p['vertices'])-camera).tolist()
    data=review_ground(data,camera)
    im=Image.new('RGB',(1080,610),(224,224,216)); d=ImageDraw.Draw(im)
    im.paste(render(data,(9,6.5,12),(680,500),8.2,fit=False),(0,60))
    d.text((16,12),'GREAT STRIDER / CONTINUOUS ROUTE / STUDY 04',font=font(21),fill='#252c32')
    d.text((16,42),f'{t:.2f}s / {state["label"]}',font=font(16),fill='#252c32')
    d.rounded_rectangle((705,85,1060,490),radius=12,fill='#273440')
    d.text((725,100),'WORLD ROUTE / TOP VIEW',font=font(17),fill='#f0e6cf')
    # Fixed world scale and bounds across every frame. This is a route diagram, not a battlefield.
    point=lambda v:(880+float(v[0])*55,350-float(v[2])*55)
    path=[point(route.state(s)['center']) for s in np.linspace(0,route.seconds,200)]
    d.line(path,fill='#8e9d9f',width=3)
    now=point(state['center']); forward=state['rotation']@np.array([0,0,.35]); tip=point(state['center']+forward)
    d.line([now,tip],fill='#e9c47f',width=5)
    d.ellipse((now[0]-6,now[1]-6,now[0]+6,now[1]+6),fill='#e9c47f')
    for j in state['joints'].values():
        x,y=point(j['points'][2]); color='#efbd75' if j['swing'] else '#8abfc1'
        d.ellipse((x-3,y-3,x+3,y+3),fill=color)
    d.text((725,460),'Gold: heading / lifting foot',font=font(14),fill='#f0e6cf')
    d.text((16,565),'Actual mesh / fixed world heading / camera follows travel',font=font(16),fill='#252c32')
    d.text((16,590),'Provisional motion. Full collision clearance and physical balance remain unverified.',font=font(14),fill='#444c52')
    return im


def render_chunk(index,chunks,frames,workers,output):
    output.mkdir(parents=True,exist_ok=True)
    indices=list(range(index,frames,chunks))
    with ProcessPoolExecutor(max_workers=workers) as pool:
        images=list(pool.map(frame,[(i,frames) for i in indices]))
    encoded=[]
    for i,im in zip(indices,images):
        stream=io.BytesIO(); im.save(stream,format='PNG')
        encoded.append(dict(index=i,image='data:image/png;base64,'+base64.b64encode(stream.getvalue()).decode()))
    (output/f'chunk-{index}.json').write_text(json.dumps(dict(frame_count=frames,chunk_count=chunks,frames=encoded)),encoding='utf-8')
    print(f'Rendered chunk {index}: {len(indices)} frames',flush=True)


def pack(output,frames,chunks):
    data={}
    for index in range(chunks):
        part=json.loads((output/f'chunk-{index}.json').read_text(encoding='utf-8'))
        assert part['frame_count']==frames and part['chunk_count']==chunks
        for entry in part['frames']:
            assert entry['index'] not in data
            data[entry['index']]=entry['image']
    assert sorted(data)==list(range(frames)), 'Missing or duplicate route frames'
    route=Route(); report=route.validate()
    clip=dict(label='Complete route',seconds=route.seconds,loop=False,frames=[data[i] for i in range(frames)])
    template=Path(__file__).with_name('strider_sequence_viewer.html').read_text(encoding='utf-8')
    template=template.replace('Motion Study 03','Motion Study 04').replace('clips.idle','clips.route')
    template=re.sub(r'<select id="clip">.*?</select>','<select id="clip"><option value="route">Complete route</option></select>',template)
    template=template.replace('Inspect the actual study mesh in synchronized three-quarter, front and side views.', 'Inspect a continuous movement route with an actual-mesh view and world-route diagram.')
    template=template.replace('Great Strider movement in three synchronized views','Great Strider continuous route and world-route diagram')
    template=re.sub(r'<p class="note">.*?</p>','<p class="note">One continuous route: idle, start, walk, stop, left turn, walk, right turn, walk and settle. The final pose holds. Restart resets the entire review; it is not a motion transition. Scrubbing pauses playback.</p>',template)
    template=template.replace('/*CLIP_DATA*/',json.dumps(dict(route=clip)))
    (output/'index.html').write_text(template,encoding='utf-8')
    (output/'checks.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    (output/'preview.png').write_bytes(base64.b64decode(data[0].split(',',1)[1]))
    print(output/'index.html')


def main():
    p=argparse.ArgumentParser(); p.add_argument('--validate-only',action='store_true');p.add_argument('--pack',action='store_true')
    p.add_argument('--chunk',type=int);p.add_argument('--chunks',type=int,default=4);p.add_argument('--frames',type=int,default=192)
    p.add_argument('--workers',type=int,default=2);p.add_argument('--output',type=Path,default=OUTPUT)
    a=p.parse_args()
    if a.frames<32 or a.chunks<1 or a.chunks>a.frames or a.workers<1 or (a.chunk is not None and not 0<=a.chunk<a.chunks):
        p.error('Invalid frame, chunk or worker counts')
    if a.validate_only:
        print(json.dumps(Route().validate(),indent=2));return
    if a.pack:
        pack(a.output,a.frames,a.chunks);return
    print(json.dumps(Route().validate(),indent=2),flush=True)
    for chunk in ([a.chunk] if a.chunk is not None else range(a.chunks)):
        render_chunk(chunk,a.chunks,a.frames,a.workers,a.output)
    if a.chunk is None:
        pack(a.output,a.frames,a.chunks)

if __name__=='__main__':
    main()
