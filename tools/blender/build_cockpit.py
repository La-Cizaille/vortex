"""Build the player's cockpit model (ARB-90, docs/ANIMATIONS.md section 6) and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_cockpit.py -- art-src/cockpit/Cockpit.blend

Then export it for Unity:

    blender --background --disable-autoexec art-src/cockpit/Cockpit.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Cockpit/Cockpit.fbx --budget 3000

A console under the player's ship, 3.84 x 1 m and 0.1 m deep, the proportions of the player's panel in the interface.
It stands upright like a card: width along X, height along Z, depth along Y, its front facing +Y (-Z in Unity, towards
the camera). Seen from the front, from left to right:
- the attack socket, a dark recess framed by a raised bezel, where the attack card lies; a metal clamp comes over the
  card's bottom left corner, so the card looks plugged in;
- the name plate, over the hit point gauge: a glass tube between two metal caps, filled with a glowing liquid;
- under them, the overcharge toggle switch with its diode below, and the three technology diodes;
- the shield manometer: a metal bezel, a face graduated from 0 to 8 with a red zone at 0, the needle, and a glass;
- the defense socket, its clamp over the card's bottom right corner.
A trim in the seat colour runs along the top and the bottom.

The parts the game moves or reads carry fixed names, with their origin where they pivot:
- Remplissage_PV: the liquid, origin at its left end; the game scales it along X (1 = full);
- Aiguille_Bouclier: the needle, origin at the dial's centre; the game turns it around the depth axis, from -120 degrees
  (0) to +120 degrees (8), pointing up at 4;
- Levier_Surcharge: the switch lever, origin at its hinge; the game tilts it around X (up: armed);
- Diode_Surcharge: lit while the ship holds an overcharge token;
- Diode_1, Diode_2, Diode_3: the technology diodes, whose colour and glow the game sets;
- Zone_Rouge: the red zone of the dial, lit while the shield protects nothing;
- Verre_Cadran, Tube_PV: the glass parts, which the game may give its glass material;
- empties Zone_Nom, Zone_PV, Chiffre_0 to Chiffre_8, Socket_ATK, Socket_DEF: where the game writes the name, the hit
  points and the dial's figures, and lays the cards; their X and Z scales give the zone's size.

The card plane: the game sets the console so that the cards lie at Y = CARD_PLANE (in front of the socket floor, behind
the bezel's top), under the clamps.

Materials: Console (dark brushed metal), Panneau, Levier, Aiguille (light brushed metal), Siege (painted in the seat
colour, like the ships), Socket, Piste, Encre, Diode, Jauge (the liquid), Pointe (the needle's tip), Alerte (the red
zone), Verre (glass). The brushed metal is computed here, in the manner of the cards (build_card.py): its colours,
already tinted (Cockpit_Dark_BaseColor, Cockpit_Light_BaseColor) and its relief (Cockpit_Normal) are written next to the
.blend file and plugged straight into the shaders, so the FBX file links them.
"""

import math
import os
import sys

import bpy
import numpy as np

WIDTH = 3.84
HEIGHT = 1.0
BACK = -0.06
FRONT = 0.04
CARD_PLANE = 0.057  # where the cards lie, between the socket floor and the bezel's top

SOCKETS = (-1.42, 1.42)  # centres x of the attack and defense sockets, as seen from the front
SOCKET_SIZE = (0.7, 0.93)  # inside of the recess: width, height (a card of the panel is 0.625 x 0.875)
CARD_SIZE = (0.625, 0.875)
BEZEL = 0.035
BEZEL_TOP = 0.075
CLAMP_LEG = 0.24  # length of each leg of a clamp, from its outer corner
CLAMP_WIDTH = 0.1  # width of a clamp's plate, which covers the card's corner
CLAMP_FOOT = 0.045  # width of the foot that holds the plate, outside the card
CLAMP_FRONT = (0.085, 0.1)  # depth of the plate, in front of the card

NAME_PLATE = (-0.95, 0.14, 0.27, 0.43)  # x0, x1, z0, z1
GAUGE = (-0.95, 0.14, 0.03, 0.19)  # the trough behind the tube
TUBE_RADIUS = 0.075
LIQUID_RADIUS = 0.056
CAP_LENGTH = 0.045
DIAL = (0.6, 0.0, 0.38)  # centre x, centre z, radius of the face
DIAL_SWEEP = 120.0  # degrees on each side of straight up
SWITCH = (-0.78, -0.17)  # centre x, z
SWITCH_DIODE = (-0.78, -0.37, 0.04)  # centre x, z, radius
DIODES = (-0.42, -0.22, -0.02)  # centres x
DIODE_Z = -0.22
DIODE_RADIUS = 0.055
SIDES = 24
TEXTURE_SIZE = 1024

DARK_METAL = (0.11, 0.12, 0.14)
LIGHT_METAL = (0.74, 0.76, 0.8)

# name: (colour, metallic, smoothness, emission, texture)
COLOURS = {
    "Console": (DARK_METAL, 0.85, 0.55, None, "Cockpit_Dark_BaseColor"),
    "Panneau": (LIGHT_METAL, 0.9, 0.5, None, "Cockpit_Light_BaseColor"),
    "Levier": (LIGHT_METAL, 1.0, 0.7, None, "Cockpit_Light_BaseColor"),
    "Aiguille": (LIGHT_METAL, 1.0, 0.8, None, "Cockpit_Light_BaseColor"),
    "Siege": ((0.85, 0.85, 0.85), 0.6, 0.4, None, None),
    "Socket": ((0.025, 0.028, 0.035), 0.5, 0.2, None, None),
    "Piste": ((0.015, 0.015, 0.02), 0.2, 0.3, None, None),
    "Jauge": ((0.25, 0.85, 0.35), 0.0, 0.9, (0.25, 0.85, 0.35), None),
    "Pointe": ((0.9, 0.12, 0.08), 0.3, 0.6, None, None),
    "Alerte": ((0.5, 0.04, 0.03), 0.0, 0.4, None, None),
    "Encre": ((0.06, 0.06, 0.07), 0.0, 0.3, None, None),
    "Diode": ((0.2, 0.2, 0.22), 0.0, 0.8, None, None),
    "Verre": ((0.85, 0.92, 1.0), 0.0, 0.95, None, None),
}


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
    """The brushed metal of the cards, tinted dark and light, and its relief."""
    rng = np.random.default_rng(3)
    size = TEXTURE_SIZE
    # The console is 3.84 times wider than high and its UVs span it whole: streaks shorter along X than on the cards.
    brush = 0.7 * streaks(rng, size, 30, 1.2) + 0.3 * streaks(rng, size, 6, 0.8)
    sheen = streaks(rng, size, 60, 120)
    shade = np.clip(0.88 + 0.05 * brush + 0.035 * sheen, 0, 1)

    height = 0.5 * brush
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / 2
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / 2
    strength = 0.2
    nx, ny, nz = -dx * strength, -dy * strength, np.ones_like(height)
    length = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    one = np.ones_like(shade)

    def tinted(tint):
        return np.dstack([np.clip(shade * tint[0] / 0.88, 0, 1), np.clip(shade * tint[1] / 0.88, 0, 1), np.clip(shade * tint[2] / 0.88, 0, 1), one])

    maps = {
        "Cockpit_Dark_BaseColor": (tinted(DARK_METAL), "sRGB"),
        "Cockpit_Light_BaseColor": (tinted(LIGHT_METAL), "sRGB"),
        "Cockpit_Normal": (np.dstack([nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5, one]), "Non-Color"),
    }
    images = {}
    for name, (pixels, space) in maps.items():
        image = bpy.data.images.new(name, size, size, alpha=False)
        image.colorspace_settings.name = space
        image.pixels.foreach_set(pixels.astype(np.float32).ravel())
        path = os.path.join(directory, name + ".png")
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        image.filepath = path
        images[name] = image
    return images


IMAGES = {}


def material(name):
    found = bpy.data.materials.get(name)
    if found is not None:
        return found
    colour, metallic, smoothness, emission, texture = COLOURS[name]
    made = bpy.data.materials.new(name)
    nodes, links = made.node_tree.nodes, made.node_tree.links
    shader = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*colour, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = 1.0 - smoothness
    if texture is not None:
        # Straight into the shader, so that the FBX file links it; the relief through a normal map node.
        image = nodes.new("ShaderNodeTexImage")
        image.image = IMAGES[texture]
        links.new(image.outputs["Color"], shader.inputs["Base Color"])
        relief = nodes.new("ShaderNodeTexImage")
        relief.image = IMAGES["Cockpit_Normal"]
        normal_map = nodes.new("ShaderNodeNormalMap")
        normal_map.inputs["Strength"].default_value = 0.6
        links.new(relief.outputs["Color"], normal_map.inputs["Color"])
        links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.8
    if name == "Verre":
        shader.inputs["Alpha"].default_value = 0.15
        for attribute, value in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
            try:
                setattr(made, attribute, value)
            except (AttributeError, TypeError):
                pass
    made.diffuse_color = (*colour, 0.15 if name == "Verre" else 1.0)
    return made


# ----------------------------------------------------------------------------------------------------------- shapes

def front_uv(obj):
    """One planar projection of the front for every face, read the right way from the front: the streaks run along X."""
    mesh = obj.data
    layer = mesh.uv_layers.new(name="UVMap")
    offset = obj.location
    for loop in mesh.loops:
        co = mesh.vertices[loop.vertex_index].co
        x, z = -(co.x + offset.x), co.z + offset.z  # front-view x, as the helpers take it
        layer.data[loop.index].uv = (0.5 + x / WIDTH, 0.5 + z / HEIGHT)


def finish(obj, name, material_name, parent):
    obj.data.materials.append(material(material_name))
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    fix_normals(obj)
    front_uv(obj)
    return obj


def prism(name, outline, y0, y1, material_name, origin=(0.0, 0.0, 0.0), parent=None):
    """An object made of a flat outline (x right as seen from the front, z up) pushed from depth y0 to y1.

    Seen from the front (+Y), Blender's +X points to the left: x is mirrored when the mesh is built. The vertices are
    relative to origin, given in the same front-view terms, so that a moving part turns around it.
    """
    ox, oy, oz = origin
    count = len(outline)
    vertices = [(-(x - ox), y0 - oy, z - oz) for x, z in outline] + [(-(x - ox), y1 - oy, z - oz) for x, z in outline]
    # The outline is counter-clockwise seen from the front; mirrored, it is clockwise, so the caps are wound to face out.
    faces = [tuple(range(count)), tuple(reversed(range(count, 2 * count)))]
    for i in range(count):
        j = (i + 1) % count
        faces.append((i, count + i, count + j, j))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.validate()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = (-ox, oy, oz)
    return finish(obj, name, material_name, parent)


def cylinder_x(name, x0, x1, y, z, radius, material_name, origin=None, sides=16):
    """A cylinder along X (front view) from x0 to x1, its axis at depth y and height z; origin defaults to its middle."""
    ox, oy, oz = origin if origin is not None else ((x0 + x1) / 2, y, z)
    ring = [(y + radius * math.cos(2 * math.pi * i / sides), z + radius * math.sin(2 * math.pi * i / sides)) for i in range(sides)]
    vertices = [(-(x0 - ox), cy - oy, cz - oz) for cy, cz in ring] + [(-(x1 - ox), cy - oy, cz - oz) for cy, cz in ring]
    faces = [tuple(range(sides)), tuple(reversed(range(sides, 2 * sides)))]
    for i in range(sides):
        j = (i + 1) % sides
        faces.append((i, j, sides + j, sides + i))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.validate()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = (-ox, oy, oz)
    return finish(obj, name, material_name, None)


def fix_normals(obj):
    bpy.context.view_layer.objects.active = obj
    for other in bpy.context.selected_objects:
        other.select_set(False)
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


def rectangle(x0, x1, z0, z1):
    return [(x0, z0), (x1, z0), (x1, z1), (x0, z1)]


def rounded(x0, x1, z0, z1, radius, segments=4):
    centres = [(x1 - radius, z0 + radius, -90), (x1 - radius, z1 - radius, 0), (x0 + radius, z1 - radius, 90), (x0 + radius, z0 + radius, 180)]
    points = []
    for cx, cz, start in centres:
        for step in range(segments + 1):
            angle = math.radians(start + 90 * step / segments)
            points.append((cx + radius * math.cos(angle), cz + radius * math.sin(angle)))
    return points


def circle(cx, cz, radius, sides=SIDES):
    return [(cx + radius * math.cos(2 * math.pi * i / sides), cz + radius * math.sin(2 * math.pi * i / sides)) for i in range(sides)]


def arc_band(cx, cz, inner, outer, start, end, steps=6):
    """A band of the dial between two radii, from angle start to end (degrees, counter-clockwise from the right)."""
    angles = [math.radians(start + (end - start) * i / steps) for i in range(steps + 1)]
    outside = [(cx + outer * math.cos(a), cz + outer * math.sin(a)) for a in angles]
    inside = [(cx + inner * math.cos(a), cz + inner * math.sin(a)) for a in reversed(angles)]
    return outside + inside


def ell(corner_x, corner_z, leg, width, towards_right):
    """An L seen from the front, its outer corner at the bottom, one leg along X (right or left), the other up."""
    points = [(0.0, 0.0), (leg, 0.0), (leg, width), (width, width), (width, leg), (0.0, leg)]
    if towards_right:
        return [(corner_x + x, corner_z + z) for x, z in points]
    return [(corner_x - x, corner_z + z) for x, z in reversed(points)]  # mirrored: reversed to stay counter-clockwise


def empty(name, x, y, z, width, height):
    """A plain-axes empty at (x, y, z) in front-view terms, whose X and Z scales give a zone's size."""
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.location = (-x, y, z)
    obj.scale = (width, 1.0, height)
    bpy.context.scene.collection.objects.link(obj)
    return obj


# ------------------------------------------------------------------------------------------------------------ parts

def sockets():
    """The sockets: a dark floor framed by a raised bezel, and a clamp over the card's outer bottom corner."""
    for label, cx in zip(("ATK", "DEF"), SOCKETS):
        w, h = SOCKET_SIZE[0] / 2, SOCKET_SIZE[1] / 2
        prism("Socket " + label, rectangle(cx - w, cx + w, -h, h), FRONT, FRONT + 0.002, "Socket")
        prism("Cadre " + label + " haut", rectangle(cx - w - BEZEL, cx + w + BEZEL, h, h + BEZEL), FRONT, BEZEL_TOP, "Console")
        prism("Cadre " + label + " bas", rectangle(cx - w - BEZEL, cx + w + BEZEL, -h - BEZEL, -h), FRONT, BEZEL_TOP, "Console")
        prism("Cadre " + label + " gauche", rectangle(cx - w - BEZEL, cx - w, -h, h), FRONT, BEZEL_TOP, "Console")
        prism("Cadre " + label + " droit", rectangle(cx + w, cx + w + BEZEL, -h, h), FRONT, BEZEL_TOP, "Console")
        empty("Socket_" + label, cx, CARD_PLANE, 0.0, SOCKET_SIZE[0], SOCKET_SIZE[1])

        # The clamp: its outer corner on the bezel, a foot outside the card up to the plate, the plate over the card's
        # corner. Attack: bottom left; defense: bottom right.
        right = label == "ATK"
        side = -1 if right else 1
        corner_x, corner_z = cx + side * (w + BEZEL / 2), -h - BEZEL / 2
        prism("Pied attache " + label, ell(corner_x, corner_z, CLAMP_LEG, CLAMP_FOOT, right), BEZEL_TOP, CLAMP_FRONT[0], "Levier")
        prism("Attache_" + label, ell(corner_x, corner_z, CLAMP_LEG, CLAMP_WIDTH, right), CLAMP_FRONT[0], CLAMP_FRONT[1], "Levier")
        rivet_x = corner_x - side * CLAMP_WIDTH / 2
        prism("Rivet " + label, circle(rivet_x, corner_z + CLAMP_WIDTH / 2, 0.018, 8), CLAMP_FRONT[1], CLAMP_FRONT[1] + 0.006, "Console")


def gauge():
    """The name plate, then the hit point gauge: a glass tube between metal caps, and the liquid the game scales."""
    x0, x1, z0, z1 = NAME_PLATE
    prism("Plaque du nom", rounded(x0, x1, z0, z1, 0.03), FRONT, FRONT + 0.02, "Panneau")
    empty("Zone_Nom", (x0 + x1) / 2, FRONT + 0.022, (z0 + z1) / 2, x1 - x0 - 0.06, z1 - z0 - 0.03)

    x0, x1, z0, z1 = GAUGE
    zc = (z0 + z1) / 2
    axis = FRONT + TUBE_RADIUS + 0.005
    prism("Piste PV", rounded(x0, x1, z0, z1, 0.03), FRONT, FRONT + 0.01, "Piste")
    inside0, inside1 = x0 + CAP_LENGTH, x1 - CAP_LENGTH
    cylinder_x("Embout gauche", x0, inside0, axis, zc, TUBE_RADIUS + 0.012, "Levier")
    cylinder_x("Embout droit", inside1, x1, axis, zc, TUBE_RADIUS + 0.012, "Levier")
    cylinder_x("Tube_PV", inside0, inside1, axis, zc, TUBE_RADIUS, "Verre")
    cylinder_x("Remplissage_PV", inside0, inside1, axis, zc, LIQUID_RADIUS, "Jauge", origin=(inside0, axis, zc))
    empty("Zone_PV", (x0 + x1) / 2, axis + TUBE_RADIUS + 0.01, zc, inside1 - inside0 - 0.1, z1 - z0 - 0.02)


def switch_and_diodes():
    """The overcharge switch, its diode under it, and the technology diodes."""
    sx, sz = SWITCH
    prism("Platine interrupteur", rounded(sx - 0.12, sx + 0.12, sz - 0.11, sz + 0.11, 0.03), FRONT, FRONT + 0.015, "Panneau")
    hinge = FRONT + 0.015
    lever = prism("Levier_Surcharge", circle(sx, sz, 0.025, 8), hinge, hinge + 0.16, "Levier", origin=(sx, hinge, sz))
    knob = prism("Bouton du levier", circle(sx, sz, 0.045, 10), hinge + 0.16, hinge + 0.2, "Levier", origin=(sx, hinge, sz))
    bpy.context.view_layer.update()
    knob.parent = lever
    knob.matrix_parent_inverse = lever.matrix_world.inverted()

    # Down: disarmed. The game tilts it up while the token is armed.
    lever.rotation_euler = (math.radians(-25), 0.0, 0.0)
    bpy.context.view_layer.update()

    dx, dz, radius = SWITCH_DIODE
    prism("Bague diode surcharge", circle(dx, dz, radius + 0.02, 12), FRONT, FRONT + 0.01, "Encre")
    prism("Diode_Surcharge", circle(dx, dz, radius, 12), FRONT + 0.01, FRONT + 0.035, "Diode", origin=(dx, FRONT + 0.01, dz))

    for number, x in enumerate(DIODES, start=1):
        prism("Bague diode " + str(number), circle(x, DIODE_Z, DIODE_RADIUS + 0.02, 12), FRONT, FRONT + 0.01, "Encre")
        prism("Diode_" + str(number), circle(x, DIODE_Z, DIODE_RADIUS, 12), FRONT + 0.01, FRONT + 0.035, "Diode",
              origin=(x, FRONT + 0.01, DIODE_Z))


def manometer():
    """A metal bezel, a light face, a red zone at 0, nine marks and their figures, the needle pointing up (4), a glass."""
    cx, cz, radius = DIAL
    prism("Fond cadran", circle(cx, cz, radius + 0.045), FRONT, FRONT + 0.03, "Console")
    for half, start in (("haut", 0), ("bas", 180)):
        prism("Bague cadran " + half, arc_band(cx, cz, radius, radius + 0.045, start, start + 180, 12), FRONT, FRONT + 0.075, "Levier")
    prism("Cadran", circle(cx, cz, radius), FRONT + 0.03, FRONT + 0.035, "Panneau")

    def angle_of(mark):
        return 90 + DIAL_SWEEP - mark * DIAL_SWEEP / 4

    prism("Zone_Rouge", arc_band(cx, cz, radius * 0.72, radius * 0.97, angle_of(0) + 3, angle_of(0.8), 8), FRONT + 0.035, FRONT + 0.037, "Alerte")
    for mark in range(9):
        angle = math.radians(angle_of(mark))
        ux, uz = math.cos(angle), math.sin(angle)
        px, pz = -uz, ux  # across the mark
        inner, outer, half = radius * 0.74, radius * 0.92, 0.02 if mark % 4 == 0 else 0.012
        prism("Graduation " + str(mark), [
            (cx + ux * inner - px * half, cz + uz * inner - pz * half),
            (cx + ux * outer - px * half, cz + uz * outer - pz * half),
            (cx + ux * outer + px * half, cz + uz * outer + pz * half),
            (cx + ux * inner + px * half, cz + uz * inner + pz * half),
        ], FRONT + 0.037, FRONT + 0.039, "Encre")
        figure = radius * 0.55
        empty("Chiffre_" + str(mark), cx + ux * figure, FRONT + 0.03, cz + uz * figure, 0.1, 0.075)

    needle_base = FRONT + 0.042
    needle = prism("Aiguille_Bouclier", [(cx - 0.022, cz - 0.06), (cx + 0.022, cz - 0.06), (cx + 0.007, cz + radius * 0.88), (cx - 0.007, cz + radius * 0.88)],
                   needle_base, needle_base + 0.008, "Aiguille", origin=(cx, needle_base, cz))
    tip = prism("Pointe aiguille", [(cx - 0.0095, cz + radius * 0.62), (cx + 0.0095, cz + radius * 0.62), (cx + 0.0072, cz + radius * 0.885), (cx - 0.0072, cz + radius * 0.885)],
                needle_base + 0.008, needle_base + 0.011, "Pointe", origin=(cx, needle_base, cz))
    bpy.context.view_layer.update()
    tip.parent = needle
    tip.matrix_parent_inverse = needle.matrix_world.inverted()
    prism("Moyeu", circle(cx, cz, 0.038, 12), needle_base, needle_base + 0.02, "Levier")
    prism("Verre_Cadran", circle(cx, cz, radius + 0.01), FRONT + 0.068, FRONT + 0.072, "Verre")


def build(directory):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    IMAGES.update(brushed_metal(directory))

    half_w, half_h = WIDTH / 2, HEIGHT / 2
    prism("Console", rounded(-half_w, half_w, -half_h, half_h, 0.08), BACK, FRONT, "Console")
    prism("Liseré haut", rectangle(-half_w + 0.12, half_w - 0.12, half_h - 0.06, half_h - 0.035), FRONT, FRONT + 0.012, "Siege")
    prism("Liseré bas", rectangle(-half_w + 0.12, half_w - 0.12, -half_h + 0.035, -half_h + 0.06), FRONT, FRONT + 0.012, "Siege")
    sockets()
    gauge()
    switch_and_diodes()
    manometer()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1:
        raise SystemExit("usage: blender --background --python build_cockpit.py -- <output.blend>")
    output = os.path.abspath(argv[0])
    directory = os.path.dirname(output)
    os.makedirs(directory, exist_ok=True)
    build(directory)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=output, relative_remap=True)
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.data.objects if o.type == "MESH")
    print("Cockpit saved to", output, "-", triangles, "triangles")


main()
