"""Anatomy and civic architecture revision 04. Unseen contours are proposals.

Only existing canonical anatomy: two Vaskel limbs, sensory crescent and flexible
counterweight; eight muscular Tervan limbs and independent civic infrastructure.
"""
import math
from build_great_strider import Mesh


def ridge(m,name,mat,points,widths,depths):
    """Tapered tissue/mineral ribbon, with a gently raised longitudinal keel."""
    m.loft(name,mat,list(zip(points,widths,depths)),24)
    p=m.parts[-1];v=p['vertices'];s=p['loft_sides']
    for start in range(0,len(v)-2,s):
        center=[sum(v[start+j][k] for j in range(s))/s for k in range(3)]
        for j in range(s):
            q=v[start+j];a=math.tau*j/s
            scale=1+.075*math.cos(3*a)
            v[start+j]=tuple(center[k]+(q[k]-center[k])*scale for k in range(3))


def skin_relief(part,amplitude):
    """Broad longitudinal tissue planes and shallow flexure folds in the mesh."""
    v=part['vertices'];s=part['loft_sides'];count=(len(v)-2)//s
    for ring in range(count):
        t=ring/(count-1);start=ring*s
        center=[sum(v[start+j][k] for j in range(s))/s for k in range(3)]
        for j in range(s):
            angle=math.tau*j/s
            fold=math.exp(-((t-.58)/.14)**2)*math.sin(9*math.pi*t)
            scale=1+amplitude*math.sin(math.pi*t)**2*(.65*math.cos(4*angle+.6*t)+.35*fold)
            v[start+j]=tuple(center[k]+(v[start+j][k]-center[k])*scale for k in range(3))


def side_point(part,ring,side):
    s=part['loft_sides'];row=part['vertices'][ring*s:(ring+1)*s]
    return max(row,key=lambda v:side*v[0])


def surface_folds(m,part):
    """Trace shallow folds from the parent surface so they cannot float free."""
    v=part['vertices'];s=part['loft_sides'];count=(len(v)-2)//s
    for fold,j in enumerate((3,7,15,19)):
        points=[]
        for ring in range(2,count-2):
            row=v[ring*s:(ring+1)*s];p=row[j]
            center=[sum(q[k] for q in row)/s for k in range(3)]
            length=math.sqrt(sum((p[k]-center[k])**2 for k in range(3)))
            points.append(tuple(p[k]-.006*(p[k]-center[k])/length for k in range(3)))
        widths=[.003+.012*math.sin(math.pi*i/(len(points)-1)) for i in range(len(points))]
        ridge(m,'Vaskel_SurfaceFold_'+str(fold),'vaskel_flexure',points,widths,[w*.8 for w in widths])


def vaskel():
    m=Mesh()
    # The crescent is now sagittal, with a deep inward waist and offset keel.
    body=[(-.10,.81,-.42),(-.09,1.06,-.53),(-.04,1.32,-.46),
          (.04,1.48,-.24),(.10,1.48,.05),(.13,1.38,.29),(.12,1.23,.43)]
    ridge(m,'Vaskel_CrescentTorso','vaskel_tissue',body,
          [.075,.15,.215,.24,.20,.13,.025],[.09,.14,.19,.20,.14,.095,.025])
    skin_relief(m.parts[-1],.13)
    surface_folds(m,m.parts[-1])
    ridge(m,'Vaskel_WaistWeb','vaskel_flexure',
          [(-.12,1.23,-.39),(-.08,1.15,-.30),(.08,1.13,-.18),(.22,1.12,-.04)],
          [.06,.13,.14,.07],[.07,.13,.14,.08])
    # Unequal loaded limbs: thick roots enter the waist; the hock narrows before
    # spreading into a soft pad. No third limb or conventional mammalian head.
    limbs={
        'L':([(-.16,1.08,-.29),(-.29,.89,-.12),(-.36,.59,-.28),(-.32,.28,-.13),(-.30,.10,.04)],
             [.15,.125,.085,.065,.095],[.17,.17,.095,.07,.11]),
        'R':([(.15,1.20,-.10),(.31,.98,.13),(.37,.64,.02),(.34,.34,.22),(.32,.10,.43)],
             [.17,.145,.09,.065,.09],[.18,.18,.095,.065,.105])}
    for side,(points,width,depth) in limbs.items():
        ridge(m,'Vaskel_Limb_'+side,'vaskel_tissue',points,width,depth)
        skin_relief(m.parts[-1],.19)
        x,_,z=points[-1]
        m.oval('Vaskel_Contact_'+side,'soft_charcoal',(x,.065,z),(.12,.065,.16))
        p=m.parts[-1];bottom=min(v[1] for v in p['vertices'])
        p['vertices']=[(a,b-bottom,c) for a,b,c in p['vertices']]
        m.oval('Vaskel_AnkleRoot_'+side,'vaskel_flexure',(x,.135,z),(.085,.10,.105))
    # Flexible flattened counterweight on the opposite side of the offset torso.
    ridge(m,'Vaskel_Counterweight','vaskel_tissue',
          [(-.16,1.03,-.33),(-.35,.97,-.49),(-.55,1.04,-.64),(-.69,1.24,-.66),(-.68,1.40,-.53)],
          [.12,.13,.105,.065,.008],[.10,.085,.05,.025,.008])
    skin_relief(m.parts[-1],.10)
    # Pale sensory crescent sits against the outer body, with layered tapered
    # margins. Its tiny pits and broken channels remain recessed/non-emissive.
    crest=[(-.08,1.13,-.67),(-.03,1.48,-.50),(.04,1.72,-.23),(.10,1.69,.10),(.12,1.46,.37),(.12,1.29,.45)]
    ridge(m,'Vaskel_SensoryCrescent','grown_mineral',crest,
          [.012,.085,.10,.085,.055,.008],[.012,.025,.035,.03,.02,.008])
    sensory=m.parts[-1]
    skin_relief(sensory,.06)
    for side in (-1,1):
        for tier in range(2):
            path=[(x+side*(.13+.035*tier),y-.075-.065*tier,z-.015-.055*tier) for x,y,z in crest[1:5]]
            path[-1]=(path[-1][0],path[-1][1]+.045*tier,path[-1][2]-.035*tier)
            ridge(m,'Vaskel_CrescentMargin_'+str(side)+'_'+str(tier),'grown_mineral',path,
                  [.008,.075,.065,.006],[.008,.028,.020,.006])
        for k in range(3):
            x,y,z=side_point(sensory,3+3*k,side)
            m.oval('Vaskel_SensoryPit_'+str(side)+'_'+str(k),'eye',
                   (x-side*.003,y,z),(.009,.008,.009))
        # Secondary visual organ in the front sensory fold, not a glowing eye.
        m.oval('Vaskel_SecondaryEye_'+str(side),'eye',(.12+side*.072,1.37,.397),(.014,.011,.008))
        for k in range(2):
            a,b=crest[k+1],crest[k+2]
            pts=[tuple(a[j]+(b[j]-a[j])*t for j in range(3)) for t in (.18,.38,.58)]
            ridge(m,'Vaskel_ChannelSegment_'+str(side)+'_'+str(k),'anchor_channel',
                  [(x+side*.14,y-.075,z) for x,y,z in pts],[.006,.012,.006],[.006,.008,.006])
    m.oval('Vaskel_ContinuityAnchor','ceramic',(.28,1.30,-.18),(.025,.043,.035))
    return m


def civic_box(m,name,center,size,mat='ceramic'):
    """Chamfered rectangular module; walls and roof remain separate equipment."""
    x,y,z=center;w,h,d=size
    m.loft(name,mat,[((x,y-h/2,z),w*.46,d*.46),((x,y-h*.37,z),w/2,d/2),
                    ((x,y+h*.37,z),w/2,d/2),((x,y+h/2,z),w*.46,d*.46)],16)
    p=m.parts[-1];v=p['vertices'];s=p['loft_sides']
    for start in range(0,len(v)-2,s):
        rx=max(abs(v[start+j][0]-x) for j in range(s));rz=max(abs(v[start+j][2]-z) for j in range(s))
        for j in range(s):
            a,b,c=v[start+j];u=(a-x)/rx;t=(c-z)/rz
            v[start+j]=(x+rx*math.copysign(abs(u)**.30,u),b,z+rz*math.copysign(abs(t)**.30,t))


def civic_arch(m,name,x,y,z,width,height):
    """Closed inset arched panel in the outer wall; not a new creature feature."""
    contour=[(-width/2,0),(width/2,0)]
    contour += [(width*.5*math.cos(i*math.pi/12),height-width*.5+width*.5*math.sin(i*math.pi/12)) for i in range(13)]
    n=len(contour);vertices=[];faces=[]
    for side in (-1,1):vertices += [(x+side*.009,y+b,z+a) for a,b in contour]
    for layer in range(2):
        center=len(vertices);vertices.append((x+(-1 if layer==0 else 1)*.009,y+height*.42,z))
        for j in range(n):
            a=layer*n+j;b=layer*n+(j+1)%n
            faces.append((center,b,a) if layer==0 else (center,a,b))
    for j in range(n):
        k=(j+1)%n;faces.extend(((j,k,k+n),(j,k+n,j+n)))
    m.part(name,'soft_charcoal',vertices,faces)


def tervan():
    m=Mesh()
    ridge(m,'Tervan_Torso','tervan_hide',
          [(0,1.05,-2.70),(0,1.10,-2.48),(0,1.18,-2.12),(0,1.27,-1.18),(0,1.25,.10),(0,1.17,1.35),(0,1.02,2.34)],
          [.025,.54,.89,1.20,1.27,1.04,.47],[.025,.32,.48,.52,.49,.40,.26])
    p=m.parts[-1]
    p['vertices']=[(x,1.48+.075*(x/1.3)**2+.10*(y-1.48) if y>1.48 else y,z) for x,y,z in p['vertices']]
    # Low, slightly projecting sensory hood is layered into the front torso.
    ridge(m,'Tervan_SensoryHood','tervan_hide',
          [(0,1.16,1.65),(0,1.11,2.12),(0,1.03,2.55),(0,.96,2.76)],
          [.62,.72,.55,.20],[.22,.30,.24,.09])
    for side in (-1,1):
        s='L' if side<0 else 'R'
        ridge(m,'Tervan_HoodMargin_'+s,'grown_mineral',
              [(side*.20,1.38,1.93),(side*.52,1.37,2.22),(side*.50,1.16,2.63),(side*.20,1.05,2.80)],
              [.035,.16,.095,.01],[.02,.035,.03,.01])
        m.oval('Tervan_RecessedEye_'+s,'eye',(side*.51,1.17,2.54),(.021,.018,.024))
        for k,z in enumerate((-1.92,-.67,.58,1.65)):
            root=side*(.85 if k in (0,3) else 1.07);foot=side*(1.28 if k in (0,3) else 1.53)
            ridge(m,'Tervan_Limb_'+s+str(k),'tervan_hide',
                  [(root*.78,1.33,z-.10),(root,1.12,z),(foot*.96,.84,z+.14),
                   (foot,.50,z+.06),(foot,.20,z+.15)],
                  [.29,.36,.31,.21,.25],[.32,.40,.33,.22,.28])
            skin_relief(m.parts[-1],.11)
            # Fan-shaped support ridges emerge from the back and cover the root.
            ridge(m,'Tervan_SupportRidge_'+s+str(k),'grown_mineral',
                  [(side*.32,1.52,z-.35),(side*.70,1.62,z-.22),(root,1.48,z),(foot*.94,1.06,z+.16)],
                  [.03,.26,.31,.045],[.018,.055,.07,.025])
            skin_relief(m.parts[-1],.09)
            ridge(m,'Tervan_RootFold_'+s+str(k),'tervan_joint',
                  [(root*.82,1.28,z+.27),(root,1.10,z+.36),(foot*.94,.80,z+.37)],
                  [.02,.035,.012],[.02,.026,.012])
            m.oval('Tervan_Contact_'+s+str(k),'tervan_joint',(foot,.13,z+.17),(.34,.13,.36))
            p=m.parts[-1];bottom=min(v[1] for v in p['vertices'])
            p['vertices']=[(a,b-bottom,c) for a,b,c in p['vertices']]
    ridge(m,'Tervan_FeedingFold','tervan_joint',[(-.28,.86,2.61),(0,.81,2.71),(.28,.86,2.61)],
          [.02,.045,.02],[.02,.035,.02])
    # Civic platform: load spreaders, colonnaded compartments and roof modules.
    # The arrangement is a proposal based on the locked mobile architecture.
    for side in (-1,1):
        for k,z in enumerate((-1.6,-.35,.90)):
            civic_box(m,'CivicFrame_LoadShoe_'+str(side)+str(k),(side*.66,1.64,z),(.28,.16,.55),'continuance_frame')
        civic_box(m,'CivicFrame_LongSill_'+str(side),(side*.72,1.81,-.24),(.13,.20,4.02),'continuance_frame')
    civic_box(m,'CivicFrame_Deck',(0,1.91,-.24),(1.65,.14,4.12),'continuance_frame')
    for k,z in enumerate((-1.55,-.22,1.07)):
        for side in (-1,1):
            label=('Archive','Care','Shelter')[k]+str(side);x=side*.45
            height=(.61,.43,.18)[k];top=1.97+height
            civic_box(m,'CivicModule_'+label,(x,1.97+height/2,z),(.65,height,1.06))
            if k<2:
                for bay in range(3):
                    zz=z-.34+bay*.34
                    civic_arch(m,'CivicModule_Arch_'+label+str(bay),side*.782,2.02,zz,.21,height-.10)
            # Archive terraces, care vaults and open shelter framing read as
            # distinct functions; exact arrangement remains an art proposal.
            if k==0:
                civic_box(m,'CivicModule_Terrace_'+label,(x,top+.03,z),(.74,.09,1.15),'continuance_frame')
                civic_box(m,'CivicModule_UpperArchive_'+label,(x,top+.14,z-.12),(.47,.18,.66))
                civic_box(m,'CivicModule_ArchiveCap_'+label,(x,top+.25,z-.12),(.54,.06,.74),'continuance_frame')
            elif k==1:
                ridge(m,'CivicModule_Vault_'+label,'ceramic',
                      [(x,top,z-.54),(x,top+.06,z-.35),(x,top+.08,z+.24),(x,top,z+.54)],
                      [.32,.36,.36,.32],[.025,.12,.12,.025])
                for zz in (z-.38,z+.30):
                    m.strand('CivicModule_VaultRib_'+label+str(zz),'continuance_frame',
                        [(x-.34,top+.02,zz),(x-.22,top+.15,zz),(x,top+.20,zz),(x+.22,top+.15,zz),(x+.34,top+.02,zz)],.014)
            else:
                for corner in (-1,1):
                    for end in (-1,1):
                        civic_box(m,'CivicModule_CanopyPost_'+label+str(corner)+str(end),
                                  (x+corner*.28,2.35,z+end*.44),(.045,.55,.045),'continuance_frame')
                ridge(m,'CivicModule_ShelterCanopy_'+label,'ceramic',
                      [(x,2.59,z-.56),(x,2.64,z-.34),(x,2.64,z+.34),(x,2.59,z+.56)],
                      [.33,.38,.38,.33],[.018,.055,.055,.018])
                for tier in range(2):
                    civic_box(m,'CivicModule_FoldedPanel_'+label+str(tier),(x,2.19+tier*.045,z),(.54,.026,.82),'continuance_frame')
            for bay in range(3 if k<2 else 0):
                zz=z-.34+bay*.34
                for edge in (-1,1):
                    m.strand('CivicModule_Pier_'+label+str(bay)+str(edge),'continuance_frame',
                        [(side*.80,1.99,zz+edge*.13),(side*.80,top,zz+edge*.13)],.014)
            for end in (-1,1):
                m.strand('CivicModule_RoofRail_'+label+str(end),'ceramic',
                    [(x-.30,top+.08,z+end*.51),(x,top+.12,z+end*.51),(x+.30,top+.08,z+end*.51)],.016)
    civic_box(m,'Convergence_StowedHousing',(0,2.17,-.22),(.20,.38,1.02),'continuance_frame')
    for side in (-1,1):
        ridge(m,'Convergence_FoldedSupport_'+str(side),'ceramic',
              [(side*.09,1.98,-.63),(side*.13,2.35,-.22),(side*.09,2.21,.24)],
              [.02,.035,.02],[.02,.025,.02])
    return m
