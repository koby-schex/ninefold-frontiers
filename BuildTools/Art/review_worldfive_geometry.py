"""Bounded organic geometry changes; retain topology and fixed contact anatomy."""
import numpy as np
import argparse,json,subprocess
from build_great_strider import ROOT,MATERIALS,FINISH

ALLOWED=('Ilyth_Limb_','Ilyth_GrownSheath_','Ilyth_ShortNeck','Avarin_Arm_',
         'Avarin_Leg_','Avarin_Torso','Olyra_Core','Olyra_Lobe_','Olyra_Rib_','Olyra_Vein_')

def parts(text):
    result={};current=None;offset=0;count=0
    for line in text.splitlines():
        row=line.split()
        if not row:continue
        if row[0]=='o':
            current=dict(vertices=[],faces=[],material=None);result[row[1]]=current;offset=count
        elif row[0]=='v':current['vertices'].append(tuple(map(float,row[1:])));count+=1
        elif row[0]=='usemtl':current['material']=row[1]
        elif row[0]=='f':current['faces'].append(tuple(int(x.split('/')[0])-1-offset for x in row[1:]))
    return result

def verify_geometry(before,after):
    old,new=parts(before),parts(after);assert old.keys()==new.keys(),'Part identity changed'
    changed={}
    for name,a in old.items():
        b=new[name]
        assert a['faces']==b['faces'] and a['material']==b['material'],name
        va,vb=np.asarray(a['vertices']),np.asarray(b['vertices'])
        assert va.shape==vb.shape,name
        delta=float(np.linalg.norm(vb-va,axis=1).max())
        if delta>1e-5:
            assert name.startswith(ALLOWED),(name,'unexpected geometry change')
            assert delta<.075,(name,'excessive displacement',delta)
            changed[name]=delta
        else:assert np.allclose(va,vb,atol=1e-5),name
    assert changed,'Expected an actual geometry refinement'
    return changed

def review():
    from render_great_strider import render,font
    from PIL import Image,ImageDraw
    from build_living_veil import COLORS
    baseline='8d5a8003fb936ae34767cfb0e3a664502e6aef4d'
    out=ROOT/'SourceArt/LivingVeil/Review/OrganicGeometry';out.mkdir(parents=True,exist_ok=True)
    sheet=Image.new('RGB',(1440,1140),'#eeede5');draw=ImageDraw.Draw(sheet);reports={}
    draw.text((16,14),'ORGANIC GEOMETRY / MATCHED CAMERAS AND FLAT MATERIALS',font=font(24),fill='#262c32')
    draw.text((16,48),'Actual old/new source meshes. Texture detail intentionally omitted to expose form changes.',font=font(16),fill='#596069')
    cases=[('Ilyth','SourceArt/GreatStrider/GreatStrider.obj',(7,6,10),(0,5.2,.8),2.5,('Ilyth_',)),
           ('Olyra','SourceArt/LivingVeil/LivingVeil.obj',(3,3.6,6),(0,2.7,0),1.7,('Olyra_',))]
    for row,(name,file,eye,target,extent,prefix) in enumerate(cases):
        before=subprocess.check_output(['git','show',baseline+':'+file],cwd=ROOT,text=True);after=(ROOT/file).read_text()
        reports[name]=verify_geometry(before,after)
        for col,text in enumerate((before,after)):
            parsed=parts(text)
            data=dict(parts=[dict(name=n,**p) for n,p in parsed.items() if n.startswith(prefix)],materials=COLORS,finish=FINISH,opacity={'sail_membrane':.68,'olyra_membrane':.27})
            sheet.paste(render(data,eye,(720,480),extent,target,False),(col*720,120+row*500))
            draw.text((col*720+16,92+row*500),name+' / '+('previous' if col==0 else 'refined'),font=font(18),fill='#262c32')
    sheet.save(out/'Geometry-Comparison.png');(out/'checks.json').write_text(json.dumps(reports,indent=2)+'\n')

if __name__=='__main__':review()
