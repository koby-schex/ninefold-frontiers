#!/usr/bin/env python3
"""Current pair palette comparisons; real geometry and matched studio lighting.

Previous palette is applied to CURRENT surface detail to isolate the color change.
This is explicitly not a screenshot reconstruction of the prior release.
"""
import argparse,copy,json,subprocess,tempfile
from pathlib import Path
from build_great_strider import ROOT,make_model as strider_model,write as write_strider,MATERIALS
from build_living_veil import make_model as veil_model,COLORS
from review_living_veil import review_data
from review_worldfive_geometry import verify_geometry
BASELINE='82741eef61caca9debc9becf58fa46ace505556f'
OLD={'living_pearl':(.62,.65,.63),'limb_tissue':(.38,.40,.42),
     'soft_charcoal':(.12,.13,.16),'grown_mineral':(.84,.86,.85),
     'sail_membrane':(.32,.28,.43),'sail_vein':(.64,.59,.76),
     'continuance_frame':(.36,.38,.40),'ceramic':(.65,.66,.60),
     'avar_skin':(.32,.245,.33),'cloth':(.115,.12,.155),
     'survey_light':(.47,.68,.79),'eye':(.055,.065,.07),
     'olyra_core':(.16,.125,.18),'olyra_membrane':(.53,.54,.55)}
OUT=ROOT/'SourceArt/LivingVeil/Review/WorldFiveColor'

def geometry(text):
    rows=[]
    for line in text.splitlines():
        if line.startswith(('v ','o ','usemtl ')):rows.append(line)
        elif line.startswith('f '):rows.append('f '+' '.join(c.split('/')[0] for c in line.split()[1:]))
    return rows

def check():
    from PIL import Image
    import numpy as np
    for file in ('SourceArt/GreatStrider/GreatStrider.obj','SourceArt/LivingVeil/LivingVeil.obj','SourceArt/LivingVeil/LivingVeilProtective.obj'):
        old=subprocess.check_output(['git','show',BASELINE+':'+file],cwd=ROOT,text=True)
        verify_geometry(old,(ROOT/file).read_text())
    # Alpha remains exactly the same at every pixel, despite the new membrane tint.
    import io
    old=subprocess.check_output(['git','show',BASELINE+':SourceArt/LivingVeil/Textures/OlyraMembrane.png'],cwd=ROOT)
    before=np.asarray(Image.open(io.BytesIO(old)))[:,:,3]
    after=np.asarray(Image.open(ROOT/'SourceArt/LivingVeil/Textures/OlyraMembrane.png'))[:,:,3]
    assert np.array_equal(before,after),'Canopy opacity changed'
    for file in ('BuildTools/Art/living_veil_motion.py','Assets/Ninefold/Editor/LivingVeilMotion.cs'):
        assert subprocess.check_output(['git','show',BASELINE+':'+file],cwd=ROOT)==(ROOT/file).read_bytes(),('Motion changed',file)
    previous=json.loads(subprocess.check_output(['git','show',BASELINE+':SourceArt/LivingVeil/motion-study.json'],cwd=ROOT))
    current=json.loads((ROOT/'SourceArt/LivingVeil/motion-study.json').read_text())
    assert previous['duration_seconds']==current['duration_seconds']
    assert [s['frame'] for s in previous['samples']]==[s['frame'] for s in current['samples']]
    # Readable broad material separation; not device/readability certification.
    def luminance(rgb):return sum(c*w for c,w in zip(rgb,(.2126,.7152,.0722)))
    assert luminance(MATERIALS['grown_mineral'])>luminance(MATERIALS['living_pearl'])>luminance(MATERIALS['limb_tissue'])
    assert luminance(COLORS['avar_skin'])>1.8*luminance(COLORS['cloth'])
    assert luminance(COLORS['olyra_membrane'])>3*luminance(COLORS['olyra_core'])
    return dict(baseline=BASELINE,topology_and_assignments_unchanged=True,bounded_organic_geometry=True,canopy_alpha_identical=True,
                living_veil_motion_unchanged=True,palette_separation=True,
                limits='Color proposals; no native Unity, mobile or final visual acceptance')

def main(render_images):
    report=check();OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'checks.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2),flush=True)
    if not render_images:return
    from render_great_strider import render,font
    from PIL import Image,ImageDraw
    with tempfile.TemporaryDirectory() as d:
        write_strider(strider_model(),Path(d))
        strider=json.loads((Path(d)/'review_mesh.json').read_text())
    veil=review_data(veil_model('protective'))
    sheet=Image.new('RGB',(1440,1140),'#eeede5');labels=ImageDraw.Draw(sheet)
    labels.text((16,12),'WORLD FIVE / COLOR AND SURFACE READABILITY',font=font(25),fill='#262c32')
    labels.text((16,50),'Previous palette vs revised palette on current surface detail. Matched cameras and light; actual meshes.',font=font(16),fill='#596069')
    cases=[('Great Strider',strider,(8,12,10),(0,3,0),8.5,(6,4,8),(0,4.3,.12),1.9),
           ('Living Veil',veil,(6,9,9),(.3,1.7,0),7.1,(3,3.6,6),(0,3.1,0),1.65)]
    for row,(name,data,eye,target,extent,close_eye,close_target,close_extent) in enumerate(cases):
        y=115+row*505
        for col in range(4):
            previous=col%2==0;close=col>=2
            shown=copy.deepcopy(data)
            if previous:shown['materials']={**shown['materials'],**OLD}
            if close:
                prefix=('Avarin_','Cradle_','Waycaster_') if row==0 else ('Olyra_Core','Olyra_SensoryCrest','Olyra_RecessedOrgan','Olyra_Lobe_','Olyra_Rib_','Olyra_Vein_')
                shown['parts']=[p for p in shown['parts'] if p['name'].startswith(prefix)]
            sheet.paste(render(shown,close_eye if close else eye,(360,455),close_extent if close else extent,close_target if close else target,False),(col*360,y))
            labels.text((col*360+8,y-25),name+' / '+('previous' if previous else 'revised'),font=font(14),fill='#262c32')
    labels.text((16,1100),'Software color/average-finish review. Normal maps and full layered transparency require native Unity review.',font=font(16),fill='#596069')
    sheet.save(OUT/'Pair-Color-Comparison.png')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--render',action='store_true');main(p.parse_args().render)
