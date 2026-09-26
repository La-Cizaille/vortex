"""Build the card model, a pluggable ship module (docs/DIRECTION_ARTISTIQUE.md §6.5 bis, ARB-95 to ARB-97), and
save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_card.py -- art-src/cards/Card.blend

The card is fully described by the numbers below, so it is built by this script rather than by hand: changing the
layout means changing a number and running the script again. The saved .blend stays the source that gets exported
(tools/blender/export_unity.py) and can still be opened in Blender.

A card is a module that plugs into a bay of the cockpit, as on the art direction's board v3: 1 x 1.4 m and 0.04 m thick
overall. It stands upright: width along X, height along Z, thickness along Y. Its front faces +Y, which
export_unity.py turns into -Z in Unity, the side the card prefab shows to the camera. It has:
- a blackened steel case, screwed at its four corners, with a handle on top and a pin connector below;
- a dark plate for the name, with the slot tag (ATK, DEF...) at its right end;
- the screen of the illustration (16:9), in a bezel;
- the terminal of the rule text, dark and recessed;
- a status row: the faction lamp and its LEDs, the usage by the fuse, the id; a vent grille under it.

Materials, which the game replaces with its own (Theme/Materials) and paints per card:
- Cadre: the steel (case, bezels, screws, handle, connector, fuse holder, lamp mount);
- Panneau: the dark faces that carry text (name plate, terminal);
- Lisere: the trims (around the front, inside the screen's bezel) and the LEDs, in the faction colour, or the colour
  that marks a card (a purchase would replace it, a decision offers it);
- Gemme: the faction lamp, lit for a faction's card.
Cadre and Panneau share one texture, painted here in the layout of the front (one planar projection): blackened steel
with soot, bare metal and rust on the worn edges, brass pins, screw heads, vent slots, the terminal's scan lines, the
tag's amber frame. Card_BaseColor, Card_Normal and Card_MetallicSmoothness are written next to the .blend; the export
copies them next to the FBX, where Unity picks them up.

Empties named Zone_* mark where the game lays the illustration and the texts; their X and Z scales give the zone's size.
Zone_Dos gives the size of the back, which the game covers with the card back.
"""

import math
import os
import sys

import bmesh
import bpy
import numpy as np

WIDTH = 1.0
HEIGHT = 1.4

# The case: the card without its handle and its connector.
CASE = (-0.5, 0.5, -0.645, 0.645)
CORNER_RADIUS = 0.04
CORNER_SEGMENTS = 5
RIM_WIDTH = 0.006
RIM_SEGMENTS = 2

# Heights along Y (thickness): the back, the case's front, then the raised parts. The card spans -0.02 to +0.02.
BACK = -0.02
FRONT = 0.012
SCREEN = 0.0132  # the terminal's glass, recessed in its bezel
BEZEL_TOP = 0.0145
PANEL_TOP = 0.016
MOUNT_TOP = 0.017
STUD_TOP = 0.018
GEM_TOP = 0.02
CHAMFER = 0.003  # the sloped edge of every raised part
TRIM_SLOPE = 0.007  # the inner edge of the screen's bezel, a trim wide enough to see on a small card

# Layout on the front, in metres from the centre of the card, as seen from the front: x to the right, z up. Seen from
# the front (+Y), Blender's +X points to the left, so the mesh and the zones are mirrored when built (x becomes -x).
HANDLE = (0.25, 0.06, 0.638, 0.675, 0.70, 0.009)  # half-width, post width, z bottom, bar bottom, top, half-thickness
CONNECTOR = (0.38, -0.70, -0.638, 0.007)  # half-width, z bottom, z top, half-thickness
PIN_PITCH = 0.04
EDGE_TRIM = (0.012, 0.022)  # the trim around the front: from and to this distance from the case's edge
SCREWS = [(sx * 0.455, sz * 0.6) for sx in (-1, 1) for sz in (-1, 1)]
SCREW_RADIUS = 0.018
NAME_PLATE = (-0.44, 0.44, 0.515, 0.625)
TAG = (0.29, 0.42, 0.535, 0.605)  # the slot tag's frame, at the right end of the plate
ART_WIDTH = 0.84
ART_HEIGHT = ART_WIDTH * 9 / 16
ART_TOP = 0.485
BEZEL = 0.02
TERMINAL = (-0.44, 0.44, -0.50, -0.035)  # the glass; its bezel is BEZEL wider
STATUS_Z = -0.555
LAMP = (-0.40, 0.025, 0.033)  # x, radius of the lamp, radius of its mount
LEDS = [-0.345, -0.315, -0.285]
LED_SIZE = 0.018
FUSE = (0.15, 0.25, -0.57, -0.54)
VENT = (-0.30, 0.30, -0.632, -0.604, 0.012, 0.024)  # x0, x1, z0, z1, slot width, pitch

TEXTURE_WIDTH = 1024
TEXTURE_HEIGHT = 1434  # the card's proportions: one texel is square on the card
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


def rectangle(x0, x1, z0, z1):
    return [(x1, z1), (x0, z1), (x0, z0), (x1, z0)]


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


def centre(outline):
    return sum(x for x, _ in outline) / len(outline), sum(z for _, z in outline) / len(outline)


def raised(bm, outline, y0, y1, top_mat, side_mat, chamfer=CHAMFER):
    """A part standing on the front: its outline at height y0, a sloped edge up to its top face at height y1."""
    top = inset(outline, chamfer) if chamfer > 0 else outline
    lower = [bm.verts.new((x, y0, z)) for x, z in outline]
    upper = [bm.verts.new((x, y1, z)) for x, z in top]
    cx, cz = centre(outline)
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


def slab(bm, outline, half_thickness, mat, chamfer=0.002):
    """A closed part seen from both sides (handle, connector): chamfered on its front and its back."""
    y = half_thickness
    rings = [
        [bm.verts.new((x, y, z)) for x, z in inset(outline, chamfer)],
        [bm.verts.new((x, y - chamfer, z)) for x, z in outline],
        [bm.verts.new((x, -y + chamfer, z)) for x, z in outline],
        [bm.verts.new((x, -y, z)) for x, z in inset(outline, chamfer)],
    ]
    cx, cz = centre(outline)
    count = len(outline)
    for band, lean in ((0, 0.5), (1, 0.0), (2, -0.5)):
        for i in range(count):
            j = (i + 1) % count
            a, b = rings[band], rings[band + 1]
            face = bm.faces.new((a[i], a[j], b[j], b[i]))
            mx = (outline[i][0] + outline[j][0]) / 2 - cx
            mz = (outline[i][1] + outline[j][1]) / 2 - cz
            orient(face, (mx, lean * math.hypot(mx, mz), mz))
            face.material_index = mat
    front = bm.faces.new(rings[0])
    orient(front, (0, 1, 0))
    back = bm.faces.new(rings[-1])
    orient(back, (0, -1, 0))
    front.material_index = back.material_index = mat


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
    cx, cz = centre(inner)
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
    """A cut lamp: a ring of facets from its girdle up to a flat table."""
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
    """Cross-section of the case's edge, from the front face to the back face: (outward offset, y) pairs."""
    half = (FRONT - BACK) / 2
    middle = (FRONT + BACK) / 2
    front = []
    for step in range(RIM_SEGMENTS + 1):
        angle = math.radians(90 * step / RIM_SEGMENTS)
        front.append((-RIM_WIDTH * (1 - math.sin(angle)), half - RIM_WIDTH * (1 - math.cos(angle))))
    return [(offset, middle + y) for offset, y in front] + [(offset, middle - y) for offset, y in reversed(front)]


def case(bm):
    """The case: a slab with rounded corners and a rounded rim, front at FRONT and back at BACK."""
    x0, x1, z0, z1 = CASE
    profile = rim_profile()
    outline = rounded_rectangle(x0, x1, z0, z1, CORNER_RADIUS, CORNER_SEGMENTS)
    # Each outline point moves outward along the corner's radius; points of the straight sides share it.
    rings = []
    for x, z in outline:
        cx = max(x0 + CORNER_RADIUS, min(x1 - CORNER_RADIUS, x))
        cz = max(z0 + CORNER_RADIUS, min(z1 - CORNER_RADIUS, z))
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
    front.material_index = back.material_index = CADRE


def art_rectangle():
    return (-ART_WIDTH / 2, ART_WIDTH / 2, ART_TOP - ART_HEIGHT, ART_TOP)


def build_mesh():
    bm = bmesh.new()
    case(bm)
    x0, x1, z0, z1 = CASE

    # The handle, a U above the case, and the pin connector below it: seen from both sides.
    half, post, hz0, hz1, hz2, hy = HANDLE
    slab(bm, rectangle(-half, -half + post, hz0, hz1), hy, CADRE)
    slab(bm, rectangle(half - post, half, hz0, hz1), hy, CADRE)
    slab(bm, chamfered_rectangle(-half, half, hz1, hz2, 0.012), hy, CADRE)
    chalf, cz0, cz1, cy = CONNECTOR
    slab(bm, chamfered_rectangle(-chalf, chalf, cz0, cz1, 0.01), cy, CADRE)

    # The trim around the front: it shows the faction colour, or the colour that marks the card, at any size.
    near, far = EDGE_TRIM
    edge_outer = rounded_rectangle(x0 + near, x1 - near, z0 + near, z1 - near, CORNER_RADIUS - near, CORNER_SEGMENTS)
    edge_inner = rounded_rectangle(x0 + far, x1 - far, z0 + far, z1 - far, CORNER_RADIUS - far, CORNER_SEGMENTS)
    raised_frame(bm, edge_outer, edge_inner, FRONT, BEZEL_TOP, LISERE, CADRE, CADRE, chamfer=0.002, inner_chamfer=0.002)

    for sx, sz in SCREWS:
        raised(bm, circle(sx, sz, SCREW_RADIUS, 8), FRONT, STUD_TOP, CADRE, CADRE, chamfer=0.004)

    raised(bm, chamfered_rectangle(*NAME_PLATE, 0.02), FRONT, PANEL_TOP, PANNEAU, CADRE)

    ax0, ax1, az0, az1 = art_rectangle()
    raised_frame(bm, rectangle(ax0 - BEZEL, ax1 + BEZEL, az0 - BEZEL, az1 + BEZEL), rectangle(ax0, ax1, az0, az1),
                 FRONT, BEZEL_TOP, CADRE, CADRE, LISERE, inner_chamfer=TRIM_SLOPE)

    tx0, tx1, tz0, tz1 = TERMINAL
    raised_frame(bm, rectangle(tx0 - BEZEL, tx1 + BEZEL, tz0 - BEZEL, tz1 + BEZEL), rectangle(tx0, tx1, tz0, tz1),
                 FRONT, BEZEL_TOP, CADRE, CADRE, CADRE, inner_chamfer=0.004)
    raised(bm, rectangle(tx0, tx1, tz0, tz1), FRONT, SCREEN, PANNEAU, CADRE, chamfer=0.0)

    lx, lamp_radius, mount_radius = LAMP
    raised(bm, circle(lx, STATUS_Z, mount_radius, 12), FRONT, MOUNT_TOP, CADRE, CADRE, chamfer=0.003)
    gem(bm, lx, STATUS_Z, lamp_radius, MOUNT_TOP, GEM_TOP)
    for led in LEDS:
        h = LED_SIZE / 2
        raised(bm, rectangle(led - h, led + h, STATUS_Z - h, STATUS_Z + h), FRONT, STUD_TOP, LISERE, CADRE, chamfer=0.003)
    raised(bm, chamfered_rectangle(*FUSE, 0.006), FRONT, STUD_TOP, CADRE, CADRE, chamfer=0.003)

    # The layout is drawn as seen from the front: mirror it so that it reads the right way from +Y.
    bmesh.ops.scale(bm, vec=(-1.0, 1.0, 1.0), verts=bm.verts[:])
    bmesh.ops.reverse_faces(bm, faces=bm.faces[:])

    # One planar projection of the front for every face, read the right way from the front: the texture is painted in
    # the layout of the front.
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
    ax0, ax1, az0, az1 = art_rectangle()
    nx0, nx1, nz0, nz1 = NAME_PLATE
    gx0, gx1, gz0, gz1 = TAG
    tx0, tx1, tz0, tz1 = TERMINAL
    fx0 = FUSE[0]
    lx, _, mount_radius = LAMP
    usage_left = LEDS[-1] + LED_SIZE / 2 + 0.02
    x0, x1, z0, z1 = CASE
    layout = {
        "Zone_Illustration": ((ax0 + ax1) / 2, FRONT, (az0 + az1) / 2, ax1 - ax0, az1 - az0),
        "Zone_Nom": ((nx0 + 0.02 + gx0 - 0.02) / 2, PANEL_TOP, (nz0 + nz1) / 2, gx0 - 0.02 - (nx0 + 0.02), nz1 - nz0 - 0.035),
        "Zone_Emplacement": ((gx0 + gx1) / 2, PANEL_TOP, (gz0 + gz1) / 2, gx1 - gx0 - 0.02, gz1 - gz0 - 0.01),
        "Zone_Texte": (0.0, SCREEN, (tz0 + tz1) / 2, tx1 - tx0 - 0.05, tz1 - tz0 - 0.03),
        "Zone_Usage": ((usage_left + fx0 - 0.015) / 2, FRONT, STATUS_Z, fx0 - 0.015 - usage_left, 0.045),
        "Zone_Identifiant": (0.37, FRONT, STATUS_Z, 0.16, 0.032),
        "Zone_Tourments": (0.4, PANEL_TOP, 0.62, 0.3, 0.3),
        "Zone_Dos": ((x0 + x1) / 2, BACK, (z0 + z1) / 2, x1 - x0 - 2 * RIM_WIDTH, z1 - z0 - 2 * RIM_WIDTH),
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

def blur(rng, shape, along, across):
    """Tileable noise, blurred by a Gaussian of `along` texels along X and `across` texels along Z; unit variance."""
    noise = rng.standard_normal(shape)
    fz = np.fft.fftfreq(shape[0])[:, None]
    fx = np.fft.fftfreq(shape[1])[None, :]
    kernel = np.exp(-2 * math.pi ** 2 * ((along * fx) ** 2 + (across * fz) ** 2))
    field = np.real(np.fft.ifft2(np.fft.fft2(noise) * kernel))
    return (field - field.mean()) / field.std()


def box(x, z, x0, x1, z0, z1):
    return (x >= x0) & (x <= x1) & (z >= z0) & (z <= z1)


def rounded_box_distance(x, z, x0, x1, z0, z1, radius):
    """Signed distance to a rounded rectangle, negative inside."""
    cx, cz = (x0 + x1) / 2, (z0 + z1) / 2
    hx, hz = (x1 - x0) / 2 - radius, (z1 - z0) / 2 - radius
    qx, qz = np.abs(x - cx) - hx, np.abs(z - cz) - hz
    return np.hypot(np.maximum(qx, 0), np.maximum(qz, 0)) + np.minimum(np.maximum(qx, qz), 0) - radius


def srgb(hex_colour):
    return np.array([int(hex_colour[i:i + 2], 16) / 255 for i in (1, 3, 5)])


def paint(colour, mask, value, amount=1.0):
    """Lays a colour (3 values, or a per-texel array) over the texels of a mask, blended by amount (0..1)."""
    weight = (mask.astype(np.float64) * amount)[..., None]
    value = np.asarray(value, dtype=np.float64)
    if value.ndim == 1:
        value = value[None, None, :]
    colour[:] = colour * (1 - weight) + value * weight


def module_textures(directory):
    """The front of the module, painted texel by texel in its layout: colour, relief and shine (docs §6.3, §6.5 bis)."""
    rng = np.random.default_rng(7)
    h, w = TEXTURE_HEIGHT, TEXTURE_WIDTH
    # Texel centres in card coordinates, as seen from the front; row 0 is the bottom, as Blender stores pixels.
    x = ((np.arange(w) + 0.5) / w - 0.5)[None, :] * WIDTH + np.zeros((h, 1))
    z = ((np.arange(h) + 0.5) / h - 0.5)[:, None] * HEIGHT + np.zeros((1, w))
    texel = WIDTH / w

    brush = 0.7 * blur(rng, (h, w), 60, 0.8) + 0.3 * blur(rng, (h, w), 10, 0.5)
    soot = blur(rng, (h, w), 70, 70)
    grain = blur(rng, (h, w), 1.2, 1.2)
    scratches = blur(rng, (h, w), 25, 0.6)

    colour = np.zeros((h, w, 3))
    height = np.zeros((h, w))
    metal = np.ones((h, w))
    smooth = np.full((h, w), 0.5)

    # Blackened steel, satin, with soot blotches (§6.3: the inside of the ship is darker and more matte).
    steel = srgb("#2E3236")
    colour[:] = steel * (1 + 0.06 * brush + 0.03 * grain)[..., None] * (1 - 0.18 * np.clip(soot, 0, 3) / 3)[..., None]
    height += 0.15 * brush
    smooth += 0.05 * brush - 0.06 * np.clip(soot, 0, 3) / 3

    # Worn edges: bare metal where hands and the bay rub it, a little rust (§6.3: wear only on edges).
    x0, x1, z0, z1 = CASE
    edge = -rounded_box_distance(x, z, x0, x1, z0, z1, CORNER_RADIUS)
    worn = np.clip(1 - edge / 0.03, 0, 1) * np.clip(0.5 + 0.7 * scratches, 0, 1)
    worn[z > z1] = np.clip(0.4 + 0.6 * scratches[z > z1], 0, 1)  # the handle is held
    paint(colour, worn > 0.55, srgb("#6B7075") * (1 + 0.1 * grain)[..., None], 0.8)
    rust = (worn > 0.3) & (soot > 1.2)
    paint(colour, rust, srgb("#6E3A22"), 0.85)
    metal[rust] = 0.2
    smooth[worn > 0.55] = 0.62
    smooth[rust] = 0.25

    # The handle's grip: grooves across it.
    handle = z > HANDLE[3]
    grooves = handle & (np.mod(x, 0.024) < 0.009)
    paint(colour, grooves, srgb("#16181A"), 0.9)
    height[grooves] -= 0.6

    # The pin connector: brass pins on dark insulation.
    chalf, cz0, cz1, _ = CONNECTOR
    connector = box(x, z, -chalf, chalf, cz0, cz1)
    paint(colour, connector, srgb("#1A1B1D"))
    pins = connector & (np.mod(x + chalf, PIN_PITCH) > 0.012) & (np.mod(x + chalf, PIN_PITCH) < 0.034) & (z < cz1 - 0.012)
    paint(colour, pins, srgb("#C9A24A") * (1 + 0.08 * brush)[..., None])
    height[pins] += 0.5
    smooth[connector] = 0.3
    smooth[pins] = 0.75

    # The name plate and the terminal: near-black faces that carry text (the game's material makes them glassy).
    nx0, nx1, nz0, nz1 = NAME_PLATE
    plate = box(x, z, nx0, nx1, nz0, nz1)
    paint(colour, plate, srgb("#111315") * (1 + 0.15 * grain)[..., None])
    tx0, tx1, tz0, tz1 = TERMINAL
    terminal = box(x, z, tx0, tx1, tz0, tz1)
    rows = np.mod(np.arange(h), 3)[:, None] + np.zeros((1, w))
    glow = np.clip(1 - np.abs(x) / 0.6, 0, 1) * np.clip(1 - np.abs(z - (tz0 + tz1) / 2) / 0.4, 0, 1)
    screen = srgb("#06110A")[None, None, :] * (0.8 + 0.5 * glow)[..., None] * np.where(rows == 0, 0.55, 1.0)[..., None]
    paint(colour, terminal, screen)
    for mask in (plate, terminal):
        metal[mask] = 0.0
        height[mask] = 0.0

    # The slot tag's amber frame, at the right end of the plate.
    gx0, gx1, gz0, gz1 = TAG
    frame = box(x, z, gx0, gx1, gz0, gz1) & ~box(x, z, gx0 + 3 * texel, gx1 - 3 * texel, gz0 + 3 * texel, gz1 - 3 * texel)
    paint(colour, frame, srgb("#E0922F"))

    # Screw heads: bare steel with a cross slot.
    for sx, sz in SCREWS:
        head = np.hypot(x - sx, z - sz) < SCREW_RADIUS
        paint(colour, head, srgb("#7C8187") * (1 + 0.1 * grain)[..., None])
        slot = head & ((np.abs(x - sx) < 0.003) | (np.abs(z - sz) < 0.003))
        paint(colour, slot, srgb("#141516"))
        height[slot] -= 0.8
        smooth[head] = 0.6

    # The vent grille, under the status row.
    vx0, vx1, vz0, vz1, slot_width, pitch = VENT
    vent = box(x, z, vx0, vx1, vz0, vz1) & (np.mod(x - vx0, pitch) < slot_width)
    paint(colour, vent, srgb("#060708"))
    height[vent] -= 1.0
    metal[vent] = 0.0
    smooth[vent] = 0.1

    # The fuse: a glass tube with its amber filament, brass caps (§6.5 bis: usage by the fuse).
    fx0, fx1, fz0, fz1 = FUSE
    tube = box(x, z, fx0 + 0.012, fx1 - 0.012, fz0 + 0.008, fz1 - 0.008)
    paint(colour, tube, srgb("#1C2226"))
    paint(colour, tube & (np.abs(z - (fz0 + fz1) / 2) < 0.0015), srgb("#E0922F"))
    caps = box(x, z, fx0 + 0.004, fx1 - 0.004, fz0 + 0.006, fz1 - 0.006) & ~tube
    paint(colour, caps, srgb("#C9A24A"))
    smooth[tube] = 0.9

    colour = np.clip(colour, 0, 1)
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / 2
    dz = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / 2
    strength = 0.35
    nx, nz, ny = -dx * strength, -dz * strength, np.ones_like(height)
    length = np.sqrt(nx ** 2 + nz ** 2 + ny ** 2)
    ones = np.ones((h, w))
    maps = {
        "Card_BaseColor": (np.dstack([colour, ones]), "sRGB"),
        "Card_Normal": (np.dstack([nx / length * 0.5 + 0.5, nz / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, ones]), "Non-Color"),
        "Card_MetallicSmoothness": (np.dstack([np.clip(metal, 0, 1), 0 * ones, 0 * ones, np.clip(smooth, 0, 1)]), "Non-Color"),
    }
    images = {}
    for name, (pixels, space) in maps.items():
        image = bpy.data.images.new(name, w, h, alpha=True)
        image.colorspace_settings.name = space
        image.pixels.foreach_set(pixels.astype(np.float32).ravel())
        path = os.path.join(directory, name + ".png")
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        image.filepath = path
        images[name] = image
    return images


def material(name, images, roughness, metallic=None, emission=None, textured=True):
    """A Blender preview of the module; the game uses its own materials with the same textures."""
    mat = bpy.data.materials.new(name)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    if textured:
        color = nodes.new("ShaderNodeTexImage")
        color.image = images["Card_BaseColor"]
        links.new(color.outputs["Color"], bsdf.inputs["Base Color"])
        shine = nodes.new("ShaderNodeTexImage")
        shine.image = images["Card_MetallicSmoothness"]
        if metallic is None:
            split = nodes.new("ShaderNodeSeparateColor")
            links.new(shine.outputs["Color"], split.inputs["Color"])
            links.new(split.outputs["Red"], bsdf.inputs["Metallic"])
        rough = nodes.new("ShaderNodeMath")
        rough.operation = "SUBTRACT"
        rough.inputs[0].default_value = 1.0
        links.new(shine.outputs["Alpha"], rough.inputs[1])
        links.new(rough.outputs[0], bsdf.inputs["Roughness"])
        relief = nodes.new("ShaderNodeTexImage")
        relief.image = images["Card_Normal"]
        normal_map = nodes.new("ShaderNodeNormalMap")
        links.new(relief.outputs["Color"], normal_map.inputs["Color"])
        links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])
    else:
        bsdf.inputs["Roughness"].default_value = roughness
    if metallic is not None:
        bsdf.inputs["Metallic"].default_value = metallic
    if emission:
        bsdf.inputs["Base Color"].default_value = (*emission[0], 1.0)
        bsdf.inputs["Emission Color"].default_value = (*emission[0], 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission[1]
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
    images = module_textures(directory)
    mesh.materials[CADRE] = material("Cadre", images, 0.5)
    mesh.materials[PANNEAU] = material("Panneau", images, 0.2, metallic=0.0)
    mesh.materials[LISERE] = material("Lisere", images, 0.3, metallic=0.2, emission=((0.23, 0.48, 0.84), 1.0), textured=False)
    mesh.materials[GEMME] = material("Gemme", images, 0.1, metallic=0.1, emission=((0.23, 0.48, 0.84), 3.0), textured=False)
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
