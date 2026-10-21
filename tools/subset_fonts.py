"""Build renamed OFL UI subsets; Latin Inter files retain their complete glyph sets."""
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'../tools/python'))
from fontTools.ttLib import TTFont
from fontTools import subset
from fontTools.varLib.instancer import instantiateVariableFont
import shutil
original=root/'../tests/font-preview'
dest=root/'assets/fonts'
dest.mkdir(parents=True,exist_ok=True)
text=''.join(p.read_text(encoding='utf-8-sig') for p in (root/'src').glob('*.cs') if p.name not in ('Program.cs','Tests.cs'))
chars=set(text)|set(chr(i) for i in range(32,127))|set('✓●≈—→…：，。分钟小时射手村维多利亚岛')
(dest/'characters.txt').write_text(''.join(sorted(chars)),encoding='utf-8')
def build(path,family,weight,target):
    font=TTFont(path)
    opts=subset.Options();opts.name_IDs=['*'];opts.name_legacy=True;opts.name_languages=['*']
    sub=subset.Subsetter(options=opts);sub.populate(unicodes=[ord(c) for c in chars]);sub.subset(font)
    if 'fvar' in font:font=instantiateVariableFont(font,{'wght':weight},inplace=True)
    style='Bold' if weight==700 else 'Medium' if weight==500 else 'Regular'
    for rec in font['name'].names[:]:
        if rec.nameID in (1,2,3,4,6,16,17):font['name'].names.remove(rec)
    for nid,value in {1:family,2:style,3:family+'-'+style+'-1.0',4:family+' '+style,6:family.replace(' ','')+'-'+style,16:family,17:style}.items():
        font['name'].setName(value,nid,3,1,0x409)
    font['OS/2'].usWeightClass=weight
    font['OS/2'].fsSelection=(font['OS/2'].fsSelection & ~0x61)|(0x20 if weight==700 else 0x40)
    font['head'].macStyle=1 if weight==700 else 0
    font.save(dest/target)
    saved=TTFont(dest/target);cmap=saved.getBestCmap();missing=[c for c in chars if '\u4e00'<=c<='\u9fff' and ord(c) not in cmap]
    assert not missing,(target,missing)
    print(target,(dest/target).stat().st_size,'bytes; UI CJK coverage PASS')
build(original/'source-vf.ttf','MM Sans UI',400,'MM-Sans-Regular.ttf')
build(original/'source-vf.ttf','MM Sans UI',500,'MM-Sans-Medium.ttf')
build(original/'source-vf.ttf','MM Sans UI',700,'MM-Sans-Bold.ttf')
build(original/'wenkai.ttf','MM Wen UI',400,'MM-Wen-Regular.ttf')
for style in ('Regular','Medium','Bold'):
    shutil.copyfile(original/('inter/extras/ttf/Inter-'+style+'.ttf'),dest/('Inter-'+style+'.ttf'))
(dest/'NOTICE.txt').write_text('MM Sans UI: subset/static instances derived from Adobe Source Han Sans SC.\nMM Wen UI: subset derived from LXGW WenKai.\nDerived families renamed; originals retain their copyrights and OFL licenses.\nInter Regular/Medium/Bold: unmodified, complete glyph coverage.\nBuild recipe: tools/subset_fonts.py. System font fallback handles out-of-subset CJK.\n',encoding='utf-8')
