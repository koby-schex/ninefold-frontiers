#!/usr/bin/env python3
"""Editable Great Strider form study. Standard library only; no Blender required.

Authored lofts and membrane surfaces, not a final sculpt or rigged game asset.
Coordinates: X right, Y up, Z forward; meters. Each part remains a named OBJ object.
"""
import argparse
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MATERIALS = {
    'living_pearl': (.70, .72, .69),
    'soft_charcoal': (.16, .18, .20),
    'grown_mineral': (.84, .85, .78),
    'sail_membrane': (.36, .31, .48),
    'sail_vein': (.64, .59, .76),
    'continuance_frame': (.39, .38, .32),
    'ceramic': (.65, .66, .60),
    'avar_skin': (.30, .25, .32),
    'cloth': (.19, .20, .24),
    'survey_light': (.47, .68, .79),
    'eye': (.055, .065, .07),
}

def add(a, b): return tuple(x+y for x, y in zip(a, b))
def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def mul(a, s): return tuple(x*s for x in a)
def dot(a, b): return sum(x*y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a): return mul(a, 1/max(math.sqrt(dot(a,a)), 1e-12))

class Mesh:
    def __init__(self): self.parts = []

    def part(self, name, mat, vertices, faces):
        self.parts.append(dict(name=name, material=mat, vertices=vertices, faces=faces))

    def loft(self, name, mat, rings, sides=12):
        """rings: (center, side radius, forward radius); smooth joined contour."""
        # Smooth profile interpolation keeps knees and soft tissue continuous.
        controls=[tuple(p)+(w,h) for p,w,h in rings]
        smooth=[]
        for i in range(len(controls)-1):
            a,b,c,d=[controls[max(0,min(len(controls)-1,j))] for j in (i-1,i,i+1,i+2)]
            for step in range(3):
                t=step/3
                q=[.5*((2*b[k])+(-a[k]+c[k])*t+(2*a[k]-5*b[k]+4*c[k]-d[k])*t*t+(-a[k]+3*b[k]-3*c[k]+d[k])*t*t*t) for k in range(5)]
                smooth.append((tuple(q[:3]),max(.002,q[3]),max(.002,q[4])))
        rings=smooth+[rings[-1]]
        vertices=[]
        for i, (p, w, h) in enumerate(rings):
            axis=unit(sub(rings[min(i+1,len(rings)-1)][0],rings[max(0,i-1)][0]))
            basis=unit(cross(axis, (0,0,1) if abs(axis[2])<.9 else (0,1,0)))
            other=unit(cross(axis,basis))
            for j in range(sides):
                a=j*math.tau/sides
                vertices.append(add(p,add(mul(basis,w*math.cos(a)),mul(other,h*math.sin(a)))))
        faces=[]
        for i in range(len(rings)-1):
            for j in range(sides):
                a=i*sides+j; b=i*sides+(j+1)%sides; c=b+sides; d=a+sides
                faces.extend([(a,b,c),(a,c,d)])
        # Individual center vertices avoid degenerate pole rings.
        for ring, reverse in [(0,True),(len(rings)-1,False)]:
            center=len(vertices); vertices.append(rings[ring][0])
            for j in range(sides):
                a=ring*sides+j; b=ring*sides+(j+1)%sides
                faces.append((center,b,a) if reverse else (center,a,b))
        self.part(name,mat,vertices,faces)

    def strand(self,name,mat,points,radius=.025):
        self.loft(name,mat,[(p,radius,radius) for p in points],8)

    def oval(self,name,mat,center,scale):
        rings=[]
        for i in range(9):
            a=-math.pi/2+.025+(math.pi-.05)*i/8
            rings.append((add(center,(0,scale[1]*math.sin(a),0)),scale[0]*math.cos(a),scale[2]*math.cos(a)))
        self.loft(name,mat,rings,16)

    def membrane(self,name,mat,root,tip,width):
        # A double-sided closed leaf volume, with curved leading/trailing edges.
        axis=unit(sub(tip,root)); lateral=unit(cross(axis,(0,1,0)))
        vertices=[]; rows=9; cols=7
        for side in [-1,1]:
            for i in range(rows):
                t=i/(rows-1); p=add(root,mul(sub(tip,root),t))
                breadth=width*(.025+math.sin(math.pi*t)**.8)
                for j in range(cols):
                    v=2*j/(cols-1)-1
                    vertices.append(add(add(p,mul(lateral,v*breadth)),(0,.16*math.sin(math.pi*t)*(1-v*v)+side*.006,0)))
        faces=[]; layer=rows*cols
        for k in range(2):
            for i in range(rows-1):
                for j in range(cols-1):
                    a=k*layer+i*cols+j; b=a+1; c=b+cols; d=a+cols
                    faces.extend([(a,b,c),(a,c,d)] if k else [(a,c,b),(a,d,c)])
        boundary=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+list(range(layer-2,layer-cols-1,-1))+[i*cols for i in range(rows-2,0,-1)]
        for a,b in zip(boundary,boundary[1:]+boundary[:1]):
            faces.extend([(a,b,b+layer),(a,b+layer,a+layer)])
        self.part(name,mat,vertices,faces)

def make_model():
    m=Mesh()
    # Narrow organic torso, with grown reinforcement limited to stress-bearing zones.
    m.loft('Ilyth_Torso','living_pearl',[
        ((0,4.89,-1.06),.14,.18),((0,5.10,-.78),.36,.18),
        ((0,5.21,-.25),.43,.14),((0,5.23,.38),.40,.15),
        ((0,5.14,.88),.29,.21),((0,5.14,1.10),.17,.17)],20)
    m.loft('Ilyth_ShortNeck','living_pearl',[
        ((0,5.05,.77),.26,.19),((0,5.24,.99),.23,.21),
        ((0,5.40,1.16),.20,.24),((0,5.55,1.29),.16,.19)],16)
    m.loft('Ilyth_CompactHead','living_pearl',[
        ((0,5.60,1.05),.09,.12),((0,5.68,1.26),.25,.22),
        ((0,5.63,1.53),.22,.17),((0,5.55,1.78),.12,.09),
        ((0,5.54,1.84),.025,.04)],16)
    m.loft('Ilyth_CranialMineral','grown_mineral',[
        ((0,5.85,1.07),.08,.035),((0,5.87,1.35),.20,.055),
        ((0,5.76,1.59),.12,.04),((0,5.59,1.80),.015,.015)],12)
    m.strand('Ilyth_FeedingSeam','soft_charcoal',[(-.10,5.53,1.74),(0,5.51,1.82),(.10,5.53,1.74)],.012)
    for side in [-1,1]:
        s='L' if side<0 else 'R'
        m.oval('Ilyth_Eye_'+s,'eye',(side*.207,5.66,1.47),(.025,.027,.04))
        root=(side*.16,5.76,1.09); tip=(side*1.13,5.77,-.02)
        m.membrane('Ilyth_SensorySail_'+s,'sail_membrane',root,tip,.37)
        for k in range(4):
            end=(side*(.65+k*.16),5.78,-.12+k*.12)
            m.strand('Ilyth_SailVein_'+s+str(k),'sail_vein',[root,(side*.5,5.91,.65),end],.012)
        for fore in [False,True]:
            n=s+('_Fore' if fore else '_Rear')
            a=(side*.32,5.10,.67 if fore else -.65)
            b=(side*.89,4.20,1.03 if fore else -.98)
            c=(side*1.11,2.66,.72 if fore else -1.18)
            d=(side*1.29,1.52,1.10 if fore else -1.27)
            e=(side*1.47,.25,1.43 if fore else -1.56)
            m.loft('Ilyth_Limb_'+n,'living_pearl',[(a,.24,.29),(b,.25,.26),
                (add(mul(b,.6),mul(c,.4)),.18,.19),(c,.12,.14),
                (d,.115,.13),(add(mul(d,.45),mul(e,.55)),.075,.09),(e,.085,.10)],12)
            m.oval('Ilyth_Shoulder_'+n,'grown_mineral',add(b,(side*.035,.06,0)),(.14,.43,.25))
            m.oval('Ilyth_Elbow_'+n,'soft_charcoal',c,(.135,.16,.16))
            m.oval('Ilyth_Ankle_'+n,'soft_charcoal',e,(.105,.17,.12))
            # Splayed soft contact pads with individual load-spreading digits.
            for toe in range(3):
                end=(e[0]+(toe-1)*.12,.05,e[2]+(.25 if fore else -.23))
                m.loft('Ilyth_Contact_'+n+str(toe),'soft_charcoal',[(e,.07,.085),
                    (add(mul(e,.4),mul(end,.6)),.07,.065),(end,.055,.04)],8)
            m.strand('Ilyth_GrownRidge_'+n,'grown_mineral',[
                add(b,(side*.12,0,.04)),add(c,(side*.07,.13,.07)),add(d,(side*.06,0,.04))],.032)
    # Fitted recessed frame sits within the torso envelope; no saddle or back building.
    for side in [-1,1]:
        s='L' if side<0 else 'R'
        m.strand('Cradle_LoadFrame_'+s,'continuance_frame',[
            (side*.37,4.80,-.48),(side*.48,4.34,-.43),(side*.44,3.83,-.17),
            (side*.41,3.79,.45),(side*.46,4.25,.77),(side*.32,4.81,.68)],.048)
        m.loft('Cradle_CeramicShell_'+s,'ceramic',[
            ((side*.42,3.87,-.23),.04,.05),((side*.46,3.99,.05),.07,.15),
            ((side*.45,4.08,.39),.065,.15),((side*.39,4.13,.67),.02,.04)],10)
        m.strand('Cradle_Handrail_'+s,'continuance_frame',[(side*.34,4.2,-.1),(side*.40,4.31,.40),(side*.27,4.38,.76)],.025)
        m.oval('Waycaster_Archive_'+s,'ceramic',(side*.45,4.41,-.55),(.14,.20,.17))
        m.strand('Waycaster_Routing_'+s,'soft_charcoal',[(side*.42,4.46,-.51),(side*.40,4.44,.15),(side*.29,4.40,.64)],.018)
    m.loft('Cradle_Floor','continuance_frame', [((0,3.84,-.28),.33,.04),((0,3.84,.2),.38,.05),((0,3.90,.55),.28,.04)],12)
    m.oval('Waycaster_Console','ceramic',(0,4.35,.60),(.25,.09,.18))
    m.oval('Waycaster_Feedback','survey_light',(0,4.43,.61),(.115,.012,.07))
    # Seated Avarin. Scale is based on an unfolded 1.82 m body, not seated height.
    m.loft('Avarin_Torso','cloth', [((0,4.10,-.14),.18,.13),((0,4.33,-.18),.21,.14),((0,4.61,-.21),.23,.14),((0,4.66,-.20),.15,.12)],12)
    m.loft('Avarin_Neck','avar_skin',[((0,4.65,-.18),.072,.065),((0,4.78,-.18),.065,.064)],10)
    m.oval('Avarin_Head','avar_skin',(0,4.89,-.17),(.11,.16,.11))
    m.strand('Avarin_Mouth','soft_charcoal',[(-.03,4.83,-.065),(0,4.824,-.06),(.03,4.83,-.065)],.006)
    for side in [-1,1]:
        s='L' if side<0 else 'R'
        m.oval('Avarin_Eye_'+s,'eye',(side*.047,4.92,-.075),(.018,.015,.013))
        m.strand('Avarin_Temple_'+s,'grown_mineral',[(side*.10,4.97,-.14),(side*.085,4.89,-.095),(side*.075,4.83,-.115)],.013)
        m.loft('Avarin_Arm_'+s,'cloth',[((side*.21,4.57,-.19),.09,.085),((side*.29,4.30,.05),.065,.06),((side*.18,4.38,.45),.048,.046)],10)
        m.oval('Avarin_Hand_'+s,'avar_skin',(side*.18,4.38,.47),(.05,.04,.07))
        m.loft('Avarin_Leg_'+s,'cloth',[((side*.105,4.14,-.10),.10,.10),((side*.19,4.04,.22),.09,.085),((side*.19,3.63,.23),.052,.065)],10)
        m.oval('Avarin_Foot_'+s,'soft_charcoal',(side*.19,3.61,.29),(.06,.05,.13))
        m.membrane('Avarin_Mantle_'+s,'sail_membrane',(0,4.61,-.37),(side*.45,4.80,-.30),.20)
        for k in range(3):
            m.strand('Avarin_SensoryRibbon_'+s+str(k),'sail_vein',[(side*(.21+k*.07),4.69,-.44),(side*(.24+k*.075),4.54,-.47),(side*(.26+k*.08),4.45,-.43)],.012)
    return m

def write(mesh, output):
    output.mkdir(parents=True,exist_ok=True)
    # Overall neutral envelope is uniformly 6 m; exact scale can be changed in source.
    top=max(v[1] for p in mesh.parts for v in p['vertices'])
    bottom=min(v[1] for p in mesh.parts for v in p['vertices'])
    factor=6/(top-bottom)
    for p in mesh.parts: p['vertices']=[mul(sub(v,(0,bottom,0)),factor) for v in p['vertices']]
    obj=['# Great Strider form study 01; meters; Y up; Z forward', 'mtllib GreatStrider.mtl']
    offset=1
    for p in mesh.parts:
        obj.extend(['o '+p['name'],'usemtl '+p['material'],'s 1'])
        obj.extend('v '+' '.join(f'{x:.5f}' for x in v) for v in p['vertices'])
        obj.extend('f '+' '.join(str(i+offset) for i in f) for f in p['faces'])
        offset+=len(p['vertices'])
    (output/'GreatStrider.obj').write_text('\n'.join(obj)+'\n')
    mtl=[]
    for name,rgb in MATERIALS.items():
        mtl.extend(['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks 0.16 0.16 0.16','Ns 38','d 1',''])
    (output/'GreatStrider.mtl').write_text('\n'.join(mtl))
    report=dict(status='form study; not production ready',units='meters',up='Y',forward='Z',
        height=6,parts=len(mesh.parts),vertices=offset-1,
        triangles=sum(len(p['faces']) for p in mesh.parts),
        materials=list(MATERIALS),limbs=['L_Fore','R_Fore','L_Rear','R_Rear'],
        limitations=['No UV unwrap, skin weights, rig, animation or LODs',
          'Procedural surfaces require sculpt and topology refinement',
          'No Unity import or device performance verification',
          'Reference-study connections are proposals, not new lore'])
    (output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    # This same mesh feeds the review renderer. No image-generation substitution.
    (output/'review_mesh.json').write_text(json.dumps(dict(materials=MATERIALS,parts=mesh.parts),separators=(',',':')))
    print(json.dumps(report,indent=2))

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,default=ROOT/'SourceArt/GreatStrider')
    write(make_model(),parser.parse_args().output)
