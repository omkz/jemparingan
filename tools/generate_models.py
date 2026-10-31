"""Generate three original low-poly OBJ/MTL files for Jemparingan Rasa.
Requires numpy and trimesh. Geometry uses meters and Y-up, importable directly by Unity.
"""
from pathlib import Path
import numpy as np
import trimesh

DEST = Path(__file__).resolve().parents[1] / 'Assets/Jemparingan/Models'
COLORS = {
    'wood': (.44, .23, .13), 'wood_dark': (.20, .11, .10), 'wood_light': (.76, .53, .30),
    'red': (.65, .12, .12), 'gold': (.88, .66, .29), 'cream': (.97, .85, .69),
    'indigo': (.11, .18, .30), 'jade': (.19, .62, .53)
}

class Model:
    def __init__(self):
        self.groups=[]
    def add(self, mesh, material):
        self.groups.append((mesh, material))
    def box(self, pos, extents, material):
        m = trimesh.creation.box(extents=extents)
        m.apply_translation(pos)
        self.add(m, material)
    def sphere(self, pos, scale, material):
        m = trimesh.creation.icosphere(subdivisions=1)
        m.apply_scale(scale)
        m.apply_translation(pos)
        self.add(m, material)
    def beam(self, start, end, diameter, material, sections=8):
        a, b = np.array(start,dtype=float), np.array(end,dtype=float)
        v=b-a
        m=trimesh.creation.cylinder(radius=diameter*0.5,height=float(np.linalg.norm(v)),sections=sections)
        m.apply_transform(trimesh.geometry.align_vectors([0,0,1],v))
        m.apply_translation((a+b)*.5)
        self.add(m,material)
    def save(self,name):
        used=sorted(set(k for _,k in self.groups))
        mtl=name+'.mtl'
        path=DEST/(name+'.obj')
        lines=['# Jemparingan - Rasa original low-poly asset','mtllib '+mtl,'o '+name]
        vi=1
        for idx,(mesh,mat) in enumerate(self.groups):
            lines.append('g part_'+str(idx))
            lines.append('usemtl '+mat)
            for v in mesh.vertices: lines.append('v %.6f %.6f %.6f'%tuple(v))
            for f in mesh.faces: lines.append('f '+' '.join(str(vi+int(j)) for j in f))
            vi += len(mesh.vertices)
        path.write_text('\n'.join(lines)+'\n',encoding='utf-8')
        mtl_lines=['# Original procedural palette - no texture files']
        for key in used:
            rgb=COLORS[key]
            mtl_lines.extend(['newmtl '+key, 'Ka 0.08 0.08 0.08', 'Kd %.4f %.4f %.4f'%rgb,'Ks 0.04 0.04 0.04', 'Ns 24.0', 'illum 2',''])
        (DEST/mtl).write_text('\n'.join(mtl_lines)+'\n',encoding='utf-8')
        return len(self.groups), sum(len(x.faces) for x,_ in self.groups)


def bandulan():
    m=Model()
    m.beam((0,-.23,0),(0,.23,0),.097,'red',sections=10)
    for y in (-.17,0,.17):m.beam((0,y-.005,0),(0,y+.005,0),.105,'gold',sections=10)
    m.beam((0,.23,0),(0,.27,0),.05,'gold')
    m.beam((0,.27,0),(0,.52,0),.009,'cream')
    m.sphere((0,-.245,0),.038,'gold')
    return m.save('jemparingan_bandulan')

def busur():
    m=Model()
    top=[(0,0,0),(.055,.22,.01),(.07,.38,.045),(.025,.55,.09)]
    bottom=[(x,-y,z) for x,y,z in top]
    for chain in (top,bottom):
        for i in range(len(chain)-1):m.beam(chain[i],chain[i+1],.028-i*.004,'wood_light')
    m.beam((0,-.10,0),(0,.10,0),.065,'wood_dark',sections=10)
    m.beam(top[-1],(0,0,-.03),.003,'cream')
    m.beam(bottom[-1],(0,0,-.03),.003,'cream')
    for y in (-.54,.54):m.sphere((.02,y,.09),.018,'gold')
    return m.save('jemparingan_busur')

def pendopo():
    m=Model()
    m.box((0,-.86,0),(1.85,.10,.86),'wood_dark')
    m.box((0,-.795,0),(1.72,.026,.74),'wood')
    for x in np.linspace(-.70,.70,6):m.box((x,-.776,0),(.014,.007,.72),'wood_light')
    for s in (-1,1):
        x=s*.65
        m.box((x,-.06,.10),(.085,1.52,.085),'wood_dark')
        m.box((x,-.78,.10),(.17,.13,.17),'gold')
        m.box((x,.59,.10),(.15,.11,.15),'gold')
        m.beam((x,.65,.10),(x-s*.15,.87,.1),.05,'wood')
    m.box((0,.73,.10),(1.63,.09,.19),'wood')
    m.box((0,.84,.10),(1.75,.05,.28),'wood_dark')
    m.beam((-.87,.88,.10),(0,1.13,.10),.045,'gold')
    m.beam((.87,.88,.10),(0,1.13,.10),.045,'gold')
    m.sphere((0,1.13,.10),.065,'gold')
    m.box((0,-.742,-.15),(.79,.025,.31),'indigo')
    for x in np.linspace(-.27,.27,7):
        m.box((x,-.72,-.15),(.04,.007,.04),'gold')
    return m.save('jemparingan_pendopo_frame')

if __name__=='__main__':
    DEST.mkdir(parents=True,exist_ok=True)
    for name,fn in [('Bandulan',bandulan),('Busur',busur),('Pendopo',pendopo)]:
        count,faces=fn()
        print('%s: %s parts / %s triangles'%(name,count,faces))
