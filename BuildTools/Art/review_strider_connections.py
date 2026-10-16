#!/usr/bin/env python3
"""Connection and rider refinement review against the PR 52 source mesh."""
import json
import numpy as np
from PIL import Image, ImageDraw
from build_great_strider import ROOT
from review_strider_surface import load_meshes, SURFACE_RELEASE
from render_great_strider import render, font

OUTPUT=ROOT/'SourceArt/GreatStrider/ConnectionReview'


def inside(point,part):
    """Odd/even ray test with duplicate edge hits coalesced."""
    v=np.asarray(part['vertices']); f=np.asarray(part['faces']); a,b,c=v[f[:,0]],v[f[:,1]],v[f[:,2]]
    direction=np.array([1.,.371,.217]);e1=b-a;e2=c-a
    h=np.cross(direction,e2);det=np.sum(e1*h,axis=1);valid=abs(det)>1e-12
    inv=np.divide(1,det,out=np.zeros_like(det),where=valid);s=point-a
    u=inv*np.sum(s*h,axis=1);q=np.cross(s,e1);w=inv*(q@direction);t=inv*np.sum(e2*q,axis=1)
    hits=np.sort(t[valid&(u>=-1e-9)&(w>=-1e-9)&(u+w<=1+1e-9)&(t>1e-8)])
    unique=hits[np.r_[True,np.diff(hits)>1e-7]] if len(hits) else hits
    return len(unique)%2==1


def cap_points(part,end=False):
    sides=part['loft_sides'];v=np.array(part['vertices']);rows=(len(v)-2)//sides
    return v[(rows-1)*sides:rows*sides] if end else v[:sides]


def checks(before,after):
    old={p['name']:p for p in before['parts']};new={p['name']:p for p in after['parts']}
    # Equipment, seat, contact pads, sail shape and creature head are protected.
    protected=[k for k in old if k.startswith(('Cradle_','Waycaster_','Ilyth_Contact','Ilyth_SensorySail')) or k in ('Ilyth_Torso','Ilyth_CompactHead')]
    for k in protected:
        assert old[k]['vertices']==new[k]['vertices'] and old[k]['faces']==new[k]['faces'],k
    caps={}
    for key in ('L_Fore','R_Fore','L_Rear','R_Rear'):
        name='Ilyth_Limb_'+key
        caps[name]=sum(inside(v,new['Ilyth_Torso']) for v in cap_points(new[name]))
    for end,host in ((False,'Ilyth_Torso'),(True,'Ilyth_CompactHead')):
        name='Ilyth_ShortNeck';caps[name+('_tip' if end else '_root')]=sum(inside(v,new[host]) for v in cap_points(new[name],end))
    # All end-cap ring vertices should be buried; separate meshes are not welded.
    assert all(n==32 for n in caps.values()),caps
    before_v=np.concatenate([p['vertices'] for p in before['parts']]);after_v=np.concatenate([p['vertices'] for p in after['parts']])
    error=float(np.max(abs(np.array([before_v.min(0),before_v.max(0)])-np.array([after_v.min(0),after_v.max(0)]))))
    assert error<1e-12
    # Below the hip transition the original limb vertices are exactly retained.
    for key in ('L_Fore','R_Fore','L_Rear','R_Rear'):
        name='Ilyth_Limb_'+key;a=np.array(old[name]['vertices']);b=np.array(new[name]['vertices'])
        assert np.allclose(a[6*32:-2],b[6*32:-2],atol=1e-12)
    # Exercise these six connections throughout the existing turning/walking route.
    from route_great_strider import Route
    route=Route();source={p['name']:p for p in route.rig.mesh.parts};sample_count=17
    cap_vertex_samples=0
    for t in np.linspace(0,route.seconds,sample_count):
        posed={p['name']:p for p in route.pose(float(t))['parts']}
        for name in ['Ilyth_Limb_'+key for key in route.rig.bones]+['Ilyth_ShortNeck']:
            v=np.array(posed[name]['vertices']);n=source[name]['loft_sides']
            hosts=[(False,'Ilyth_Torso'),(True,'Ilyth_CompactHead')] if name=='Ilyth_ShortNeck' else [(False,'Ilyth_Torso')]
            for end,host in hosts:
                points=v[-2-n:-2] if end else v[:n]
                assert all(inside(p,posed[host]) for p in points),(t,name,end)
                cap_vertex_samples+=len(points)
    assert 'Avarin_Face' not in new, 'The overlapping face mask must remain removed'
    return dict(route_samples=sample_count,contained_route_cap_vertices=cap_vertex_samples,baseline=SURFACE_RELEASE,parts=len(after['parts']),protected_parts=len(protected),
                buried_cap_ring_vertices=caps,max_envelope_change_m=error,
                triangles=sum(len(p['faces']) for p in after['parts']),
                scope='Sampled cap containment and protected geometry, not a welded mesh or full collision certificate')


def main():
    before,after=load_meshes(SURFACE_RELEASE,None); report=checks(before,after)
    OUTPUT.mkdir(parents=True,exist_ok=True);(OUTPUT/'checks.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2),flush=True)
    rows=[('Neck and hip joins',(6,7,10),(0,5.35,.85),1.9,'all'),
          ('Mineral / upper forelimb',(6,4,8),(-.8,4.1,1),1.9,'limb'),
          ('Avarin face / isolated',(1,4.8,7),(0,4.72,-.10),.49,'face'),
          ('Hands / clothing / cutaway',(5,5,8),(0,4.15,.25),1.2,'rider')]
    sheet=Image.new('RGB',(1200,2250),'#eeede5');d=ImageDraw.Draw(sheet)
    d.text((24,18),'GREAT STRIDER / CONNECTION STUDY 07',font=font(27),fill='#262c32')
    d.text((24,58),'Actual mesh / identical cameras / neutral pose / not final game art',font=font(17),fill='#596069')
    for row,(label,eye,target,extent,scope) in enumerate(rows):
        for col,(title,data) in enumerate((('BEFORE / PR 52',before),('AFTER / STUDY 07',after))):
            if scope=='limb':data={**data,'parts':[p for p in data['parts'] if 'L_Fore' in p['name']]}
            if scope=='face':data={**data,'parts':[p for p in data['parts'] if p['name'].startswith('Avarin_') and any(k in p['name'] for k in ('Head','Face','Eye_','Lid_','Nose','Mouth','Temple','Forehead','Neck'))]}
            if scope=='rider':data={**data,'parts':[p for p in data['parts'] if p['name'].startswith(('Avarin_','Cradle_','Waycaster_'))]}
            tile=render(data,eye,(600,500),extent,target,False);sheet.paste(tile,(col*600,130+row*530))
            d.text((col*600+14,100+row*530),title+' — '+label,font=font(15),fill='#262c32')
    sheet.save(OUTPUT/'comparison.png')
    mobile=Image.new('RGB',(780,844),'#eeede5');d=ImageDraw.Draw(mobile)
    for col,(title,data) in enumerate((('BEFORE',before),('AFTER',after))):
        mobile.paste(render(data,(8,12,10),(390,620),9.5),(col*390,100))
        d.text((col*390+16,30),title+' / 390 px portrait',font=font(18),fill='#262c32')
    d.text((16,775),'Actual source mesh / proposed camera / no Unity claim',font=font(17),fill='#596069');mobile.save(OUTPUT/'mobile.png')

if __name__=='__main__':main()
