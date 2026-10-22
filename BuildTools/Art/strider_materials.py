"""Deterministic, low-contrast study textures and rest-space box UVs.

Standard library only. No geometry displacement or new anatomical markings.
"""
import math
import struct
import zlib
from functools import lru_cache

# (variation amplitude, tile width in meters, finish description)
SURFACES = {
    'living_pearl': (.10, .65, 'soft satin organic tissue'),
    'limb_tissue': (.11, .45, 'matte flexible tissue'),
    'soft_charcoal': (.045, .25, 'matte contact tissue'),
    'grown_mineral': (.11, .32, 'pale natural mineral'),
    'avar_skin': (.045, .18, 'smoky plum organic skin'),
    'cloth': (.14, .12, 'matte fine fabric'),
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
