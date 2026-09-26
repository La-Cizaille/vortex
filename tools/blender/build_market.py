"""Build the black market's rack (docs/DIRECTION_ARTISTIQUE.md §2.3, §6.3, §6.5 bis: "le marché noir devient un rack de
modules récupérés"), and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --disable-autoexec --python tools/blender/build_market.py -- art-src/market/Marche.blend

Then export it for Unity:

    blender --background --disable-autoexec art-src/market/Marche.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Market/Marche.fbx --budget 3000

A salvage rack standing upright like the cockpit (tools/blender/front_view.py: width along X, height along Z, its front
facing +Y), sized for the modules at their own size (build_card.py: 1 x 1.4, connector at the bottom). As in the
interface (docs/INTERFACE.md §3.3), the attack market on the left, the defense market on the right; each has a crate
at its outer end, where its deck stands, stencilled as fallen off a cargo, and a shelf of five slots, each with a pin
socket that takes a module's connector. A central post carries a caged lamp; a header carries the sign.

Empty markers, where the game would put the cards (their X and Z scales give the module's size):
Emplacement_ATK_1 to _5 and Emplacement_DEF_1 to _5, from left to right; Pioche_ATK and Pioche_DEF, the decks in their
crates. The rack is not in the game yet: the market is an interface (ADR-0015); the game may put it behind the open
market, or build the market on it.

Materials: Cadre (blackened steel), Fond (the perforated back panel), Peinture (the sign and the crates), Acier (bare
steel: clamps, bolts, the lamp's cage), Gaine (cables), Lampe (the bulb, the only light). The first three share one
texture drawn here in the front view, like the cockpit's.
"""

import math
import os
import shutil
import sys
import tempfile

import bpy
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import front_view as fv  # noqa: E402  (next to this script)

NAME = "Marche"
WIDTH, HEIGHT = 14.4, 3.6  # the texture's frame; the rack fills its width
TEX_W, TEX_H = 2048, 512  # 7 mm texels, square
STEEL = (1024, 256)

MODULE = (1.0, 1.4)
MODULE_PLANE = 0.08  # depth of the modules' back, in front of the back panel
SLOTS = 5
SLOT_Z = -0.1  # the modules' centre
SHELF = (-1.02, -0.8, 0.2)  # bottom z, top z, front y of the shelf beam
HEADER = (0.72, 1.12, 0.14)  # bottom z, top z, front y of the header beam
BACK = (-1.02, 0.72, -0.12, 0.0)  # bottom z, top z, back y, front y of the back panel
UPRIGHTS = (6.95, 0.0)  # x of the outer uprights (both sides), of the central post
CRATE = (1.2, -1.02, -0.18, 0.9)  # width, bottom z, top z, depth
SIDE = (6.8, 0.26)  # outer and inner x of each side's usable width
SIGN = (-1.5, 1.5, 0.77, 1.07)
LAMP = (0.0, 0.5, 0.09, 0.32)  # x, z, radius of the bulb, depth: under the header, over the central post

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STENCIL_FONT = os.path.join(REPO, "unity", "Assets", "_Vortex", "Art", "Fonts", "BigShouldersStencil", "BigShouldersStencilDisplay-Black.ttf")

BLACKENED = 0.03
BARE_STEEL = np.array([0.15, 0.155, 0.165])
RUST = np.array([0.1, 0.028, 0.01])
BONE = np.array([0.62, 0.55, 0.39])
CRATE_PAINT = np.array([0.085, 0.075, 0.04])  # faded olive drab
HAZARD = np.array([0.69, 0.37, 0.006])
INK = np.array([0.007, 0.0065, 0.006])
BRASS = np.array([0.45, 0.3, 0.08])
LAMP_COLOUR = (1.0, 0.45, 0.12)

# name: (colour, metallic, smoothness, emission, texture)
COLOURS = {
    "Cadre": ((1.0, 1.0, 1.0), 0.7, 0.5, None, "Marche"),
    "Fond": ((1.0, 1.0, 1.0), 0.6, 0.3, None, "Marche"),
    "Peinture": ((1.0, 1.0, 1.0), 0.15, 0.35, None, "Marche"),
    "Acier": ((1.0, 1.0, 1.0), 0.9, 0.55, None, "Marche_Acier"),
    "Gaine": ((0.018, 0.018, 0.02), 0.0, 0.35, None, None),
    "Lampe": (LAMP_COLOUR, 0.0, 0.8, LAMP_COLOUR, None),
}
PAINTED = ("Cadre", "Fond", "Peinture")
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
        image = nodes.new("ShaderNodeTexImage")
        image.image = IMAGES[texture + "_BaseColor"]
        links.new(image.outputs["Color"], shader.inputs["Base Color"])
        relief = nodes.new("ShaderNodeTexImage")
        relief.image = IMAGES[texture + "_Normal"]
        normal_map = nodes.new("ShaderNodeNormalMap")
        links.new(relief.outputs["Color"], normal_map.inputs["Color"])
        links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])
    if emission is not None:
        # Above the Bloom threshold (1.5) in red only: it glows amber, not white (the game uses no tone mapping).
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 2.0
    made.diffuse_color = (*colour, 1.0)
    return made


CANVAS = fv.Canvas(WIDTH, HEIGHT, TEX_W, TEX_H)
MODEL = fv.Model(CANVAS, material)


def slot_centres(side):
    """Centres x of a side's five slots, from left to right (side -1: attack, left; +1: defense, right)."""
    outer, inner = SIDE
    start = outer - CRATE[0] - 0.2
    pitch = (start - inner) / SLOTS
    xs = [side * (inner + pitch * (k + 0.5)) for k in range(SLOTS)]
    return sorted(xs)


def crate_centre(side):
    return side * (SIDE[0] - CRATE[0] / 2)


# ----------------------------------------------------------------------------------------------------------- parts

def frame():
    bottom, top, back, front = BACK
    MODEL.prism("Panneau arriere", fv.rectangle(-UPRIGHTS[0], UPRIGHTS[0], bottom, top), back, front, "Fond")
    z0, z1, depth = SHELF
    MODEL.prism("Etagere", fv.chamfered(-UPRIGHTS[0], UPRIGHTS[0], z0, z1, 0.03), 0.0, depth, "Cadre")
    z0, z1, depth = HEADER
    MODEL.prism("Fronton", fv.chamfered(-UPRIGHTS[0] - 0.12, UPRIGHTS[0] + 0.12, z0, z1, 0.05), -0.12, depth, "Cadre")
    for x, half, top in ((-UPRIGHTS[0], 0.15, HEADER[1] - 0.02), (UPRIGHTS[0], 0.15, HEADER[1] - 0.02), (UPRIGHTS[1], 0.18, HEADER[0])):
        MODEL.prism("Montant %.1f" % x, fv.chamfered(x - half, x + half, -1.3, top, 0.03), -0.14, 0.18, "Cadre")
        MODEL.prism("Pied %.1f" % x, fv.chamfered(x - half - 0.06, x + half + 0.06, -1.36, -1.22, 0.02), -0.18, 0.22, "Peinture")
        for z in (-0.9, -0.2, 0.5):
            MODEL.prism("Boulon %.1f %.1f" % (x, z), fv.circle(x, z, 0.035, 6), 0.18, 0.2, "Acier")
    x0, x1, z0, z1 = SIGN
    MODEL.prism("Enseigne", fv.chamfered(x0, x1, z0, z1, 0.04), HEADER[2], HEADER[2] + 0.03, "Peinture")


def shelves():
    """Each side's five slots: a pin socket on the shelf and a clamp on each side of the module's foot."""
    width, _ = MODULE
    for side, label in ((-1, "ATK"), (1, "DEF")):
        for number, x in enumerate(slot_centres(side), start=1):
            MODEL.prism("Prise %s %d" % (label, number), fv.chamfered(x - 0.41, x + 0.41, SHELF[1] - 0.02, SHELF[1] + 0.09, 0.02), MODULE_PLANE - 0.05, MODULE_PLANE + 0.1, "Cadre")
            for edge in (-1, 1):
                cx = x + edge * (width / 2 + 0.03)
                MODEL.prism("Taquet %s %d %d" % (label, number, edge), fv.chamfered(cx - 0.03, cx + 0.03, SHELF[1], SHELF[1] + 0.3, 0.01), MODULE_PLANE - 0.02, MODULE_PLANE + 0.12, "Acier")
            MODEL.empty("Emplacement_%s_%d" % (label, number), x, MODULE_PLANE, SLOT_Z, width, MODULE[1])


def crates():
    width, bottom, top, depth = CRATE
    for side, label in ((-1, "ATK"), (1, "DEF")):
        x = crate_centre(side)
        MODEL.prism("Caisse " + label, fv.chamfered(x - width / 2, x + width / 2, bottom, top, 0.04), 0.0, depth, "Peinture")
        for edge in (-1, 1):
            MODEL.prism("Coin %s %d" % (label, edge), fv.rectangle(x + edge * width / 2 - 0.05, x + edge * width / 2 + 0.05, bottom, top), depth, depth + 0.02, "Acier")
        MODEL.empty("Pioche_" + label, x, depth / 2, SLOT_Z, MODULE[0], MODULE[1])


def lamp_and_cables():
    x, z, radius, y = LAMP
    MODEL.tube("Bras lampe", [(x, 0.1, HEADER[0]), (x, y, HEADER[0] - 0.04), (x, y, z + 0.14)], 0.02, "Acier")
    for k in range(6):
        a = 2 * math.pi * k / 6
        MODEL.tube("Cage %d" % k, [(x + 0.14 * math.cos(a), y + 0.14 * math.sin(a), z - 0.14), (x + 0.14 * math.cos(a), y + 0.14 * math.sin(a), z + 0.14)], 0.008, "Acier", sides=4)
    MODEL.cylinder_x("Lampe", x - radius * 0.8, x + radius * 0.8, y, z, radius, "Lampe", sides=10)
    for side in (-1, 1):
        points = [(side * t, HEADER[2] + 0.03, HEADER[0] + 0.02 - 0.1 * math.sin(math.pi * t / 6.8)) for t in np.linspace(0.3, 6.8, 12)]
        MODEL.tube("Cable %d" % side, points, 0.025, "Gaine")


# ---------------------------------------------------------------------------------------------------------- texture

def kind_of(name, material_name):
    if material_name == "Fond":
        return "panel"
    if name == "Enseigne":
        return "sign"
    if name.startswith("Caisse"):
        return "crate"
    if name.startswith("Pied"):
        return "hazard"
    if name.startswith("Prise"):
        return "socket"
    return "metal"


def paint_rack():
    rng = np.random.default_rng(12)
    shape = CANVAS.shape
    X, Z = CANVAS.X[None, :], CANVAS.Z[:, None]
    kinds, depth, front, outline = MODEL.surfaces(PAINTED, kind_of, -0.12, casts_no_shadow=lambda name, material_name: material_name == "Lampe")
    body = ~kinds.pop("base")
    none = np.zeros(shape, dtype=bool)
    metal = kinds.get("metal", none) | kinds.get("socket", none)
    painted = kinds.get("sign", none) | kinds.get("crate", none) | kinds.get("hazard", none)
    panel = kinds.get("panel", none)
    height = np.zeros(shape)
    mottle = fv.fractal(rng, (48, 16, 4), shape)
    colour = np.zeros(shape + (3,)) + (BLACKENED * (0.6 + 0.6 * mottle))[..., None]

    # The back panel: perforated, darker; the sign in bone; the crates in faded olive; the feet in hazard stripes.
    holes = panel & (np.hypot(np.mod(X, 0.12) - 0.06, np.mod(Z, 0.12) - 0.06) < 0.022)
    colour[panel] *= 0.7
    colour[holes] = 0.003
    height -= np.where(holes, 1.0, 0.0)
    for kind, value in (("sign", BONE), ("crate", CRATE_PAINT)):
        mask = kinds.get(kind, none)
        colour[mask] = value * (0.85 + 0.3 * mottle[mask])[..., None]
    hazard = kinds.get("hazard", none)
    stripes = hazard & (np.mod((X + Z) / 0.16, 1.0) < 0.5)
    colour[hazard] = 0.018
    colour[stripes] = HAZARD * (0.85 + 0.2 * mottle[stripes])[..., None]
    crate = kinds.get("crate", none)
    ribs = crate & (np.mod(Z / 0.17, 1.0) < 0.12)
    height += np.where(ribs, 0.6, 0.0)
    socket = kinds.get("socket", none)
    for side in (-1, 1):
        for x in slot_centres(side):
            slot = socket & (np.abs(X - x) < 0.39) & (Z > SHELF[1] + 0.03) & (Z < SHELF[1] + 0.06)
            colour[slot] = 0.002
            colour[slot & (np.mod((X - x) / 0.04, 1.0) < 0.45)] = BRASS
            height -= np.where(slot, 1.0, 0.0)

    rivets = [(x, z) for x in np.arange(-6.8, 6.85, 0.34) for z in (SHELF[0] + 0.05, SHELF[1] - 0.05, HEADER[0] + 0.06, HEADER[1] - 0.06)]
    rivets += [(x, z) for x in (SIGN[0] + 0.07, SIGN[1] - 0.07) for z in (SIGN[2] + 0.07, SIGN[3] - 0.07)]
    rivet_mask = CANVAS.rivets(rivets, 0.02, colour, height)

    clean = fv.smoothstep(np.where(kinds.get("sign", none), fv.blur(kinds.get("sign", none).astype(np.float32), 8), 0.0), 0.5, 0.9)
    chips = fv.weather(CANVAS, rng, colour, height, dict(
        metal=metal | panel, painted=painted, clean=clean, rivets=rivet_mask, body=body, outline=outline, depth=depth, front=front),
        dict(bare_steel=BARE_STEEL, blackened=BLACKENED, rust=RUST, scratches=(260, 0.25), shadow=(8, 0.12), low=(0.3, 1.3), chips=(2, 0.6, 4, 0.72)))

    # Stencil markings: the sign, and each crate's origin.
    folder = tempfile.mkdtemp(prefix="vortex_market_")
    try:
        markings = [("MARCHÉ NOIR", SIGN[0] + 0.15, SIGN[1] - 0.15, SIGN[2] + 0.05, SIGN[3] - 0.05)]
        for side in (-1, 1):
            x = crate_centre(side)
            markings.append(("TOMBÉ D'UN CARGO", x - 0.5, x + 0.5, -0.75, -0.55))
        for text, x0, x1, z0, z1 in markings:
            cover = CANVAS.stamp(fv.render_text(text, STENCIL_FONT, folder), x0, x1, z0, z1) * 0.9 * (1 - 0.5 * chips)
            colour[:] = colour * (1 - cover[..., None]) + INK * cover[..., None]
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    IMAGES["Marche_BaseColor"].pixels.foreach_set(np.dstack([fv.to_srgb(colour), np.ones(shape)]).astype(np.float32).ravel())
    IMAGES["Marche_Normal"].pixels.foreach_set(fv.normal_from(fv.blur(height, 1), 1.0).astype(np.float32).ravel())

    steel, relief = fv.brushed_steel(rng, (STEEL[1], STEEL[0]), 0.11, RUST)
    IMAGES["Marche_Acier_BaseColor"].pixels.foreach_set(np.dstack([fv.to_srgb(steel), np.ones((STEEL[1], STEEL[0]))]).astype(np.float32).ravel())
    IMAGES["Marche_Acier_Normal"].pixels.foreach_set(fv.normal_from(relief, 0.6).astype(np.float32).ravel())


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --disable-autoexec --python build_market.py -- <output.blend>")
    path = os.path.abspath(argv[0])
    directory = os.path.dirname(path)
    os.makedirs(directory, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name, width, height in (("Marche", TEX_W, TEX_H), ("Marche_Acier", *STEEL)):
        for image in fv.textured_images(name, width, height):
            IMAGES[image.name] = image

    frame()
    shelves()
    crates()
    lamp_and_cables()
    paint_rack()
    fv.save_images(IMAGES.values(), directory)
    rack = MODEL.join(NAME, {"Lampe"})
    bpy.data.objects["Lampe"].parent = rack
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    print("Saved %s: %d triangles, %d objects" % (path, triangles, len(meshes)))


main()
