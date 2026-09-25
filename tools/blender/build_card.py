"""Build the card model (docs/ASSETS.md section 2, ADR-0017, ARB-85) and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_card.py -- art-src/cards/Card.blend

The card is fully described by the numbers below, so it is built by this script rather than by hand: changing the
layout means changing a number and running the script again. The saved .blend stays the source that gets exported
(tools/blender/export_unity.py) and can still be opened in Blender.

A futuristic brushed-metal card, 1 x 1.4 m and 0.04 m thick overall. It stands upright: width along X, height along Z,
thickness along Y. Its front faces +Y, which export_unity.py turns into -Z in Unity, the side the card prefab shows to
the camera. On a dark titanium body, slightly raised:
- the name bar, with the disc of the card's slot (ATK / DEF) at its left end;
- the window of the illustration (16:9), set in a bezel;
- the gem of the card's technology, on a tab over the text panel;
- the text panel, in light aluminium;
- the tab of the card's usage (Durable, Usage unique...), at the bottom.

Materials, which the game replaces with its own (Theme/Materials) and paints per card:
- Cadre: the dark metal (body, disc, usage tab, gem mount);
- Panneau: the light metal (name bar, text panel);
- Lisere: the trims (around the front, inside of the bezel, edge of the disc), in the technology colour, or the colour
  that marks a card (a purchase would replace it, a decision offers it);
- Gemme: the gem, glowing in the technology colour.

Empties named Zone_* mark where the game lays the illustration and the texts; their X and Z scales give the zone's size.
The brushed-metal textures (Card_BaseColor, Card_Normal, Card_MetallicSmoothness) are computed here and written next
to the .blend; the export copies them next to the FBX, where Unity picks them up.
"""

import math
import os
import sys

import bmesh
import bpy
import numpy as np

WIDTH = 1.0
HEIGHT = 1.4
CORNER_RADIUS = 0.05
CORNER_SEGMENTS = 6
RIM_WIDTH = 0.006
RIM_SEGMENTS = 2

# Heights along Y (thickness): the back, the body's front, then the raised parts. The card spans -0.02 to +0.02.
BACK = -0.02
FRONT = 0.012
BEZEL_TOP = 0.0145
PANEL_TOP = 0.016
MOUNT_TOP = 0.017
BADGE_TOP = 0.018
GEM_TOP = 0.02
CHAMFER = 0.003  # the sloped edge of every raised part
TRIM_SLOPE = 0.007  # the inner edge of the illustration's bezel, a trim wide enough to see on a small card

# Layout on the front, in metres from the centre of the card, as seen from the front: x to the right, z up. Seen from
# the front (+Y), Blender's +X points to the left, so the mesh and the zones are mirrored when built (x becomes -x).
ART_WIDTH = 0.84
ART_HEIGHT = ART_WIDTH * 9 / 16
ART_TOP = 0.475
BEZEL = 0.02
EDGE_TRIM = (0.012, 0.024)  # the trim around the front: from and to this distance from the card's edge
NAME_BAR = (-0.33, 0.46, 0.505, 0.645)  # x0, x1, z0, z1
SLOT_DISC = (-0.37, 0.575, 0.095)  # centre x, centre z, radius
PANEL = (-0.46, 0.46, -0.61, -0.045)
PANEL_RADIUS = 0.05
GEM = (0.0, -0.02, 0.065)  # centre x, centre z, radius
GEM_MOUNT = 0.082
GEM_TAB = (0.16, 0.11, -0.045, -0.008)  # half-width at the bottom, at the top, z bottom, z top
USAGE_TAB = (-0.2, 0.2, -0.675, -0.585)

TEXTURE_SIZE = 1024
CADRE, PANNEAU, LISERE, GEMME = range(4)
MATERIALS = ("Cadre", "Panneau", "Lisere", "Gemme")


# ------------------------------------------------------------------------------------------------------- outlines

def rounded_rectangle(x0, x1, z0, z1, radius, segments):
    """Counter-clockwise outline (seen with X right and Z up) of a rectangle with rounded corners."""
    centres = [(x1 - radius, z1 - radius), (x0 + radius, z1 - radius), (x0 + radius, z0 + radius), (x1 - radius, z0 + radius)]
    points = []
    for corner, (cx, cz) in enumerate(centres):
        for step in range(segments + 1):
            angle = math.radians(90 * corner + 90 * step / segments)
            points.append((cx + radius * math.cos(angle), cz + radius * math.sin(angle)))
    return points


def chamfered_rectangle(x0, x1, z0, z1, cut):
    """Counter-clockwise outline of a rectangle with its four corners cut at 45 degrees."""
    return [(x1, z1 - cut), (x1 - cut, z1), (x0 + cut, z1), (x0, z1 - cut), (x0, z0 + cut), (x0 + cut, z0), (x1 - cut, z0), (x1, z0 + cut)]


def circle(cx, cz, radius, sides):
    return [(cx + radius * math.cos(2 * math.pi * i / sides), cz + radius * math.sin(2 * math.pi * i / sides)) for i in range(sides)]


def inset(outline, distance):
    """The outline of a convex counter-clockwise polygon, moved inwards by distance."""
    count = len(outline)
    result = []
    for i in range(count):
        (ax, az), (bx, bz), (cx, cz) = outline[i - 1], outline[i], outline[(i + 1) % count]
        n1 = normal_in(bx - ax, bz - az)
        n2 = normal_in(cx - bx, cz - bz)
        mx, mz = n1[0] + n2[0], n1[1] + n2[1]
        length = math.hypot(mx, mz)
        mx, mz = mx / length, mz / length
        scale = distance / max(mx * n1[0] + mz * n1[1], 0.2)
        result.append((bx + mx * scale, bz + mz * scale))
    return result


def normal_in(dx, dz):
    length = math.hypot(dx, dz)
    return (-dz / length, dx / length)


# ----------------------------------------------------------------------------------------------------------- mesh

def orient(face, direction):
    face.normal_update()
    if face.normal.dot(direction) < 0:
        face.normal_flip()


def raised(bm, outline, y0, y1, top_mat, side_mat, chamfer=CHAMFER):
    """A part standing on the front: its outline at height y0, a sloped edge up to its top face at height y1."""
    top = inset(outline, chamfer)
    lower = [bm.verts.new((x, y0, z)) for x, z in outline]
    upper = [bm.verts.new((x, y1, z)) for x, z in top]
    cx = sum(x for x, _ in outline) / len(outline)
    cz = sum(z for _, z in outline) / len(outline)
    count = len(outline)
    for i in range(count):
        j = (i + 1) % count
        face = bm.faces.new((lower[i], lower[j], upper[j], upper[i]))
        mx = (outline[i][0] + outline[j][0]) / 2 - cx
        mz = (outline[i][1] + outline[j][1]) / 2 - cz
        orient(face, (mx, 0.3 * math.hypot(mx, mz), mz))
        face.material_index = side_mat
    face = bm.faces.new(upper)
    orient(face, (0, 1, 0))
    face.material_index = top_mat


def raised_frame(bm, outer, inner, y0, y1, top_mat, outer_mat, inner_mat, chamfer=CHAMFER, inner_chamfer=CHAMFER):
    """A frame standing on the front, between two outlines with the same number of points (a bezel)."""
    top_outer = inset(outer, chamfer)
    top_inner = inset(inner, -inner_chamfer)
    rings = [
        [bm.verts.new((x, y0, z)) for x, z in outer],
        [bm.verts.new((x, y1, z)) for x, z in top_outer],
        [bm.verts.new((x, y1, z)) for x, z in top_inner],
        [bm.verts.new((x, y0, z)) for x, z in inner],
    ]
    cx = sum(x for x, _ in inner) / len(inner)
    cz = sum(z for _, z in inner) / len(inner)
    count = len(outer)
    for band, mat, outwards in ((0, outer_mat, 1), (1, top_mat, 0), (2, inner_mat, -1)):
        for i in range(count):
            j = (i + 1) % count
            a, b = rings[band], rings[band + 1]
            face = bm.faces.new((a[i], a[j], b[j], b[i]))
            mx = (outer[i][0] + outer[j][0]) / 2 - cx
            mz = (outer[i][1] + outer[j][1]) / 2 - cz
            orient(face, (outwards * mx, 1.0 if outwards == 0 else 0.3 * math.hypot(mx, mz), outwards * mz))
            face.material_index = mat


def gem(bm, cx, cz, radius, y0, y1, sides=8):
    """A cut gem: a ring of facets from its girdle up to a flat table."""
    girdle = [bm.verts.new((x, y0, z)) for x, z in circle(cx, cz, radius, sides)]
    crown = [bm.verts.new((x, y0 + (y1 - y0) * 0.6, z)) for x, z in circle(cx, cz, radius * 0.8, sides)]
    table = [bm.verts.new((x, y1, z)) for x, z in circle(cx, cz, radius * 0.45, sides)]
    for a, b in ((girdle, crown), (crown, table)):
        for i in range(sides):
            j = (i + 1) % sides
            face = bm.faces.new((a[i], a[j], b[j], b[i]))
            mx, mz = (a[i].co.x + a[j].co.x) / 2 - cx, (a[i].co.z + a[j].co.z) / 2 - cz
            orient(face, (mx, 0.5 * math.hypot(mx, mz), mz))
            face.material_index = GEMME
    face = bm.faces.new(table)
    orient(face, (0, 1, 0))
    face.material_index = GEMME


def rim_profile():
    """Cross-section of the body's edge, from the front face to the back face: (outward offset, y) pairs."""
    half = (FRONT - BACK) / 2
    middle = (FRONT + BACK) / 2
    front = []
    for step in range(RIM_SEGMENTS + 1):
        angle = math.radians(90 * step / RIM_SEGMENTS)
        front.append((-RIM_WIDTH * (1 - math.sin(angle)), half - RIM_WIDTH * (1 - math.cos(angle))))
    return [(offset, middle + y) for offset, y in front] + [(offset, middle - y) for offset, y in reversed(front)]


def body(bm):
    """The body: a slab with rounded corners and a rounded rim, front at FRONT and back at BACK."""
    profile = rim_profile()
    outline = rounded_rectangle(-WIDTH / 2, WIDTH / 2, -HEIGHT / 2, HEIGHT / 2, CORNER_RADIUS, CORNER_SEGMENTS)
    # Each outline point moves outward along the corner's radius; points of the straight sides share it.
    half_w, half_h = WIDTH / 2 - CORNER_RADIUS, HEIGHT / 2 - CORNER_RADIUS
    rings = []
    for x, z in outline:
        cx = max(-half_w, min(half_w, x))
        cz = max(-half_h, min(half_h, z))
        dx, dz = x - cx, z - cz
        length = math.hypot(dx, dz)
        dx, dz = dx / length, dz / length
        rings.append([bm.verts.new((cx + (CORNER_RADIUS + offset) * dx, y, cz + (CORNER_RADIUS + offset) * dz)) for offset, y in profile])
    for index, ring in enumerate(rings):
        following = rings[(index + 1) % len(rings)]
        for step in range(len(profile) - 1):
            face = bm.faces.new((ring[step], ring[step + 1], following[step + 1], following[step]))
            orient(face, (ring[step].co.x, 0, ring[step].co.z))
            face.material_index = CADRE
            face.smooth = True
    front = bm.faces.new([ring[0] for ring in rings])
    orient(front, (0, 1, 0))
    back = bm.faces.new([ring[-1] for ring in rings])
    orient(back, (0, -1, 0))
    for face in (front, back):
        face.material_index = CADRE


def art_rectangle():
    return (-ART_WIDTH / 2, ART_WIDTH / 2, ART_TOP - ART_HEIGHT, ART_TOP)


def build_mesh():
    bm = bmesh.new()
    body(bm)

    x0, x1, z0, z1 = art_rectangle()
    inner = [(x1, z1), (x0, z1), (x0, z0), (x1, z0)]
    outer = [(x1 + BEZEL, z1 + BEZEL), (x0 - BEZEL, z1 + BEZEL), (x0 - BEZEL, z0 - BEZEL), (x1 + BEZEL, z0 - BEZEL)]
    raised_frame(bm, outer, inner, FRONT, BEZEL_TOP, CADRE, CADRE, LISERE, inner_chamfer=TRIM_SLOPE)

    # The trim around the front: it shows the technology colour, or the colour that marks the card, at any size.
    near, far = EDGE_TRIM
    edge_outer = rounded_rectangle(-WIDTH / 2 + near, WIDTH / 2 - near, -HEIGHT / 2 + near, HEIGHT / 2 - near, CORNER_RADIUS - near, CORNER_SEGMENTS)
    edge_inner = rounded_rectangle(-WIDTH / 2 + far, WIDTH / 2 - far, -HEIGHT / 2 + far, HEIGHT / 2 - far, CORNER_RADIUS - far, CORNER_SEGMENTS)
    raised_frame(bm, edge_outer, edge_inner, FRONT, BEZEL_TOP, LISERE, CADRE, CADRE, chamfer=0.002, inner_chamfer=0.002)

    raised(bm, chamfered_rectangle(*NAME_BAR, 0.03), FRONT, PANEL_TOP, PANNEAU, PANNEAU)
    px0, px1, pz0, pz1 = PANEL
    raised(bm, rounded_rectangle(px0, px1, pz0, pz1, PANEL_RADIUS, CORNER_SEGMENTS), FRONT, PANEL_TOP, PANNEAU, PANNEAU)
    bottom, top, tz0, tz1 = GEM_TAB
    raised(bm, [(bottom, tz0), (top, tz1), (-top, tz1), (-bottom, tz0)], FRONT, PANEL_TOP, PANNEAU, PANNEAU)

    sx, sz, radius = SLOT_DISC
    raised(bm, circle(sx, sz, radius, 32), FRONT, BADGE_TOP, CADRE, LISERE, chamfer=0.006)
    raised(bm, chamfered_rectangle(*USAGE_TAB, 0.03), FRONT, BADGE_TOP, CADRE, CADRE)
    gx, gz, gem_radius = GEM
    raised(bm, circle(gx, gz, GEM_MOUNT, 16), FRONT, MOUNT_TOP, CADRE, CADRE)
    gem(bm, gx, gz, gem_radius, MOUNT_TOP, GEM_TOP)

    # The layout is drawn as seen from the front: mirror it so that it reads the right way from +Y.
    bmesh.ops.scale(bm, vec=(-1.0, 1.0, 1.0), verts=bm.verts[:])
    bmesh.ops.reverse_faces(bm, faces=bm.faces[:])

    # One planar projection of the front for every face, read the right way from the front: the brushed streaks run
    # across the whole card.
    uv = bm.loops.layers.uv.new("UVMap")
    for face in bm.faces:
        for loop in face.loops:
            loop[uv].uv = (0.5 - loop.vert.co.x / WIDTH, loop.vert.co.z / HEIGHT + 0.5)

    mesh = bpy.data.meshes.new("Card")
    for _ in MATERIALS:
        mesh.materials.append(None)  # slots first: they keep the faces' material indices
    bm.to_mesh(mesh)
    bm.free()
    return mesh


def zones(parent):
    """Empties where the game lays the illustration and the texts: centre on the front, size in X and Z scale."""
    x0, x1, z0, z1 = art_rectangle()
    sx, sz, radius = SLOT_DISC
    px0, px1, pz0, pz1 = PANEL
    ux0, ux1, uz0, uz1 = USAGE_TAB
    gx, gz, gem_radius = GEM
    nx0, nx1, nz0, nz1 = NAME_BAR
    layout = {
        "Zone_Illustration": ((x0 + x1) / 2, FRONT, (z0 + z1) / 2, x1 - x0, z1 - z0),
        "Zone_Nom": ((sx + radius + 0.02 + nx1 - 0.03) / 2, PANEL_TOP, (nz0 + nz1) / 2, nx1 - 0.03 - (sx + radius + 0.02), nz1 - nz0 - 0.03),
        "Zone_Emplacement": (sx, BADGE_TOP, sz, radius * 1.3, radius * 0.9),
        "Zone_Texte": (0.0, PANEL_TOP, (pz0 + 0.035 + gz - gem_radius - 0.02) / 2, px1 - px0 - 0.08, gz - gem_radius - 0.02 - pz0 - 0.035),
        "Zone_Usage": ((ux0 + ux1) / 2, BADGE_TOP, (uz0 + uz1) / 2, ux1 - ux0 - 0.06, uz1 - uz0 - 0.02),
        "Zone_Identifiant": (0.37, FRONT, (-HEIGHT / 2 + pz0) / 2, 0.16, 0.04),
        "Zone_Tourments": (0.4, PANEL_TOP, 0.62, 0.3, 0.3),
    }
    scene = bpy.context.scene
    for name, (x, y, z, width, height) in layout.items():
        empty = bpy.data.objects.new(name, None)
        empty.empty_display_type = "CUBE"
        empty.empty_display_size = 0.5
        empty.location = (-x, y, z)  # mirrored like the mesh
        empty.scale = (width, 0.01, height)
        empty.parent = parent
        scene.collection.objects.link(empty)


# ------------------------------------------------------------------------------------------------------- textures

def streaks(rng, size, along, across):
    """Tileable noise stretched along X: a Gaussian blur of `along` pixels along X and `across` pixels along Y."""
    noise = rng.standard_normal((size, size))
    fy = np.fft.fftfreq(size)[:, None]
    fx = np.fft.fftfreq(size)[None, :]
    kernel = np.exp(-2 * math.pi ** 2 * ((along * fx) ** 2 + (across * fy) ** 2))
    field = np.real(np.fft.ifft2(np.fft.fft2(noise) * kernel))
    return (field - field.mean()) / field.std()


def brushed_metal(directory):
    """Brushed metal, the same for every card and both metals (the game tints it): colour, relief, shine."""
    rng = np.random.default_rng(1)
    size = TEXTURE_SIZE
    brush = 0.7 * streaks(rng, size, 90, 0.8) + 0.3 * streaks(rng, size, 14, 0.5)
    sheen = streaks(rng, size, 160, 120)

    color = np.clip(0.88 + 0.035 * brush + 0.025 * sheen, 0, 1)
    height = 0.5 * brush
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / 2
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / 2
    strength = 0.2
    nx, ny, nz = -dx * strength, -dy * strength, np.ones_like(height)
    length = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    smooth = np.clip(0.7 + 0.06 * brush + 0.04 * sheen, 0, 1)

    maps = {
        "Card_BaseColor": (np.dstack([color, color, color, np.ones_like(color)]), "sRGB"),
        "Card_Normal": (np.dstack([nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5, np.ones_like(color)]), "Non-Color"),
        "Card_MetallicSmoothness": (np.dstack([np.ones_like(color), np.zeros_like(color), np.zeros_like(color), smooth]), "Non-Color"),
    }
    images = {}
    for name, (pixels, space) in maps.items():
        image = bpy.data.images.new(name, size, size, alpha=True)
        image.colorspace_settings.name = space
        image.pixels.foreach_set(pixels.astype(np.float32).ravel())
        path = os.path.join(directory, name + ".png")
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        image.filepath = path
        images[name] = image
    return images


def material(name, images, tint, roughness, emission=None):
    """A Blender preview of the metal; the game uses its own materials with the same textures."""
    mat = bpy.data.materials.new(name)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    color = nodes.new("ShaderNodeTexImage")
    color.image = images["Card_BaseColor"]
    tinted = nodes.new("ShaderNodeMix")
    tinted.data_type = "RGBA"
    tinted.blend_type = "MULTIPLY"
    tinted.inputs["Factor"].default_value = 1.0
    links.new(color.outputs["Color"], tinted.inputs["A"])
    tinted.inputs["B"].default_value = (*tint, 1.0)
    links.new(tinted.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Metallic"].default_value = 1.0
    shine = nodes.new("ShaderNodeTexImage")
    shine.image = images["Card_MetallicSmoothness"]
    rough = nodes.new("ShaderNodeMath")
    rough.operation = "MULTIPLY_ADD"
    links.new(shine.outputs["Alpha"], rough.inputs[0])
    rough.inputs[1].default_value = -1.0
    rough.inputs[2].default_value = 1.0 + roughness - 0.3
    links.new(rough.outputs[0], bsdf.inputs["Roughness"])
    relief = nodes.new("ShaderNodeTexImage")
    relief.image = images["Card_Normal"]
    normal_map = nodes.new("ShaderNodeNormalMap")
    links.new(relief.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission[0], 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission[1]
    mat.diffuse_color = (*tint, 1.0)
    return mat


# ----------------------------------------------------------------------------------------------------------- main

def output_path():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_card.py -- <output.blend>")
    return os.path.abspath(argv[0])


def build(directory):
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    mesh = build_mesh()
    images = brushed_metal(directory)
    mesh.materials[CADRE] = material("Cadre", images, (0.28, 0.29, 0.31), 0.35)
    mesh.materials[PANNEAU] = material("Panneau", images, (1.0, 1.0, 1.0), 0.3)
    mesh.materials[LISERE] = material("Lisere", images, (0.2, 0.75, 1.0), 0.3, emission=((0.2, 0.75, 1.0), 1.0))
    mesh.materials[GEMME] = material("Gemme", images, (0.1, 0.35, 1.0), 0.1, emission=((0.15, 0.4, 1.0), 3.0))
    card = bpy.data.objects.new("Card", mesh)
    scene.collection.objects.link(card)
    zones(card)

    # The source keeps quads and n-gons, easy to edit; the export triangulates through this modifier.
    triangulate = card.modifiers.new("Triangulate", "TRIANGULATE")
    triangulate.quad_method = "BEAUTY"
    triangulate.ngon_method = "BEAUTY"
    return card


def main():
    path = output_path()
    directory = os.path.dirname(path)
    os.makedirs(directory, exist_ok=True)
    card = build(directory)

    # Drop what the startup file brings and nothing uses (default material, brushes), so the source holds the card only.
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no Card.blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = card.evaluated_get(depsgraph).to_mesh()
    triangles = len(evaluated.polygons)
    card.evaluated_get(depsgraph).to_mesh_clear()
    print("Saved %s: %d triangles, %s m" % (path, triangles, tuple(round(d, 3) for d in card.dimensions)))


if __name__ == "__main__":
    main()
