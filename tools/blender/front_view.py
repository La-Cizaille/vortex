"""Helpers to build a model seen from the front and to paint its texture with numpy: the cockpit (build_cockpit.py) and
the market rack (build_market.py); the die and the sky use its noise and colour helpers.

A front model stands upright like a card: width along X, height along Z, depth along Y, its front facing +Y. Its parts
are flat outlines pushed in depth, cylinders, and tubes swept along a path, given in front-view terms (x to the right as
seen from the front, which is Blender's -X). Every part is unwrapped by one planar projection of the front, so a point
(x, z) of the front view has one pixel of the texture, whatever part shows there; the model remembers every part's
outline and depth to paint it.

Colours are handled linear and written in sRGB at the end (to_srgb). Nothing here draws at random: every function that
needs noise takes the caller's numpy generator, so a model is the same every time it is built.
"""

import math
import os

import bpy
import mathutils
import numpy as np


class Canvas:
    """The texture of a front view width x height metres, centred on the origin, tex_w x tex_h pixels (square texels
    expected), rows from the bottom (Blender's pixel order)."""

    def __init__(self, width, height, tex_w, tex_h):
        self.width, self.height = width, height
        self.shape = (tex_h, tex_w)
        self.pixel = width / tex_w
        self.X = (np.arange(tex_w) + 0.5) / tex_w * width - width / 2  # front-view x of each column
        self.Z = (np.arange(tex_h) + 0.5) / tex_h * height - height / 2  # z of each row

    def column(self, x):
        return int(round((x + self.width / 2) / self.pixel))

    def row(self, z):
        return int(round((z + self.height / 2) / self.pixel))

    def uv(self, x, z):
        return 0.5 + x / self.width, 0.5 + z / self.height

    def fill(self, outline):
        """The pixels inside a front-view outline (even-odd rule), computed over its bounding box only."""
        h, w = self.shape
        xs, zs = [p[0] for p in outline], [p[1] for p in outline]
        c0, c1 = max(0, self.column(min(xs)) - 1), min(w, self.column(max(xs)) + 2)
        r0, r1 = max(0, self.row(min(zs)) - 1), min(h, self.row(max(zs)) + 2)
        mask = np.zeros(self.shape, dtype=bool)
        if c0 >= c1 or r0 >= r1:
            return mask
        px, pz = self.X[None, c0:c1], self.Z[r0:r1, None]
        inside = np.zeros((r1 - r0, c1 - c0), dtype=bool)
        with np.errstate(divide="ignore", invalid="ignore"):
            for (xa, za), (xb, zb) in zip(outline, outline[1:] + outline[:1]):
                if za == zb:
                    continue
                crosses = (za > pz) != (zb > pz)
                inside ^= crosses & (px < (xb - xa) * (pz - za) / (zb - za) + xa)
        mask[r0:r1, c0:c1] = inside
        return mask

    def stamp(self, cover, x0, x1, z0, z1):
        """A coverage array (rows from the bottom) fitted in a front-view rectangle, centred, keeping its proportions."""
        mh, mw = cover.shape
        scale = min((x1 - x0) / mw, (z1 - z0) / mh)
        left, bottom = (x0 + x1 - mw * scale) / 2, (z0 + z1 - mh * scale) / 2
        u = (self.X[None, :] - left) / (mw * scale)
        v = (self.Z[:, None] - bottom) / (mh * scale)
        inside = (u >= 0) & (u < 1) & (v >= 0) & (v < 1)
        cols = np.clip((u * mw).astype(int), 0, mw - 1)
        rows = np.clip((v * mh).astype(int), 0, mh - 1)
        return np.where(inside, cover[rows, cols], 0.0)

    def scratches(self, rng, count, longest):
        """Short straight lines, mostly along the width: a mask."""
        h, w = self.shape
        mask = np.zeros(self.shape, dtype=bool)
        for _ in range(count):
            x, z = rng.uniform(-self.width / 2, self.width / 2), rng.uniform(-self.height / 2, self.height / 2)
            angle = rng.normal(0, 0.35) + (math.pi / 2 if rng.random() < 0.15 else 0)
            length = rng.uniform(longest / 9, longest)
            t = np.linspace(0, length, max(4, int(length / self.pixel * 1.5)))
            cols = np.clip(((x + t * math.cos(angle)) + self.width / 2) / self.pixel, 0, w - 1).astype(int)
            rows = np.clip(((z + t * math.sin(angle)) + self.height / 2) / self.pixel, 0, h - 1).astype(int)
            mask[rows, cols] = True
        return mask

    def rivets(self, points, radius, colour, height):
        """Domed rivets at front-view points: raised in the height map, a dark ring of grime around each; a mask."""
        h, w = self.shape
        mask = np.zeros(self.shape, dtype=bool)
        reach = int(radius * 1.8 / self.pixel) + 1
        for x, z in points:
            c, r = self.column(x), self.row(z)
            c0, c1, r0, r1 = max(0, c - reach), min(w, c + reach + 1), max(0, r - reach), min(h, r + reach + 1)
            d = np.sqrt((self.X[None, c0:c1] - x) ** 2 + (self.Z[r0:r1, None] - z) ** 2) / radius
            dome = np.sqrt(np.clip(1 - d ** 2, 0, 1))
            height[r0:r1, c0:c1] = np.maximum(height[r0:r1, c0:c1], 1.6 * dome)
            mask[r0:r1, c0:c1] |= d < 1
            colour[r0:r1, c0:c1] *= np.where((d > 1) & (d < 1.6), 0.6, 1.0)[..., None]
            colour[r0:r1, c0:c1] += np.where(d < 1, 0.012 * dome, 0.0)[..., None]
        return mask


class Model:
    """The parts of a front model, as they are built: objects in the scene, and for the texture each part's name,
    material, front-view outline and front depth."""

    def __init__(self, canvas, material):
        self.canvas = canvas
        self.material = material  # a material's name -> the Blender material
        self.parts = []

    def _finish(self, obj, material_name, parent):
        obj.data.materials.append(self.material(material_name))
        bpy.context.scene.collection.objects.link(obj)
        if parent is not None:
            obj.parent = parent
        _fix_normals(obj)
        bpy.context.view_layer.update()
        layer = obj.data.uv_layers.new(name="UVMap")
        world = obj.matrix_world
        for loop in obj.data.loops:
            co = world @ obj.data.vertices[loop.vertex_index].co
            layer.data[loop.index].uv = self.canvas.uv(-co.x, co.z)
        return obj

    def prism(self, name, outline, y0, y1, material_name, origin=(0.0, 0.0, 0.0), parent=None):
        """A flat outline (counter-clockwise from the front) pushed from depth y0 to y1. Seen from the front, Blender's
        +X points to the left: x is mirrored. The vertices are relative to origin, in the same terms, so that a moving
        part turns around it."""
        ox, oy, oz = origin
        count = len(outline)
        vertices = [(-(x - ox), y0 - oy, z - oz) for x, z in outline] + [(-(x - ox), y1 - oy, z - oz) for x, z in outline]
        faces = [tuple(range(count)), tuple(reversed(range(count, 2 * count)))]
        for i in range(count):
            j = (i + 1) % count
            faces.append((i, count + i, count + j, j))
        obj = _object(name, vertices, faces)
        obj.location = (-ox, oy, oz)
        self.parts.append((name, material_name, outline, max(y0, y1)))
        return self._finish(obj, material_name, parent)

    def cylinder_x(self, name, x0, x1, y, z, radius, material_name, origin=None, sides=16):
        """A cylinder along X from x0 to x1, its axis at depth y and height z; origin defaults to its middle."""
        ox, oy, oz = origin if origin is not None else ((x0 + x1) / 2, y, z)
        ring = [(y + radius * math.cos(2 * math.pi * i / sides), z + radius * math.sin(2 * math.pi * i / sides)) for i in range(sides)]
        vertices = [(-(x0 - ox), cy - oy, cz - oz) for cy, cz in ring] + [(-(x1 - ox), cy - oy, cz - oz) for cy, cz in ring]
        faces = [tuple(range(sides)), tuple(reversed(range(sides, 2 * sides)))]
        for i in range(sides):
            j = (i + 1) % sides
            faces.append((i, j, sides + j, sides + i))
        obj = _object(name, vertices, faces)
        obj.location = (-ox, oy, oz)
        self.parts.append((name, material_name, rectangle(x0, x1, z - radius, z + radius), y + radius))
        return self._finish(obj, material_name, None)

    def tube(self, name, points, radius, material_name, sides=6):
        """A cable, a pipe or a handle: a polygon swept along a polyline of points (x, depth y, z), closed."""
        path = [mathutils.Vector((-x, y, z)) for x, y, z in points]
        rings = []
        for i, point in enumerate(path):
            ahead = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            towards = mathutils.Vector((0, 1, 0)) if abs(ahead.y) < 0.9 else mathutils.Vector((0, 0, 1))
            side = ahead.cross(towards).normalized()
            up = side.cross(ahead).normalized()
            rings.append([point + radius * (math.cos(a) * side + math.sin(a) * up) for a in (2 * math.pi * k / sides for k in range(sides))])
        vertices = [tuple(v) for ring in rings for v in ring]
        faces = []
        for r in range(len(rings) - 1):
            for k in range(sides):
                a, b = r * sides, (r + 1) * sides
                faces.append((a + k, a + (k + 1) % sides, b + (k + 1) % sides, b + k))
        faces.append(tuple(reversed(range(sides))))
        faces.append(tuple(range((len(rings) - 1) * sides, len(rings) * sides)))
        obj = _object(name, vertices, faces)
        for (x0, y0, z0), (x1, y1, z1) in zip(points, points[1:]):
            # The swept outline of each segment, for the shadows it casts in the texture.
            length = math.hypot(x1 - x0, z1 - z0) or 1.0
            nx, nz = -(z1 - z0) / length * radius, (x1 - x0) / length * radius
            self.parts.append((name, material_name, [(x0 + nx, z0 + nz), (x0 - nx, z0 - nz), (x1 - nx, z1 - nz), (x1 + nx, z1 + nz)], max(y0, y1) + radius))
        return self._finish(obj, material_name, None)

    @staticmethod
    def empty(name, x, y, z, width, height):
        """A plain-axes empty at (x, y, z), whose X and Z scales give a zone's size."""
        obj = bpy.data.objects.new(name, None)
        obj.empty_display_type = "PLAIN_AXES"
        obj.location = (-x, y, z)
        obj.scale = (width, 1.0, height)
        bpy.context.scene.collection.objects.link(obj)
        return obj

    @staticmethod
    def join(name, kept):
        """Joins every root mesh whose name is not in kept into one object: few objects, few draw calls."""
        trim = [o for o in bpy.context.scene.objects if o.type == "MESH" and o.name not in kept and o.parent is None]
        target = trim[0]
        with bpy.context.temp_override(active_object=target, object=target, selected_objects=trim, selected_editable_objects=trim):
            bpy.ops.object.join()
        target.name = name
        target.data.name = name
        return target

    def surfaces(self, painted_materials, kind_of, base_depth, skip=(), casts_no_shadow=lambda name, material: False):
        """What the texture shows at each pixel. Returns: the mask of each kind of surface (kind_of(name, material) of
        the frontmost part whose material is in painted_materials; "base" elsewhere), the depth of that surface, the
        front depth of anything at all (for the shadows), and the outlines where the surface changes."""
        shape = self.canvas.shape
        surface = np.full(shape, "base", dtype=object)
        front = np.full(shape, base_depth)
        nearest = np.full(shape, -np.inf)
        for name, material_name, outline, part_front in self.parts:
            if name in skip:
                continue
            mask = self.canvas.fill(outline)
            if not casts_no_shadow(name, material_name):
                front = np.where(mask, np.maximum(front, part_front), front)
            if material_name in painted_materials:
                nearer = mask & (part_front > nearest)
                nearest[nearer] = part_front
                surface[nearer] = kind_of(name, material_name)
        depth = np.where(np.isfinite(nearest), nearest, base_depth)
        kinds = {kind: surface == kind for kind in set(surface.ravel())}
        code = np.zeros(shape, dtype=np.int64)
        for index, kind in enumerate(sorted(kinds)):
            code[kinds[kind]] = index + 1
        code = code * 100000 + np.round(depth * 10000).astype(np.int64)
        return kinds, depth, front, outlines(code)


def _object(name, vertices, faces):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.validate()
    return bpy.data.objects.new(name, mesh)


def _fix_normals(obj):
    bpy.context.view_layer.objects.active = obj
    for other in bpy.context.selected_objects:
        other.select_set(False)
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


# ---------------------------------------------------------------------------------- outlines, counter-clockwise

def rectangle(x0, x1, z0, z1):
    return [(x0, z0), (x1, z0), (x1, z1), (x0, z1)]


def chamfered(x0, x1, z0, z1, cut):
    return [(x0 + cut, z0), (x1 - cut, z0), (x1, z0 + cut), (x1, z1 - cut), (x1 - cut, z1), (x0 + cut, z1), (x0, z1 - cut), (x0, z0 + cut)]


def rounded(x0, x1, z0, z1, radius, segments=4):
    centres = [(x1 - radius, z0 + radius, -90), (x1 - radius, z1 - radius, 0), (x0 + radius, z1 - radius, 90), (x0 + radius, z0 + radius, 180)]
    points = []
    for cx, cz, start in centres:
        for step in range(segments + 1):
            angle = math.radians(start + 90 * step / segments)
            points.append((cx + radius * math.cos(angle), cz + radius * math.sin(angle)))
    return points


def circle(cx, cz, radius, sides=24):
    return [(cx + radius * math.cos(2 * math.pi * i / sides), cz + radius * math.sin(2 * math.pi * i / sides)) for i in range(sides)]


def arc_band(cx, cz, inner, outer, start, end, steps=6):
    """A band between two radii, from angle start to end (degrees, counter-clockwise from the right)."""
    angles = [math.radians(start + (end - start) * i / steps) for i in range(steps + 1)]
    outside = [(cx + outer * math.cos(a), cz + outer * math.sin(a)) for a in angles]
    inside = [(cx + inner * math.cos(a), cz + inner * math.sin(a)) for a in reversed(angles)]
    return outside + inside


def ell(corner_x, corner_z, leg, width, towards_right):
    """An L, its outer corner at the bottom, one leg along X (right or left), the other up."""
    points = [(0.0, 0.0), (leg, 0.0), (leg, width), (width, width), (width, leg), (0.0, leg)]
    if towards_right:
        return [(corner_x + x, corner_z + z) for x, z in points]
    return [(corner_x - x, corner_z + z) for x, z in reversed(points)]  # mirrored: reversed to stay counter-clockwise


# ------------------------------------------------------------------------------------------------------- painting

def blur(field, radius):
    """Three box blurs of the given radius in pixels: close to a Gaussian, edges clamped."""
    out = field.astype(np.float32)
    for axis in (0, 1):
        for _ in range(3):
            padded = np.concatenate([np.repeat(out.take([0], axis), radius + 1, axis), out, np.repeat(out.take([-1], axis), radius, axis)], axis)
            summed = np.cumsum(padded, axis=axis)
            size = out.shape[axis]
            upper = summed.take(np.arange(2 * radius + 1, 2 * radius + 1 + size), axis)
            lower = summed.take(np.arange(0, size), axis)
            out = (upper - lower) / (2 * radius + 1)
    return out


def value_noise(rng, cell, shape):
    """Smooth noise from 0 to 1: a coarse random grid, one value every `cell` pixels, interpolated smoothly."""
    h, w = shape
    grid = rng.random((int(h / cell) + 2, int(w / cell) + 2))
    ys, xs = np.arange(h) / cell, np.arange(w) / cell
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = (ys - y0)[:, None], (xs - x0)[None, :]
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    a, b = grid[y0][:, x0], grid[y0][:, x0 + 1]
    c, d = grid[y0 + 1][:, x0], grid[y0 + 1][:, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fractal(rng, cells, shape, falloff=None):
    """Octaves of value noise, one per cell size; octave i weighs falloff ** i, or 1 / (i + 1) by default."""
    total, weight = np.zeros(shape), 0.0
    for i, cell in enumerate(cells):
        share = falloff ** i if falloff is not None else 1 / (i + 1)
        total += value_noise(rng, cell, shape) * share
        weight += share
    return total / weight


def smoothstep(value, low, high):
    t = np.clip((value - low) / (high - low), 0, 1)
    return t * t * (3 - 2 * t)


def dilate(mask, radius):
    out = mask.copy()
    for _ in range(radius):
        grown = out.copy()
        grown[1:] |= out[:-1]
        grown[:-1] |= out[1:]
        grown[:, 1:] |= out[:, :-1]
        grown[:, :-1] |= out[:, 1:]
        out = grown
    return out


def outlines(code):
    """Where a map of surface codes changes from one pixel to the next."""
    edge = np.zeros(code.shape, dtype=bool)
    for axis in (0, 1):
        for shift in (1, -1):
            edge |= code != np.roll(code, shift, axis)
    return edge


def rust_streaks(rng, seeds, shape):
    """Rust running down from the seed pixels, fading, strong in some columns only."""
    streak = np.zeros(shape, dtype=np.float32)
    decay = 0.97 + 0.025 * value_noise(rng, 16, shape)[0]
    streak[-1] = seeds[-1]
    for r in range(shape[0] - 2, -1, -1):
        streak[r] = np.maximum(seeds[r], streak[r + 1] * decay)
    return blur(streak * smoothstep(value_noise(rng, 5, (1, shape[1]))[0], 0.45, 0.85)[None, :], 1)


def weather(canvas, rng, colour, height, surfaces, look):
    """Years of use on a painted front: scratches, chipped edges (bare steel on the metal, bare metal under the paint,
    rusted in places), rust running down from the chips and the rivets, grime next to taller parts, low down and in
    blotches. surfaces: masks metal, painted, clean (0..1, kept clean: where the game writes), rivets, body (where
    the model is), the outlines, the surface depth and the front depth of anything. look: bare_steel, blackened, rust
    (linear colours), scratches (count, longest in metres), shadow (blur in pixels, depth in metres), low (z from,
    z to), and optionally chips (reach in pixels and noise threshold of the narrow chips, then of the wide chips on the
    paint; (3, 0.5, 7, 0.66) by default). Changes colour and height in place; returns the chipped mask."""
    shape = canvas.shape
    metal, painted, clean, body = surfaces["metal"], surfaces["painted"], surfaces["clean"], surfaces["body"]
    bare, rust = np.asarray(look["bare_steel"]), np.asarray(look["rust"])

    scratches = canvas.scratches(rng, *look["scratches"]) & (clean < 0.5)
    height -= np.where(scratches, 0.25, 0.0)
    colour[scratches & metal] = colour[scratches & metal] * 0.6 + bare * 0.25
    colour[scratches & painted] *= 0.75

    reach, threshold, wide_reach, wide_threshold = look.get("chips", (3, 0.5, 7, 0.66))
    chips_noise = fractal(rng, (24, 6, 3), shape)
    chips = dilate(surfaces["outline"], reach) & body & (chips_noise > threshold)
    chips |= dilate(surfaces["outline"], wide_reach) & body & (chips_noise > wide_threshold) & painted
    blotch = fractal(rng, (40, 12), shape)
    rusty = chips & (blotch > 0.55)
    colour[chips & metal] = bare * (0.8 + 0.4 * chips_noise[chips & metal])[..., None]
    colour[chips & ~metal] = np.array([look["blackened"] * 1.3] * 3)
    colour[rusty] = rust * (0.8 + 0.5 * blotch[rusty])[..., None]
    height -= np.where(chips & painted, 0.5, 0.0)

    seeds = (rusty | (surfaces["rivets"] & (blotch > 0.45))).astype(np.float32)
    streak = rust_streaks(rng, seeds, shape) * body * 0.5 * (1 - clean)
    colour[:] = colour * (1 - streak[..., None]) + rust * streak[..., None]

    reach, deep = look["shadow"]
    shadow = np.clip((blur(surfaces["front"], reach) - surfaces["depth"]) / deep, 0, 1)
    low = smoothstep(-canvas.Z[:, None], *look["low"]) * np.ones(shape) * (1 - 0.6 * clean)
    blotches = smoothstep(fractal(rng, (64, 20), shape), 0.55, 0.8) * (1 - 0.7 * clean)
    grime = np.clip(0.55 * shadow + 0.25 * low + 0.35 * blotches, 0, 0.85)
    colour *= (1 - grime)[..., None]
    return chips


def brushed_steel(rng, shape, shade, rust_colour):
    """Worn bare steel brushed along the width, for small steel parts: its linear colour and its height."""
    brush = blur(value_noise(rng, 1, shape), 1)
    along = np.cumsum(rng.standard_normal(shape), axis=1)
    along = along - blur(along, 24)
    along = along / (np.abs(along).max() + 1e-6)
    grain = 0.6 * along + 0.4 * (brush - 0.5)
    dirt = smoothstep(fractal(rng, (32, 10), shape), 0.5, 0.85)
    value = shade * (1 + 0.2 * grain) * (1 - 0.7 * dirt)
    colour = np.dstack([value * 1.0, value * 1.02, value * 1.06])
    rust = smoothstep(fractal(rng, (16, 5), shape), 0.68, 0.8)
    colour = colour * (1 - rust[..., None]) + np.asarray(rust_colour) * rust[..., None]
    return colour, grain - rust


def to_srgb(linear):
    linear = np.clip(linear, 0, 1)
    return np.where(linear <= 0.0031308, linear * 12.92, 1.055 * np.power(linear, 1 / 2.4) - 0.055)


def normal_from(height, strength):
    """A tangent-space normal map (OpenGL: green up) from a height map, for a planar front projection."""
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / 2
    dz = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / 2
    nx, ny, nz = -dx * strength, -dz * strength, np.ones_like(height)
    length = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    return np.dstack([nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5, np.ones_like(height)])


def textured_images(name, width, height):
    """A colour image and a normal image, empty, for a material to link before they are painted."""
    colour = bpy.data.images.new(name + "_BaseColor", width, height, alpha=False)
    normal = bpy.data.images.new(name + "_Normal", width, height, alpha=False)
    normal.colorspace_settings.name = "Non-Color"
    return colour, normal


def save_images(images, directory):
    for image in images:
        path = os.path.join(directory, image.name + ".png")
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        image.filepath = path


def render_text(text, font_path, folder):
    """The text in a font, white on black, as a coverage array (rows from the bottom), rendered by Cycles in a scene of
    its own that is removed afterwards."""
    scene = bpy.data.scenes.new("Marking")
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 8
    scene.world = bpy.data.worlds.new("Marking")
    scene.world.color = (0.0, 0.0, 0.0)
    curve = bpy.data.curves.new("Marking", "FONT")
    curve.body = text
    curve.font = bpy.data.fonts.load(font_path, check_existing=True)
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    words = bpy.data.objects.new("Marking", curve)
    white = bpy.data.materials.new("Marking")
    emit = white.node_tree.nodes.new("ShaderNodeEmission")
    white.node_tree.links.new(emit.outputs["Emission"], next(n for n in white.node_tree.nodes if n.type == "OUTPUT_MATERIAL").inputs["Surface"])
    curve.materials.append(white)
    scene.collection.objects.link(words)
    try:
        scene.view_settings.view_transform = "Standard"  # white text renders as 1, not as a tone-mapped grey
    except TypeError:
        pass
    layer = scene.view_layers[0]
    layer.update()
    evaluated = words.evaluated_get(layer.depsgraph)
    width, height = max(evaluated.dimensions.x, 0.1), max(evaluated.dimensions.y, 0.1)

    camera_data = bpy.data.cameras.new("Marking")
    camera_data.type = "ORTHO"
    camera_data.sensor_fit = "HORIZONTAL"  # the scale spans the width, even for a text taller than wide
    camera_data.ortho_scale = width * 1.08
    camera = bpy.data.objects.new("Marking", camera_data)
    camera.location = (0.0, 0.0, 5.0)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.resolution_x = 1024
    scene.render.resolution_y = max(16, int(1024 * height * 1.2 / (width * 1.08)))
    scene.render.resolution_percentage = 100
    path = os.path.join(folder, "marking.png")
    scene.render.filepath = path
    scene.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True, scene=scene.name)

    image = bpy.data.images.load(path)
    w, h = image.size
    pixels = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)[..., 0]
    bpy.data.images.remove(image)
    for block in (words, camera):
        bpy.data.objects.remove(block, do_unlink=True)
    bpy.data.scenes.remove(scene)
    return pixels
