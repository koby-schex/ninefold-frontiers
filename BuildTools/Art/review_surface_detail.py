#!/usr/bin/env python3
"""Check authored surface maps and export a literal texture inspection sheet."""
import json,tempfile
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
from build_great_strider import ROOT,FINISH
from strider_materials import SURFACES,write_detail_maps
from living_veil_materials import SURFACES as VEIL

def check():
    source=ROOT/'SourceArt/GreatStrider/Textures'
    with tempfile.TemporaryDirectory() as d:
        write_detail_maps(Path(d),FINISH)
        for f in Path(d).glob('*.png'):
            assert f.read_bytes()==(source/f.name).read_bytes(),f.name
    reports=[]
    for model,names in (('GreatStrider',SURFACES),('LivingVeil',VEIL)):
        folder=ROOT/'SourceArt'/model/'Textures'
        for name in names:
            n=np.asarray(Image.open(folder/(name+'_Normal.png')),dtype=float)/255*2-1
            mask=np.asarray(Image.open(folder/(name+'_Finish.png')))
            assert n.shape==(128,128,3) and mask.shape==(128,128,4)
            assert np.max(abs(np.linalg.norm(n,axis=-1)-1))<.012,(model,name,'non-unit normal')
            assert n[:,:,2].min()>.65,(model,name,'excess relief')
            assert np.ptp(mask[:,:,3])>0,(model,name,'flat finish')
            assert not mask[:,:,1:3].any()
            if name in ('living_pearl','limb_tissue','grown_mineral','olyra_core'):
                assert np.max(np.linalg.norm(n[:,:,:2],axis=-1))>.075,(model,name,'imperceptible relief')
            reports.append(dict(model=model,material=name,peak_normal_tilt=float(np.max(np.linalg.norm(n[:,:,:2],axis=-1)))))
    out=ROOT/'SourceArt/LivingVeil/Review/SurfaceDetail';out.mkdir(parents=True,exist_ok=True)
    (out/'checks.json').write_text(json.dumps(reports,indent=2)+'\n')
    selected=[('GreatStrider',n) for n in ('living_pearl','limb_tissue','grown_mineral','cloth')]+[('LivingVeil',n) for n in ('olyra_core','avar_skin','cloth','sail_membrane')]
    sheet=Image.new('RGB',(896,800),'#ebe9e2');draw=ImageDraw.Draw(sheet)
    draw.text((16,15),'ACTUAL SURFACE MAPS / albedo - tangent normal - smoothness',fill='#202830')
    for i,(model,name) in enumerate(selected):
        x=(i%4)*224;y=55+(i//4)*365
        folder=ROOT/'SourceArt'/model/'Textures'
        draw.text((x+8,y),model+' / '+name,fill='#202830')
        sheet.paste(Image.open(folder/(name+'.png')).resize((208,208)),(x+8,y+25))
        sheet.paste(Image.open(folder/(name+'_Normal.png')).resize((96,96)),(x+8,y+245))
        sheet.paste(Image.open(folder/(name+'_Finish.png')).getchannel('A').resize((96,96)),(x+120,y+245))
    sheet.save(out/'Surface-Maps.png')
    print('Surface maps passed: 8 Strider and 7 Veil normal/finish pairs. No native shading certification.')

if __name__=='__main__':check()
