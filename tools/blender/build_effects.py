"""Build the material of the table's visual effects (docs/ANIMATIONS.md section 6, docs/DIRECTION_ARTISTIQUE.md §6.7:
weight and gravity, no gags, the dark palette), and save it as a .blend source with its images.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_effects.py -- art-src/effects/Effets.blend

Then export the meshes for Unity (the images are copied next to them):

    blender --background --disable-autoexec art-src/effects/Effets.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Effects/Effets.fbx --budget 1000

The game plays its effects with Unity's particle systems, which the game's import builds from what is made here
(Editor/EffectSync.cs); nothing here moves. Made here:
- meshes: four shards of torn plating (Eclat_1 to Eclat_4, about one unit), the shockwave ring (Onde, flat, radius 1),
  the bolt of a shot (Trait, its hot core, and Gaine, its sheath; they point along Unity's +Z, the head at the origin);
- flipbooks of 4 x 4 frames, 256 pixels each, read left to right from the top: Feu (a fireball that burns out into
  soot), Fumee (a puff of smoke that swells and thins), Arc (bolts of electricity, one per frame); Spore, 2 x 2 spores;
- single images: Etincelle (a hot point, stretched along its flight by the game), Onde (the ring's glow, bright on its
  inner edge), Plasma (a tileable crackle for the shield's shader).
The colour images are grey or white: the game tints them. Nothing is drawn at random: every image comes from a fixed
seed, so a build gives the same files.
"""

import math
import os
import sys

import bmesh
import bpy
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import front_view as fv  # noqa: E402  (next to this script)

FRAME = 256
SHEET = 4


# ---------------------------------------------------------------------------------------------------------- noise

def lattice(rng, size=64):
    return rng.random((size, size))


def sample(grid, x, y):
    """Smooth noise from a lattice at any coordinates, in lattice cells; it wraps, so it tiles."""
    n = grid.shape[0]
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = x - x0, y - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    x0, y0 = x0 % n, y0 % n
    x1, y1 = (x0 + 1) % n, (y0 + 1) % n
    return (grid[y0, x0] * (1 - fx) + grid[y0, x1] * fx) * (1 - fy) + (grid[y1, x0] * (1 - fx) + grid[y1, x1] * fx) * fy


def fbm(grid, x, y, octaves=4):
    total, weight = 0.0, 0.0
    for i in range(octaves):
        total = total + sample(grid, x * 2 ** i + 17.3 * i, y * 2 ** i + 5.1 * i) * 0.5 ** i
        weight += 0.5 ** i
    return total / weight


def frame_coordinates(size=FRAME):
    c = (np.arange(size) + 0.5) / size * 2 - 1
    return np.meshgrid(c, -c)  # u to the right, v up (rows from the top here)


def sheet(frames, size=FRAME, columns=SHEET):
    """Lays frames (rows from the top) in a grid read left to right from the top; returns rows from the bottom, as
    Blender stores pixels."""
    rows = math.ceil(len(frames) / columns)
    out = np.zeros((rows * size, columns * size, frames[0].shape[2]), dtype=np.float32)
    for index, frame in enumerate(frames):
        r, c = divmod(index, columns)
        out[r * size:(r + 1) * size, c * size:(c + 1) * size] = frame
    return out[::-1]


def ramp(value, stops):
    """A colour ramp: stops are (position, (r, g, b)) in increasing position."""
    positions = np.array([p for p, _ in stops])
    colours = np.array([c for _, c in stops])
    out = np.zeros(value.shape + (3,))
    for channel in range(3):
        out[..., channel] = np.interp(value, positions, colours[:, channel])
    return out


# --------------------------------------------------------------------------------------------------------- images

FIRE = [(0.0, (0.02, 0.018, 0.016)), (0.25, (0.28, 0.04, 0.01)), (0.5, (0.85, 0.33, 0.04)), (0.75, (1.0, 0.62, 0.18)), (1.0, (1.0, 0.93, 0.72))]


def fire_frames():
    """A fireball that swells, rises a little, and burns out into soot."""
    rng = np.random.default_rng(31)
    warp_grid, body_grid = lattice(rng), lattice(rng)
    u, v = frame_coordinates()
    radius = np.sqrt(u * u + v * v)
    frames = []
    for index in range(SHEET * SHEET):
        t = index / (SHEET * SHEET - 1)
        wx = fbm(warp_grid, u * 2.5 + t * 1.4, v * 2.5, 3)
        wy = fbm(warp_grid, u * 2.5 + 11.0, v * 2.5 - t * 1.4, 3)
        n = fbm(body_grid, (u + 0.35 * wx) * 3.0, (v + 0.35 * wy) * 3.0 - t * 2.2, 5)
        size = 0.55 + 0.33 * t
        body = fv.smoothstep(size + 0.45 * (n - 0.5) - radius, 0.0, 0.14)
        heat = np.clip((1.05 - 1.25 * t) * (1 - radius / max(size, 1e-3)) * 1.6 + (n - 0.5) * 0.7, 0, 1)
        colour = ramp(heat, FIRE)
        alpha = body * (1 - 0.75 * fv.smoothstep(t, 0.55, 1.0))
        frames.append(np.dstack([fv.to_srgb(colour), alpha]))
    return frames


def smoke_frames():
    """A puff of smoke that swells and thins: grey, lit a little from above; the game tints it."""
    rng = np.random.default_rng(32)
    warp_grid, body_grid = lattice(rng), lattice(rng)
    u, v = frame_coordinates()
    radius = np.sqrt(u * u + v * v)
    frames = []
    for index in range(SHEET * SHEET):
        t = index / (SHEET * SHEET - 1)
        wx = fbm(warp_grid, u * 2 + t, v * 2, 3)
        n = fbm(body_grid, (u + 0.4 * wx) * 2.6, v * 2.6 - t * 1.2, 5)
        size = 0.4 + 0.45 * t
        body = fv.smoothstep(size + 0.5 * (n - 0.5) - radius, 0.0, 0.3)
        shade = np.clip(0.45 + 0.35 * v + 0.4 * (n - 0.5), 0.1, 1.0)
        alpha = body * 0.9 * (1 - t * t)
        frames.append(np.dstack([fv.to_srgb(np.dstack([shade] * 3)), alpha]))
    return frames


def bolt_path(rng, start, end, depth, spread):
    """A jagged path by midpoint displacement: a list of points."""
    points = [np.array(start), np.array(end)]
    for level in range(depth):
        refined = [points[0]]
        for a, b in zip(points, points[1:]):
            middle = (a + b) / 2
            along = b - a
            normal = np.array([-along[1], along[0]])
            middle = middle + normal * rng.normal(0, spread / (1.6 ** level))
            refined += [middle, b]
        points = refined
    return points


def distance_to_path(u, v, points):
    best = np.full(u.shape, np.inf)
    for a, b in zip(points, points[1:]):
        ab = b - a
        length = max(float(ab @ ab), 1e-9)
        t = np.clip(((u - a[0]) * ab[0] + (v - a[1]) * ab[1]) / length, 0, 1)
        dx, dy = u - (a[0] + t * ab[0]), v - (a[1] + t * ab[1])
        best = np.minimum(best, np.sqrt(dx * dx + dy * dy))
    return best


def arc_frames():
    """An electric arc across each frame, with a branch or two: a thin white core in a soft glow."""
    rng = np.random.default_rng(33)
    u, v = frame_coordinates()
    frames = []
    for _ in range(SHEET * SHEET):
        main = bolt_path(rng, (-0.85, rng.uniform(-0.2, 0.2)), (0.85, rng.uniform(-0.2, 0.2)), 6, 0.12)
        d = distance_to_path(u, v, main)
        for _ in range(rng.integers(1, 3)):
            root = main[rng.integers(8, len(main) - 8)]
            tip = root + np.array([rng.uniform(0.2, 0.5), rng.uniform(-0.5, 0.5)])
            d = np.minimum(d, distance_to_path(u, v, bolt_path(rng, tuple(root), tuple(tip), 4, 0.12)) * 1.6)
        # Faded towards the frame's edges, so that no arc ends on a straight cut.
        light = np.clip(np.exp(-(d / 0.012) ** 2) + 0.35 * np.exp(-d / 0.06), 0, 1) * fv.smoothstep(1 - np.maximum(np.abs(u), np.abs(v)), 0.0, 0.18)
        frames.append(np.dstack([np.ones_like(u), np.ones_like(u), np.ones_like(u), light]))
    return frames


def spore_frames():
    """Four spores: a lumpy, pitted cell, dark inside, a dull rim, a few short threads; grey, tinted by the game. Horror
    suggested, not drawn: no clean shapes."""
    rng = np.random.default_rng(34)
    grid = lattice(rng)
    size = FRAME
    u, v = frame_coordinates(size)
    radius = np.sqrt(u * u + v * v)
    angle = np.arctan2(v, u)
    frames = []
    for index in range(4):
        edge = 0.34 + 0.2 * (fbm(grid, np.cos(angle) * 3 + index * 3, np.sin(angle) * 3, 4) - 0.5) * 2
        inside = fv.smoothstep(edge - radius, 0.0, 0.03)
        rim = np.exp(-((radius - edge + 0.05) / 0.06) ** 2)
        pits = fv.smoothstep(fbm(grid, u * 9 + index * 5, v * 9, 3), 0.55, 0.7)
        shade = np.clip(0.2 + 0.45 * rim + 0.25 * fbm(grid, u * 6 + index, v * 6, 3) - 0.25 * pits, 0, 1)
        hairs = np.zeros_like(u)
        for _ in range(4):
            a = rng.uniform(0, 2 * math.pi)
            path = [np.array([math.cos(a), math.sin(a)]) * 0.3]
            for _ in range(3):
                a += rng.normal(0, 0.5)
                path.append(path[-1] + np.array([math.cos(a), math.sin(a)]) * 0.07)
            hairs = np.maximum(hairs, np.exp(-(distance_to_path(u, v, path) / 0.006) ** 2) * 0.5)
        alpha = np.clip(np.maximum(inside * 0.9 * (1 - 0.35 * pits), hairs), 0, 1)
        shade = np.where(inside > 0.5, shade, 0.5)
        frames.append(np.dstack([fv.to_srgb(np.dstack([shade] * 3)), alpha]))
    return frames


def spark_image(size=64):
    u, v = frame_coordinates(size)
    radius = np.sqrt(u * u + v * v)
    light = np.clip(np.exp(-(radius / 0.22) ** 2) + 0.35 * np.exp(-radius / 0.3), 0, 1) * fv.smoothstep(1 - radius, 0, 0.2)
    return np.dstack([np.ones_like(u)] * 3 + [light])[::-1]


def ring_image(width=256, height=64):
    """The shockwave's glow across the ring (v from the inner edge to the outer one), broken along it (u)."""
    rng = np.random.default_rng(35)
    grid = lattice(rng, 32)
    u = (np.arange(width) + 0.5) / width
    v = (np.arange(height) + 0.5) / height
    uu, vv = np.meshgrid(u, v)
    profile = np.where(vv < 0.3, fv.smoothstep(vv, 0.0, 0.3), np.exp(-(vv - 0.3) / 0.25))
    broken = 0.55 + 0.45 * fbm(grid, uu * 32, vv * 2, 3)
    light = np.clip(profile * broken * fv.smoothstep(1 - vv, 0, 0.15), 0, 1)
    return np.dstack([np.ones_like(uu)] * 3 + [light])


def plasma_image(size=512):
    """A tileable crackle: the edges of wrapped Voronoi cells and a noise, grey."""
    rng = np.random.default_rng(36)
    grid = lattice(rng, 16)
    points = rng.random((48, 2))
    c = (np.arange(size) + 0.5) / size
    x, y = np.meshgrid(c, c)
    first = np.full(x.shape, np.inf)
    second = np.full(x.shape, np.inf)
    for px, py in points:
        dx = np.abs(x - px)
        dy = np.abs(y - py)
        d = np.sqrt(np.minimum(dx, 1 - dx) ** 2 + np.minimum(dy, 1 - dy) ** 2)
        second = np.where(d < first, first, np.minimum(second, d))
        first = np.minimum(first, d)
    cracks = np.exp(-((second - first) / 0.012) ** 2)
    noise = fbm(grid, x * 16, y * 16, 4)
    value = np.clip(0.65 * cracks + 0.5 * fv.smoothstep(noise, 0.45, 0.8), 0, 1)
    return np.dstack([value, value, value, np.ones_like(value)])


def save(name, pixels, directory):
    h, w = pixels.shape[:2]
    image = bpy.data.images.new(name, w, h, alpha=True)
    image.pixels.foreach_set(pixels.astype(np.float32).ravel())
    path = os.path.join(directory, name + ".png")
    image.filepath_raw = path
    image.file_format = "PNG"
    image.alpha_mode = "STRAIGHT"
    image.save()
    image.filepath = path
    return image


# --------------------------------------------------------------------------------------------------------- meshes

def mesh_object(name, bm, material):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def shards(material):
    """Torn plating: flat, jagged convex pieces about one unit across."""
    rng = np.random.default_rng(37)
    for number in range(1, 5):
        bm = bmesh.new()
        for _ in range(8):
            point = rng.normal(0, 1, 3) * np.array([0.5, 0.32, 0.07])
            bm.verts.new(tuple(point))
        bmesh.ops.convex_hull(bm, input=bm.verts[:])
        loose = [v for v in bm.verts if not v.link_faces]
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
        mesh_object("Eclat_%d" % number, bm, material)


def ring(material, segments=48, inner=0.75, outer=1.0):
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    rings = []
    for radius in (inner, outer):
        rings.append([bm.verts.new((radius * math.cos(2 * math.pi * k / segments), radius * math.sin(2 * math.pi * k / segments), 0.0)) for k in range(segments)])
    for k in range(segments):
        j = (k + 1) % segments
        face = bm.faces.new((rings[0][k], rings[1][k], rings[1][j], rings[0][j]))
        for loop, (uu, vv) in zip(face.loops, ((k, 0), (k, 1), (k + 1, 1), (k + 1, 0))):
            loop[uv].uv = (uu / segments, vv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for face in bm.faces:
        if face.normal.z < 0:
            face.normal_flip()
    mesh_object("Onde", bm, material)


def spindle(name, material, radius, tail, sides=8):
    """A spindle along Blender's -Y (Unity's +Z): its head just past the origin, its tail behind."""
    profile = [(0.05, 0.0), (0.0, 0.8), (-0.1, 1.0), (tail * 0.6, 0.7), (tail, 0.0)]  # (Unity z, share of the radius)
    bm = bmesh.new()
    head = bm.verts.new((0.0, -profile[0][0], 0.0))
    rings = []
    for z, share in profile[1:-1]:
        rings.append([bm.verts.new((radius * share * math.cos(2 * math.pi * k / sides), -z, radius * share * math.sin(2 * math.pi * k / sides))) for k in range(sides)])
    end = bm.verts.new((0.0, -profile[-1][0], 0.0))
    for k in range(sides):
        j = (k + 1) % sides
        bm.faces.new((head, rings[0][j], rings[0][k]))
        for a, b in zip(rings, rings[1:]):
            bm.faces.new((a[k], a[j], b[j], b[k]))
        bm.faces.new((rings[-1][k], rings[-1][j], end))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mesh_object(name, bm, material)


def textured_material(name, image):
    material = bpy.data.materials.new(name)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
    return material


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_effects.py -- <output.blend>")
    path = os.path.abspath(argv[0])
    directory = os.path.dirname(path)
    os.makedirs(directory, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)

    images = {
        "Feu": save("Feu", sheet(fire_frames()), directory),
        "Fumee": save("Fumee", sheet(smoke_frames()), directory),
        "Arc": save("Arc", sheet(arc_frames()), directory),
        "Spore": save("Spore", sheet(spore_frames(), columns=2), directory),
        "Etincelle": save("Etincelle", spark_image(), directory),
        "Onde": save("Onde", ring_image()[::-1], directory),
        "Plasma": save("Plasma", plasma_image(), directory),
    }

    shards(textured_material("Eclat", images["Fumee"]))
    ring(textured_material("Onde", images["Onde"]))
    spindle("Trait", textured_material("Trait", images["Etincelle"]), 0.035, -0.8)
    spindle("Gaine", textured_material("Gaine", images["Feu"]), 0.085, -0.6)
    for image in images.values():
        # Kept even when no mesh uses it, so that the export copies it next to the model (export_unity.py).
        image.use_fake_user = True

    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.data.objects if o.type == "MESH")
    print("Saved %s: %d triangles, %d images" % (path, triangles, len(images)))


main()
