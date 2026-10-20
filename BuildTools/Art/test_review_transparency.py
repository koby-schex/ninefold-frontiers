#!/usr/bin/env python3
"""Occlusion and nearest-shell regression checks for the software review renderer."""
import numpy as np
from render_great_strider import render

def plane(name,z,material):
    return dict(name=name,material=material,vertices=[[-1,-1,z],[1,-1,z],[1,1,z],[-1,1,z]],faces=[[0,1,2],[0,2,3]])

def draw(parts,opacity):
    data=dict(parts=parts,materials={'solid':(.7,.2,.1),'glass':(.2,.3,.7)},opacity={'glass':opacity})
    return np.array(render(data,(0,0,5),(64,64),3,(0,0,0),False))

solid=plane('background',0,'solid'); glass=plane('membrane',1,'glass')
a=draw([solid],1); b=draw([glass],1); result=draw([solid,glass],.5)
assert np.max(abs(result[30,30].astype(float)-(a[30,30].astype(float)+b[30,30])/2))<=1
assert np.array_equal(result,draw([glass,solid],.5)), 'Part order changes transparency'
assert np.array_equal(draw([solid,plane('hidden',-1,'glass')],.5),a), 'Membrane leaks through opaque foreground'
assert np.array_equal(result,draw([solid,plane('back shell',.9,'glass'),glass],.5)), 'Thin closed shell double-counted'
assert np.array_equal(draw([solid,glass],0),a), 'Zero opacity blocks background'
print('PASS: alpha blend, opaque occlusion, order independence, nearest-shell and zero-opacity checks')

# Interpolated membrane alpha is opt-in; retain the old constant-opacity path.
gradient=plane('gradient',1,'glass');gradient['vertex_opacity']=[0,1,1,0]
result=draw([solid,gradient],.5)
for x in (20,30,42):
    expected_alpha=((x+.5-32)/(64/3)+1)/2
    expected=a[30,x]*(1-expected_alpha)+b[30,x]*expected_alpha
    assert np.max(abs(result[30,x].astype(float)-expected))<=1, 'Vertex alpha interpolation failed'
print('PASS: continuous vertex alpha interpolation')
