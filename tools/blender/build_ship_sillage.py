"""Build the first ship, Sillage (docs/ASSETS.md section 2, ARB-76, ARB-77), and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_ship_sillage.py -- \
        art-src/ships/Ship_Sillage.blend

The shape was drafted with the designer in a live Blender session (Blender MCP), from an image they chose as a
reference, then kept here so that changing a proportion means changing a number below and running the script again.
The saved .blend stays the source that gets exported (tools/blender/export_unity.py) and can still be reworked by
hand in Blender; from then on, this script is no longer run.

The ship is about 2 m long and 1.4 m wide, nose towards -Y, top towards +Z, origin at its centre. It is built from
lofted sections (fuselage, canopy, spine, nacelles) and flat plates (fins, winglets, rear comb), low-poly, with a
bevel on its sharp edges so that they catch the light.

Three materials (docs/ASSETS.md section 2):
- Siege: the light hull, which the game tints with the seat colour;
- Coque: the dark parts and the amber canopy;
- Feux: the glowing back of the two reactors, the only light of the ship (emission above 1, for Bloom).
Siege and Coque share one texture atlas, baked here from procedural shaders: panel lines, a slight tone per panel
and some wear in the base colour (<name>_BaseColor.png), the panel grooves in the relief (<name>_Normal.png). The
images are written next to the .blend.
"""

import math
import os
import sys

import bmesh
import bpy

NAME = "Ship_Sillage"
TEXTURE_SIZE = 1024
BEVEL_WIDTH = 0.006
BEVEL_ANGLE = 30  # degrees: only edges sharper than this get a bevel
# Depth of the reactor recess: shallow, so that the glow shows from the table camera, well above the ship.
REACTOR_DEPTH = 0.02
# Reactor glow: a saturated orange, strong enough for Bloom (threshold 1.5) while its green stays under 1, so that
# the screen shows orange rather than a colour clipped to white (the game uses no tone mapping).
REACTOR_COLOR = (1.0, 0.35, 0.06)
REACTOR_STRENGTH = 2.5

# Material slots while building. The canopy has its own colour in the atlas, then joins Coque (FINAL_SLOT).
SIEGE, COQUE, VERRE, FEUX = range(4)
FINAL_SLOT = {SIEGE: 0, COQUE: 1, VERRE: 1, FEUX: 2}

# Panel grid of the textures, in metres along X, Y, Z; lines are GROOVE wide.
PANEL = (0.18, 0.22, 0.12)
PANEL_OFFSET = (0.09, 0.05, 0.03)
GROOVE = 0.004


# ---------------------------------------------------------------------------------------------------------- shape

def loft(bm, sections, band_mats, cap_start=None, cap_end=None, first_mat=None):
    """Joins rings of points (same count, going around) with quads.

    band_mats is one material per band (or a single material for all); first_mat overrides the first segment."""
    rings = [[bm.verts.new(p) for p in section] for section in sections]
    count = len(rings[0])
    for segment, (a, b) in enumerate(zip(rings, rings[1:])):
        for band in range(count):
            nxt = (band + 1) % count
            face = bm.faces.new((a[band], a[nxt], b[nxt], b[band]))
            if first_mat is not None and segment == 0:
                face.material_index = first_mat
            elif isinstance(band_mats, int):
                face.material_index = band_mats
            else:
                face.material_index = band_mats[band]
    if cap_start is not None:
        bm.faces.new(list(reversed(rings[0]))).material_index = cap_start
    if cap_end is not None:
        bm.faces.new(rings[-1]).material_index = cap_end
    return rings


def fuselage(bm):
    # y, top half-width, top z, ridge z, chine half-width, chine z, bottom half-width, bottom z
    stations = [
        (-1.00, 0.15, -0.030, -0.025, 0.20, -0.055, 0.15, -0.075),
        (-0.93, 0.16, -0.010, 0.000, 0.22, -0.050, 0.17, -0.090),
        (-0.60, 0.16, 0.050, 0.065, 0.24, -0.030, 0.19, -0.110),
        (-0.30, 0.15, 0.100, 0.110, 0.25, -0.020, 0.20, -0.120),
        (0.10, 0.15, 0.120, 0.130, 0.26, -0.010, 0.20, -0.130),
        (0.55, 0.15, 0.120, 0.130, 0.25, 0.000, 0.19, -0.120),
        (0.85, 0.13, 0.110, 0.120, 0.21, 0.000, 0.15, -0.100),
    ]
    sections = []
    for y, wt, zt, zc, wm, zm, wb, zb in stations:
        sections.append([(0, y, zc), (wt, y, zt), (wm, y, zm), (wb, y, zb), (0, y, zb), (-wb, y, zb), (-wm, y, zm), (-wt, y, zt)])
    # Top and upper sides light, lower sides and belly dark; the nose segment is dark.
    bands = [SIEGE, SIEGE, COQUE, COQUE, COQUE, COQUE, SIEGE, SIEGE]
    loft(bm, sections, bands, cap_start=COQUE, cap_end=COQUE, first_mat=COQUE)


def canopy(bm):
    # y, bottom half-width, top half-width, bottom z, top z
    stations = [(-0.64, 0.07, 0.05, 0.03, 0.06), (-0.45, 0.11, 0.07, 0.07, 0.17), (-0.22, 0.11, 0.07, 0.10, 0.21), (-0.12, 0.10, 0.06, 0.11, 0.20)]
    sections = [[(ht, y, zt), (hb, y, zb), (-hb, y, zb), (-ht, y, zt)] for y, hb, ht, zb, zt in stations]
    loft(bm, sections, [VERRE, COQUE, VERRE, VERRE], cap_start=VERRE, cap_end=COQUE)


def spine(bm):
    stations = [(-0.16, 0.09, 0.05, 0.10, 0.19), (0.60, 0.09, 0.05, 0.10, 0.19), (0.82, 0.07, 0.04, 0.10, 0.16)]
    sections = [[(ht, y, zt), (hb, y, zb), (-hb, y, zb), (-ht, y, zt)] for y, hb, ht, zb, zt in stations]
    loft(bm, sections, [SIEGE, COQUE, SIEGE, SIEGE], cap_start=SIEGE, cap_end=COQUE)


def box(bm, x0, x1, y0, y1, z0, z1, mat):
    sections = [[(x1, y, z1), (x1, y, z0), (x0, y, z0), (x0, y, z1)] for y in (y0, y1)]
    loft(bm, sections, mat, cap_start=mat, cap_end=mat)


def turret(bm):
    box(bm, -0.05, 0.05, -0.03, 0.07, 0.18, 0.23, COQUE)
    for x in (-0.022, 0.022):
        box(bm, x - 0.007, x + 0.007, -0.22, -0.03, 0.198, 0.212, COQUE)


def octagon(cx, cz, hw, hh, ch, y, scale):
    """An octagonal section (a rectangle with cut corners) centred on (cx, cz), at depth y."""
    w, h, c = hw * scale, hh * scale, ch * scale
    pts = [(w - c, h), (w, h - c), (w, -h + c), (w - c, -h), (-w + c, -h), (-w, -h + c), (-w, h - c), (-w + c, h)]
    return [(cx + x, y, cz + z) for x, z in pts]


def nacelle(bm, side):
    shape = (0.36 * side, -0.01, 0.12, 0.11, 0.03)
    bands = [SIEGE, SIEGE, COQUE, COQUE, COQUE, SIEGE, SIEGE, SIEGE]  # everything but the belly
    rings = loft(bm, [octagon(*shape, 0.18, 0.6), octagon(*shape, 0.32, 1.0), octagon(*shape, 0.86, 1.0)], bands, cap_start=COQUE)
    # The reactor: a frame, a recess, and a glowing back face.
    frame = [bm.verts.new(p) for p in octagon(*shape, 0.86, 0.72)]
    recess = [bm.verts.new(p) for p in octagon(*shape, 0.86 - REACTOR_DEPTH, 0.72)]
    outer = rings[-1]
    for i in range(8):
        j = (i + 1) % 8
        bm.faces.new((outer[i], outer[j], frame[j], frame[i])).material_index = SIEGE
        bm.faces.new((frame[i], frame[j], recess[j], recess[i])).material_index = COQUE
    bm.faces.new(recess).material_index = FEUX


def plate(bm, outline, z0, z1, top_mat, edge_mat, lead_edge):
    """A flat part: an outline in (x, y) extruded between z0 and z1; edge number lead_edge gets edge_mat."""
    bottom = [bm.verts.new((x, y, z0)) for x, y in outline]
    top = [bm.verts.new((x, y, z1)) for x, y in outline]
    bm.faces.new(top).material_index = top_mat
    bm.faces.new(list(reversed(bottom))).material_index = COQUE
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bottom[i], bottom[j], top[j], top[i])).material_index = edge_mat if i == lead_edge else top_mat


def winglet(bm, side):
    outline = [(0.47 * side, 0.50), (0.70 * side, 0.68), (0.70 * side, 0.84), (0.47 * side, 0.84)]
    if side < 0:
        outline = list(reversed(outline))
    plate(bm, outline, -0.035, -0.012, SIEGE, COQUE, 0 if side > 0 else 2)


def fin(bm, side):
    # Profile in (y, height), canted outward by 12 degrees, 0.025 thick.
    profile = [(0.36, 0.0), (0.82, 0.0), (0.86, 0.42), (0.72, 0.42)]
    base_x, base_z, half, cant = 0.29, 0.08, 0.0125, math.tan(math.radians(12))
    inner = [bm.verts.new((side * (base_x - half + h * cant), y, base_z + h)) for y, h in profile]
    outer = [bm.verts.new((side * (base_x + half + h * cant), y, base_z + h)) for y, h in profile]
    for ring, flip in ((inner, True), (outer, False)):
        bm.faces.new(list(reversed(ring)) if flip else ring).material_index = SIEGE
    for i in range(4):
        j = (i + 1) % 4
        mat = COQUE if i == 3 else SIEGE  # edge 3 is the leading edge (top front to base front)
        bm.faces.new((inner[i], inner[j], outer[j], outer[i])).material_index = mat


def comb(bm):
    box(bm, -0.12, 0.12, 0.84, 0.92, -0.07, 0.03, COQUE)
    for x in (-0.09, -0.03, 0.03, 0.09):
        box(bm, x - 0.012, x + 0.012, 0.92, 0.99, -0.05, 0.01, COQUE)


def build_mesh():
    bm = bmesh.new()
    fuselage(bm)
    canopy(bm)
    spine(bm)
    turret(bm)
    comb(bm)
    for side in (1, -1):
        nacelle(bm, side)
        winglet(bm, side)
        fin(bm, side)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    mesh = bpy.data.meshes.new(NAME)
    # Material slots before the geometry: clearing a mesh's materials also drops its face material indices.
    for _ in range(4):
        mesh.materials.append(None)
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = False  # faceted low-poly look; the bevels catch the light
    return mesh


def unwrap(ship):
    bpy.context.view_layer.objects.active = ship
    ship.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.01, correct_aspect=True, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")


# -------------------------------------------------------------------------------------------------------- texture

def detail_group():
    """Node group giving, at each point of the hull: Lines (1 in a panel groove), Tone (0..1 per panel), Wear (0..1)."""
    group = bpy.data.node_groups.new("SillageDetail", "ShaderNodeTree")
    for name in ("Lines", "Tone", "Wear"):
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

    coords = nodes.new("ShaderNodeTexCoord").outputs["Object"]
    position = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coords, position.inputs[0])
    normal = nodes.new("ShaderNodeSeparateXYZ")
    links.new(nodes.new("ShaderNodeNewGeometry").outputs["Normal"], normal.inputs[0])

    # A groove where the surface crosses a grid plane; a face parallel to a plane family ignores it (it would be
    # all groove or none).
    lines = None
    for axis in range(3):
        size, offset = PANEL[axis], PANEL_OFFSET[axis]
        t = math_node("DIVIDE", math_node("SUBTRACT", position.outputs[axis], offset), size)
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
    wear.inputs["Scale"].default_value = 6.0
    wear.inputs["Detail"].default_value = 4.0
    wear_range = nodes.new("ShaderNodeMapRange")
    links.new(coords, wear.inputs["Vector"])
    links.new(wear.outputs["Fac"], wear_range.inputs["Value"])
    wear_range.inputs["From Min"].default_value = 0.45
    wear_range.inputs["From Max"].default_value = 0.75
    links.new(wear_range.outputs[0], out.inputs["Wear"])
    return group


# Bake look of each building slot: base grey or colour, tone and wear amounts, groove colour and depth.
BAKE_LOOK = {
    SIEGE: dict(color=(0.82, 0.82, 0.82), tone=0.08, wear=0.10, groove=(0.35, 0.35, 0.35), depth=0.6),
    COQUE: dict(color=(0.05, 0.05, 0.055), tone=0.35, wear=0.15, groove=(0.018, 0.018, 0.02), depth=0.6),
    VERRE: dict(color=(0.25, 0.17, 0.08), tone=0.0, wear=0.0, groove=(0.25, 0.17, 0.08), depth=0.0),
    FEUX: dict(color=REACTOR_COLOR, tone=0.0, wear=0.0, groove=REACTOR_COLOR, depth=0.0),
}


def bake_material(slot, group, target):
    """A throwaway material that shows the procedural look of a slot, with the bake target as active node."""
    look = BAKE_LOOK[slot]
    material = bpy.data.materials.new("bake_%d" % slot)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    detail = nodes.new("ShaderNodeGroup")
    detail.node_tree = group

    # base * (1 - tone/2 + tone * Tone) * (1 - wear * Wear), then towards the groove colour in the grooves.
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
    grooved = nodes.new("ShaderNodeMix")
    grooved.data_type = "RGBA"
    links.new(detail.outputs["Lines"], grooved.inputs["Factor"])
    links.new(tinted.outputs["Result"], grooved.inputs["A"])
    grooved.inputs["B"].default_value = (*look["groove"], 1.0)
    links.new(grooved.outputs["Result"], bsdf.inputs["Base Color"])

    bump = nodes.new("ShaderNodeBump")
    bump.invert = True  # grooves go in
    bump.inputs["Strength"].default_value = look["depth"]
    bump.inputs["Distance"].default_value = 0.002
    links.new(detail.outputs["Lines"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    image_node = nodes.new("ShaderNodeTexImage")
    image_node.image = target
    nodes.active = image_node
    return material, image_node


def bake(ship, directory):
    color = bpy.data.images.new(NAME + "_BaseColor", TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
    normal = bpy.data.images.new(NAME + "_Normal", TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
    normal.colorspace_settings.name = "Non-Color"

    group = detail_group()
    targets = []
    for slot in range(4):
        material, image_node = bake_material(slot, group, color)
        ship.data.materials[slot] = material
        targets.append(image_node)

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 16  # enough to smooth the groove edges
    scene.render.bake.margin = 8
    bpy.context.view_layer.objects.active = ship
    ship.select_set(True)

    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, margin=8, use_clear=True)
    for image_node in targets:
        image_node.image = normal
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", margin=8, use_clear=True)

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
    mesh.materials.append(textured_material("Siege", color, normal, roughness=0.45))
    mesh.materials.append(textured_material("Coque", color, normal, roughness=0.6))
    mesh.materials.append(light_material("Feux", REACTOR_COLOR, strength=REACTOR_STRENGTH))
    mesh.polygons.foreach_set("material_index", [FINAL_SLOT[slot] for slot in slots])
    mesh.update()


# ------------------------------------------------------------------------------------------------------------ main

def output_path():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_ship_sillage.py -- <output.blend>")
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
