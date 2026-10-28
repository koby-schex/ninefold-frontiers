"""Small deterministic tissue maps; study detail, no new anatomical markings."""
import math
from functools import lru_cache
import numpy as np
from strider_materials import tiles
from living_veil_materials import png

LOCAL={'tervan_hide','tervan_joint','vaskel_tissue','vaskel_flexure'}

@lru_cache(maxsize=None)
def values(name,fallback):
    if name not in LOCAL:return tiles()[fallback]
    y,x=np.mgrid[:128,:128];u=x*math.tau/128;v=y*math.tau/128
    broad=.5+.24*np.sin(u+.7*np.sin(v))+.16*np.cos(2*v)
    grain=.5+.5*np.sin(13*u+1.2*np.sin(3*v))*np.cos(11*v)
    fine=.5+.5*np.sin(23*v+.6*np.sin(7*u))
    amplitude=.23 if name=='tervan_hide' else .17
    return np.rint(255*(1-amplitude*(.72*broad+.18*grain+.10*fine))).astype('uint8').tolist()

def write_details(folder,name,fallback,exponent):
    h=np.asarray(values(name,fallback),dtype=float)/255
    dx=(np.roll(h,-1,1)-np.roll(h,1,1))*1.8
    dy=(np.roll(h,1,0)-np.roll(h,-1,0))*1.8
    normal=np.stack((-dx,-dy,np.ones_like(h)),axis=-1)
    normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    normal=np.rint(255*(normal*.5+.5)).astype('uint8')
    mask=np.zeros((128,128,4),dtype='uint8')
    mask[:,:,3]=np.rint(255*np.clip(1-(2/(exponent+2))**.25+.2*(h-h.mean()),0,1))
    png(folder/(name+'_Normal.png'),normal.tolist(),3)
    png(folder/(name+'_Finish.png'),mask.tolist(),4)
