# Runs inside Blender. Exports the open .blend as an .fbx. Usage: blender -b file.blend --python blender_export.py -- out.fbx
import sys
import bpy

# The output path comes after the "--" argument.
out_path = sys.argv[sys.argv.index("--") + 1]

bpy.ops.export_scene.fbx(
    filepath=out_path,
    use_selection=False,
    object_types={'MESH', 'EMPTY'},
    use_mesh_modifiers=True,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=False,
    add_leaf_bones=False,
    path_mode='AUTO',
)
