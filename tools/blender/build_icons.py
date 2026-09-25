"""Draw the slot icons of the cards (docs/ASSETS.md section 3, ARB-86): clean single-line shapes, white on transparent.

Usage, without opening Blender's window (Blender is only used here for numpy and PNG writing, bundled with it):

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_icons.py -- \
        unity/Assets/_Vortex/Art/Icons

Writes Slot_ATK.png (an arrowhead), Slot_DEF.png (a shield), Slot_EVT.png (a four-point spark) and Slot_TECH.png (a
hexagon with a core), 256 x 256. The game shows them in the disc of each card, tinted with the interface text colour; a
PNG of the same name dropped in the icons folder replaces one, and without it the disc shows the short text again.

Each icon is a polyline drawn from its distance field, so its edges are smooth at any size the texture is sampled at.
"""

import math
import os
import sys

import bpy
import numpy as np

SIZE = 256
STROKE = 0.1  # line width, in units of the icon's half-size
PIXEL = 2.0 / SIZE


def grid():
    coords = (np.arange(SIZE) + 0.5) * PIXEL - 1.0
    x, y = np.meshgrid(coords, coords)  # row 0 is the bottom of the image, as Blender stores pixels
    return x, y


def distance_to_polyline(x, y, points, closed=True):
    """Distance from every pixel to the nearest segment of the polyline."""
    best = np.full(x.shape, np.inf)
    pairs = list(zip(points, points[1:] + (points[:1] if closed else [])))
    for (ax, ay), (bx, by) in pairs:
        dx, dy = bx - ax, by - ay
        t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0.0, 1.0)
        best = np.minimum(best, np.hypot(x - (ax + t * dx), y - (ay + t * dy)))
    return best


def coverage(distance):
    """Anti-aliased coverage of a shape whose edge is where distance crosses zero (negative inside)."""
    return np.clip(0.5 - distance / PIXEL, 0.0, 1.0)


def stroke(x, y, points, closed=True):
    return coverage(distance_to_polyline(x, y, points, closed) - STROKE / 2)


def bezier(p0, p1, p2, steps=12):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (i / steps for i in range(steps + 1))]


def attack(x, y):
    """An arrowhead pointing up."""
    return stroke(x, y, [(0.0, 0.8), (0.6, -0.62), (0.0, -0.3), (-0.6, -0.62)])


def defense(x, y):
    """A shield: a flat top, straight sides, and a curved point at the bottom."""
    right = bezier((0.6, 0.1), (0.58, -0.5), (0.0, -0.8))
    left = [(-px, py) for px, py in reversed(right)]
    return stroke(x, y, [(-0.6, 0.68), (0.6, 0.68)] + right + left[1:])


def event(x, y):
    """A four-point spark."""
    points = []
    for i in range(8):
        angle = math.pi / 2 + i * math.pi / 4
        radius = 0.84 if i % 2 == 0 else 0.26
        points.append((radius * math.cos(angle), radius * math.sin(angle)))
    return stroke(x, y, points)


def technology(x, y):
    """A hexagon with a core."""
    hexagon = [(0.8 * math.cos(math.pi / 2 + i * math.pi / 3), 0.8 * math.sin(math.pi / 2 + i * math.pi / 3)) for i in range(6)]
    core = coverage(np.hypot(x, y) - 0.17)
    return np.maximum(stroke(x, y, hexagon), core)


ICONS = {"Slot_ATK": attack, "Slot_DEF": defense, "Slot_EVT": event, "Slot_TECH": technology}


def output_folder():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1:
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_icons.py -- <icons folder>")
    return os.path.abspath(argv[0])


def main():
    folder = output_folder()
    os.makedirs(folder, exist_ok=True)
    x, y = grid()
    for name, draw in ICONS.items():
        alpha = draw(x, y)
        pixels = np.dstack([np.ones_like(alpha), np.ones_like(alpha), np.ones_like(alpha), alpha])
        image = bpy.data.images.new(name, SIZE, SIZE, alpha=True)
        image.alpha_mode = "STRAIGHT"
        image.pixels.foreach_set(pixels.astype(np.float32).ravel())
        image.filepath_raw = os.path.join(folder, name + ".png")
        image.file_format = "PNG"
        image.save()
        print("Wrote " + image.filepath_raw)


if __name__ == "__main__":
    main()
