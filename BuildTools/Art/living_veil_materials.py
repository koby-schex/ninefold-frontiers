"""Partner surface study 06. Deterministic maps; no new anatomical markings.

UVs are attached before partner pose translation. Box seams are provisional;
production unwrap and native URP material acceptance remain outstanding.
"""
import math
import struct
import zlib
from functools import lru_cache

SIZE = 128
# tile meters, albedo amplitude, normal strength, smoothness, metallic
SURFACES = {
    'olyra_core': (.32, .25, .65, .38, 0),
    'avar_skin': (.20, .12, .35, .32, 0),
    'cloth': (.16, .24, .40, .16, 0),
    'soft_charcoal': (.18, .12, .35, .22, 0),
    'sail_membrane': (.32, .17, .35, .47, 0),
    'continuance_frame': (.24, .045, .025, .55, .25),
    'ceramic': (.24, .035, .018, .62, 0),
}

def field(name, u, v):
    u *= math.tau; v *= math.tau
    broad = .45*math.sin(u+.7*math.sin(v))+.30*math.cos(2*v+.4*math.sin(2*u))
    if name in ('cloth', 'soft_charcoal'):
        return .55*broad+.25*math.cos(24*u)*math.cos(24*v)+.15*math.sin(3*u+math.sin(v))
    if name == 'olyra_core':
        return .70*broad+.20*math.sin(5*u+math.sin(2*v))*math.cos(4*v)
    if name == 'sail_membrane':
        return .60*broad+.25*math.sin(8*u+1.4*math.sin(v))**3
    return .75*broad+.15*math.sin(9*u+math.sin(3*v))*math.cos(7*v)

@lru_cache(maxsize=1)
def maps():
    result = {}
    for name, (_, amplitude, strength, smoothness, metallic) in SURFACES.items():
        heights = [[field(name,x/SIZE,y/SIZE) for x in range(SIZE)] for y in range(SIZE)]
        albedo=[];normal=[];mask=[]
        for y in range(SIZE):
            ar=[];nr=[];mr=[]
            for x in range(SIZE):
                h=heights[y][x]
                ar.append(1-amplitude*(.5+.5*h))
                # Tangent +V points up while PNG rows run down. No baked light.
                dx=(heights[y][(x+1)%SIZE]-heights[y][(x-1)%SIZE])*strength
                dy=(heights[(y-1)%SIZE][x]-heights[(y+1)%SIZE][x])*strength
                length=math.sqrt(1+dx*dx+dy*dy)
                nr.append(tuple(round(255*(.5+.5*q/length)) for q in (-dx,-dy,1)))
                mr.append((round(255*metallic),0,0,round(255*(smoothness+.065*h))))
            albedo.append(ar);normal.append(nr);mask.append(mr)
        result[name]=(albedo,normal,mask)
    return result

def png(path, pixels, channels):
    def chunk(tag,data):
        return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
    raw=b''.join(b'\0'+bytes(c for pixel in row for c in pixel) for row in pixels)
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',SIZE,SIZE,8,2 if channels==3 else 6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))

def write_maps(folder, colors):
    def srgb(v):return round(255*(12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055))
    for name,(albedo,normal,mask) in maps().items():
        png(folder/(name+'.png'),[[tuple(srgb(c*a) for c in colors[name]) for a in row] for row in albedo],3)
        png(folder/(name+'_Normal.png'),normal,3)
        png(folder/(name+'_Finish.png'),mask,4)

def attach_uvs(parts):
    for p in parts:
        if p['material'] not in SURFACES:continue
        width=SURFACES[p['material']][0];coords=[];faces=[];cache={}
        for face in p['faces']:
            a,b,c=[p['vertices'][i] for i in face]
            ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)]
            n=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
            axis=max(range(3),key=lambda i:abs(n[i]));axes=((2,1),(0,2),(0,1))[axis];ids=[]
            for i in face:
                key=i,axis
                if key not in cache:
                    cache[key]=len(coords);coords.append(tuple(p['vertices'][i][k]/width for k in axes))
                ids.append(cache[key])
            faces.append(ids)
        p['uvs']=coords;p['uv_faces']=faces
