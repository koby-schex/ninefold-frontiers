"""Deterministic, low-contrast study textures and rest-space box UVs.

No geometry displacement or new anatomical markings. Detail maps use NumPy.
"""
import math
import struct
import zlib
from functools import lru_cache

# (variation amplitude, tile width in meters, finish description)
SURFACES = {
    'living_pearl': (.24, .65, 'soft satin organic tissue'),
    'limb_tissue': (.26, .45, 'matte flexible tissue'),
    'soft_charcoal': (.045, .25, 'matte contact tissue'),
    'grown_mineral': (.20, .32, 'pale natural mineral'),
    'avar_skin': (.12, .18, 'smoky plum organic skin'),
    'cloth': (.22, .12, 'matte fine fabric'),
    'continuance_frame': (.025, .30, 'restrained engineered satin'),
    'ceramic': (.035, .28, 'smooth ceramic'),
}
SIZE = 128

@lru_cache(maxsize=1)
def tiles():
    result = {}
    for name, (amplitude, _, _) in SURFACES.items():
        values = []
        for y in range(SIZE):
            row = []
            for x in range(SIZE):
                u, v = 2*math.pi*x/SIZE, 2*math.pi*y/SIZE
                broad = .48*math.sin(u+1.3*math.sin(v)) + .32*math.cos(2*v+.8*math.sin(3*u))
                fine = .20*math.sin(7*u+2*math.sin(5*v))*math.cos(9*v)
                if name == 'living_pearl':
                    fine = .20*math.sin(5*u+.7*math.sin(3*v))*math.cos(4*v)
                elif name == 'grown_mineral':
                    fine = .20*math.sin(8*u+1.1*math.sin(v))**3
                elif name == 'cloth':
                    fine = .20*math.cos(16*u)*math.cos(16*v)
                # Linear multiplier, bounded [1-amplitude, 1]. No painted highlights.
                row.append(round(255*(1-amplitude*(.5+.5*(broad+fine)))))
            values.append(row)
        result[name] = values
    return result


def albedo_pixels(values, color):
    def srgb(v):
        return round(255*(12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055))
    return [[[srgb(c*value/255) for c in color] for value in row] for row in values]


def png_albedo(path, values, color):
    """Lossless sRGB albedo PNG; deterministic bytes, no Pillow build dependency."""
    def chunk(kind, content):
        return struct.pack('!I',len(content))+kind+content+struct.pack('!I',zlib.crc32(kind+content)&0xffffffff)
    h, w = len(values), len(values[0])
    raw = b''.join(b'\0'+bytes(c for pixel in row for c in pixel) for row in albedo_pixels(values,color))
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',w,h,8,2,0,0,0))+
                     chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))


def attach_uvs(parts):
    """Per-triangle dominant-axis projection; UV seams never split geometry.

Tiny low-contrast tiling makes these provisional seams unobtrusive. This is
not a production unwrap. UVs are calculated once in rest pose and travel with it.
"""
    for p in parts:
        if p['material'] not in SURFACES:
            continue
        width = SURFACES[p['material']][1]
        coords, uv_faces, cache = [], [], {}
        for face in p['faces']:
            a,b,c = [p['vertices'][i] for i in face]
            ab=[b[i]-a[i] for i in range(3)]; ac=[c[i]-a[i] for i in range(3)]
            n=[ab[1]*ac[2]-ab[2]*ac[1], ab[2]*ac[0]-ab[0]*ac[2], ab[0]*ac[1]-ab[1]*ac[0]]
            axis=max(range(3),key=lambda i:abs(n[i])); axes=((2,1),(0,2),(0,1))[axis]
            ids=[]
            for index in face:
                key=index,axis
                if key not in cache:
                    cache[key]=len(coords)
                    coords.append([float(p['vertices'][index][k])/width for k in axes])
                ids.append(cache[key])
            uv_faces.append(ids)
        p['uvs'],p['uv_faces']=coords,uv_faces


def detail_maps(finish):
    """Tangent normals and metallic/smoothness masks from the same tissue field.

    RGB albedo stays sRGB; normals/masks are linear data, never baked lighting.
    Strength is a study parameter; production UV baking remains outstanding.
    """
    import numpy as np
    result = {}
    for name, values in tiles().items():
        amplitude = SURFACES[name][0]
        h = (1-np.asarray(values,dtype=float)/255)/amplitude
        strength = .9 if name in ('living_pearl','limb_tissue','grown_mineral') else .55
        dx = (np.roll(h,-1,axis=1)-np.roll(h,1,axis=1))*strength
        dy = (np.roll(h,1,axis=0)-np.roll(h,-1,axis=0))*strength
        n = np.stack((-dx,-dy,np.ones_like(h)),axis=-1)
        n /= np.linalg.norm(n,axis=-1,keepdims=True)
        normal = np.rint(255*(.5+.5*n)).astype('uint8')
        smoothness = 1-(2/(finish[name][0]+2))**.25
        mask = np.zeros((SIZE,SIZE,4),dtype='uint8')
        mask[:,:,3] = np.rint(255*np.clip(smoothness+.09*(h-.5),0,1))
        result[name] = (normal,mask)
    return result


def write_detail_maps(folder, finish):
    from living_veil_materials import png
    for name,(normal,mask) in detail_maps(finish).items():
        png(folder/(name+'_Normal.png'),normal.tolist(),3)
        png(folder/(name+'_Finish.png'),mask.tolist(),4)
