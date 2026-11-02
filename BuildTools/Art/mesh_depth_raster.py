"""Deterministic orthographic static-mesh reviewer with per-pixel depth.

This is a software inspection tool, not Unity, skinning or final PBR.
Meshes, positions, camera depth and source swatch colors are supplied by
the actual OBJ comparison pipeline. Larger camera_depth is nearer.
"""
import math

import numpy as np
from PIL import Image


def draw_mesh_with_depth(base_image, screen_xy, camera_depth, triangles, shades):
    """Rasterize actual indexed faces, interpolate their depth per pixel.

    A centroid-sorted painter cannot depict crossing or overlapping
    anatomical surfaces faithfully. This pixel-center z-buffer makes the
    visibility result independent of triangle submission order.
    """
    if base_image.mode != "RGB":
        raise ValueError("Expected an RGB base image")
    width, height = base_image.size
    screen_xy = np.asarray(screen_xy, dtype=np.float64)
    camera_depth = np.asarray(camera_depth, dtype=np.float64)
    triangles = np.asarray(triangles, dtype=np.int32)
    shades = np.asarray(shades, dtype=np.uint8)
    if screen_xy.ndim != 2 or screen_xy.shape[1] != 2:
        raise ValueError("Expected projected (x,y) coordinates")
    if camera_depth.shape != (len(screen_xy),):
        raise ValueError("Depth and vertex lengths disagree")
    if (triangles.ndim != 2 or triangles.shape[1] != 3 or
            shades.shape != (len(triangles), 3)):
        raise ValueError("Triangles and material shades disagree")
    if not np.isfinite(screen_xy).all() or not np.isfinite(camera_depth).all():
        raise ValueError("Nonfinite input geometry")
    if triangles.size and (triangles.min() < 0 or triangles.max() >= len(screen_xy)):
        raise ValueError("Triangle vertex out of bounds")

    rgb = np.asarray(base_image, dtype=np.uint8).copy()
    zbuffer = np.full((height, width), -np.inf, dtype=np.float64)
    epsilon = 1e-10
    for tri, color in zip(triangles, shades):
        p = screen_xy[tri]
        x0, y0 = p[0]
        x1, y1 = p[1]
        x2, y2 = p[2]
        minx = max(0, int(math.floor(min(x0, x1, x2))))
        maxx = min(width-1, int(math.ceil(max(x0, x1, x2))))
        miny = max(0, int(math.floor(min(y0, y1, y2))))
        maxy = min(height-1, int(math.ceil(max(y0, y1, y2))))
        if minx > maxx or miny > maxy:
            continue
        denom = (y1-y2)*(x0-x2)+(x2-x1)*(y0-y2)
        if abs(denom) < 1e-12:
            continue
        yy, xx = np.ogrid[miny:maxy+1, minx:maxx+1]
        xx = xx+.5
        yy = yy+.5
        b0 = ((y1-y2)*(xx-x2)+(x2-x1)*(yy-y2))/denom
        b1 = ((y2-y0)*(xx-x2)+(x0-x2)*(yy-y2))/denom
        b2 = 1.0-b0-b1
        inside = (b0 >= -epsilon) & (b1 >= -epsilon) & (b2 >= -epsilon)
        if not inside.any():
            continue
        candidate = (b0*camera_depth[tri[0]] +
                     b1*camera_depth[tri[1]] +
                     b2*camera_depth[tri[2]])
        region = zbuffer[miny:maxy+1, minx:maxx+1]
        closer = inside & (candidate > region)
        if closer.any():
            region[closer] = candidate[closer]
            rgb[miny:maxy+1, minx:maxx+1][closer] = color
    return Image.fromarray(rgb, "RGB")
