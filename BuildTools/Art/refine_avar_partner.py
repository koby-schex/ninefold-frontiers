"""Living Veil partner study 05. Local art proposals, not new species canon.

Standing partner geometry only; never changes Ilyth's seated partner generator.
All additions use existing materials and retain stable descriptive part names.
"""
import math
from build_great_strider import Mesh, add


def refine_partner(model):
    replacement=Mesh();extra=Mesh();cx=1.95

    def mantle(t,v):
        a=.05+(math.pi-.10)*t;r=.23+.24*v
        return (cx+r*math.cos(a),1.37+r*.91*math.sin(a),
                .19-.065*v+.10*math.cos(a)**2-.018*math.sin(3*a)*math.sin(math.pi*v))
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
            ((cx+side*.21,1.35,.40),.082,.078),
            ((cx+side*.25,1.25,.41),.078,.073),
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
            return (cx+side*(.030+.140*v+.095*t),
                    1.00-.78*t+.045*t*(2*v-1)**2,
                    .58-.11*v*v+.10*t+.016*math.sin(4*math.pi*v+.5*t)*math.sin(math.pi*t))
        extra.panel('Avarin_FrontDrape_'+suffix,'cloth',tabard,24,13,.006)
        for edge in (0,1):
            extra.strand('Avarin_DrapeSeam_'+suffix+str(edge),'soft_charcoal',
                [add(tabard(j/18,edge),(0,0,.008)) for j in range(19)],.004)
        def outer(t,v):
            angle=-1+2*v
            return (cx+side*(.19+.08*t)*math.cos(angle),
                    1.05-.73*t+.10*t*v,
                    .42+(.15+.03*t)*math.sin(angle)-.06*t+.012*math.sin(v*math.pi*3)*math.sin(t*math.pi))
        extra.panel('Avarin_OuterDrape_'+suffix,'soft_charcoal',outer,22,13,.006,normal=(side,0,0))
        # Light fabric collar and cuffs; not enlarged combat armor.
        extra.loft('Avarin_Collar_'+suffix,'cloth',[
            ((cx+side*.05,1.48,.36),.035,.025),
            ((cx+side*.14,1.43,.40),.040,.027),
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
    replacements={p['name']:p for p in replacement.parts}
    assert replacements.keys() <= {p['name'] for p in model.parts}
    model.parts=[replacements.get(p['name'],p) for p in model.parts]+extra.parts
