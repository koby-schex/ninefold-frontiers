#!/usr/bin/env python3
"""Compare actual meshes in identical framing; optional local approved art reference.

Does not upload the reference. Baseline is the merged first form-study generator.
"""
import argparse
import importlib.util
import json
import subprocess
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw
from render_great_strider import render, font, ROOT

BASELINE='4e25ebf95e629537059d6cb280525698df83c146'

def comparison(output,reference=None):
    with tempfile.TemporaryDirectory() as temp:
        temp=Path(temp)
        source=subprocess.check_output(['git','show',BASELINE+':BuildTools/Art/build_great_strider.py'],cwd=ROOT)
        # Preserve generator path depth so its repository-root lookup is valid.
        script=temp/'BuildTools'/'Art'/'baseline.py'; script.parent.mkdir(parents=True)
        script.write_bytes(source)
        spec=importlib.util.spec_from_file_location('strider_baseline',script)
        module=importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        module.write(module.make_model(),temp/'mesh')
        before=json.loads((temp/'mesh/review_mesh.json').read_text())
    after=json.loads((output/'review_mesh.json').read_text())
    sheet=Image.new('RGB',(1600,1100),(238,237,229)); d=ImageDraw.Draw(sheet)
    d.text((35,25),'GREAT STRIDER / ACTUAL MESH COMPARISON',font=font(29),fill='#262c32')
    d.text((35,73),'Same camera, framing and lighting. Both are unfinished form studies.',font=font(18),fill='#596069')
    for x,data,label in [(25,before,'FORM STUDY 01'),(815,after,'FORM STUDY 02')]:
        sheet.paste(render(data,(9,6.5,12),(760,880),7.2),(x,125))
        d.text((x+20,1030),label,font=font(23),fill='#262c32')
    sheet.save(output/'GreatStrider-Comparison.png')
    if reference:
        # Local-only reference comparison; never committed or uploaded by CI.
        sheet=Image.new('RGB',(1600,1120),(238,237,229)); d=ImageDraw.Draw(sheet)
        d.text((25,22),'APPROVED DIRECTION / ACTUAL MESH — QUALITY GAP REMAINS',font=font(25),fill='#262c32')
        ref=Image.open(reference).convert('RGB'); ref.thumbnail((790,870))
        sheet.paste(ref,((800-ref.width)//2,125+(870-ref.height)//2))
        sheet.paste(render(after,(9,6.5,12),(760,870),7.2),(815,125))
        d.text((25,1030),'Reference Study 02: approved visual direction',font=font(18),fill='#262c32')
        d.text((830,1030),'Form Study 02: editable geometry, unfinished',font=font(18),fill='#262c32')
        sheet.save(output/'GreatStrider-ReferenceComparison.png')

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--reference',type=Path)
    p.add_argument('--output',type=Path,default=ROOT/'SourceArt/GreatStrider')
    args=p.parse_args(); comparison(args.output,args.reference)
