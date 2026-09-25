"""Export the open .blend file to an FBX file for Unity (conventions: docs/ASSETS.md, ADR-0016).

Usage, without opening Blender's window:

    blender --background --disable-autoexec art-src/ships/Ship_Faucon.blend --python tools/blender/export_unity.py -- \
        unity/Assets/_Vortex/Art/Ships/Ship_Faucon.fbx --budget 5000

(--disable-autoexec keeps the .blend file from running the scripts it may embed.)

What it does:
- checks the scene first and refuses to export when a check fails: metric units at scale 1 (1 Blender unit =
  1 metre = 1 Unity unit), object scales applied, triangle count within the budget;
- exports the meshes (with their modifiers applied) and empties, never cameras or lights, with the axis
  conversion Unity expects (Y up, -Z forward) and the transforms baked in, so objects arrive in Unity without
  rotation or scale: Blender's -Y becomes Unity's +Z (checked on the first export, 2026-09-25);
- copies the textures into a <name>.fbm folder next to the FBX file, where Unity links them to the materials.
"""

import argparse
import os
import sys

import bpy


def parse_arguments():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(prog="export_unity.py", description="Export the open .blend file to FBX for Unity.")
    parser.add_argument("output", help="FBX file to write, e.g. unity/Assets/_Vortex/Art/Ships/Ship_Faucon.fbx")
    parser.add_argument("--budget", type=int, default=5000, help="maximum number of triangles (default 5000)")
    return parser.parse_args(argv)


def triangle_count(obj, depsgraph):
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        return sum(len(polygon.vertices) - 2 for polygon in mesh.polygons)
    finally:
        evaluated.to_mesh_clear()


def check_scene(scene, budget):
    problems = []
    units = scene.unit_settings
    if units.system != "METRIC" or abs(units.scale_length - 1.0) > 1e-6:
        problems.append("units must be metric with a unit scale of 1 (Scene properties > Units)")

    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    if not meshes:
        problems.append("the scene has no mesh to export")

    for obj in meshes:
        if any(abs(value - 1.0) > 1e-4 for value in obj.scale):
            problems.append(obj.name + ": scale not applied (Object > Apply > Scale)")

    depsgraph = bpy.context.evaluated_depsgraph_get()
    total = sum(triangle_count(obj, depsgraph) for obj in meshes)
    if total > budget:
        problems.append("%d triangles, over the budget of %d" % (total, budget))

    return problems, total


def main():
    args = parse_arguments()
    output = os.path.abspath(args.output)
    if not output.lower().endswith(".fbx"):
        sys.exit("The output must be an .fbx file.")

    problems, total = check_scene(bpy.context.scene, args.budget)
    if problems:
        for problem in problems:
            print("ERROR: " + problem)
        sys.exit(1)

    os.makedirs(os.path.dirname(output), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=output,
        use_selection=False,
        object_types={"MESH", "EMPTY", "ARMATURE"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=False,
    )
    print("Exported %d triangles to %s" % (total, output))


if __name__ == "__main__":
    main()
