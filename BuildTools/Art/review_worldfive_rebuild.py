"""Matched actual-mesh comparisons; no generated concept art or canon approval."""
import copy,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
from build_worldfive_roster import BUILDERS,COLORS,FINISH,ROOT
from review_worldfive_geometry import parts
from render_great_strider import render,font

BASE='27816165084cac51ecdd4f309e35e1939aa71a55'

def main():
    out=ROOT/'SourceArt/WorldFiveRoster/Review';out.mkdir(parents=True,exist_ok=True)
    sheet=Image.new('RGB',(1920,1160),'#eeede5');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),'WORLD FIVE / ANATOMY AND ARCHITECTURE 04 / GEOMETRY COMPARISON',font=font(24),fill='#262c32')
    draw.text((20,48),'Matched framing and flat materials. Actual mesh proposals; visual acceptance pending.',font=font(17),fill='#596069')
    for row,name in enumerate(('Riftwalker','Burdenbeast')):
        prefix='Vaskel_' if row==0 else ('Tervan_','Civic','Convergence_')
        old=parts(subprocess.check_output(['git','show',BASE+':SourceArt/'+name+'/'+name+'.obj'],cwd=ROOT,text=True))
        oldparts=[dict(name=n,**p) for n,p in old.items() if n.startswith(prefix)]
        newparts=[copy.deepcopy(p) for p in BUILDERS[name]().parts if p['name'].startswith(prefix)]
        target=(-.05,.90,-.05) if row==0 else (0,1.3,0);extent=2.25 if row==0 else 6.4
        for col,(label,mesh,eye) in enumerate((('Previous / three-quarter',oldparts,(7,3,10)),
                ('Rebuilt / three-quarter',newparts,(7,3,10)),('Rebuilt / side',newparts,(10,2,0)),('Rebuilt / front',newparts,(0,2,10)))):
            data=dict(parts=mesh,materials=COLORS,finish=FINISH)
            sheet.paste(render(data,eye,(480,480),extent,target,False),(480*col,120+520*row))
            draw.text((480*col+12,94+520*row),name+' / '+label,font=font(15),fill='#262c32')
    sheet.save(out/'Rebuild-Comparison04.png')
    deliver=ROOT/'Docs/Review/WorldFiveAnatomy04';deliver.mkdir(parents=True,exist_ok=True)
    sheet.save(deliver/'Comparison.png')

if __name__=='__main__':main()
