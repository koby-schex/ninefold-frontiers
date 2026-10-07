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
known={line.split()[1] for line in (folder/'GreatStrider.mtl').read_text().splitlines() if line.startswith('newmtl ')}
assert materials<=known
print(f'PASS: {len(parts)} parts; {len(vertices)} vertices; {len(faces)} triangles; 6.0 m; closed consistently wound parts; reproducible files.')
print('Not checked: inter-part intersections, skin deformation, Unity import, device performance, final canon/art acceptance.')
