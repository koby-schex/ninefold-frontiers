"""Living Veil silhouette study 07. Local art proposals, not new species canon.

Standing partner geometry only; never changes Ilyth's seated partner generator.
All additions use existing materials and retain stable descriptive part names.
"""
import math
from build_great_strider import Mesh, add


def refine_partner(model):
    replacement=Mesh();extra=Mesh();cx=1.95

    def mantle(t,v):
        a=.05+(math.pi-.10)*t
        r=.23+.24*v-.016*v*v*math.sin(5*a)**2
        return (cx+r*math.cos(a),1.37+r*.91*math.sin(a),
                .19-.065*v+.10*math.cos(a)**2-.028*math.sin(3*a)*math.sin(math.pi*v))
    replacement.panel('Avarin_CrescentMantle','sail_membrane',mantle,33,11,.005)
    replacement.strand('Avarin_MantleRim','sail_vein',[mantle(k/24,1) for k in range(25)],.010)
    for side in (-1,1):
        suffix='L' if side<0 else 'R'
        for k in range(3):
            t=.025+.075*k
            if side<0:t=1-t
            root=mantle(t,1)
            def ribbon(u,v):
                return (root[0]+side*(.025*math.sin(math.pi*u)+.012*u)+(v-.5)*.038*(1-.78*u),
                        root[1]-(.18+.02*k)*u,
                        root[2]-.035*math.sin(math.pi*u)+.045*u*u)
            replacement.panel('Avarin_SensoryRibbon_'+suffix+str(k),'sail_vein',ribbon,12,5,.003)
            extra.strand('Avarin_MantleVein_'+suffix+str(k),'grown_mineral',
                [add(mantle(t,.12+.82*j/10),(0,0,.007)) for j in range(11)],.005)

        replacement.loft('Avarin_Arm_'+suffix,'cloth',[
            ((cx+side*.205,1.37,.40),.088,.079),
            ((cx+side*.245,1.26,.41),.082,.072),
            ((cx+side*.29,1.10,.43),.060,.058),
            ((cx+side*.305,1.02,.47),.060,.052),
            ((cx+side*.32,.90,.51),.036,.036)],20)
        replacement.loft('Avarin_Hand_'+suffix,'avar_skin',[
            ((cx+side*.32,.91,.51),.032,.029),
            ((cx+side*.325,.875,.52),.042,.027),
            ((cx+side*.33,.835,.53),.040,.023),
            ((cx+side*.328,.797,.546),.030,.019),
            ((cx+side*.324,.78,.550),.015,.010)],20)
        replacement.loft('Avarin_Leg_'+suffix,'cloth',[
            ((cx+side*.10,.99,.44),.092,.103),
            ((cx+side*.13,.75,.43),.084,.091),
            ((cx+side*.14,.53,.46),.063,.072),
            ((cx+side*.15,.36,.45),.063,.068),
            ((cx+side*.16,.13,.48),.044,.054)],20)
        # Layered split front panels leave both feet and the leg division visible.
        def tabard(t,v):
            return (cx+side*(.030+.155*v+.072*t+.032*math.sin(math.pi*t)*v),
                    1.02-.78*t+.055*t*(2*v-1)**2+.018*t*t*math.sin(math.pi*v),
                    .58-.11*v*v+.085*t+.024*math.sin(4*math.pi*v+.75*t)*math.sin(math.pi*t))
        extra.panel('Avarin_FrontDrape_'+suffix,'cloth',tabard,24,13,.006)
        for edge in (0,1):
            extra.strand('Avarin_DrapeSeam_'+suffix+str(edge),'soft_charcoal',
                [add(tabard(j/18,edge),(0,0,.008)) for j in range(19)],.004)
        def outer(t,v):
            angle=-1+2*v
            return (cx+side*(.19+.12*t+.025*math.sin(math.pi*t))*math.cos(angle),
                    1.05-.78*t+.10*t*v,
                    .42+(.15+.03*t)*math.sin(angle)-.06*t+.020*math.sin(v*math.pi*3+.5*t)*math.sin(t*math.pi))
        extra.panel('Avarin_OuterDrape_'+suffix,'soft_charcoal',outer,22,13,.006,normal=(side,0,0))
        # Soft shoulder layers wrap into the torso and over the sleeve roots.
        def shoulder(t,v):
            angle=-1.35+2.8*t;r=.105+.235*v
            return (cx+side*r*math.cos(angle),
                    1.515-.16*v-.025*math.cos(angle)+.009*math.sin(3*angle)*math.sin(math.pi*v),
                    .40+.70*r*math.sin(angle))
        extra.panel('Avarin_ShoulderDrape_'+suffix,'cloth',shoulder,19,13,.005)
        # Rear layers begin inside the tunic and flare gently clear of the calves.
        def rear(t,v):
            return (cx+side*(.012+.205*v+.085*t*v),
                    1.12-.84*t+.05*t*v+.025*t*t*math.sin(math.pi*v),
                    .29-.11*t+.045*v*v+.021*math.sin(3*math.pi*v+.3*t)*math.sin(math.pi*t))
        extra.panel('Avarin_RearDrape_'+suffix,'soft_charcoal',rear,25,13,.006)
        # Light fabric collar and cuffs; not enlarged combat armor.
        extra.loft('Avarin_Collar_'+suffix,'cloth',[
            ((cx+side*.055,1.50,.405),.036,.037),
            ((cx+side*.13,1.445,.44),.044,.032),
            ((cx+side*.22,1.36,.42),.035,.025)],16)
        extra.loft('Avarin_Cuff_'+suffix,'soft_charcoal',[
            ((cx+side*.316,.94,.493),.043,.041),
            ((cx+side*.32,.905,.508),.041,.039)],20)
        # Separate clip loop and hinge help its safety function read at close range.
        x=cx+side*.15
        extra.strand('VeilHarness_ClipLoop_'+suffix,'continuance_frame',[
            (x-.020,1.07,.612),(x-.022,1.035,.638),(x,1.020,.642),
            (x+.022,1.035,.638),(x+.020,1.07,.612)],.005)
        extra.oval('VeilHarness_ClipHinge_'+suffix,'soft_charcoal',(x,1.078,.632),(.018,.010,.008))
        # A stowed coil, attached to its housing; no dangling control leash.
        coil=[(cx+side*.21,.96,.438)]
        for j in range(55):
            t=j/54;a=math.tau*2.6*t
            coil.append((cx+side*.21+.036*math.cos(a),.96+.045*math.sin(a),.453+.016*t))
        extra.strand('VeilHarness_LineCoil_'+suffix,'soft_charcoal',coil,.004)

    extra.oval('VeilHarness_MonitorBezel','continuance_frame',(cx,1.15,.640),(.037,.042,.008))
    # Keep the existing monitor surface in front of its bezel.
    for p in model.parts:
        if p['name']=='VeilHarness_Monitor':p['vertices']=[(x,y,z+.009) for x,y,z in p['vertices']]
        if p['name']=='Avarin_ChestProtection':
            p['vertices']=[(cx+(x-cx)*.86,y,z) for x,y,z in p['vertices']]
        if p['name']=='Avarin_Head':
            # Quiet jaw shaping below the mouth; eyes and temple bands stay seated.
            p['vertices']=[(cx+(x-cx)*(1-.08*max(0,(1.60-y)/.10)),y,z) for x,y,z in p['vertices']]
    # Subtle sleeve compression around the elbows, fading at all attachments.
    for p in replacement.parts:
        if p['name'].startswith('Avarin_Arm_'):
            side=-1 if p['name'].endswith('_L') else 1
            verts=[]
            for x,y,z in p['vertices']:
                fade=math.exp(-((y-1.10)/.105)**2)
                z+=.006*fade*math.sin((y-1.10)*82)*max(0,min(1,(z-.40)/.12))
                verts.append((x,y,z))
            p['vertices']=verts
    # Keep eyes and facial bands seated while relaxing the oval jaw contour.
    facial=('Avarin_Head','Avarin_Mouth','Avarin_Eye','Avarin_Temple','Avarin_UpperLid','Avarin_LowerLid','Avarin_Forehead')
    for p in model.parts:
        if p['name'].startswith(facial):
            p['vertices']=[(cx+(x-cx)*(1-.09*math.exp(-((y-1.565)/.055)**2)),y,z)
                           for x,y,z in p['vertices']]
        if p['name']=='Avarin_Neck':
            p['vertices']=[(cx+(x-cx)*(1+.18*max(0,(1.50-y)/.08)),y,z)
                           for x,y,z in p['vertices']]
    replacements={p['name']:p for p in replacement.parts}
    assert replacements.keys() <= {p['name'] for p in model.parts}
    model.parts=[replacements.get(p['name'],p) for p in model.parts]+extra.parts
