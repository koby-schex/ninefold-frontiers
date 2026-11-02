#!/usr/bin/env python3
"""Executable depth-visibility tests (independent of any real character art)."""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0,str(Path(__file__).resolve().parent))
from mesh_depth_raster import draw_mesh_with_depth


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def test_order_independent_overlap():
    # Two coincident screen triangles, front is green. Painter ordering
    # would fail when the front triangle is submitted first.
    xy=np.asarray([[3,3],[29,3],[3,29],
                   [3,3],[29,3],[3,29]],np.float64)
    tris=np.asarray([[0,1,2],[3,4,5]],np.int32)
    depth=np.asarray([0,0,0,1,1,1],np.float64)
    colors=np.asarray([[225,35,35],[35,225,35]],np.uint8)
    base=Image.new("RGB",(32,32),(0,0,0))
    first=draw_mesh_with_depth(base,xy,depth,tris,colors)
    flipped=draw_mesh_with_depth(base,xy,depth,tris[::-1],colors[::-1])
    check(np.array_equal(np.asarray(first),np.asarray(flipped)),
          "Visibility must not depend on submitted triangle order")
    check(tuple(np.asarray(first)[10,10])==(35,225,35),
          "Far triangle is painting over the near one")


def test_per_pixel_intersection():
    # Both triangles have identical projection but one crosses the other's
    # constant depth from behind to ahead. Centroid sorting cannot solve it.
    xy=np.asarray([[2,2],[30,2],[2,30],
                   [2,2],[30,2],[2,30]],np.float64)
    tris=np.asarray([[0,1,2],[3,4,5]],np.int32)
    # First (red) triangle is nearer on right but behind on left.
    dep=np.asarray([0,2,0,1,1,1],np.float64)
    colors=np.asarray([[230,30,30],[30,230,30]],np.uint8)
    base=Image.new("RGB",(35,35),(0,0,0))
    im=np.asarray(draw_mesh_with_depth(base,xy,dep,tris,colors))
    check(tuple(im[5,24])==(230,30,30),
          "Per-pixel interpolation failed to reveal near red triangle")
    check(tuple(im[20,5])==(30,230,30),
          "Per-pixel interpolation failed to reveal near green triangle")


def test_reject_invalid_input():
    base=Image.new("RGB",(8,8))
    xy=np.asarray([[0,0],[7,0],[0,7]],float)
    tris=np.asarray([[0,1,2]],np.int32)
    colors=np.asarray([[120,130,140]],np.uint8)
    try:
        draw_mesh_with_depth(base,xy,np.asarray([0,np.nan,0]),tris,colors)
    except ValueError:
        pass
    else:
        raise AssertionError("Bad depth must never silently rasterize")
    # Clipped offscreen, degenerate and fully out-of-frame triangles
    # must not cause invalid writes or fall outside image dimensions.
    no_hit=draw_mesh_with_depth(base,np.asarray([[-5,-5],[-4,-5],[-5,-4]],float),
                               np.asarray([1,1,1]),tris,colors)
    check(np.asarray(no_hit).shape==(8,8,3),"Unexpected framebuffer dimensions")


if __name__=="__main__":
    test_order_independent_overlap()
    test_per_pixel_intersection()
    test_reject_invalid_input()
    print("PASS genuine per-pixel z-test order independence and intersecting triangle visibility")
    print("PASS invalid depth rejection and offscreen geometry safety")
