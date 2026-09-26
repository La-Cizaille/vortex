"""Build the player's cockpit model (ARB-90, ARB-92, docs/ANIMATIONS.md section 6) in the art direction of
docs/DIRECTION_ARTISTIQUE.md (§6.3, §6.4, §6.5 bis: blackened metal, visible machinery, bays for the modules), and save
it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_cockpit.py -- art-src/cockpit/Cockpit.blend

Then export it for Unity:

    blender --background --disable-autoexec art-src/cockpit/Cockpit.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Cockpit/Cockpit.fbx --budget 4000

A console under the player's ship, 3.84 x 1 m and 0.1 m deep, the proportions of the player's panel in the interface.
It stands upright like a card: width along X, height along Z, depth along Y, its front facing +Y (-Z in Unity, towards
the camera). Seen from the front, from left to right:
- a grab handle and bolts on the console's end;
- the attack bay, where the attack module is plugged: a dark recess in a frame painted in the seat colour, a pin socket
  at the bottom that takes the module's connector, and a steel clamp over the module's bottom left corner;
- an armoured conduit, from top to bottom;
- the name plate, a painted plate, over the hit point gauge: a glass tube between two metal caps, filled with a glowing
  liquid;
- under them, the overcharge toggle switch on a plate in hazard stripes, with its diode below, the three technology
  diodes, and a valve with its handwheel;
- the shield manometer: a metal bezel, a face graduated from 0 to 8 with a red zone at 0, the needle, and a glass;
- the defense bay, its clamp over the module's bottom right corner; the other end's handle and bolts.
A strip painted in the seat colour runs along the top, with stencil markings; a bundle of sheathed cables, held by
steel clips, runs along the bottom from one bay to the other.

The parts the game moves or reads carry fixed names, with their origin where they pivot:
- Remplissage_PV: the liquid, origin at its left end; the game scales it along X (1 = full);
- Aiguille_Bouclier: the needle, origin at the dial's centre; the game turns it around the depth axis, from -120 degrees
  (0) to +120 degrees (8), pointing up at 4;
- Levier_Surcharge: the switch lever, origin at its hinge; the game tilts it around X (up: armed);
- Diode_Surcharge: lit while the ship holds an overcharge token;
- Diode_1, Diode_2, Diode_3: the technology diodes, whose colour and glow the game sets;
- Zone_Rouge: the red zone of the dial, lit while the shield protects nothing;
- Verre_Cadran, Tube_PV: the glass parts, which the game may give its glass material;
- Console: the console's body alone, whose box answers a touch (it stays behind the modules);
- empties Zone_Nom, Zone_PV, Chiffre_0 to Chiffre_8, Socket_ATK, Socket_DEF: where the game writes the name, the hit
  points and the dial's figures, and lays the cards; their X and Z scales give the zone's size.
Every other part is joined into one object, Habillage, so the console draws in few calls.

The card plane: the game sets the console so that the cards lie at Y = CARD_PLANE (in front of the bay floor, behind
the frame's top), under the clamps.

Materials: Console (blackened varnished steel), Siege (painted in the seat colour, like the ships), Panneau (painted
plates: the name plate, the dial's face, the switch plate), Socket (bay floor), Piste (gauge trough), Levier and
Aiguille (worn bare steel), Gaine and Gaine_Rouge (cable sheaths), Encre, Diode, Jauge (the liquid), Pointe (the
needle's tip), Alerte (the red zone), Verre (glass). The console, painted and floor materials share one texture drawn
here in the front view (every part's UVs are a front projection): chipped edges showing bare metal, rust streaks, grime
in the hollows, rivets, scratches, hazard stripes and stencil markings in the art direction's stencil font. The steel
parts have their own brushed texture. The images are written next to the .blend file and plugged straight into the
shaders, so the FBX file links them.
"""

import math
import os
import shutil
import sys
import tempfile

import bpy
import mathutils
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import front_view as fv  # noqa: E402  (next to this script)

WIDTH = 3.84
HEIGHT = 1.0
BACK = -0.06
FRONT = 0.04
CARD_PLANE = 0.057  # where the cards lie, between the bay floor and the frame's top

SOCKETS = (-1.42, 1.42)  # centres x of the attack and defense bays, as seen from the front
SOCKET_SIZE = (0.7, 0.93)  # inside of the recess: width, height (a card of the panel is 0.625 x 0.875)
BEZEL = 0.035
BEZEL_TOP = 0.075
CLAMP_LEG = 0.24  # length of each leg of a clamp, from its outer corner
CLAMP_WIDTH = 0.1  # width of a clamp's plate, which covers the card's corner
CLAMP_FOOT = 0.045  # width of the foot that holds the plate, outside the card
CLAMP_FRONT = (0.085, 0.1)  # depth of the plate, in front of the card
# The pin socket at the bottom of a bay: the module's connector (build_card.py, 0.76 of the card's width, at its very
# bottom) goes into it, so its top covers the connector's lower half.
PIN_SOCKET = (0.26, -0.425, 0.068)  # half-width, top z, front y

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
VALVE = (0.1, -0.35, 0.058)  # centre x, z, radius of the handwheel
CONDUIT = (-0.99, 0.021)  # x, radius
TOP_STRIP = (-0.955, 1.0, 0.44, 0.49)  # x0, x1, z0, z1
CABLES = [(-0.44, 0.009, "Gaine"), (-0.458, 0.014, "Gaine"), (-0.478, 0.011, "Gaine_Rouge")]  # z, radius, material
CLIPS = (-0.62, -0.1, 0.42, 0.9)  # x of the cable clips
END_STRIP = 1.865  # x of the handles and bolts on both ends, beyond the bays
SIDES = 24

# Texture of the front view: square texels, 1.9 mm each.
TEX_W, TEX_H = 2048, 512
STEEL_W, STEEL_H = 1024, 256

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STENCIL_FONT = os.path.join(REPO, "unity", "Assets", "_Vortex", "Art", "Fonts", "BigShouldersStencil", "BigShouldersStencilDisplay-Black.ttf")
# Stencil markings on the seat strip (§6.3): text, x0, x1, z0, z1 in the front view.
MARKINGS = [
    ("0427", -0.9, -0.66, 0.449, 0.482),
    ("GARANTIE ANNULÉE", 0.32, 0.96, 0.449, 0.482),
]

# Linear colours (the texture is written in sRGB at the end).
BLACKENED = 0.03
BARE_STEEL = np.array([0.15, 0.155, 0.165])
RUST = np.array([0.1, 0.028, 0.01])
PAINT = 0.55  # the seat paint, which the game multiplies by the seat colour
BONE = np.array([0.62, 0.55, 0.39])  # #CFC4A8
DIAL_FACE = np.array([0.68, 0.62, 0.47])
HAZARD = np.array([0.69, 0.37, 0.006])  # #D9A514
INK = np.array([0.007, 0.0065, 0.006])  # #141312
BRASS = np.array([0.45, 0.3, 0.08])

# name: (colour, metallic, smoothness, emission, texture)
COLOURS = {
    "Console": ((1.0, 1.0, 1.0), 0.7, 0.55, None, "Cockpit"),
    "Siege": ((1.0, 1.0, 1.0), 0.25, 0.62, None, "Cockpit"),
    "Panneau": ((1.0, 1.0, 1.0), 0.1, 0.35, None, "Cockpit"),
    "Socket": ((1.0, 1.0, 1.0), 0.4, 0.2, None, "Cockpit"),
    "Piste": ((1.0, 1.0, 1.0), 0.3, 0.3, None, "Cockpit"),
    "Levier": ((1.0, 1.0, 1.0), 0.9, 0.6, None, "Cockpit_Acier"),
    "Aiguille": ((1.0, 1.0, 1.0), 0.9, 0.7, None, "Cockpit_Acier"),
    "Gaine": ((0.018, 0.018, 0.02), 0.0, 0.35, None, None),
    "Gaine_Rouge": ((0.16, 0.03, 0.018), 0.0, 0.35, None, None),
    "Jauge": ((0.25, 0.85, 0.35), 0.0, 0.9, (0.25, 0.85, 0.35), None),
    "Pointe": ((0.9, 0.12, 0.08), 0.3, 0.6, None, None),
    "Alerte": ((0.5, 0.04, 0.03), 0.0, 0.4, None, None),
    "Encre": ((0.02, 0.02, 0.022), 0.3, 0.3, None, None),
    "Diode": ((0.2, 0.2, 0.22), 0.0, 0.8, None, None),
    "Verre": ((0.85, 0.92, 1.0), 0.0, 0.95, None, None),
}
ATLAS_MATERIALS = ("Console", "Siege", "Panneau", "Socket", "Piste")

# Parts the game reads or moves, kept apart; everything else joins Habillage.
KEPT = {"Console", "Remplissage_PV", "Aiguille_Bouclier", "Pointe aiguille", "Levier_Surcharge", "Bouton du levier",
        "Diode_Surcharge", "Diode_1", "Diode_2", "Diode_3", "Zone_Rouge", "Verre_Cadran", "Tube_PV"}
MOVING = {"Aiguille_Bouclier", "Pointe aiguille", "Levier_Surcharge", "Bouton du levier", "Remplissage_PV"}

IMAGES = {}


# ----------------------------------------------------------------------------------------------------------- materials

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
        image.image = IMAGES[texture + "_BaseColor"]
        links.new(image.outputs["Color"], shader.inputs["Base Color"])
        relief = nodes.new("ShaderNodeTexImage")
        relief.image = IMAGES[texture + "_Normal"]
        normal_map = nodes.new("ShaderNodeNormalMap")
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


def new_images():
    for name, width, height in (("Cockpit", TEX_W, TEX_H), ("Cockpit_Acier", STEEL_W, STEEL_H)):
        for image in fv.textured_images(name, width, height):
            IMAGES[image.name] = image


CANVAS = fv.Canvas(WIDTH, HEIGHT, TEX_W, TEX_H)
MODEL = fv.Model(CANVAS, material)


# ------------------------------------------------------------------------------------------------------------ parts

def bays():
    """The bays: a dark floor in a frame painted in the seat colour, a pin socket at the bottom, and a steel clamp
    over the module's outer bottom corner."""
    for label, cx in zip(("ATK", "DEF"), SOCKETS):
        w, h = SOCKET_SIZE[0] / 2, SOCKET_SIZE[1] / 2
        MODEL.prism("Socket " + label, fv.rectangle(cx - w, cx + w, -h, h), FRONT, FRONT + 0.002, "Socket")
        MODEL.prism("Cadre " + label + " haut", fv.rectangle(cx - w - BEZEL, cx + w + BEZEL, h, h + BEZEL), FRONT, BEZEL_TOP, "Siege")
        MODEL.prism("Cadre " + label + " bas", fv.rectangle(cx - w - BEZEL, cx + w + BEZEL, -h - BEZEL, -h), FRONT, BEZEL_TOP, "Siege")
        MODEL.prism("Cadre " + label + " gauche", fv.rectangle(cx - w - BEZEL, cx - w, -h, h), FRONT, BEZEL_TOP, "Siege")
        MODEL.prism("Cadre " + label + " droit", fv.rectangle(cx + w, cx + w + BEZEL, -h, h), FRONT, BEZEL_TOP, "Siege")
        half, top, front = PIN_SOCKET
        MODEL.prism("Prise " + label, fv.chamfered(cx - half, cx + half, -h, top, 0.012), FRONT, front, "Console")
        MODEL.empty("Socket_" + label, cx, CARD_PLANE, 0.0, SOCKET_SIZE[0], SOCKET_SIZE[1])

        # The clamp: its outer corner on the frame, a foot outside the card up to the plate, the plate over the card's
        # corner. Attack: bottom left; defense: bottom right.
        right = label == "ATK"
        side = -1 if right else 1
        corner_x, corner_z = cx + side * (w + BEZEL / 2), -h - BEZEL / 2
        MODEL.prism("Pied attache " + label, fv.ell(corner_x, corner_z, CLAMP_LEG, CLAMP_FOOT, right), BEZEL_TOP, CLAMP_FRONT[0], "Levier")
        MODEL.prism("Attache " + label, fv.ell(corner_x, corner_z, CLAMP_LEG, CLAMP_WIDTH, right), CLAMP_FRONT[0], CLAMP_FRONT[1], "Levier")
        rivet_x = corner_x - side * CLAMP_WIDTH / 2
        MODEL.prism("Rivet " + label, fv.circle(rivet_x, corner_z + CLAMP_WIDTH / 2, 0.018, 6), CLAMP_FRONT[1], CLAMP_FRONT[1] + 0.008, "Levier")


def gauge():
    """The name plate, then the hit point gauge: a glass tube between metal caps, and the liquid the game scales."""
    x0, x1, z0, z1 = NAME_PLATE
    MODEL.prism("Plaque du nom", fv.chamfered(x0, x1, z0, z1, 0.025), FRONT, FRONT + 0.02, "Panneau")
    MODEL.empty("Zone_Nom", (x0 + x1) / 2, FRONT + 0.022, (z0 + z1) / 2, x1 - x0 - 0.06, z1 - z0 - 0.03)

    x0, x1, z0, z1 = GAUGE
    zc = (z0 + z1) / 2
    axis = FRONT + TUBE_RADIUS + 0.005
    MODEL.prism("Piste PV", fv.rounded(x0, x1, z0, z1, 0.03), FRONT, FRONT + 0.01, "Piste")
    inside0, inside1 = x0 + CAP_LENGTH, x1 - CAP_LENGTH
    MODEL.cylinder_x("Embout gauche", x0, inside0, axis, zc, TUBE_RADIUS + 0.012, "Levier", sides=8)
    MODEL.cylinder_x("Embout droit", inside1, x1, axis, zc, TUBE_RADIUS + 0.012, "Levier", sides=8)
    MODEL.cylinder_x("Tube_PV", inside0, inside1, axis, zc, TUBE_RADIUS, "Verre")
    MODEL.cylinder_x("Remplissage_PV", inside0, inside1, axis, zc, LIQUID_RADIUS, "Jauge", origin=(inside0, axis, zc))
    MODEL.empty("Zone_PV", (x0 + x1) / 2, axis + TUBE_RADIUS + 0.01, zc, inside1 - inside0 - 0.1, z1 - z0 - 0.02)


def switch_and_diodes():
    """The overcharge switch on its hazard plate, its diode under it, and the technology diodes."""
    sx, sz = SWITCH
    MODEL.prism("Platine interrupteur", fv.chamfered(sx - 0.12, sx + 0.12, sz - 0.11, sz + 0.11, 0.025), FRONT, FRONT + 0.015, "Panneau")
    hinge = FRONT + 0.015
    lever = MODEL.prism("Levier_Surcharge", fv.circle(sx, sz, 0.025, 8), hinge, hinge + 0.16, "Levier", origin=(sx, hinge, sz))
    knob = MODEL.prism("Bouton du levier", fv.circle(sx, sz, 0.045, 10), hinge + 0.16, hinge + 0.2, "Levier", origin=(sx, hinge, sz))
    bpy.context.view_layer.update()
    knob.parent = lever
    knob.matrix_parent_inverse = lever.matrix_world.inverted()

    # Down: disarmed. The game tilts it up while the token is armed.
    lever.rotation_euler = (math.radians(-25), 0.0, 0.0)
    bpy.context.view_layer.update()

    dx, dz, radius = SWITCH_DIODE
    MODEL.prism("Bague diode surcharge", fv.circle(dx, dz, radius + 0.02, 12), FRONT, FRONT + 0.012, "Levier")
    MODEL.prism("Diode_Surcharge", fv.circle(dx, dz, radius, 12), FRONT + 0.01, FRONT + 0.035, "Diode", origin=(dx, FRONT + 0.01, dz))

    for number, x in enumerate(DIODES, start=1):
        MODEL.prism("Bague diode " + str(number), fv.circle(x, DIODE_Z, DIODE_RADIUS + 0.02, 12), FRONT, FRONT + 0.012, "Levier")
        MODEL.prism("Diode_" + str(number), fv.circle(x, DIODE_Z, DIODE_RADIUS, 12), FRONT + 0.01, FRONT + 0.035, "Diode",
              origin=(x, FRONT + 0.01, DIODE_Z))


def manometer():
    """A metal bezel, a painted face, a red zone at 0, nine marks and their figures, the needle pointing up (4), a glass."""
    cx, cz, radius = DIAL
    MODEL.prism("Fond cadran", fv.circle(cx, cz, radius + 0.045), FRONT, FRONT + 0.03, "Console")
    for half, start in (("haut", 0), ("bas", 180)):
        MODEL.prism("Bague cadran " + half, fv.arc_band(cx, cz, radius, radius + 0.045, start, start + 180, 12), FRONT, FRONT + 0.075, "Levier")
    for angle in range(45, 360, 90):
        bx, bz = cx + (radius + 0.0225) * math.cos(math.radians(angle)), cz + (radius + 0.0225) * math.sin(math.radians(angle))
        MODEL.prism("Vis cadran %d" % angle, fv.circle(bx, bz, 0.013, 6), FRONT + 0.075, FRONT + 0.082, "Console")
    MODEL.prism("Cadran", fv.circle(cx, cz, radius), FRONT + 0.03, FRONT + 0.035, "Panneau")

    def angle_of(mark):
        return 90 + DIAL_SWEEP - mark * DIAL_SWEEP / 4

    MODEL.prism("Zone_Rouge", fv.arc_band(cx, cz, radius * 0.72, radius * 0.97, angle_of(0) + 3, angle_of(0.8), 8), FRONT + 0.035, FRONT + 0.037, "Alerte")
    for mark in range(9):
        angle = math.radians(angle_of(mark))
        ux, uz = math.cos(angle), math.sin(angle)
        px, pz = -uz, ux  # across the mark
        inner, outer, half = radius * 0.74, radius * 0.92, 0.02 if mark % 4 == 0 else 0.012
        MODEL.prism("Graduation " + str(mark), [
            (cx + ux * inner - px * half, cz + uz * inner - pz * half),
            (cx + ux * outer - px * half, cz + uz * outer - pz * half),
            (cx + ux * outer + px * half, cz + uz * outer + pz * half),
            (cx + ux * inner + px * half, cz + uz * inner + pz * half),
        ], FRONT + 0.037, FRONT + 0.039, "Encre")
        figure = radius * 0.55
        MODEL.empty("Chiffre_" + str(mark), cx + ux * figure, FRONT + 0.03, cz + uz * figure, 0.1, 0.075)

    needle_base = FRONT + 0.042
    needle = MODEL.prism("Aiguille_Bouclier", [(cx - 0.022, cz - 0.06), (cx + 0.022, cz - 0.06), (cx + 0.007, cz + radius * 0.88), (cx - 0.007, cz + radius * 0.88)],
                   needle_base, needle_base + 0.008, "Aiguille", origin=(cx, needle_base, cz))
    tip = MODEL.prism("Pointe aiguille", [(cx - 0.0095, cz + radius * 0.62), (cx + 0.0095, cz + radius * 0.62), (cx + 0.0072, cz + radius * 0.885), (cx - 0.0072, cz + radius * 0.885)],
                needle_base + 0.008, needle_base + 0.011, "Pointe", origin=(cx, needle_base, cz))
    bpy.context.view_layer.update()
    tip.parent = needle
    tip.matrix_parent_inverse = needle.matrix_world.inverted()
    MODEL.prism("Moyeu", fv.circle(cx, cz, 0.038, 12), needle_base, needle_base + 0.02, "Levier")
    MODEL.prism("Verre_Cadran", fv.circle(cx, cz, radius + 0.01), FRONT + 0.068, FRONT + 0.072, "Verre")


def machinery():
    """The machinery (§6.3): the seat strip, the cable bundle and its clips, the conduit, the valve, the end handles."""
    x0, x1, z0, z1 = TOP_STRIP
    MODEL.prism("Liseré haut", fv.chamfered(x0, x1, z0, z1, 0.012), FRONT, FRONT + 0.012, "Siege")

    # Cables from one bay to the other, sagging a little between the clips.
    stops = [-1.06, *CLIPS, 1.06]
    for number, (z, radius, sheath) in enumerate(CABLES):
        depth = FRONT + radius + 0.002
        points = []
        for a, b in zip(stops, stops[1:]):
            for step in range(3):
                t = step / 3
                sag = 0.012 * math.sin(math.pi * t) * (1 + 0.3 * number)
                points.append((a + (b - a) * t, depth, z - sag))
        points.append((stops[-1], depth, z))
        MODEL.tube("Cable %d" % number, points, radius, sheath)
    for number, x in enumerate(CLIPS):
        MODEL.prism("Collier %d" % number, fv.chamfered(x - 0.02, x + 0.02, -0.495, -0.422, 0.008), FRONT, FRONT + 0.036, "Levier")

    # The armoured conduit between the attack bay and the name plate, with its flanges.
    cx, radius = CONDUIT
    MODEL.tube("Conduit", [(cx, FRONT + radius, -0.5), (cx, FRONT + radius, 0.5)], radius, "Console", sides=8)
    for z in (-0.3, 0.12, 0.38):
        MODEL.tube("Bride %.2f" % z, [(cx, FRONT + radius, z - 0.012), (cx, FRONT + radius, z + 0.012)], radius + 0.009, "Levier", sides=8)

    # The valve: a pipe from the cable bundle, a handwheel on its hub.
    vx, vz, vr = VALVE
    MODEL.tube("Tuyau vanne", [(vx, FRONT + 0.016, -0.43), (vx, FRONT + 0.016, vz), (vx + 0.3, FRONT + 0.016, vz)], 0.014, "Levier", sides=6)
    wheel = [(vx + vr * math.cos(2 * math.pi * k / 12), FRONT + 0.05, vz + vr * math.sin(2 * math.pi * k / 12)) for k in range(13)]
    MODEL.tube("Volant", wheel, 0.0075, "Levier", sides=5)
    for k in range(3):
        a = 2 * math.pi * k / 3 + math.pi / 2
        MODEL.tube("Rayon %d" % k, [(vx, FRONT + 0.05, vz), (vx + vr * math.cos(a), FRONT + 0.05, vz + vr * math.sin(a))], 0.005, "Levier", sides=4)
    MODEL.prism("Moyeu vanne", fv.circle(vx, vz, 0.016, 6), FRONT + 0.016, FRONT + 0.058, "Console")

    # A grab handle and three bolts on each end of the console, beyond the bays.
    for side in (-1, 1):
        x = side * END_STRIP
        MODEL.tube("Poignee %d" % side, [(x, FRONT, -0.26), (x, FRONT + 0.05, -0.22), (x, FRONT + 0.05, 0.22), (x, FRONT, 0.26)], 0.016, "Levier", sides=6)
        for z in (-0.4, 0.0, 0.4):
            MODEL.prism("Boulon %d %.1f" % (side, z), fv.circle(x, z, 0.02, 6), FRONT, FRONT + 0.014, "Levier")


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    new_images()

    half_w, half_h = WIDTH / 2, HEIGHT / 2
    MODEL.prism("Console", fv.chamfered(-half_w, half_w, -half_h, half_h, 0.06), BACK, FRONT, "Console")
    bays()
    gauge()
    switch_and_diodes()
    manometer()
    machinery()


# ---------------------------------------------------------------------------------------------------------- textures

def kind_of(name, material_name):
    if material_name == "Siege":
        return "paint"
    if material_name == "Panneau":
        return {"Plaque du nom": "plate", "Cadran": "dial"}.get(name, "hazard")
    if material_name == "Socket":
        return "floor"
    if material_name == "Piste":
        return "trough"
    if name.startswith("Prise"):
        return "socket"
    return "metal"


def paint_cockpit():
    """The front-view texture of the console, the painted parts and the bay floors, and its relief."""
    rng = np.random.default_rng(7)
    shape = (TEX_H, TEX_W)

    # Which surface shows at each pixel (the frontmost part drawn with this texture), its depth, the depth of anything
    # (glass shows what is behind it, and the moving parts' shadows would not follow them), and the outlines.
    kinds, depth, front, outline = MODEL.surfaces(
        ATLAS_MATERIALS, kind_of, FRONT, skip={"Console"},
        casts_no_shadow=lambda name, material_name: material_name == "Verre" or name in MOVING)
    kinds["metal"] = kinds.pop("base") | kinds.pop("metal", np.zeros(shape, dtype=bool))
    metal = kinds["metal"]
    painted = np.zeros(shape, dtype=bool)
    for kind in ("paint", "plate", "dial", "hazard"):
        painted |= kinds.get(kind, painted)
    body = CANVAS.fill(fv.chamfered(-WIDTH / 2, WIDTH / 2, -HEIGHT / 2, HEIGHT / 2, 0.06))
    outline |= fv.outlines(body.astype(np.int64))  # the console's own edge wears too

    height = np.zeros(shape)
    colour = np.zeros(shape + (3,))

    # Blackened steel: a dark base, mottled, a little lighter where the varnish has worn thin.
    mottle = fv.fractal(rng, (96, 32, 8), shape)
    colour[:] = (BLACKENED * (0.6 + 0.6 * mottle))[..., None]
    base = {
        "paint": np.array([PAINT] * 3),
        "plate": BONE,
        "dial": DIAL_FACE,
        "floor": np.array([0.012, 0.012, 0.013]),
        "trough": np.array([0.008, 0.008, 0.009]),
        "socket": np.array([0.014, 0.014, 0.015]),
    }
    for kind, value in base.items():
        if kind in kinds:
            colour[kinds[kind]] = value * (0.9 + 0.2 * mottle[kinds[kind]])[..., None]

    # Hazard stripes on the switch plate, the bay floors ribbed, the pin sockets' slot and contacts.
    if "hazard" in kinds:
        stripes = np.mod((CANVAS.X[None, :] + CANVAS.Z[:, None]) / 0.05, 1.0) < 0.5
        yellow = kinds["hazard"] & stripes
        colour[kinds["hazard"]] = np.array([0.018, 0.018, 0.02])
        colour[yellow] = HAZARD * (0.85 + 0.2 * mottle[yellow])[..., None]
    if "floor" in kinds:
        ribs = np.mod(CANVAS.Z[:, None] / 0.024, 1.0) < 0.2
        height -= np.where(kinds["floor"] & ribs, 0.4, 0.0)
    if "socket" in kinds:
        for cx in SOCKETS:
            half, top, _ = PIN_SOCKET
            slot = kinds["socket"] & (np.abs(CANVAS.X[None, :] - cx) < half - 0.02) & (CANVAS.Z[:, None] > top - 0.014) & (CANVAS.Z[:, None] < top - 0.005)
            colour[slot] = 0.002
            height -= np.where(slot, 1.0, 0.0)
            contacts = slot & (np.mod((CANVAS.X[None, :] - cx) / 0.025, 1.0) < 0.45)
            colour[contacts] = BRASS

    # Rivets: along the bay frames, around the plates, on the console's ends and along the seat strip.
    rivets = []
    for cx in SOCKETS:
        w, h = SOCKET_SIZE[0] / 2 + BEZEL / 2, SOCKET_SIZE[1] / 2 + BEZEL / 2
        rivets += [(cx + t, h) for t in np.arange(-w + 0.04, w - 0.02, 0.09)]
        rivets += [(cx + side * w, z) for side in (-1, 1) for z in np.arange(-h + 0.05, h - 0.02, 0.09)]
    for x0, x1, z0, z1 in (NAME_PLATE, (SWITCH[0] - 0.12, SWITCH[0] + 0.12, SWITCH[1] - 0.11, SWITCH[1] + 0.11)):
        rivets += [(x0 + 0.02, z0 + 0.02), (x1 - 0.02, z0 + 0.02), (x0 + 0.02, z1 - 0.02), (x1 - 0.02, z1 - 0.02)]
    for side in (-1, 1):
        rivets += [(side * 1.895, z) for z in np.arange(-0.42, 0.44, 0.06)]
    rivets += [(x, 0.465) for x in np.arange(TOP_STRIP[0] + 0.03, TOP_STRIP[1], 0.14)]
    rivets += [(x, z) for x in np.arange(-0.62, 0.3, 0.07) for z in (-0.49,)]
    rivet_mask = CANVAS.rivets(rivets, 0.0075, colour, height)

    # The name plate and the dial's face keep a clean middle, where the game writes; then years of use.
    clean = np.zeros(shape, dtype=np.float32)
    for kind in ("plate", "dial"):
        if kind in kinds:
            clean = np.maximum(clean, np.where(kinds[kind], fv.blur(kinds[kind].astype(np.float32), 14), 0.0))
    clean = fv.smoothstep(clean, 0.55, 0.9)
    chips = fv.weather(CANVAS, rng, colour, height, dict(
        metal=metal, painted=painted, clean=clean, rivets=rivet_mask, body=body, outline=outline, depth=depth, front=front),
        dict(bare_steel=BARE_STEEL, blackened=BLACKENED, rust=RUST, scratches=(450, 0.09), shadow=(10, 0.035), low=(0.1, 0.5)))

    # Stencil markings in ink on the seat strip.
    folder = tempfile.mkdtemp(prefix="vortex_cockpit_")
    try:
        for text, x0, x1, z0, z1 in MARKINGS:
            cover = CANVAS.stamp(fv.render_text(text, STENCIL_FONT, folder), x0, x1, z0, z1) * 0.88 * (1 - 0.5 * chips)
            colour = colour * (1 - cover[..., None]) + INK * cover[..., None]
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    # The console's edge shows the chips too; outside the console, nothing is seen.
    pixels = np.dstack([fv.to_srgb(colour), np.ones(shape)])
    IMAGES["Cockpit_BaseColor"].pixels.foreach_set(pixels.astype(np.float32).ravel())
    IMAGES["Cockpit_Normal"].pixels.foreach_set(fv.normal_from(fv.blur(height, 1), 1.2).astype(np.float32).ravel())


def paint_steel():
    """Worn bare steel for the clamps, the lever, the needle, the rings and the machinery."""
    colour, height = fv.brushed_steel(np.random.default_rng(11), (STEEL_H, STEEL_W), 0.11, RUST)
    IMAGES["Cockpit_Acier_BaseColor"].pixels.foreach_set(np.dstack([fv.to_srgb(colour), np.ones((STEEL_H, STEEL_W))]).astype(np.float32).ravel())
    IMAGES["Cockpit_Acier_Normal"].pixels.foreach_set(fv.normal_from(height, 0.6).astype(np.float32).ravel())


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1:
        raise SystemExit("usage: blender --background --python build_cockpit.py -- <output.blend>")
    output = os.path.abspath(argv[0])
    directory = os.path.dirname(output)
    os.makedirs(directory, exist_ok=True)
    build()
    paint_cockpit()
    paint_steel()
    fv.save_images(IMAGES.values(), directory)
    MODEL.join("Habillage", KEPT)
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=output, relative_remap=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    print("Cockpit saved to", output, "-", triangles, "triangles,", len(meshes), "objects")


main()
