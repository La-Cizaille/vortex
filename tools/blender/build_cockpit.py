"""Build the player's cockpit model (ARB-90, docs/ANIMATIONS.md section 6) and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_cockpit.py -- art-src/cockpit/Cockpit.blend

Then export it for Unity:

    blender --background --disable-autoexec art-src/cockpit/Cockpit.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Cockpit/Cockpit.fbx --budget 3000

A console under the player's ship, 3.84 x 1 m and 0.1 m deep, the proportions of the player's panel in the interface
(920 x 240 units). It stands upright like a card: width along X, height along Z, depth along Y, its front facing +Y
(-Z in Unity, towards the camera). Seen from the front, from left to right:
- the attack socket, a dark recess framed by a raised bezel, where the attack card lies;
- the name plate, over the hit point gauge (a track and its fill);
- under them, the overcharge toggle switch and the three technology diodes;
- the shield manometer: a dial graduated from 0 to 8, with its needle;
- the defense socket.
A trim in the seat colour runs along the top and the bottom.

The parts the game moves or reads carry fixed names, with their origin where they pivot:
- Remplissage_PV: the gauge's fill, origin at its left end; the game scales it along X (1 = full);
- Aiguille_Bouclier: the needle, origin at the dial's centre; the game turns it around the depth axis, from -120 degrees
  (0) to +120 degrees (8), pointing up at 4;
- Levier_Surcharge: the switch lever, origin at its hinge; the game tilts it around X (up: overcharged);
- Diode_1, Diode_2, Diode_3: the diodes, whose colour and glow the game sets;
- empties Zone_Nom, Zone_PV, Socket_ATK, Socket_DEF: where the game writes the name and the hit points, and lays the
  cards; their X and Z scales give the zone's size.

Materials, plain colours the game may replace: Console, Panneau, Siege (painted in the seat colour, like the ships),
Socket, Piste, Jauge, Aiguille, Encre, Levier, Diode.
"""

import math
import os
import sys

import bpy

WIDTH = 3.84
HEIGHT = 1.0
BACK = -0.06
FRONT = 0.04

SOCKETS = (-1.42, 1.42)  # centres x of the attack and defense sockets, as seen from the front
SOCKET_SIZE = (0.7, 0.93)  # inside of the recess: width, height (a card of the panel is 0.63 x 0.88)
BEZEL = 0.035
BEZEL_TOP = 0.075

NAME_PLATE = (-0.95, 0.14, 0.27, 0.43)  # x0, x1, z0, z1
GAUGE = (-0.95, 0.14, 0.03, 0.19)
GAUGE_INSET = 0.02
DIAL = (0.6, 0.0, 0.38)  # centre x, centre z, radius of the face
DIAL_SWEEP = 120.0  # degrees on each side of straight up
SWITCH = (-0.78, -0.28)  # centre x, z
DIODES = (-0.42, -0.22, -0.02)  # centres x, at z -0.28
DIODE_RADIUS = 0.055
SIDES = 24

COLOURS = {
    "Console": ((0.07, 0.08, 0.1), 0.8, 0.45, None),
    "Panneau": ((0.72, 0.74, 0.78), 0.9, 0.35, None),
    "Siege": ((0.85, 0.85, 0.85), 0.6, 0.4, None),
    "Socket": ((0.025, 0.028, 0.035), 0.5, 0.2, None),
    "Piste": ((0.015, 0.015, 0.02), 0.2, 0.3, None),
    "Jauge": ((0.25, 0.85, 0.35), 0.0, 0.6, (0.25, 0.85, 0.35)),
    "Aiguille": ((0.9, 0.18, 0.12), 0.3, 0.5, None),
    "Encre": ((0.06, 0.06, 0.07), 0.0, 0.3, None),
    "Levier": ((0.75, 0.76, 0.8), 1.0, 0.6, None),
    "Diode": ((0.2, 0.2, 0.22), 0.0, 0.8, None),
}


def material(name):
    found = bpy.data.materials.get(name)
    if found is not None:
        return found
    colour, metallic, smoothness, emission = COLOURS[name]
    made = bpy.data.materials.new(name)
    made.use_nodes = True
    shader = next(n for n in made.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*colour, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = 1.0 - smoothness
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.6
    return made


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
    mesh.materials.append(material(material_name))
    mesh.validate()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = (-ox, oy, oz)
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    fix_normals(obj)
    return obj


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


def empty(name, x, y, z, width, height):
    """A plain-axes empty at (x, y, z) in front-view terms, whose X and Z scales give a zone's size."""
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.location = (-x, y, z)
    obj.scale = (width, 1.0, height)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    half_w, half_h = WIDTH / 2, HEIGHT / 2
    prism("Console", rounded(-half_w, half_w, -half_h, half_h, 0.08), BACK, FRONT, "Console")
    prism("Liseré haut", rectangle(-half_w + 0.12, half_w - 0.12, half_h - 0.06, half_h - 0.035), FRONT, FRONT + 0.012, "Siege")
    prism("Liseré bas", rectangle(-half_w + 0.12, half_w - 0.12, -half_h + 0.035, -half_h + 0.06), FRONT, FRONT + 0.012, "Siege")

    # The sockets: a dark floor framed by a raised bezel, so that it reads as a recess where the card lies.
    for label, cx in zip(("ATK", "DEF"), SOCKETS):
        w, h = SOCKET_SIZE[0] / 2, SOCKET_SIZE[1] / 2
        prism("Socket " + label, rectangle(cx - w, cx + w, -h, h), FRONT, FRONT + 0.002, "Socket")
        prism("Cadre " + label + " haut", rectangle(cx - w - BEZEL, cx + w + BEZEL, h, h + BEZEL), FRONT, BEZEL_TOP, "Console")
        prism("Cadre " + label + " bas", rectangle(cx - w - BEZEL, cx + w + BEZEL, -h - BEZEL, -h), FRONT, BEZEL_TOP, "Console")
        prism("Cadre " + label + " gauche", rectangle(cx - w - BEZEL, cx - w, -h, h), FRONT, BEZEL_TOP, "Console")
        prism("Cadre " + label + " droit", rectangle(cx + w, cx + w + BEZEL, -h, h), FRONT, BEZEL_TOP, "Console")
        empty("Socket_" + label, cx, FRONT + 0.004, 0.0, SOCKET_SIZE[0], SOCKET_SIZE[1])

    # The name plate, then the hit point gauge: a dark track and its fill, which the game scales from its left end.
    x0, x1, z0, z1 = NAME_PLATE
    prism("Plaque du nom", rounded(x0, x1, z0, z1, 0.03), FRONT, FRONT + 0.02, "Panneau")
    empty("Zone_Nom", (x0 + x1) / 2, FRONT + 0.022, (z0 + z1) / 2, x1 - x0 - 0.06, z1 - z0 - 0.03)
    x0, x1, z0, z1 = GAUGE
    prism("Piste PV", rounded(x0, x1, z0, z1, 0.03), FRONT, FRONT + 0.01, "Piste")
    fill_x0, fill_x1 = x0 + GAUGE_INSET, x1 - GAUGE_INSET
    prism("Remplissage_PV", rectangle(fill_x0, fill_x1, z0 + GAUGE_INSET, z1 - GAUGE_INSET), FRONT + 0.01, FRONT + 0.016, "Jauge",
          origin=(fill_x0, FRONT + 0.01, (z0 + z1) / 2))
    empty("Zone_PV", (x0 + x1) / 2, FRONT + 0.02, (z0 + z1) / 2, x1 - x0 - 0.06, z1 - z0)

    # The overcharge switch: a plate, and a lever hinged on it, pointing towards the viewer, tilted down (off).
    sx, sz = SWITCH
    prism("Platine interrupteur", rounded(sx - 0.13, sx + 0.13, sz - 0.14, sz + 0.14, 0.03), FRONT, FRONT + 0.015, "Panneau")
    hinge = FRONT + 0.015
    lever = prism("Levier_Surcharge", circle(sx, sz, 0.025, 8), hinge, hinge + 0.16, "Levier", origin=(sx, hinge, sz))
    knob = prism("Bouton du levier", circle(sx, sz, 0.045, 10), hinge + 0.16, hinge + 0.2, "Levier", origin=(sx, hinge, sz))
    bpy.context.view_layer.update()
    knob.parent = lever
    knob.matrix_parent_inverse = lever.matrix_world.inverted()

    # Off: tilted down. The game tilts it up when the ship holds an overcharge token.
    lever.rotation_euler = (math.radians(-25), 0.0, 0.0)
    bpy.context.view_layer.update()

    # The technology diodes, small domes in a dark ring.
    for number, dx in enumerate(DIODES, start=1):
        prism("Bague diode " + str(number), circle(dx, -0.28, DIODE_RADIUS + 0.02, 12), FRONT, FRONT + 0.01, "Encre")
        prism("Diode_" + str(number), circle(dx, -0.28, DIODE_RADIUS, 12), FRONT + 0.01, FRONT + 0.035, "Diode",
              origin=(dx, FRONT + 0.01, -0.28))

    # The shield manometer: a bezel, a light face, nine marks from 0 to 8, and the needle pointing up (4).
    cx, cz, radius = DIAL
    prism("Bague cadran", circle(cx, cz, radius + 0.04), FRONT, FRONT + 0.03, "Console")
    prism("Cadran", circle(cx, cz, radius), FRONT + 0.03, FRONT + 0.035, "Panneau")
    for mark in range(9):
        angle = math.radians(90 + DIAL_SWEEP - mark * DIAL_SWEEP / 4)
        ux, uz = math.cos(angle), math.sin(angle)
        px, pz = -uz, ux  # across the mark
        inner, outer, half = radius * 0.72, radius * 0.9, 0.012 if mark % 4 else 0.02
        prism("Graduation " + str(mark), [
            (cx + ux * inner - px * half, cz + uz * inner - pz * half),
            (cx + ux * outer - px * half, cz + uz * outer - pz * half),
            (cx + ux * outer + px * half, cz + uz * outer + pz * half),
            (cx + ux * inner + px * half, cz + uz * inner + pz * half),
        ], FRONT + 0.035, FRONT + 0.038, "Encre")
    needle_base = FRONT + 0.04
    prism("Aiguille_Bouclier", [(cx - 0.02, cz - 0.05), (cx + 0.02, cz - 0.05), (cx + 0.006, cz + radius * 0.85), (cx - 0.006, cz + radius * 0.85)],
          needle_base, needle_base + 0.008, "Aiguille", origin=(cx, needle_base, cz))
    prism("Moyeu", circle(cx, cz, 0.035, 12), needle_base, needle_base + 0.016, "Levier")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1:
        raise SystemExit("usage: blender --background --python build_cockpit.py -- <output.blend>")
    output = os.path.abspath(argv[0])
    build()
    os.makedirs(os.path.dirname(output), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=output)
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bpy.data.objects if o.type == "MESH")
    print("Cockpit saved to", output, "-", triangles, "triangles")


main()
