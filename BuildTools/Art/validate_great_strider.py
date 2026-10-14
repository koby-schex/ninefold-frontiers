#!/usr/bin/env python3
"""Geometric integrity of the study, not final art or rig certification."""
import collections
import math
import tempfile
from pathlib import Path
from build_great_strider import make_model, write, ROOT, cross, sub, dot

folder=ROOT/'SourceArt/GreatStrider'
with tempfile.TemporaryDirectory() as temp:
    write(make_model(),Path(temp))
    for name in ['GreatStrider.obj','GreatStrider.mtl','manifest.json']:
        assert (Path(temp)/name).read_bytes()==(folder/name).read_bytes(),f'Stale output: {name}'
vertices=[]; faces=[]; parts={}; current=None; materials=set()
for line in (folder/'GreatStrider.obj').read_text().splitlines():
    fields=line.split()
    if not fields: continue
    if fields[0]=='o':
        current=fields[1]; assert current not in parts; parts[current]=[]
    elif fields[0]=='v':
        v=tuple(map(float,fields[1:])); assert len(v)==3 and all(map(math.isfinite,v)); vertices.append(v)
    elif fields[0]=='usemtl': materials.add(fields[1])
    elif fields[0]=='f':
        f=tuple(int(x)-1 for x in fields[1:]); assert len(f)==3
        assert len(set(f))==3 and all(0<=x<len(vertices) for x in f)
        normal=cross(sub(vertices[f[1]],vertices[f[0]]),sub(vertices[f[2]],vertices[f[0]]))
        assert dot(normal,normal)>1e-16,('Degenerate triangle',current,f)
        faces.append(f); parts[current].append(f)
for name,triangles in parts.items():
    volume=sum(dot(vertices[a],cross(vertices[b],vertices[c])) for a,b,c in triangles)/6
    assert volume>0,('Inward or zero-volume part',name,volume)
    edges=collections.Counter(); directions=collections.Counter()
    for a,b,c in triangles:
        for u,v in [(a,b),(b,c),(c,a)]:
            edges[tuple(sorted((u,v)))]+=1; directions[u,v]+=1
    assert all(n==2 for n in edges.values()),('Open/nonmanifold part',name)
    assert all(directions[u,v]==directions[v,u] for u,v in edges),('Winding',name)
assert abs(min(v[1] for v in vertices))<1e-5
assert abs(max(v[1] for v in vertices)-6)<1e-5
assert len([n for n in parts if n.startswith('Ilyth_Limb_')])==4
assert len([n for n in parts if n.startswith('Ilyth_SensorySail_')])==2
assert 'Avarin_CrescentMantle' in parts
assert len([n for n in parts if n.startswith('Avarin_SensoryRibbon_')])==6
assert len([n for n in parts if n.startswith('Cradle_Attachment_')])==4
# Focused neutral-pose regressions, not a full intersection/deformation certificate.
def part_vertices(name):
    return [vertices[i] for i in {i for face in parts[name] for i in face}]

def forward_hit(origin, face):
    # Project to XY: a +Z ray intersects where the barycentric point is forward.
    a,b,c=[vertices[i] for i in face]
    den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-12: return False
    u=((b[1]-c[1])*(origin[0]-c[0])+(c[0]-b[0])*(origin[1]-c[1]))/den
    v=((c[1]-a[1])*(origin[0]-c[0])+(a[0]-c[0])*(origin[1]-c[1]))/den
    w=1-u-v
    return min(u,v,w)>=-1e-8 and u*a[2]+v*b[2]+w*c[2]>origin[2]+1e-5

for side in ['L','R']:
    eye=part_vertices('Avarin_Eye_'+side)
    origin=tuple(sum(v[i] for v in eye)/len(eye) for i in range(3))
    blockers=[name for name,triangles in parts.items() if name.startswith('Ilyth_')
              and any(forward_hit(origin,face) for face in triangles)]
    assert not blockers,('Forward eye ray blocked by creature',side,blockers)
for limb in ['L_Fore','R_Fore','L_Rear','R_Rear']:
    pads=[v for name in parts if name.startswith('Ilyth_Contact_'+limb) for v in part_vertices(name)]
    assert abs(min(v[1] for v in pads))<1e-5,('Ungrounded contact group',limb)
assert 'Cradle_Floor' not in parts and 'Cradle_Seat' in parts
for side in ['L','R']:
    leg=part_vertices('Avarin_Leg_'+side);seat=part_vertices('Cradle_Seat')
    # The seat's front edge is behind the lower calf, preserving the open leg well.
    calf=[v for v in leg if v[1]<3.7]
    assert max(v[2] for v in seat)<min(v[2] for v in calf),('Seat crosses calf region',side)
print('PASS: forward eye rays clear of Ilyth; four grounded contact groups; open calf region.')
known={line.split()[1] for line in (folder/'GreatStrider.mtl').read_text().splitlines() if line.startswith('newmtl ')}
assert materials<=known
print(f'PASS: {len(parts)} parts; {len(vertices)} vertices; {len(faces)} triangles; 6.0 m; closed outward-wound parts; reproducible files.')
print('Not checked: inter-part intersections, skin deformation, Unity import, device performance, final canon/art acceptance.')
