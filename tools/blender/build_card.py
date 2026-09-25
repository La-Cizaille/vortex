"""Build the card body model (docs/ASSETS.md section 2, ADR-0017) and save it as a .blend source.

Usage, without opening Blender's window:

    blender --background --factory-startup --python tools/blender/build_card.py -- art-src/cards/Card.blend

The card is a simple parametric shape, so it is built by this script rather than by hand: changing a dimension
means changing a constant below and running it again. The saved .blend stays the source that gets exported
(tools/blender/export_unity.py) and can still be opened and reworked in Blender.

The body is 1 x 1.4 m and 0.02 m thick, with rounded corners and a rounded rim. It stands upright: width along X,
height along Z, thickness along Y. Its front faces +Y, which export_unity.py turns into -Z in Unity, the side the
card prefab shows to the camera. The game tints the body and lays the face (illustration, texts) over it.
"""

import math
import os
import sys

import bmesh
import bpy

WIDTH = 1.0
HEIGHT = 1.4
THICKNESS = 0.02
CORNER_RADIUS = 0.05
CORNER_SEGMENTS = 6
RIM_WIDTH = 0.004
RIM_SEGMENTS = 2


def output_path():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) != 1 or not argv[0].lower().endswith(".blend"):
        sys.exit("Usage: blender --background --factory-startup --python build_card.py -- <output.blend>")
    return os.path.abspath(argv[0])


def outline():
    """The rounded rectangle, counter-clockwise seen from +Y: (corner centre x, corner centre z, outward angle)."""
    half_w = WIDTH / 2 - CORNER_RADIUS
    half_h = HEIGHT / 2 - CORNER_RADIUS
    centres = [(half_w, half_h), (-half_w, half_h), (-half_w, -half_h), (half_w, -half_h)]
    return [
        (cx, cz, math.radians(90 * corner + 90 * step / CORNER_SEGMENTS))
        for corner, (cx, cz) in enumerate(centres)
        for step in range(CORNER_SEGMENTS + 1)]


def rim_profile():
    """Cross-section of the edge, from the front face to the back face: (outward offset, y) pairs."""
    front = []
    for step in range(RIM_SEGMENTS + 1):
        angle = math.radians(90 * step / RIM_SEGMENTS)
        front.append((-RIM_WIDTH * (1 - math.sin(angle)), THICKNESS / 2 - RIM_WIDTH * (1 - math.cos(angle))))
    return front + [(offset, -y) for offset, y in reversed(front)]


def build_mesh():
    # A ring of the rim profile at each point of the outline, joined by quads; the first and last points of every
    # ring close the front and back faces. Building the rim by hand keeps the triangle count exact and small.
    bm = bmesh.new()
    profile = rim_profile()
    rings = []
    for cx, cz, angle in outline():
        rings.append([
            bm.verts.new((cx + (CORNER_RADIUS + offset) * math.cos(angle), y, cz + (CORNER_RADIUS + offset) * math.sin(angle)))
            for offset, y in profile])

    for index, ring in enumerate(rings):
        following = rings[(index + 1) % len(rings)]
        for step in range(len(profile) - 1):
            bm.faces.new((ring[step], ring[step + 1], following[step + 1], following[step]))
    bm.faces.new([ring[0] for ring in rings])
    bm.faces.new([ring[-1] for ring in rings])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    # Flat front and back, smooth rim: the flat faces keep their own normal, the rim reads as rounded under light.
    uv = bm.loops.layers.uv.new("UVMap")
    for face in bm.faces:
        face.normal_update()
        flat = abs(face.normal.y) > 0.999
        face.smooth = not flat
        # Each side maps the whole card to 0..1, read the right way from that side (from +Y, +X is on the left).
        mirror = -1.0 if face.normal.y >= 0 else 1.0
        for loop in face.loops:
            loop[uv].uv = (0.5 + mirror * loop.vert.co.x / WIDTH, 0.5 + loop.vert.co.z / HEIGHT)

    mesh = bpy.data.meshes.new("Card")
    bm.to_mesh(mesh)
    bm.free()
    return mesh


def main():
    path = output_path()

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    mesh = build_mesh()
    material = bpy.data.materials.new("Corps")
    mesh.materials.append(material)
    card = bpy.data.objects.new("Card", mesh)
    scene.collection.objects.link(card)

    # The source keeps quads and n-gons, easy to edit; the export triangulates through this modifier.
    triangulate = card.modifiers.new("Triangulate", "TRIANGULATE")
    triangulate.quad_method = "BEAUTY"
    triangulate.ngon_method = "BEAUTY"

    # Drop what the startup file brings and nothing uses (default material, brushes), so the source holds the card only.
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0  # no Card.blend1 backup: the previous version is in Git
    bpy.ops.wm.save_as_mainfile(filepath=path)
    print("Saved %s (%d faces before triangulation)" % (path, len(mesh.polygons)))


if __name__ == "__main__":
    main()
