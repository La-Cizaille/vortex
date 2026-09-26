"""Build the crew ship Carcasse (docs/ASSETS.md section 2, ARB-76, ARB-77, ARB-101), in the art direction of
docs/DIRECTION_ARTISTIQUE.md (§2.2, §6.3, §6.4), and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_ship_carcasse.py -- \
        art-src/ships/Ship_Carcasse.blend

A pirate crew ship, not a fighter (ARB-101): about 2.2 m long and 1.4 m wide, nose towards -Y, top towards +Z, origin at
its centre. Its shape mixes the designer's two references: armoured caissons with large painted panels (bow with the
cockpit visor, crew hull with its row of portholes, a wider engine block), and visible machinery (copper pipes along
the flanks, radiator fins on the engine block, an off-centre superstructure with its antennas, a ramp with hazard
stripes, landing skids). It carries a heavy dorsal turret with two barrels, and two large engine nozzles. Everything
is described by the numbers below: changing a proportion means changing a number and running the script again. The
saved .blend stays the source that gets exported (tools/blender/export_unity.py).

Three materials (docs/ASSETS.md section 2):
- Siege: the painted panels, glossy lacquer, which the game tints with the seat colour;
- Coque: the frames, the machinery, the glass and the copper (their looks are baked into the atlas);
- Feux: the glowing back of the two nozzles, the only light of the ship (emission above 1, for Bloom).
Siege and Coque share one texture atlas, baked here: panel lines, a tone per panel, chipped and rusted edges, rust
streaks running down the sides, grime and soot in the hollows, hazard stripes on the ramp, and stencil markings in the
art direction's stencil font (hull number, insurer's notice). The images are written next to the .blend.

Empty markers the game reads (docs/ANIMATIONS.md §5): Canon, at the tips of the turret's barrels; Reacteur_Gauche and
Reacteur_Droit, at the two nozzles.
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

NAME = "Ship_Carcasse"
TEXTURE_SIZE = 1024
BEVEL_WIDTH = 0.007
BEVEL_ANGLE = 30  # degrees: only edges sharper than this get a bevel
# Nozzle glow: a saturated orange, strong enough for Bloom (threshold 1.5) while its green stays under 1, so that the
# screen shows orange rather than a colour clipped to white (the game uses no tone mapping).
REACTOR_COLOR = (1.0, 0.35, 0.06)
REACTOR_STRENGTH = 2.5

# Material slots while building; the looks of the machinery are baked into the atlas, then they all join Coque.
SIEGE, COQUE, VERRE, FEUX, CUIVRE, RAMPE, RADIATEUR = range(7)
FINAL_SLOT = {SIEGE: 0, COQUE: 1, VERRE: 1, FEUX: 2, CUIVRE: 1, RAMPE: 1, RADIATEUR: 1}

# Panel grid of the textures, in metres along X, Y, Z; lines are GROOVE wide.
PANEL = (0.26, 0.3, 0.15)
PANEL_OFFSET = (0.1, 0.06, 0.04)
GROOVE = 0.005

# The hull: chamfered sections (y, half-width, bottom z, top z, chamfer).
BOW_AND_CREW = [
    (-1.10, 0.16, -0.10, 0.05, 0.04),
    (-0.96, 0.27, -0.17, 0.13, 0.06),
    (-0.64, 0.36, -0.21, 0.23, 0.07),
    (-0.40, 0.38, -0.22, 0.27, 0.07),
    (0.44, 0.38, -0.22, 0.27, 0.07),
]
ENGINE_BLOCK = [
    (0.38, 0.47, -0.21, 0.23, 0.09),
    (1.02, 0.47, -0.21, 0.23, 0.09),
]
NOZZLES = [(0.24, 0.0), (-0.24, 0.0)]  # x, z of each nozzle's axis
NOZZLE = (1.02, 1.13, 0.15, 0.19, 0.155, 1.05, 0.13)  # y start, y end, radius, flared radius, lip, glow y, glow radius

TURRET = (0.0, -0.30)  # x, y of its axis on the crew hull's top
BARRELS = (0.05, 0.37, -0.40, -0.80, 0.022)  # half spacing, z, y from, y to, radius

# Markers (ANIMATIONS §5). The ship's left is +X (nose towards -Y, top towards +Z).
MARKERS = {
    "Canon": (0.0, -0.83, 0.37),
    "Reacteur_Gauche": (0.24, 1.13, 0.0),
    "Reacteur_Droit": (-0.24, 1.13, 0.0),
}

# Stencil markings (§6.3): text, side (+1 left, -1 right), along Y from y0 to y1, along Z from z0 to z1, on faces
# turned towards that side. The paint is the art direction's ink, so the seat colour tints the hull around it.
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STENCIL_FONT = os.path.join(REPO, "unity", "Assets", "_Vortex", "Art", "Fonts", "BigShouldersStencil", "BigShouldersStencilDisplay-Black.ttf")
MARKINGS = [
    ("0427", 1, 0.08, 0.40, 0.0, 0.19),
    ("0427", -1, -0.48, -0.02, 0.0, 0.19),
    ("NON COUVERT", 1, 0.52, 0.98, -0.16, -0.06),
    ("NON COUVERT", -1, 0.05, 0.40, -0.02, 0.06),
]
INK = (0.078, 0.075, 0.071)  # #141312, as stored in the sRGB texture
PROBE_SCALE, PROBE_OFFSET = 0.5, 0.5

# Wear (§6.3: on edges, chips and rust), linear colours for the bake.
BARE_METAL = (0.11, 0.12, 0.13)
RUST = (0.155, 0.043, 0.016)
DANGER = (0.69, 0.37, 0.006)  # #D9A514


# ---------------------------------------------------------------------------------------------------------- shape

def chamfered(y, w, zb, zt, c, cx=0.0):
    """A section of a caisson: a rectangle with its corners cut, going around (top right first)."""
    pts = [(w - c, zt), (w, zt - c), (w, zb + c), (w - c, zb), (-w + c, zb), (-w, zb + c), (-w, zt - c), (-w + c, zt)]
    return [(cx + x, y, z) for x, z in pts]


# Bands of a caisson section: 0 top-right bevel, 1 right side, 2 bottom-right bevel, 3 bottom, 4 bottom-left bevel,
# 5 left side, 6 top-left bevel, 7 top. The painted panels are the sides and the top; the frames are dark.
CAISSON_BANDS = [COQUE, SIEGE, COQUE, COQUE, COQUE, SIEGE, COQUE, SIEGE]


def loft(bm, sections, band_mats, cap_start=None, cap_end=None):
    """Joins rings of points (same count, going around) with quads; band_mats is one material per band, or one."""
    rings = [[bm.verts.new(p) for p in section] for section in sections]
    count = len(rings[0])
    for a, b in zip(rings, rings[1:]):
        for band in range(count):
            nxt = (band + 1) % count
            face = bm.faces.new((a[band], a[nxt], b[nxt], b[band]))
            face.material_index = band_mats if isinstance(band_mats, int) else band_mats[band]
    if cap_start is not None:
        bm.faces.new(list(reversed(rings[0]))).material_index = cap_start
    if cap_end is not None:
        bm.faces.new(rings[-1]).material_index = cap_end
    return rings


def box(bm, x0, x1, y0, y1, z0, z1, mat):
    sections = [[(x1, y, z1), (x1, y, z0), (x0, y, z0), (x0, y, z1)] for y in (y0, y1)]
    loft(bm, sections, mat, cap_start=mat, cap_end=mat)


def ring(cx, y, cz, radius, sides=8):
    return [(cx + radius * math.cos(2 * math.pi * k / sides + math.pi / sides), y, cz + radius * math.sin(2 * math.pi * k / sides + math.pi / sides)) for k in range(sides)]


def tube(bm, points, radius, mat, sides=6):
    """A pipe, a cable or a barrel: a polygon swept along a polyline, closed at both ends."""
    points = [mathutils.Vector(p) for p in points]
    rings = []
    for i, point in enumerate(points):
        ahead = points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]
        ahead.normalize()
        up = mathutils.Vector((0, 0, 1)) if abs(ahead.z) < 0.9 else mathutils.Vector((1, 0, 0))
        side = ahead.cross(up).normalized()
        up = side.cross(ahead).normalized()
        rings.append([bm.verts.new(point + radius * (math.cos(a) * side + math.sin(a) * up))
                      for a in (2 * math.pi * k / sides for k in range(sides))])
    for a, b in zip(rings, rings[1:]):
        for k in range(sides):
            bm.faces.new((a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k])).material_index = mat
    bm.faces.new(list(reversed(rings[0]))).material_index = mat
    bm.faces.new(rings[-1]).material_index = mat


def hull(bm):
    # Bow and crew hull in one caisson: the nose is all frame, the rest painted panels on dark frames.
    sections = [chamfered(*section) for section in BOW_AND_CREW]
    rings = [[bm.verts.new(p) for p in s] for s in sections]
    for index, (a, b) in enumerate(zip(rings, rings[1:])):
        for band in range(8):
            nxt = (band + 1) % 8
            face = bm.faces.new((a[band], a[nxt], b[nxt], b[band]))
            face.material_index = COQUE if index == 0 else CAISSON_BANDS[band]
    bm.faces.new(list(reversed(rings[0]))).material_index = COQUE
    bm.faces.new(rings[-1]).material_index = COQUE
    # The engine block, wider, a caisson of its own.
    loft(bm, [chamfered(*section) for section in ENGINE_BLOCK], CAISSON_BANDS, cap_start=COQUE, cap_end=COQUE)


def visor(bm):
    """The cockpit visor, a dark glass wedge on the bow's top."""
    sections = [
        [(0.18, -0.94, 0.135), (0.20, -0.94, 0.095), (-0.20, -0.94, 0.095), (-0.18, -0.94, 0.135)],
        [(0.24, -0.70, 0.245), (0.27, -0.70, 0.19), (-0.27, -0.70, 0.19), (-0.24, -0.70, 0.245)],
    ]
    loft(bm, sections, [VERRE, COQUE, COQUE, VERRE], cap_start=VERRE, cap_end=COQUE)


def portholes(bm):
    """A row of portholes along each side of the crew hull."""
    for side in (1, -1):
        for i in range(7):
            y = -0.52 + 0.12 * i
            x0, x1 = (0.378, 0.392) if side > 0 else (-0.392, -0.378)
            box(bm, x0, x1, y - 0.035, y + 0.035, 0.155, 0.205, VERRE)


def dorsal_turret(bm):
    """The heavy dorsal turret: a ring base, a chamfered body, two barrels with muzzle brakes."""
    tx, ty = TURRET
    base = []
    for z, r in ((0.26, 0.16), (0.30, 0.15), (0.31, 0.13)):
        base.append([(tx + r * math.cos(2 * math.pi * k / 10), ty + r * math.sin(2 * math.pi * k / 10), z) for k in range(10)])
    loft(bm, base, COQUE, cap_end=COQUE)
    body = [chamfered(y, 0.13, 0.31, 0.42, 0.035, cx=tx) for y in (ty - 0.13, ty + 0.14)]
    loft(bm, body, [COQUE, SIEGE, COQUE, COQUE, COQUE, SIEGE, COQUE, SIEGE], cap_start=COQUE, cap_end=COQUE)
    spacing, z, y0, y1, radius = BARRELS
    for x in (tx - spacing, tx + spacing):
        tube(bm, [(x, y0, z), (x, y1, z)], radius, COQUE, sides=8)
        tube(bm, [(x, y1 + 0.06, z), (x, y1 - 0.02, z)], radius * 1.35, COQUE, sides=8)


def nozzles(bm):
    """Two large nozzles at the back: a flared bell, its lip, a dark throat and a glowing back."""
    y0, y1, radius, flared, lip, glow_y, glow_r = NOZZLE
    for x, z in NOZZLES:
        outer = loft(bm, [ring(x, y0, z, radius), ring(x, y0 + 0.05, z, radius * 1.02), ring(x, y1, z, flared)], COQUE)
        inner = [bm.verts.new(p) for p in ring(x, y1, z, lip)]
        throat = [bm.verts.new(p) for p in ring(x, glow_y, z, glow_r)]
        rim = outer[-1]
        for k in range(8):
            n = (k + 1) % 8
            bm.faces.new((rim[k], rim[n], inner[n], inner[k])).material_index = COQUE
            bm.faces.new((inner[k], inner[n], throat[n], throat[k])).material_index = COQUE
        bm.faces.new(throat).material_index = FEUX


def superstructure(bm):
    """An off-centre bridge on the crew hull's back, with its window slit and its antennas."""
    box(bm, -0.06, 0.24, 0.02, 0.36, 0.26, 0.36, COQUE)
    loft(bm, [chamfered(y, 0.11, 0.36, 0.47, 0.025, cx=0.09) for y in (0.06, 0.30)], [COQUE, SIEGE, COQUE, COQUE, COQUE, SIEGE, COQUE, SIEGE], cap_start=COQUE, cap_end=COQUE)
    loft(bm, [[(0.19, 0.055, 0.45), (0.20, 0.055, 0.41), (-0.02, 0.055, 0.41), (-0.01, 0.055, 0.45)],
              [(0.19, 0.045, 0.45), (0.20, 0.045, 0.41), (-0.02, 0.045, 0.41), (-0.01, 0.045, 0.45)]], VERRE, cap_start=VERRE, cap_end=VERRE)
    tube(bm, [(0.14, 0.24, 0.47), (0.14, 0.24, 0.70)], 0.006, COQUE)
    tube(bm, [(0.03, 0.16, 0.47), (0.03, 0.16, 0.61)], 0.005, COQUE)
    tube(bm, [(0.08, 0.24, 0.64), (0.20, 0.24, 0.64)], 0.004, COQUE)
    box(bm, 0.125, 0.155, 0.225, 0.255, 0.70, 0.73, COQUE)
    # A bent antenna on the bow, on the right.
    tube(bm, [(-0.22, -0.84, 0.16), (-0.25, -0.88, 0.26), (-0.25, -0.90, 0.36)], 0.005, COQUE)


def radiators(bm):
    """Radiator fins sticking out of the engine block's sides, one up and one down on each side."""
    for side in (1, -1):
        for z, lift, thickness in ((0.10, 0.25, 0.012), (-0.08, -0.15, 0.012)):
            root_x, tip_x = side * 0.46, side * 0.72
            tip_z = z + lift * 0.26
            outline = [(root_x, 0.48, z), (tip_x, 0.62, tip_z), (tip_x, 0.92, tip_z), (root_x, 0.98, z)]
            top = [bm.verts.new((x, y, zz + thickness / 2)) for x, y, zz in outline]
            bottom = [bm.verts.new((x, y, zz - thickness / 2)) for x, y, zz in outline]
            bm.faces.new(top).material_index = RADIATEUR
            bm.faces.new(list(reversed(bottom))).material_index = RADIATEUR
            for i in range(4):
                j = (i + 1) % 4
                bm.faces.new((bottom[i], bottom[j], top[j], top[i])).material_index = COQUE


def machinery(bm):
    """Copper pipes along the flanks (three on the left, two on the right), a cable up to the bridge, the ramp with
    its hazard stripes on the left, landing skids under the crew hull."""
    for z, r in ((-0.05, 0.022), (-0.10, 0.018), (-0.145, 0.02)):
        tube(bm, [(0.37, -0.62, z), (0.40, -0.45, z), (0.40, 0.30, z), (0.44, 0.40, z)], r, CUIVRE)
    for z, r in ((-0.07, 0.024), (-0.13, 0.018)):
        tube(bm, [(-0.37, -0.55, z), (-0.40, -0.40, z), (-0.40, 0.32, z), (-0.44, 0.40, z)], r, CUIVRE)
    tube(bm, [(-0.40, 0.20, -0.07), (-0.36, 0.22, 0.14), (-0.20, 0.20, 0.27), (-0.04, 0.18, 0.30)], 0.012, COQUE)
    box(bm, 0.384, 0.396, -0.34, 0.04, -0.20, 0.07, RAMPE)
    for x, z, r in ((-0.20, 0.285, 0.022), (-0.26, 0.28, 0.016)):
        tube(bm, [(x, -0.20, z), (x, 0.30, z), (x - 0.02, 0.42, z - 0.03), (x - 0.02, 0.70, z - 0.035)], r, CUIVRE)
    box(bm, 0.16, 0.32, -0.62, -0.46, 0.265, 0.285, RADIATEUR)
    box(bm, -0.34, -0.10, 0.62, 0.92, 0.225, 0.245, RADIATEUR)
    box(bm, -0.30, -0.18, -0.12, 0.00, 0.265, 0.29, COQUE)
    box(bm, 0.06, 0.26, 0.52, 0.70, 0.225, 0.33, COQUE)
    box(bm, 0.10, 0.30, 0.74, 0.86, 0.225, 0.30, COQUE)
    tube(bm, [(0.04, 0.61, 0.335), (0.28, 0.61, 0.335)], 0.006, COQUE)
    for x in (0.34, 0.40):
        tube(bm, [(x, 0.93, 0.22), (x, 0.93, 0.36)], 0.03, COQUE, sides=8)
    for x in (0.24, -0.24):
        box(bm, x - 0.05, x + 0.05, -0.46, 0.30, -0.28, -0.215, COQUE)


def build_mesh():
    bm = bmesh.new()
    hull(bm)
    visor(bm)
    portholes(bm)
    dorsal_turret(bm)
    nozzles(bm)
    superstructure(bm)
    radiators(bm)
    machinery(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    mesh = bpy.data.meshes.new(NAME)
    # Material slots before the geometry: clearing a mesh's materials also drops its face material indices.
    for _ in range(7):
        mesh.materials.append(None)
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = False  # faceted low-poly look; the bevels catch the light
    return mesh


def markers(ship):
    for name, location in MARKERS.items():
        empty = bpy.data.objects.new(name, None)
        empty.empty_display_type = "ARROWS"
        empty.empty_display_size = 0.1
        empty.location = location
        empty.parent = ship
        bpy.context.scene.collection.objects.link(empty)


def unwrap(ship):
    bpy.context.view_layer.objects.active = ship
    ship.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.008, correct_aspect=True, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")


# -------------------------------------------------------------------------------------------------------- texture

def detail_group():
    """Node group giving, at each point of the hull: Lines (1 in a panel groove), Tone (0..1 per panel), Wear (0..1),
    Edge (1 on a worn edge, chipped), Grime (1 in a hollow), Streaks (rust running down a side) and Stripes (hazard)."""
    group = bpy.data.node_groups.new("CarcasseDetail", "ShaderNodeTree")
    for name in ("Lines", "Tone", "Wear", "Edge", "Grime", "Streaks", "Stripes", "Low", "Soot", "Scratches", "Dents"):
        group.interface.new_socket(name=name, in_out="OUTPUT", socket_type="NodeSocketFloat")
    nodes, links = group.nodes, group.links
    out = nodes.new("NodeGroupOutput")

    def math_node(operation, a, b=None):
        node = nodes.new("ShaderNodeMath")
        node.operation = operation
        for index, value in enumerate((a, b)):
            if value is None:
                continue
            if isinstance(value, (int, float)):
                node.inputs[index].default_value = value
            else:
                links.new(value, node.inputs[index])
        return node.outputs[0]

    def map_range(value, low, high):
        node = nodes.new("ShaderNodeMapRange")
        node.interpolation_type = "SMOOTHSTEP"
        links.new(value, node.inputs["Value"])
        node.inputs["From Min"].default_value = low
        node.inputs["From Max"].default_value = high
        return node.outputs[0]

    coords = nodes.new("ShaderNodeTexCoord").outputs["Object"]
    position = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coords, position.inputs[0])
    geometry = nodes.new("ShaderNodeNewGeometry")
    normal = nodes.new("ShaderNodeSeparateXYZ")
    links.new(geometry.outputs["Normal"], normal.inputs[0])

    # A groove where the surface crosses a grid plane; a face parallel to a plane family ignores it.
    lines = None
    row = math_node("MODULO", math_node("FLOOR", math_node("DIVIDE", position.outputs[2], PANEL[2])), 2.0)
    for axis in range(3):
        size, offset = PANEL[axis], PANEL_OFFSET[axis]
        shifted = position.outputs[axis] if axis == 2 else math_node("ADD", position.outputs[axis], math_node("MULTIPLY", row, size / 2))
        t = math_node("DIVIDE", math_node("SUBTRACT", shifted, offset), size)
        distance = math_node("MULTIPLY", math_node("ABSOLUTE", math_node("SUBTRACT", math_node("FRACT", math_node("ADD", t, 0.5)), 0.5)), size)
        groove = nodes.new("ShaderNodeMapRange")
        groove.interpolation_type = "SMOOTHSTEP"
        links.new(distance, groove.inputs["Value"])
        groove.inputs["From Min"].default_value = GROOVE * 0.3
        groove.inputs["From Max"].default_value = GROOVE
        groove.inputs["To Min"].default_value = 1.0
        groove.inputs["To Max"].default_value = 0.0
        crossing = math_node("LESS_THAN", math_node("ABSOLUTE", normal.outputs[axis]), 0.7)
        line = math_node("MULTIPLY", groove.outputs[0], crossing)
        lines = line if lines is None else math_node("MAXIMUM", lines, line)
    links.new(lines, out.inputs["Lines"])

    # One tone per panel: a random value for each grid cell.
    snap = nodes.new("ShaderNodeVectorMath")
    snap.operation = "SNAP"
    links.new(coords, snap.inputs[0])
    snap.inputs[1].default_value = PANEL
    noise = nodes.new("ShaderNodeTexWhiteNoise")
    noise.noise_dimensions = "3D"
    links.new(snap.outputs[0], noise.inputs["Vector"])
    links.new(noise.outputs["Value"], out.inputs["Tone"])

    # Wear: soft blotches.
    wear = nodes.new("ShaderNodeTexNoise")
    wear.inputs["Scale"].default_value = 5.0
    wear.inputs["Detail"].default_value = 5.0
    links.new(coords, wear.inputs["Vector"])
    links.new(map_range(wear.outputs["Fac"], 0.42, 0.72), out.inputs["Wear"])

    # Worn edges: where the rounded normal of a small bevel leaves the face's own normal, broken into chips.
    bevel = nodes.new("ShaderNodeBevel")
    bevel.samples = 8
    bevel.inputs["Radius"].default_value = 0.016
    dot = nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    links.new(bevel.outputs["Normal"], dot.inputs[0])
    links.new(geometry.outputs["True Normal"], dot.inputs[1])
    edge = map_range(math_node("SUBTRACT", 1.0, dot.outputs["Value"]), 0.003, 0.04)
    chips = nodes.new("ShaderNodeTexNoise")
    chips.inputs["Scale"].default_value = 38.0
    chips.inputs["Detail"].default_value = 6.0
    links.new(coords, chips.inputs["Vector"])
    links.new(math_node("MULTIPLY", edge, map_range(chips.outputs["Fac"], 0.38, 0.5)), out.inputs["Edge"])

    # Grime: the hollows the light hardly reaches.
    occlusion = nodes.new("ShaderNodeAmbientOcclusion")
    occlusion.samples = 16
    occlusion.inputs["Distance"].default_value = 0.1
    links.new(math_node("SUBTRACT", 1.0, occlusion.outputs["AO"]), out.inputs["Grime"])

    # Rust streaks: noise stretched downwards, on the upright faces only.
    stretch = nodes.new("ShaderNodeVectorMath")
    stretch.operation = "MULTIPLY"
    links.new(coords, stretch.inputs[0])
    stretch.inputs[1].default_value = (26.0, 26.0, 1.6)
    streak_noise = nodes.new("ShaderNodeTexNoise")
    streak_noise.inputs["Scale"].default_value = 1.0
    streak_noise.inputs["Detail"].default_value = 3.0
    links.new(stretch.outputs["Vector"], streak_noise.inputs["Vector"])
    upright = math_node("LESS_THAN", math_node("ABSOLUTE", normal.outputs[2]), 0.5)
    links.new(math_node("MULTIPLY", map_range(streak_noise.outputs["Fac"], 0.55, 0.7), upright), out.inputs["Streaks"])

    # Hazard stripes: diagonal bands along Y and Z (for the ramp, which faces sideways).
    diagonal = math_node("FRACT", math_node("DIVIDE", math_node("ADD", position.outputs[1], position.outputs[2]), 0.07))
    links.new(math_node("LESS_THAN", diagonal, 0.5), out.inputs["Stripes"])

    # Dirt low on the hull; soot on the engine block's back.
    links.new(map_range(math_node("MULTIPLY", position.outputs[2], -1.0), -0.15, 0.22), out.inputs["Low"])
    links.new(map_range(position.outputs[1], 0.8, 1.1), out.inputs["Soot"])

    # Scratches: thin lines of bare metal across the paint.
    scratch = nodes.new("ShaderNodeVectorMath")
    scratch.operation = "MULTIPLY"
    links.new(coords, scratch.inputs[0])
    scratch.inputs[1].default_value = (3.0, 60.0, 60.0)
    scratch_noise = nodes.new("ShaderNodeTexNoise")
    scratch_noise.inputs["Scale"].default_value = 1.0
    scratch_noise.inputs["Detail"].default_value = 2.0
    links.new(scratch.outputs["Vector"], scratch_noise.inputs["Vector"])
    links.new(map_range(scratch_noise.outputs["Fac"], 0.72, 0.78), out.inputs["Scratches"])

    # Dents: a slow bump over the plates, so the lacquer's reflections waver.
    dents = nodes.new("ShaderNodeTexNoise")
    dents.inputs["Scale"].default_value = 9.0
    dents.inputs["Detail"].default_value = 2.0
    links.new(coords, dents.inputs["Vector"])
    links.new(dents.outputs["Fac"], out.inputs["Dents"])
    return group


# Bake look of each building slot: base colour, tone and wear amounts, groove colour and depth, edge wear, grime,
# streaks, stripes (hazard colour over the base).
BAKE_LOOK = {
    SIEGE: dict(color=(0.42, 0.42, 0.42), tone=0.12, wear=0.15, groove=(0.12, 0.12, 0.12), depth=0.7, edge=1.0, grime=0.7, streaks=0.75, stripes=0.0),
    COQUE: dict(color=(0.045, 0.045, 0.05), tone=0.4, wear=0.25, groove=(0.015, 0.015, 0.017), depth=0.6, edge=0.8, grime=0.7, streaks=0.8, stripes=0.0),
    VERRE: dict(color=(0.03, 0.035, 0.04), tone=0.0, wear=0.0, groove=(0.03, 0.035, 0.04), depth=0.0, edge=0.2, grime=0.3, streaks=0.0, stripes=0.0),
    FEUX: dict(color=REACTOR_COLOR, tone=0.0, wear=0.0, groove=REACTOR_COLOR, depth=0.0, edge=0.0, grime=0.0, streaks=0.0, stripes=0.0),
    CUIVRE: dict(color=(0.42, 0.15, 0.05), tone=0.3, wear=0.5, groove=(0.42, 0.15, 0.05), depth=0.0, edge=0.3, grime=0.5, streaks=0.3, stripes=0.0),
    RAMPE: dict(color=(0.04, 0.04, 0.045), tone=0.1, wear=0.3, groove=(0.04, 0.04, 0.045), depth=0.0, edge=1.0, grime=0.4, streaks=0.4, stripes=1.0),
    RADIATEUR: dict(color=(0.06, 0.062, 0.066), tone=0.2, wear=0.3, groove=(0.01, 0.01, 0.012), depth=0.9, edge=0.8, grime=0.4, streaks=0.3, stripes=0.0),
}


def bake_material(slot, group, target):
    """A throwaway material that shows the procedural look of a slot, with the bake target as active node."""
    look = BAKE_LOOK[slot]
    material = bpy.data.materials.new("bake_%d" % slot)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    detail = nodes.new("ShaderNodeGroup")
    detail.node_tree = group

    def mix(factor_output, a, b, amount=1.0):
        scaled = nodes.new("ShaderNodeMath")
        scaled.operation = "MULTIPLY"
        links.new(factor_output, scaled.inputs[0])
        scaled.inputs[1].default_value = amount
        node = nodes.new("ShaderNodeMix")
        node.data_type = "RGBA"
        links.new(scaled.outputs[0], node.inputs["Factor"])
        for socket, value in (("A", a), ("B", b)):
            if isinstance(value, tuple):
                node.inputs[socket].default_value = (*value, 1.0)
            else:
                links.new(value, node.inputs[socket])
        return node.outputs["Result"]

    shade = nodes.new("ShaderNodeMath")
    shade.operation = "MULTIPLY_ADD"
    links.new(detail.outputs["Tone"], shade.inputs[0])
    shade.inputs[1].default_value = look["tone"]
    shade.inputs[2].default_value = 1.0 - look["tone"] / 2
    worn = nodes.new("ShaderNodeMath")
    worn.operation = "MULTIPLY_ADD"
    links.new(detail.outputs["Wear"], worn.inputs[0])
    worn.inputs[1].default_value = -look["wear"]
    worn.inputs[2].default_value = 1.0
    factor = nodes.new("ShaderNodeMath")
    factor.operation = "MULTIPLY"
    links.new(shade.outputs[0], factor.inputs[0])
    links.new(worn.outputs[0], factor.inputs[1])
    tinted = nodes.new("ShaderNodeMix")
    tinted.data_type = "RGBA"
    tinted.blend_type = "MULTIPLY"
    tinted.inputs["Factor"].default_value = 1.0
    tinted.inputs["A"].default_value = (*look["color"], 1.0)
    links.new(factor.outputs[0], tinted.inputs["B"])
    colour = mix(detail.outputs["Stripes"], tinted.outputs["Result"], DANGER, look["stripes"])
    colour = mix(detail.outputs["Lines"], colour, look["groove"])

    # Chipped edges show bare metal, rusted where the blotches are; rust runs down the sides; hollows collect grime.
    colour = mix(detail.outputs["Edge"], colour, BARE_METAL, look["edge"])
    rusted = nodes.new("ShaderNodeMath")
    rusted.operation = "MULTIPLY"
    links.new(detail.outputs["Edge"], rusted.inputs[0])
    links.new(detail.outputs["Wear"], rusted.inputs[1])
    colour = mix(rusted.outputs[0], colour, RUST, look["edge"])
    colour = mix(detail.outputs["Scratches"], colour, BARE_METAL, 0.8 * look["edge"])
    colour = mix(detail.outputs["Streaks"], colour, RUST, look["streaks"])
    patches = nodes.new("ShaderNodeMath")
    patches.operation = "MULTIPLY"
    links.new(detail.outputs["Wear"], patches.inputs[0])
    patches.inputs[1].default_value = look["streaks"] * 0.6
    colour = mix(patches.outputs[0], colour, RUST)
    colour = mix(detail.outputs["Low"], colour, (0.02, 0.018, 0.016), 0.55 * look["grime"])
    colour = mix(detail.outputs["Soot"], colour, (0.006, 0.006, 0.006), 0.8 * look["grime"])
    colour = mix(detail.outputs["Grime"], colour, (0.008, 0.008, 0.008), look["grime"])
    links.new(colour, bsdf.inputs["Base Color"])

    dents = nodes.new("ShaderNodeBump")
    dents.inputs["Strength"].default_value = 0.25 * look["depth"]
    dents.inputs["Distance"].default_value = 0.01
    links.new(detail.outputs["Dents"], dents.inputs["Height"])
    bump = nodes.new("ShaderNodeBump")
    bump.invert = True  # grooves go in
    bump.inputs["Strength"].default_value = look["depth"]
    bump.inputs["Distance"].default_value = 0.002
    links.new(detail.outputs["Lines"], bump.inputs["Height"])
    links.new(dents.outputs["Normal"], bump.inputs["Normal"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    image_node = nodes.new("ShaderNodeTexImage")
    image_node.image = target
    nodes.active = image_node
    return material, image_node


def probe_material(output, target):
    """A throwaway material that emits the object-space position or normal of each point, for the markings. Light
    cannot be negative: the value is stored as value * PROBE_SCALE + PROBE_OFFSET."""
    material = bpy.data.materials.new("probe_" + output)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    out = next(n for n in nodes if n.type == "OUTPUT_MATERIAL")
    emission = nodes.new("ShaderNodeEmission")
    source = nodes.new("ShaderNodeTexCoord") if output == "Position" else nodes.new("ShaderNodeNewGeometry")
    encode = nodes.new("ShaderNodeVectorMath")
    encode.operation = "MULTIPLY_ADD"
    links.new(source.outputs["Object" if output == "Position" else "True Normal"], encode.inputs[0])
    encode.inputs[1].default_value = (PROBE_SCALE,) * 3
    encode.inputs[2].default_value = (PROBE_OFFSET,) * 3
    links.new(encode.outputs["Vector"], emission.inputs["Color"])
    links.new(emission.outputs["Emission"], out.inputs["Surface"])
    image_node = nodes.new("ShaderNodeTexImage")
    image_node.image = target
    nodes.active = image_node
    return material


def render_text(text, folder):
    """The text in the stencil font, white on black, as a coverage array (rows from the bottom)."""
    scene = bpy.data.scenes.new("Marking")
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 8
    scene.world = bpy.data.worlds.new("Marking")
    scene.world.color = (0.0, 0.0, 0.0)
    curve = bpy.data.curves.new("Marking", "FONT")
    curve.body = text
    curve.font = bpy.data.fonts.load(STENCIL_FONT, check_existing=True)
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


def paint_markings(color, position, normal, folder):
    """Paints each stencil marking into the colour texture, where its surface faces its side."""
    size = TEXTURE_SIZE
    pos = (np.array(position.pixels[:], dtype=np.float32).reshape(size, size, 4) - PROBE_OFFSET) / PROBE_SCALE
    nor = (np.array(normal.pixels[:], dtype=np.float32).reshape(size, size, 4) - PROBE_OFFSET) / PROBE_SCALE
    base = np.array(color.pixels[:], dtype=np.float32).reshape(size, size, 4)
    masks = {}
    for text, side, y0, y1, z0, z1 in MARKINGS:
        if text not in masks:
            masks[text] = render_text(text, folder)
        mask = masks[text]
        mh, mw = mask.shape
        # Fit the text in the rectangle, keeping its proportions, centred.
        scale = min((y1 - y0) / mw, (z1 - z0) / mh)
        text_w, text_h = mw * scale, mh * scale
        left, bottom = (y0 + y1 - text_w) / 2, (z0 + z1 - text_h) / 2
        facing = (side * nor[..., 0] > 0.6) & (np.sign(pos[..., 0]) == side)
        along = pos[..., 1] - left if side > 0 else (left + text_w) - pos[..., 1]  # read from the ship's side
        u = along / text_w
        v = (pos[..., 2] - bottom) / text_h
        inside = facing & (u >= 0) & (u < 1) & (v >= 0) & (v < 1)
        cols = np.clip((u * mw).astype(int), 0, mw - 1)
        rows = np.clip((v * mh).astype(int), 0, mh - 1)
        cover = np.where(inside, mask[rows, cols], 0.0) * 0.9
        for channel, ink in enumerate(INK):
            base[..., channel] = base[..., channel] * (1 - cover) + ink * cover
    color.pixels.foreach_set(base.ravel())


def bake(ship, directory):
    color = bpy.data.images.new(NAME + "_BaseColor", TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
    normal = bpy.data.images.new(NAME + "_Normal", TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
    normal.colorspace_settings.name = "Non-Color"

    group = detail_group()
    targets = []
    for slot in range(len(BAKE_LOOK)):
        material, image_node = bake_material(slot, group, color)
        ship.data.materials[slot] = material
        targets.append(image_node)

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 32  # smooth groove and chip edges, clean occlusion
    scene.render.bake.margin = 6
    bpy.context.view_layer.objects.active = ship
    ship.select_set(True)

    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, margin=6, use_clear=True)
    for image_node in targets:
        image_node.image = normal
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", margin=6, use_clear=True)

    # Where each texel lies on the hull and which way it faces, to paint the markings in texture space.
    probes = {}
    for output in ("Position", "Normal"):
        image = bpy.data.images.new("probe_" + output, TEXTURE_SIZE, TEXTURE_SIZE, alpha=False, float_buffer=True)
        image.colorspace_settings.name = "Non-Color"
        probe = probe_material(output, image)
        for slot in range(len(BAKE_LOOK)):
            ship.data.materials[slot] = probe
        bpy.ops.object.bake(type="EMIT", margin=6, use_clear=True)
        probes[output] = image
    folder = tempfile.mkdtemp(prefix="vortex_markings_")
    try:
        paint_markings(color, probes["Position"], probes["Normal"], folder)
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    for image in (color, normal):
        path = os.path.join(directory, image.name + ".png")
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        image.filepath = path  # reloads from the file from now on
    return color, normal


# -------------------------------------------------------------------------------------------------------- materials

def textured_material(name, color, normal, roughness):
    material = bpy.data.materials.new(name)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    # White: the texture carries the colour; the export passes this value on as the material colour, which the
    # game replaces with the seat colour on Siege.
    bsdf.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    color_node = nodes.new("ShaderNodeTexImage")
    color_node.image = color
    links.new(color_node.outputs["Color"], bsdf.inputs["Base Color"])
    normal_node = nodes.new("ShaderNodeTexImage")
    normal_node.image = normal
    normal_map = nodes.new("ShaderNodeNormalMap")
    links.new(normal_node.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def light_material(name, color, strength):
    material = bpy.data.materials.new(name)
    bsdf = next(n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (0.0, 0.0, 0.0, 1.0)
    bsdf.inputs["Emission Color"].default_value = (*color, 1.0)
    bsdf.inputs["Emission Strength"].default_value = strength
    material.diffuse_color = (*color, 1.0)
    return material


def final_materials(mesh, color, normal):
    slots = [0] * len(mesh.polygons)
    mesh.polygons.foreach_get("material_index", slots)
    mesh.materials.clear()
    # The painted panels are lacquered and glossy (§6.3); the frames and the machinery are duller.
    mesh.materials.append(textured_material("Siege", color, normal, roughness=0.32))
    mesh.materials.append(textured_material("Coque", color, normal, roughness=0.6))
    mesh.materials.append(light_material("Feux", REACTOR_COLOR, strength=REACTOR_STRENGTH))
    mesh.polygons.foreach_set("material_index", [FINAL_SLOT[slot] for slot in slots])
    mesh.update()


# ------------------------------------------------------------------------------------------------------------ main

def output_path():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_ship_carcasse.py -- <output.blend>")
    return os.path.abspath(argv[0])


def main():
    path = output_path()
    directory = os.path.dirname(path)
    os.makedirs(directory, exist_ok=True)

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    mesh = build_mesh()
    ship = bpy.data.objects.new(NAME, mesh)
    scene.collection.objects.link(ship)
    unwrap(ship)

    # The source keeps the sharp edges and quads, easy to edit; the export applies the bevel and triangulates.
    bevel = ship.modifiers.new("Bevel", "BEVEL")
    bevel.width = BEVEL_WIDTH
    bevel.segments = 1
    bevel.limit_method = "ANGLE"
    bevel.angle_limit = math.radians(BEVEL_ANGLE)
    triangulate = ship.modifiers.new("Triangulate", "TRIANGULATE")
    triangulate.quad_method = "BEAUTY"
    triangulate.ngon_method = "BEAUTY"

    color, normal = bake(ship, directory)
    final_materials(mesh, color, normal)
    markers(ship)

    # Drop the bake materials, the detail node group and what the startup file brings, so the source holds the ship.
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = ship.evaluated_get(depsgraph).to_mesh()
    triangles = sum(len(polygon.vertices) - 2 for polygon in evaluated.polygons)
    ship.evaluated_get(depsgraph).to_mesh_clear()
    print("Saved %s: %d triangles, %s m" % (path, triangles, tuple(round(d, 3) for d in ship.dimensions)))


if __name__ == "__main__":
    main()
