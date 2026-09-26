"""Build the d8, the House's casino die (docs/ASSETS.md section 2, docs/ANIMATIONS.md §5, docs/DIRECTION_ARTISTIQUE.md
§3 and §4: "le dé du destin : un d8 de casino, forcément pipé"), and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_die.py -- art-src/dice/D8.blend

Then export it for Unity:

    blender --background --disable-autoexec art-src/dice/D8.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Dice/D8.fbx --budget 500

An octahedron one unit high (its corners half a unit from the centre on each axis), its edges chamfered. Black lacquer
faces, each with its number engraved and filled with tarnished gold inside a thin gold border; brass on the chamfers,
worn. Opposite faces add up to 9, as on a real d8. A small lamp, Voyant, is set under the 8: the House's mark, which
the game may light when the 8 comes up.

Eight empty markers Face_1 to Face_8, one on each face's centre: their forward axis (Unity's +Z, Blender's -Y) leaves
the face and their up axis (Unity's +Y, Blender's +Z) points to the top of the number. The game turns the drawn face
towards the camera, upright (DieSpinner).

The texture is drawn here: each face has its own square of the atlas, its number upright in it; the chamfers share a
patch of brass.
"""

import math
import os
import shutil
import sys
import tempfile

import bmesh
import bpy
import mathutils
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import front_view as fv  # noqa: E402  (next to this script)

NAME = "D8"
CORNER = 0.556  # distance of each sharp corner from the centre; cut, the die is one unit high
CHAMFER = 0.012  # cut into each edge; on a face, the brass strip is about three times as wide
TEX_W, TEX_H = 1024, 512
TILE = 256  # pixels of each face's square: four across, two down
FACE_SPAN = 0.45  # the face's corners reach this share of its square, from its middle
NUMBER_HEIGHT = 0.2
LAMP = (0.152, 0.022)  # distance under the face's centre, radius

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NUMBER_FONT = os.path.join(REPO, "unity", "Assets", "_Vortex", "Art", "Fonts", "Anton", "Anton-Regular.ttf")

# Face normals by sign (x, y, z) and their numbers: opposite normals carry numbers adding up to 9.
FACES = [
    ((1, 1, 1), 1), ((-1, -1, -1), 8),
    ((-1, 1, 1), 2), ((1, -1, -1), 7),
    ((1, 1, -1), 3), ((-1, -1, 1), 6),
    ((-1, 1, -1), 4), ((1, -1, 1), 5),
]

# Linear colours.
LACQUER = np.array([0.011, 0.011, 0.012])
GOLD = np.array([0.5, 0.33, 0.09])
BRASS = np.array([0.4, 0.28, 0.08])
SOOT = np.array([0.02, 0.018, 0.015])


# ---------------------------------------------------------------------------------------------------------- shape

def face_axes(signs):
    """The outward normal of a face, the up of its number (towards the corner on the vertical axis) and its right, as
    seen from outside."""
    normal = mathutils.Vector(signs).normalized()
    vertical = mathutils.Vector((0, 0, math.copysign(1, signs[2])))
    up = (vertical - vertical.dot(normal) * normal).normalized()
    right = up.cross(normal)
    return normal, up, right


def tile_of(number):
    column, row = (number - 1) % 4, (number - 1) // 4
    return column * TILE, row * TILE


def build_mesh():
    """The chamfered octahedron as the intersection of half-spaces: the eight faces, twelve edge cuts and six corner
    cuts; its hull is merged back into flat faces."""
    planes = [(mathutils.Vector(signs).normalized(), CORNER / math.sqrt(3)) for signs, _ in FACES]
    for i in range(3):
        for j in range(i + 1, 3):
            for si in (1, -1):
                for sj in (1, -1):
                    normal = mathutils.Vector((0, 0, 0))
                    normal[i], normal[j] = si, sj
                    planes.append((normal.normalized(), CORNER / math.sqrt(2) - CHAMFER))
    for axis in range(3):
        for sign in (1, -1):
            normal = mathutils.Vector((0, 0, 0))
            normal[axis] = sign
            planes.append((normal, CORNER - 0.056))
    points = []
    for a in range(len(planes)):
        for b in range(a + 1, len(planes)):
            for c in range(b + 1, len(planes)):
                (na, da), (nb, db), (nc, dc) = planes[a], planes[b], planes[c]
                matrix = mathutils.Matrix((na, nb, nc))
                if abs(matrix.determinant()) < 1e-6:
                    continue
                point = matrix.inverted() @ mathutils.Vector((da, db, dc))
                if all(n.dot(point) <= d + 1e-6 for n, d in planes) and all((point - q).length > 1e-5 for q in points):
                    points.append(point)
    bm = bmesh.new()
    for point in points:
        bm.verts.new(point)
    bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(1), verts=bm.verts[:], edges=bm.edges[:])
    bm.normal_update()
    face_normals = {tuple(signs): mathutils.Vector(signs).normalized() for signs, _ in FACES}
    chamfers = {f for f in bm.faces if max(f.normal.dot(n) for n in face_normals.values()) < 0.999}

    uv = bm.loops.layers.uv.new("UVMap")
    for face in bm.faces:
        if face in chamfers:
            # The chamfers share a patch of brass in the atlas's last corner.
            for loop in face.loops:
                loop[uv].uv = ((TEX_W - 12 + 6 * (loop.index % 2)) / TEX_W, (TEX_H - 12 + 6 * (loop.index % 3 > 0)) / TEX_H)
            face.material_index = 0
            continue
        signs = tuple(int(math.copysign(1, c)) for c in face.normal)
        number = dict(FACES)[signs]
        normal, up, right = face_axes(signs)
        centre = mathutils.Vector(signs) * (CORNER / 3)
        circumradius = CORNER * math.sqrt(2) / math.sqrt(3)
        scale = FACE_SPAN / circumradius
        u0, v0 = tile_of(number)
        for loop in face.loops:
            local = loop.vert.co - centre
            loop[uv].uv = ((u0 + TILE * (0.5 + local.dot(right) * scale)) / TEX_W, (v0 + TILE * (0.5 + local.dot(up) * scale)) / TEX_H)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mesh = bpy.data.meshes.new(NAME)
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    return mesh


def lamp(die):
    """The House's lamp under the 8: a small domed stud, its own object so the game can light it."""
    signs = next(s for s, n in FACES if n == 8)
    normal, up, right = face_axes(signs)
    centre = mathutils.Vector(signs) * (CORNER / 3) - up * LAMP[0]
    radius, sides = LAMP[1], 10
    bm = bmesh.new()
    base = [bm.verts.new(centre + radius * (math.cos(a) * right + math.sin(a) * up) - normal * 0.002) for a in (2 * math.pi * k / sides for k in range(sides))]
    top = [bm.verts.new(centre + 0.7 * radius * (math.cos(a) * right + math.sin(a) * up) + normal * 0.008) for a in (2 * math.pi * k / sides for k in range(sides))]
    for k in range(sides):
        bm.faces.new((base[k], base[(k + 1) % sides], top[(k + 1) % sides], top[k]))
    bm.faces.new(top)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mesh = bpy.data.meshes.new("Voyant")
    bm.to_mesh(mesh)
    bm.free()
    material = bpy.data.materials.new("Voyant")
    bsdf = next(n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (0.3, 0.025, 0.02, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.1
    material.diffuse_color = (0.3, 0.025, 0.02, 1.0)
    mesh.materials.append(material)
    obj = bpy.data.objects.new("Voyant", mesh)
    obj.parent = die
    bpy.context.scene.collection.objects.link(obj)


def markers(die):
    for signs, number in FACES:
        normal, up, _ = face_axes(signs)
        forward = -normal  # Blender's -Y becomes Unity's +Z: the forward axis leaves the face
        y_axis = forward
        z_axis = up
        x_axis = y_axis.cross(z_axis)
        matrix = mathutils.Matrix((x_axis, y_axis, z_axis)).transposed().to_4x4()
        matrix.translation = mathutils.Vector(signs) * (CORNER / 3)
        empty = bpy.data.objects.new("Face_%d" % number, None)
        empty.empty_display_type = "ARROWS"
        empty.empty_display_size = 0.1
        empty.matrix_world = matrix
        empty.parent = die
        bpy.context.scene.collection.objects.link(empty)


# -------------------------------------------------------------------------------------------------------- texture

def tight(cover):
    """A coverage array cropped to its ink (the rendered image is dithered: its black is not quite 0)."""
    rows, cols = np.nonzero(cover > 0.3)
    return cover[rows.min():rows.max() + 1, cols.min():cols.max() + 1]


def paint(directory):
    rng = np.random.default_rng(8)
    colour = np.zeros((TEX_H, TEX_W, 3))
    height = np.zeros((TEX_H, TEX_W))
    colour[:] = LACQUER
    circumradius = CORNER * math.sqrt(2) / math.sqrt(3)
    scale = FACE_SPAN / circumradius  # tile shares per unit of the die
    ys, xs = np.mgrid[0:TILE, 0:TILE]
    a = ((xs + 0.5) / TILE - 0.5) / scale  # right, in the die's units
    b = ((ys + 0.5) / TILE - 0.5) / scale  # up
    # The face's three edges, as seen from outside with the number upright: the top corner is up.
    corners = [(0.0, circumradius), (-circumradius * math.sqrt(3) / 2, -circumradius / 2), (circumradius * math.sqrt(3) / 2, -circumradius / 2)]
    inradius = circumradius / 2
    distance = np.full((TILE, TILE), np.inf)
    for (xa, za), (xb, zb) in zip(corners, corners[1:] + corners[:1]):
        # Signed distance to each edge, positive inside.
        ex, ez = xb - xa, zb - za
        length = math.hypot(ex, ez)
        nx, nz = -ez / length, ex / length
        if nx * (0 - xa) + nz * (0 - za) < 0:
            nx, nz = -nx, -nz
        distance = np.minimum(distance, (a - xa) * nx + (b - za) * nz)
    border = (distance > 0.075) & (distance < 0.084)

    folder = tempfile.mkdtemp(prefix="vortex_die_")
    try:
        for _, number in FACES:
            glyph = tight(fv.render_text(str(number), NUMBER_FONT, folder))
            gh, gw = glyph.shape
            size = NUMBER_HEIGHT / gh
            u = a / size + gw / 2
            v = b / size + gh / 2
            inside = (u >= 0) & (u < gw) & (v >= 0) & (v < gh)
            cover = np.where(inside, glyph[np.clip(v.astype(int), 0, gh - 1), np.clip(u.astype(int), 0, gw - 1)], 0.0)
            if number in (6,):
                # A bar under the 6, as on real dice, so it is not read upside down.
                cover = np.maximum(cover, ((np.abs(a) < NUMBER_HEIGHT * 0.22) & (b > -NUMBER_HEIGHT * 0.66) & (b < -NUMBER_HEIGHT * 0.58)).astype(float))
            engraved = cover > 0.5
            wall = fv.dilate(engraved, 2) & ~engraved
            u0, v0 = tile_of(number)
            tile = colour[v0:v0 + TILE, u0:u0 + TILE]
            grime = rng.random((TILE, TILE)) * 0.25
            tile[:] = LACQUER * (0.85 + 0.3 * rng.random((TILE, TILE)))[..., None]
            tile[border] = GOLD * 0.8
            tile[wall] = SOOT
            tile[engraved] = GOLD * (1 - grime[engraved])[..., None]
            tile[distance < 0.03] = BRASS * 0.7  # the face's rim, where the chamfer starts
            h = height[v0:v0 + TILE, u0:u0 + TILE]
            h -= np.where(engraved | wall, 1.0, 0.0)
            h -= np.where(border, 0.3, 0.0)
            # Fine scratches in the lacquer, from years of the House's tables.
            for _ in range(14):
                x0, y0 = rng.uniform(0, TILE, 2)
                angle = rng.uniform(0, math.pi)
                t = np.linspace(0, rng.uniform(6, 24), 40)
                cols = np.clip(x0 + t * math.cos(angle), 0, TILE - 1).astype(int)
                rows = np.clip(y0 + t * math.sin(angle), 0, TILE - 1).astype(int)
                keep = ~engraved[rows, cols] & (distance[rows, cols] > 0.03)
                tile[rows[keep], cols[keep]] = LACQUER * 2.2
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    # The chamfers' brass patch, in the atlas's last corner.
    colour[TEX_H - 16:, TEX_W - 16:] = BRASS
    image = bpy.data.images.new(NAME + "_BaseColor", TEX_W, TEX_H, alpha=False)
    image.pixels.foreach_set(np.dstack([fv.to_srgb(colour), np.ones((TEX_H, TEX_W))]).astype(np.float32).ravel())
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / 2
    dz = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / 2
    nx, ny, nz = -dx * 1.5, -dz * 1.5, np.ones_like(height)
    length = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    normal = bpy.data.images.new(NAME + "_Normal", TEX_W, TEX_H, alpha=False)
    normal.colorspace_settings.name = "Non-Color"
    normal.pixels.foreach_set(np.dstack([nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5, np.ones_like(height)]).astype(np.float32).ravel())
    for picture in (image, normal):
        path = os.path.join(directory, picture.name + ".png")
        picture.filepath_raw = path
        picture.file_format = "PNG"
        picture.save()
        picture.filepath = path
    return image, normal


def die_material(colour, normal):
    material = bpy.data.materials.new("De")
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    bsdf.inputs["Metallic"].default_value = 0.35
    bsdf.inputs["Roughness"].default_value = 0.25  # lacquered: sharp highlights
    image = nodes.new("ShaderNodeTexImage")
    image.image = colour
    links.new(image.outputs["Color"], bsdf.inputs["Base Color"])
    relief = nodes.new("ShaderNodeTexImage")
    relief.image = normal
    normal_map = nodes.new("ShaderNodeNormalMap")
    links.new(relief.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_die.py -- <output.blend>")
    path = os.path.abspath(argv[0])
    directory = os.path.dirname(path)
    os.makedirs(directory, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)

    mesh = build_mesh()
    colour, normal = paint(directory)
    mesh.materials.append(die_material(colour, normal))
    die = bpy.data.objects.new(NAME, mesh)
    bpy.context.scene.collection.objects.link(die)
    lamp(die)
    markers(die)

    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.data.objects if o.type == "MESH")
    print("Saved %s: %d triangles, %s" % (path, triangles, tuple(round(d, 3) for d in die.dimensions)))


main()
