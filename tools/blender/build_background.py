"""Build the sky behind the table (docs/ASSETS.md section 3, docs/ANIMATIONS.md section 6, docs/DIRECTION_ARTISTIQUE.md
§2.4 and §6.1: a dark sky, the vortex far away, wrecks adrift), and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_background.py -- \
        art-src/backgrounds/Fond.blend

Then export it for Unity:

    blender --background --disable-autoexec art-src/backgrounds/Fond.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Backgrounds/Fond.fbx --budget 100

The game places the model at the scene's origin, the table's centre (theme field Table Background). The game camera
sees the table from above and in front (GameScene: at 7.4 up and 10.6 back, tilted 33 degrees down, 42 degrees high):
what lies behind the table is the sky under the horizon, ahead. The model is two quads:
- Fond: a large backdrop, DISTANCE units from the origin, square to the direction the camera looks at, covering far
  more than it sees. Its texture holds a sky of faint cold stars, a dark nebula in rust and cold grey, dust swirling
  towards the vortex, and wrecks adrift, lit from behind by the vortex; its edges fade into the soot of the
  interface's background, so a camera that turns away (victory orbit) finds no border.
- Vortex: the vortex itself, a black core in a ring of light and spiral arms of glowing dust, standing in front of the
  backdrop in the empty sky over the table's middle, its origin at its centre: the game may turn it, and scale it
  as the End of Times comes closer (docs/DIRECTION_ARTISTIQUE.md §2.4), without touching the backdrop.
Both are meant to be drawn unlit (the game's import builds their materials, Editor/ThemeArtSync.cs). The wrecks are
rendered here by Cycles, the rest is drawn with numpy; the images are written next to the .blend file.
"""

import math
import os
import sys
import tempfile

import bmesh
import bpy
import mathutils
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import front_view as fv  # noqa: E402  (next to this script)

# The game camera (Unity: position (0, 7.4, -10.6), pitch 33 degrees), in Blender's axes: Unity (x, y, z) is Blender
# (-x, -z, y).
CAMERA = mathutils.Vector((0.0, 10.6, 7.4))
DISTANCE = 90.0  # of the backdrop from the origin
VIEW_PITCH = 37.0  # degrees under the horizon of the backdrop's middle, seen from the origin
HALF_FOV = 57.5  # degrees on each side of the backdrop's middle, across; its height follows the image's shape
FOND_SIZE = (4096, 2048)
VORTEX_PITCH = 24.5  # degrees under the horizon of the vortex's centre, seen from the game camera: in the empty sky
# over the table's middle, under the round's banner and the folded market (the view's top edge is at 12)
VORTEX_RANGE = 95.0  # from the game camera
VORTEX_ANGLE = 15.0  # degrees from the vortex's centre to its quad's edge, seen from the game camera
VORTEX_SIZE = 2048

# Linear colours (the textures are written in sRGB at the end).
SOOT = np.array([0.0044, 0.0048, 0.0056])  # #0E0F11, the interface's background
RUST = np.array([0.155, 0.043, 0.016])  # #6E3A22
COLD = np.array([0.05, 0.065, 0.085])
AMBER = np.array([0.75, 0.29, 0.03])  # #E0922F
HOT = np.array([1.0, 0.78, 0.45])

# Wrecks: pitch and heading from the game camera (degrees, heading positive to the right), range, size, seed.
WRECKS = [
    (-17.0, -26.0, 85.0, 1.6, 1),
    (-15.0, 22.0, 88.0, 1.3, 2),
    (-38.0, 36.0, 80.0, 1.2, 3),
    (-46.0, -40.0, 78.0, 1.4, 4),
    (-27.0, 9.0, 92.0, 0.8, 5),
]


def unity_direction(pitch, heading):
    """A direction in Blender's axes from a pitch under the horizon and a heading to the right of Unity's +Z."""
    p, h = math.radians(pitch), math.radians(heading)
    x, y, z = math.sin(h) * math.cos(p), math.sin(p), math.cos(h) * math.cos(p)  # Unity axes
    return mathutils.Vector((-x, -z, y))


def frame():
    """The backdrop's middle direction and its right and up axes."""
    ahead = unity_direction(-VIEW_PITCH, 0.0)
    right = ahead.cross(mathutils.Vector((0, 0, 1))).normalized()
    up = right.cross(ahead).normalized()
    return ahead, right, up


def extents():
    half_w = math.tan(math.radians(HALF_FOV))
    return half_w, half_w * FOND_SIZE[1] / FOND_SIZE[0]


def quad(name, centre, right, up, half_w, half_h, towards):
    corners = [centre + sx * half_w * right + sy * half_h * up for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(c) for c in corners], [], [(0, 1, 2, 3)])
    mesh.validate()
    if mesh.polygons[0].normal.dot(towards - centre) < 0:
        mesh.flip_normals()
    layer = mesh.uv_layers.new(name="UVMap")
    for loop in mesh.loops:
        corner = mesh.vertices[loop.vertex_index].co - centre
        layer.data[loop.index].uv = (0.5 + corner.dot(right) / (2 * half_w), 0.5 + corner.dot(up) / (2 * half_h))
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def vortex_centre():
    return CAMERA + unity_direction(-VORTEX_PITCH, 0.0) * VORTEX_RANGE


def vortex_on_backdrop():
    """Where the backdrop shows behind the vortex's centre, seen from the game camera, as (u, v)."""
    ahead, right, up = frame()
    middle = ahead * DISTANCE
    ray = unity_direction(-VORTEX_PITCH, 0.0)
    t = (middle - CAMERA).dot(ahead) / ray.dot(ahead)
    point = CAMERA + ray * t - middle
    half_w, half_h = extents()
    return 0.5 + point.dot(right) / (2 * half_w * DISTANCE), 0.5 + point.dot(up) / (2 * half_h * DISTANCE)


# ------------------------------------------------------------------------------------------------------------ noise

def to_linear(srgb):
    return np.where(srgb <= 0.04045, srgb / 12.92, np.power((srgb + 0.055) / 1.055, 2.4))


def swirl(x, y, arms, twist, rng, shape):
    """Spiral arms around the origin of (x, y): 1 on an arm, 0 between two, broken by noise."""
    radius = np.sqrt(x * x + y * y) + 1e-6
    angle = np.arctan2(y, x)
    warp = fv.fractal(rng, (shape[0] / 6, shape[0] / 18), shape, falloff=0.55)
    phase = arms * angle + twist * np.log(radius) + 5.0 * warp
    arm = (0.5 + 0.5 * np.sin(phase)) ** 3
    streaks = 0.5 + 0.5 * np.sin(9 * phase + 12 * warp)
    return arm * (0.55 + 0.45 * streaks), radius


# ---------------------------------------------------------------------------------------------------------- images

def paint_backdrop(wrecks):
    rng = np.random.default_rng(26)
    w, h = FOND_SIZE
    shape = (h, w)
    half_w, half_h = extents()
    xs = (np.arange(w) + 0.5) / w
    ys = (np.arange(h) + 0.5) / h
    colour = np.zeros(shape + (3,)) + SOOT

    # The nebula: slow clouds of rust and cold grey, very dim.
    clouds = fv.fractal(rng, (h / 2, h / 5, h / 14, h / 40), shape, falloff=0.55)
    cold = fv.fractal(rng, (h / 1.5, h / 4, h / 12), shape, falloff=0.55)
    colour += RUST * 0.09 * fv.smoothstep(clouds, 0.5, 0.85)[..., None]
    colour += COLD * 0.12 * fv.smoothstep(cold, 0.45, 0.8)[..., None]
    dust = fv.smoothstep(fv.fractal(rng, (h / 6, h / 16, h / 40, h / 100), shape, falloff=0.55), 0.45, 0.85)
    colour *= (1 - 0.35 * dust)[..., None]  # dark lanes of dust

    # Dust swirling into the vortex, brighter close to it.
    cu, cv = vortex_on_backdrop()
    x = (xs[None, :] - cu) * half_w / half_h * 2
    y = (ys[:, None] - cv) * 2
    arms, radius = swirl(x * np.ones(shape), y * np.ones(shape), 3, -5.0, rng, shape)
    halo = np.exp(-radius / 0.35)
    colour += (AMBER * 0.04 * (arms * halo)[..., None] + RUST * 0.06 * (arms * np.exp(-radius / 0.8))[..., None])

    # Stars: many faint and cold, a few brighter, some warmer.
    count = 6500
    sx, sy = rng.uniform(0, w, count), rng.uniform(0, h, count)
    bright = 0.05 + 0.5 * rng.random(count) ** 6
    tint = np.where(rng.random(count)[:, None] < 0.2, np.array([1.0, 0.85, 0.7]), np.array([0.8, 0.88, 1.0]))
    sigma = 0.55 + 0.6 * rng.random(count) ** 3
    for i in range(count):
        c0, r0 = int(sx[i]), int(sy[i])
        cs, rs = slice(max(0, c0 - 3), min(w, c0 + 4)), slice(max(0, r0 - 3), min(h, r0 + 4))
        gx, gy = np.meshgrid(np.arange(cs.start, cs.stop) + 0.5 - sx[i], np.arange(rs.start, rs.stop) + 0.5 - sy[i])
        spot = np.exp(-(gx * gx + gy * gy) / (2 * sigma[i] ** 2))
        colour[rs, cs] += (bright[i] * spot)[..., None] * tint[i]

    # The wrecks, rendered from the origin through the same frame.
    rgba = wrecks
    alpha = rgba[..., 3:4]
    colour = colour * (1 - alpha) + to_linear(rgba[..., :3]) * alpha

    # Edges fade into the soot of the interface's background.
    edge = np.minimum(np.minimum(xs[None, :], 1 - xs[None, :]) / 0.08, np.minimum(ys[:, None], 1 - ys[:, None]) / 0.1)
    fade = fv.smoothstep(edge, 0.0, 1.0)[..., None]
    colour = SOOT + (colour - SOOT) * fade
    return np.dstack([fv.to_srgb(colour), np.ones(shape)])


def paint_vortex():
    rng = np.random.default_rng(27)
    n = VORTEX_SIZE
    shape = (n, n)
    coords = (np.arange(n) + 0.5) / n * 2 - 1
    x, y = coords[None, :] * np.ones(shape), coords[:, None] * np.ones(shape)
    arms, radius = swirl(x, y, 3, -6.0, rng, shape)
    core, ring = 0.15, 0.165

    # Glowing dust: hot near the ring, amber, then rust, fading out before the quad's edge.
    heat = np.exp(-(radius - ring).clip(0) / 0.12)
    colour = RUST[None, None] * (1 - heat[..., None]) + (AMBER * (1 - heat[..., None] ** 2) + HOT * heat[..., None] ** 2) * heat[..., None]
    density = arms * np.exp(-(radius - ring).clip(0) / 0.28) * fv.smoothstep(1 - radius, 0.0, 0.25)
    light = np.clip(density * 1.4, 0, 1)
    photon = np.exp(-((radius - ring) / 0.008) ** 2)
    colour = colour * light[..., None] + HOT * photon[..., None]
    alpha = np.clip(light * 1.2 + photon, 0, 1)

    # The core: black, and it hides the stars behind it.
    inside = fv.smoothstep(core - radius, -0.004, 0.004)
    colour = colour * (1 - inside[..., None])
    alpha = np.maximum(alpha, inside)
    # Faint lensing darkness around the core.
    alpha = np.maximum(alpha, 0.85 * np.exp(-((radius - core) / 0.05).clip(0) ** 2) * (radius < ring + 0.1))
    return np.dstack([fv.to_srgb(colour), alpha])


def save_image(name, pixels, directory, alpha):
    h, w = pixels.shape[:2]
    image = bpy.data.images.new(name, w, h, alpha=alpha)
    image.pixels.foreach_set(pixels.astype(np.float32).ravel())
    path = os.path.join(directory, name + ".png")
    image.filepath_raw = path
    image.file_format = "PNG"
    if alpha:
        image.alpha_mode = "STRAIGHT"
    image.save()
    image.filepath = path
    return image


# ----------------------------------------------------------------------------------------------------------- wrecks

def wreck(name, centre, size, seed):
    """A torn hull section drifting: a caisson open at one end, bent plates, girders sticking out."""
    rng = np.random.default_rng(seed)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(0.5, 1.4, 0.4), verts=bm.verts)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=3, use_grid_fill=True)
    torn = [f for f in bm.faces if f.calc_center_median().y > 0.2 + 0.35 * rng.random()]
    bmesh.ops.delete(bm, geom=torn, context="FACES")
    for vert in bm.verts:
        if vert.co.y > 0.0:
            vert.co += mathutils.Vector(rng.normal(0, 0.06, 3))
    for _ in range(3):
        start = mathutils.Vector((rng.uniform(-0.4, 0.4), 0.5, rng.uniform(-0.3, 0.3)))
        end = start + mathutils.Vector((rng.normal(0, 0.2), rng.uniform(0.3, 0.8), rng.normal(0, 0.2)))
        girder = bmesh.ops.create_cube(bm, size=1.0)["verts"]
        direction = end - start
        matrix = mathutils.Matrix.Translation((start + end) / 2) @ direction.to_track_quat("Y", "Z").to_matrix().to_4x4() @ mathutils.Matrix.Diagonal((0.05, direction.length, 0.05, 1))
        bmesh.ops.transform(bm, matrix=matrix, verts=girder)
    fin = bmesh.ops.create_cube(bm, size=1.0)["verts"]
    bmesh.ops.transform(bm, matrix=mathutils.Matrix.Translation((0, -0.3, 0.45)) @ mathutils.Matrix.Diagonal((0.04, 0.5, 0.3, 1)), verts=fin)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    material = bpy.data.materials.new(name)
    bsdf = next(n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (0.05, 0.035, 0.028, 1.0)
    bsdf.inputs["Metallic"].default_value = 0.6
    bsdf.inputs["Roughness"].default_value = 0.55
    material.use_backface_culling = False
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    obj.location = centre
    obj.scale = (size, size, size)
    obj.rotation_euler = tuple(rng.uniform(0, 2 * math.pi, 3))
    bpy.context.scene.collection.objects.link(obj)
    return obj


def render_wrecks(folder):
    """The wrecks, lit from behind by the vortex, seen from the origin through the backdrop's frame, on transparency."""
    scene = bpy.context.scene
    objects = [wreck("Epave %d" % seed, CAMERA + unity_direction(pitch, heading) * distance, size, seed)
               for pitch, heading, distance, size, seed in WRECKS]
    ahead, right, up = frame()
    camera_data = bpy.data.cameras.new("Cadre")
    camera_data.sensor_fit = "HORIZONTAL"
    camera_data.angle = 2 * math.radians(HALF_FOV)
    camera_data.clip_end = 500.0
    camera = bpy.data.objects.new("Cadre", camera_data)
    camera.matrix_world = mathutils.Matrix((right, up, -ahead)).transposed().to_4x4()
    scene.collection.objects.link(camera)
    scene.camera = camera

    towards = (vortex_centre() - mathutils.Vector((0, 0, 0))).normalized()
    lights = []
    for colour, strength, direction in (((1.0, 0.45, 0.12), 3.0, -towards), ((0.35, 0.45, 0.6), 0.25, mathutils.Vector((0.3, 0.5, 1.0)).normalized())):
        light = bpy.data.lights.new("Lumiere", "SUN")
        light.color = colour
        light.energy = strength
        holder = bpy.data.objects.new("Lumiere", light)
        holder.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(holder)
        lights.append(holder)

    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.film_transparent = True
    scene.world = bpy.data.worlds.new("Noir")
    scene.world.color = (0, 0, 0)
    try:
        scene.view_settings.view_transform = "Standard"
    except TypeError:
        pass
    scene.render.resolution_x, scene.render.resolution_y = FOND_SIZE
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    path = os.path.join(folder, "wrecks.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    image = bpy.data.images.load(path)
    pixels = np.array(image.pixels[:], dtype=np.float32).reshape(FOND_SIZE[1], FOND_SIZE[0], 4)
    bpy.data.images.remove(image)
    for obj in objects + lights + [camera]:
        bpy.data.objects.remove(obj, do_unlink=True)
    return pixels


# ------------------------------------------------------------------------------------------------------------- main

def unlit_material(name, image, alpha):
    material = bpy.data.materials.new(name)
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (1, 1, 1, 1)
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
    if alpha:
        links.new(texture.outputs["Alpha"], bsdf.inputs["Alpha"])
    return material


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_background.py -- <output.blend>")
    path = os.path.abspath(argv[0])
    directory = os.path.dirname(path)
    os.makedirs(directory, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)

    with tempfile.TemporaryDirectory(prefix="vortex_fond_") as folder:
        wrecks = render_wrecks(folder)
    backdrop_image = save_image("Fond_BaseColor", paint_backdrop(wrecks), directory, alpha=False)
    vortex_image = save_image("Vortex_BaseColor", paint_vortex(), directory, alpha=True)

    ahead, right, up = frame()
    half_w, half_h = extents()
    origin = mathutils.Vector((0, 0, 0))
    backdrop = quad("Fond", ahead * DISTANCE, right, up, half_w * DISTANCE, half_h * DISTANCE, CAMERA)
    backdrop.data.materials.append(unlit_material("Fond", backdrop_image, alpha=False))

    centre = vortex_centre()
    facing = (CAMERA - centre).normalized()
    v_right = (-facing).cross(mathutils.Vector((0, 0, 1))).normalized()
    v_up = v_right.cross(-facing).normalized()
    half = VORTEX_RANGE * math.tan(math.radians(VORTEX_ANGLE))
    vortex = quad("Vortex", origin, v_right, v_up, half, half, facing * 10)
    vortex.location = centre  # its origin at its centre, so that it turns and grows in place
    vortex.data.materials.append(unlit_material("Vortex", vortex_image, alpha=True))

    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)
    print("Saved %s: backdrop %.0f x %.0f at %.0f, vortex %.0f wide at %s" % (
        path, 2 * half_w * DISTANCE, 2 * half_h * DISTANCE, DISTANCE, 2 * half, tuple(round(c, 1) for c in centre)))


main()
