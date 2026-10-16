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
    'living_pearl': (.62, .65, .63),
    'limb_tissue': (.38, .40, .42),
    'soft_charcoal': (.12, .13, .16),
    'grown_mineral': (.84, .85, .78),
    'sail_membrane': (.32, .28, .43),
    'sail_vein': (.64, .59, .76),
    'continuance_frame': (.36, .38, .40),
    'ceramic': (.65, .66, .60),
    'avar_skin': (.30, .25, .32),
    'cloth': (.19, .20, .24),
    'survey_light': (.47, .68, .79),
    'eye': (.055, .065, .07),
}

# Provisional finish controls, not locked material canon. Ns is shared with the MTL.
FINISH = {name: (28, .12) for name in MATERIALS}
FINISH.update(living_pearl=(32,.10), cloth=(8,.018), avar_skin=(24,.06), soft_charcoal=(16,.045),
    limb_tissue=(28,.07), grown_mineral=(72,.23), ceramic=(100,.25),
    continuance_frame=(120,.20), sail_membrane=(64,.18), survey_light=(72,.18))
# Exported opacity; software review uses the nearest membrane surface only.
OPACITY = {'sail_membrane': .68}

def add(a, b): return tuple(x+y for x, y in zip(a, b))
def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def mul(a, s): return tuple(x*s for x in a)
def dot(a, b): return sum(x*y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a): return mul(a, 1/max(math.sqrt(dot(a,a)), 1e-12))

class Mesh:
    def __init__(self): self.parts = []

    def part(self, name, mat, vertices, faces):
        # Mirrored parametric sheets can have consistent but inward winding.
        # Closed source parts must face outward for Unity's backface culling.
        volume=sum(dot(vertices[a],cross(vertices[b],vertices[c])) for a,b,c in faces)/6
        if volume < 0: faces=[(a,c,b) for a,b,c in faces]
        self.parts.append(dict(name=name, material=mat, vertices=vertices, faces=faces))

    def loft(self, name, mat, rings, sides=12):
        """rings: (center, side radius, forward radius); smooth joined contour."""
        # Add radial resolution to broad visible forms, not every fine strand.
        # Existing profiles, centerlines and control radii stay unchanged.
        if name.startswith(('Ilyth_Limb_', 'Ilyth_Torso', 'Ilyth_ShortNeck', 'Ilyth_CompactHead')):
            sides = max(sides, 32)
        elif name.startswith(('Ilyth_ThoracicArc_', 'Cradle_LoadFrame_', 'Cradle_CeramicShell_',
                              'Cradle_Seat', 'Avarin_Torso', 'Avarin_Arm_', 'Avarin_Leg_',
                              'Avarin_Neck', 'Avarin_Face', 'Avarin_ShoulderGuard_',
                              'Avarin_ForearmGuard_', 'Avarin_ChestProtection')):
            sides = max(sides, 20)
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
        vertices=[]; previous=None
        for i, (p, w, h) in enumerate(rings):
            axis=unit(sub(rings[min(i+1,len(rings)-1)][0],rings[max(0,i-1)][0]))
            # Parallel-transport the frame instead of abruptly switching axes.
            basis=unit(cross(axis, (0,0,1) if abs(axis[2])<.9 else (0,1,0))) if previous is None else unit(sub(previous,mul(axis,dot(previous,axis))))
            previous=basis
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
        self.parts[-1]['loft_sides'] = sides

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

    def panel(self,name,mat,surface,rows=18,cols=10,thickness=.008,normal=(0,0,1)):
        """Closed two-skin curved sheet with authored parametric shape."""
        vertices=[]
        for side in [-1,1]:
            for i in range(rows):
                for j in range(cols):
                    p=surface(i/(rows-1),j/(cols-1))
                    vertices.append(add(p,mul(normal,side*thickness)))
        faces=[]; count=rows*cols
        for layer in range(2):
            for i in range(rows-1):
                for j in range(cols-1):
                    a=layer*count+i*cols+j; b=a+1; c=b+cols; d=a+cols
                    faces.extend([(a,b,c),(a,c,d)] if layer else [(a,c,b),(a,d,c)])
        rim=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+list(range(count-2,count-cols-1,-1))+[i*cols for i in range(rows-2,0,-1)]
        for a,b in zip(rim,rim[1:]+rim[:1]): faces.extend([(a,b,b+count),(a,b+count,a+count)])
        self.part(name,mat,vertices,faces)

    def limb_cover(self,name,source,start,end,fore):
        """Follow the real limb surface instead of offsetting a separate blade."""
        vertices=source['vertices']; sides=source['loft_sides']
        def surface(t,v):
            row=start+(end-start)*t
            r=min(int(row),end-1); a=row-r
            taper=.14+.86*math.sin(math.pi*t)**.55
            angle=(1.5 if fore else .5)*math.pi+(v-.5)*2.2*taper
            j=(angle/math.tau*sides)%sides; k=int(j); b=j-k
            def point(i): return add(mul(vertices[i*sides+k],1-b),mul(vertices[i*sides+(k+1)%sides],b))
            p=add(mul(point(r),1-a),mul(point(r+1),a))
            def center(i): return tuple(sum(vertices[i*sides+j][axis] for j in range(sides))/sides for axis in range(3))
            c=add(mul(center(r),1-a),mul(center(r+1),a))
            offset=.018+.045*math.sin(math.pi*t)*math.sin(math.pi*v)
            # Fine longitudinal relief follows the grown surface, fading into its edges.
            offset+=.006*math.sin(v*math.pi*8)**2*math.sin(math.pi*t)*math.sin(math.pi*v)
            return add(p,mul(unit(sub(p,c)),offset))
        self.panel(name,'grown_mineral',surface,22,13,.004)

def make_model():
    m=Mesh()
    # Narrow organic torso, with grown reinforcement limited to stress-bearing zones.
    m.loft('Ilyth_Torso','living_pearl',[
        ((0,4.89,-1.06),.14,.18),((0,5.10,-.78),.36,.18),
        ((0,5.21,-.25),.43,.14),((0,5.23,.38),.40,.15),
        ((0,5.14,.88),.29,.21),((0,5.14,1.10),.17,.17)],20)
    m.loft('Ilyth_ShortNeck','living_pearl',[
        ((0,5.10,.64),.17,.15),((0,5.22,.96),.22,.19),
        ((0,5.40,1.16),.19,.20),((0,5.59,1.29),.13,.14)],16)
    m.loft('Ilyth_CompactHead','living_pearl',[
        ((0,5.60,1.05),.09,.12),((0,5.68,1.26),.25,.22),
        ((0,5.63,1.53),.22,.17),((0,5.55,1.78),.12,.09),
        ((0,5.54,1.84),.025,.04)],16)
    m.loft('Ilyth_CranialMineral','grown_mineral',[
        ((0,5.85,1.07),.08,.035),((0,5.87,1.35),.20,.055),
        ((0,5.76,1.59),.12,.04),((0,5.59,1.80),.015,.015)],12)
    m.strand('Ilyth_FeedingSeam','soft_charcoal',[(-.10,5.53,1.74),(0,5.51,1.82),(.10,5.53,1.74)],.012)
    for k in range(3):
        z=.75-k*.43
        m.loft('Ilyth_DorsalReinforcement_'+str(k),'grown_mineral',[
            ((0,5.33,z),.04,.022),((0,5.37,z-.13),.30,.045),
            ((0,5.35,z-.36),.21,.03),((0,5.25,z-.55),.03,.015)],14)
    for side in [-1,1]:
        s='L' if side<0 else 'R'
        m.oval('Ilyth_Eye_'+s,'eye',(side*.207,5.66,1.47),(.025,.027,.04))
        def sail(t,v):
            width=.035+.54*math.sin(math.pi*t)**.85
            across=(2*v-1)*width
            return (side*(.17+1.13*t+across*.53),
                5.78+.14*math.sin(math.pi*t)-.34*t*t-.18*(2*v-1)**2*math.sin(math.pi*t)+.025*math.sin(4*math.pi*t)*math.sin(math.pi*v),
                1.16-1.15*t+across*.85)
        m.panel('Ilyth_SensorySail_'+s,'sail_membrane',sail)
        for k in range(5):
            v=k/4
            m.strand('Ilyth_SailVein_'+s+str(k),'sail_vein',
                [add(sail(t/6,v),(0,.012,.012)) for t in range(7)],.009 if k%2 else .016)
        for k in range(4):
            for edge in [0,1]:
                m.strand('Ilyth_SailChannel_'+s+str(k)+str(edge),'sail_vein',
                    [add(sail(.18+k*.17+j*.025, .5+(edge-.5)*j/5),(0,.008,.008)) for j in range(6)],.005)
        m.strand('Ilyth_SailRoot_'+s,'grown_mineral',[(side*.17,5.75,1.16),(side*.42,5.77,.98),(side*.62,5.70,.74)],.035)
        m.loft('Ilyth_Brow_'+s,'grown_mineral',[
            ((side*.11,5.86,1.05),.03,.035),((side*.22,5.78,1.36),.05,.035),
            ((side*.14,5.62,1.68),.012,.012)],10)
        # Visible structural tissue around the operator recess.
        m.strand('Ilyth_ThoracicArc_'+s,'living_pearl',[(side*.26,5.12,-.80),
            (side*.50,4.86,-.42),(side*.49,4.90,.33),(side*.27,5.14,.81)],.095)
        for fore in [False,True]:
            n=s+('_Fore' if fore else '_Rear')
            a=(side*.27,5.16,.67 if fore else -.65)
            b=(side*.89,4.20,1.03 if fore else -.98)
            c=(side*1.11,2.66,.72 if fore else -1.18)
            d=(side*1.29,1.52,1.10 if fore else -1.27)
            e=(side*1.47,.25,1.43 if fore else -1.56)
            m.loft('Ilyth_Limb_'+n,'limb_tissue',[(a,.21,.24),
                (add(mul(a,.5),mul(b,.5)),.24,.21),(b,.17,.19),
                (add(mul(b,.6),mul(c,.4)),.16,.17),(c,.10,.12),
                (d,.095,.105),(add(mul(d,.45),mul(e,.55)),.065,.08),(e,.085,.10)],16)
            m.strand('Ilyth_Flexor_'+n,'soft_charcoal',[
                add(b,(-side*.15,0,.17 if fore else -.17)),
                add(c,(-side*.08,.06,.10 if fore else -.10)),
                add(d,(-side*.07,0,.07 if fore else -.07))],.04)
            m.oval('Ilyth_Elbow_'+n,'soft_charcoal',c,(.112,.135,.135))
            m.oval('Ilyth_Ankle_'+n,'soft_charcoal',e,(.09,.14,.11))
            # Splayed soft contact pads with individual load-spreading digits.
            for toe in range(3):
                end=(e[0]+(toe-1)*.12,.05,e[2]+(.25 if fore else -.23))
                m.loft('Ilyth_Contact_'+n+str(toe),'soft_charcoal',[(e,.07,.085),
                    (add(mul(e,.4),mul(end,.6)),.07,.065),(end,.055,.04)],8)
            m.oval('Ilyth_ContactHeel_'+n,'soft_charcoal',(e[0],.12,e[2]),(.12,.075,.13))
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
        def cradle_shell(t,v):
            return (side*(.43+.045*math.sin(math.pi*t)+.016*math.sin(math.pi*v)),
                3.91+.26*v+.07*math.sin(math.pi*t),-.26+.91*t)
        m.panel('Cradle_SidePanel_'+s,'ceramic',cradle_shell,14,7,.006,(1,0,0))
        m.strand('Cradle_PanelRim_'+s,'continuance_frame',[cradle_shell(k/10,1) for k in range(11)],.013)
        for k in range(3):
            m.strand('Cradle_PanelSeam_'+s+str(k),'continuance_frame',
                [add(cradle_shell(.16+k*.28,v/5),(side*.012,0,0)) for v in range(6)],.007)
        m.strand('Cradle_Handrail_'+s,'continuance_frame',[(side*.34,4.2,-.1),(side*.40,4.31,.40),(side*.27,4.38,.76)],.025)
        m.oval('Waycaster_Archive_'+s,'ceramic',(side*.45,4.41,-.55),(.14,.20,.17))
        m.strand('Waycaster_Routing_'+s,'soft_charcoal',[(side*.42,4.46,-.51),(side*.40,4.44,.15),(side*.29,4.40,.64)],.018)
        for z in [-.44,.66]:
            m.oval('Cradle_Attachment_'+s+str(z),'continuance_frame',(side*.37,4.87,z),(.10,.075,.09))
            m.oval('Cradle_AttachmentInset_'+s+str(z),'ceramic',(side*.44,4.87,z),(.032,.042,.048))
        for k in range(3):
            z=-.35+k*.28
            m.strand('Cradle_CrossBrace_'+s+str(k),'continuance_frame',[(side*.41,4.29,z),(side*.47,4.03,z+.08),(side*.23,3.85,z+.14)],.024)
        # Retracted survey cartridges, deliberately small and not gun barrels.
        for k in range(3):
            x=side*(.40+k*.045)
            m.loft('Waycaster_ProbeCartridge_'+s+str(k),'ceramic',[
                ((x,4.31,-.72),.024,.026),((x,4.59,-.72),.025,.026),((x,4.64,-.72),.012,.018)],8)
        m.oval('Waycaster_LineReel_'+s,'continuance_frame',(side*.45,4.05,-.4),(.08,.09,.09))
    # Rear seat shelf leaves an open leg well; no floor through the calves.
    m.loft('Cradle_Seat','continuance_frame', [((0,3.99,-.34),.20,.035),((0,3.99,-.17),.28,.045),((0,3.99,-.015),.22,.025)],12)
    for side in [-1,1]:
        m.loft('Cradle_Footrest_'+str(side),'continuance_frame',[((side*.19,3.55,.14),.065,.018),((side*.19,3.55,.30),.085,.02),((side*.19,3.55,.43),.065,.018)],10)
        m.strand('Cradle_FootrestSupport_'+str(side),'continuance_frame',[(side*.39,3.90,.50),(side*.32,3.65,.46),(side*.19,3.55,.40)],.018)
    m.oval('Waycaster_Console','ceramic',(0,4.35,.60),(.25,.09,.18))
    m.strand('Waycaster_ConsoleRim','continuance_frame',[(-.23,4.40,.50),(-.20,4.42,.71),(0,4.43,.77),(.20,4.42,.71),(.23,4.40,.50)],.015)
    m.oval('Waycaster_Feedback','survey_light',(0,4.43,.61),(.115,.012,.07))
    # Seated Avarin. Scale is based on an unfolded 1.82 m body, not seated height.
    m.loft('Avarin_Torso','cloth', [((0,4.10,-.14),.18,.13),((0,4.33,-.18),.21,.14),((0,4.61,-.21),.23,.14),((0,4.66,-.20),.15,.12)],12)
    m.loft('Avarin_Neck','avar_skin',[((0,4.65,-.18),.072,.065),((0,4.78,-.18),.065,.064)],10)
    m.oval('Avarin_Head','avar_skin',(0,4.89,-.17),(.11,.16,.11))
    m.loft('Avarin_Face','avar_skin',[((0,4.79,-.09),.035,.025),((0,4.86,-.065),.07,.032),((0,4.96,-.075),.075,.025)],12)
    m.strand('Avarin_ForeheadSeam','sail_vein',[(0,4.98,-.074),(0,5.026,-.095)],.004)
    m.loft('Avarin_ChestProtection','ceramic', [((0,4.23,-.015),.085,.025),((0,4.44,-.025),.15,.038),((0,4.58,-.065),.17,.03)],12)
    def mantle(t,v):
        angle=.05+(math.pi-.10)*t
        radius=.23+.24*v
        return (radius*math.cos(angle),4.60+radius*.91*math.sin(angle),-.41-.065*v+.025*math.sin(angle*3))
    m.panel('Avarin_CrescentMantle','sail_membrane',mantle,24,6)
    m.strand('Avarin_MantleRim','sail_vein',[mantle(k/14,1) for k in range(15)],.013)
    m.strand('Avarin_Mouth','soft_charcoal',[(-.03,4.83,-.035),(0,4.824,-.03),(.03,4.83,-.035)],.006)
    for side in [-1,1]:
        s='L' if side<0 else 'R'
        m.oval('Avarin_Eye_'+s,'eye',(side*.047,4.92,-.025),(.018,.015,.013))
        m.strand('Avarin_Temple_'+s,'grown_mineral',[(side*.10,4.97,-.14),(side*.085,4.89,-.095),(side*.075,4.83,-.115)],.013)
        m.loft('Avarin_Arm_'+s,'cloth',[((side*.21,4.57,-.19),.09,.085),((side*.29,4.30,.05),.065,.06),((side*.18,4.38,.45),.048,.046)],10)
        m.oval('Avarin_Hand_'+s,'avar_skin',(side*.18,4.38,.47),(.05,.04,.07))
        m.loft('Avarin_Leg_'+s,'cloth',[((side*.105,4.14,-.10),.10,.10),((side*.19,4.04,.22),.09,.085),((side*.19,3.63,.23),.052,.065)],10)
        m.oval('Avarin_Foot_'+s,'soft_charcoal',(side*.19,3.61,.29),(.06,.05,.13))
        m.loft('Avarin_ShoulderGuard_'+s,'ceramic', [((side*.19,4.64,-.22),.045,.025),((side*.25,4.57,-.17),.105,.05),((side*.27,4.47,-.09),.04,.023)],10)
        m.strand('Avarin_TunicSeam_'+s,'continuance_frame',[(side*.16,4.59,-.07),(side*.13,4.40,-.035),(side*.09,4.23,.015)],.007)
        m.loft('Avarin_ForearmGuard_'+s,'ceramic',[
            ((side*.28,4.34,.10),.055,.025),((side*.23,4.41,.27),.068,.035),((side*.18,4.43,.43),.028,.02)],10)
        def drape(t,v):
            return (side*(.13+.17*v+.03*math.sin(t*math.pi)),
                4.12-.51*t,.03+.38*t+.035*math.sin(v*math.pi*4)*math.sin(t*math.pi))
        m.panel('Avarin_SplitTabard_'+s,'cloth',drape,12,8,.007)
        for v in [.07,.93]:
            m.strand('Avarin_TabardHem_'+s+str(v),'continuance_frame',[add(drape(k/9,v),(0,0,.012)) for k in range(10)],.007)
        for k in range(3):
            m.strand('Avarin_SensoryRibbon_'+s+str(k),'sail_vein',[(side*(.25+k*.085),4.63,-.46),(side*(.26+k*.085),4.49,-.49),(side*(.28+k*.085),4.39,-.46)],.012)
    # Grown coverings follow the limb surfaces; soft joint gaps remain visible.
    for p in list(m.parts):
        if p['name'].startswith('Ilyth_Limb_'):
            name=p['name'].removeprefix('Ilyth_Limb_')
            fore=name.endswith('Fore')
            for k,(a,b) in enumerate([(0,9),(10,15),(16,21)]):
                m.limb_cover('Ilyth_GrownSheath_'+name+str(k),p,a,b,fore)
    # Lower the intact operator/equipment together by 22 cm in authoring space.
    # Keep all four body attachments fixed; smoothly lengthen only the load frame.
    for part in m.parts:
        name=part['name']
        if name.startswith('Avarin_') or name.startswith('Waycaster_'):
            part['vertices']=[add(v,(0,-.22,0)) for v in part['vertices']]
        elif name.startswith('Cradle_') and not name.startswith('Cradle_Attachment'):
            def shift(v):
                weight=max(0,min(1,(4.80-v[1])/.45)) if name.startswith('Cradle_LoadFrame') else 1
                return add(v,(0,-.22*weight,0))
            part['vertices']=[shift(v) for v in part['vertices']]
    # Ground each three-digit contact group without collapsing its topology.
    for limb in ['L_Fore','R_Fore','L_Rear','R_Rear']:
        pads=[p for p in m.parts if p['name'].startswith('Ilyth_Contact_'+limb)]
        bottom=min(v[1] for p in pads for v in p['vertices'])
        for part in pads: part['vertices']=[add(v,(0,-bottom,0)) for v in part['vertices']]
    return m

def write(mesh, output):
    output.mkdir(parents=True,exist_ok=True)
    # Overall neutral envelope is uniformly 6 m; exact scale can be changed in source.
    top=max(v[1] for p in mesh.parts for v in p['vertices'])
    bottom=min(v[1] for p in mesh.parts for v in p['vertices'])
    factor=6/(top-bottom)
    for p in mesh.parts: p['vertices']=[mul(sub(v,(0,bottom,0)),factor) for v in p['vertices']]
    obj=['# Great Strider surface study 06; meters; Y up; Z forward', 'mtllib GreatStrider.mtl']
    offset=1
    for p in mesh.parts:
        obj.extend(['o '+p['name'],'usemtl '+p['material'],'s 1'])
        obj.extend('v '+' '.join(f'{x:.5f}' for x in v) for v in p['vertices'])
        obj.extend('f '+' '.join(str(i+offset) for i in f) for f in p['faces'])
        offset+=len(p['vertices'])
    (output/'GreatStrider.obj').write_text('\n'.join(obj)+'\n')
    mtl=[]
    for name,rgb in MATERIALS.items():
        mtl.extend(['newmtl '+name,'Kd '+' '.join(map(str,rgb)),'Ks '+' '.join([str(FINISH[name][1])]*3),'Ns '+str(FINISH[name][0]),'d '+str(OPACITY.get(name,1)),''])
    (output/'GreatStrider.mtl').write_text('\n'.join(mtl))
    report=dict(status='surface study 06; not production ready',units='meters',up='Y',forward='Z',
        height=6,parts=len(mesh.parts),vertices=offset-1,
        triangles=sum(len(p['faces']) for p in mesh.parts),
        materials=list(MATERIALS),limbs=['L_Fore','R_Fore','L_Rear','R_Rear'],
        limitations=['No UV unwrap, skin weights, rig, animation or LODs',
          'Procedural surfaces require sculpt and topology refinement',
          'No Unity import or device performance verification',
          'Reference-study connections are proposals, not new lore'])
    (output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    # This same mesh feeds the review renderer. No image-generation substitution.
    (output/'review_mesh.json').write_text(json.dumps(dict(materials=MATERIALS,finish=FINISH,opacity=OPACITY,parts=mesh.parts),separators=(',',':')))
    print(json.dumps(report,indent=2))

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,default=ROOT/'SourceArt/GreatStrider')
    write(make_model(),parser.parse_args().output)
