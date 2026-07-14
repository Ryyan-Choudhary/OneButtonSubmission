"""
generate_gettingoverit_assets.py
=================================
Procedurally generate "Getting Over It"-style rocky assets and parallax
backgrounds in Blender, ready for a 2.5D Unity climber.

WHAT IT MAKES
    - Boulders / rocks   : chunky faceted stones (ico-sphere + layered displacement)
    - Ledges / platforms : rock slabs with rounded edges to land on
    - Cliff wall         : a tall craggy vertical face (mid/background)
    - Ground chunks      : wide low terrain patches
    - Background ridges  : flat, dark, low-poly mountain silhouettes at varying
                           depths (Z) for parallax behind the play plane

HOW TO RUN
    A) Inside Blender: open the Scripting workspace, open this file in the Text
       Editor, press "Run Script" (Alt+P).
    B) Headless / batch:
         blender --background --python generate_gettingoverit_assets.py
       (add  -- --export "C:/path/to/Assets/environment/generated"  to force export;
        anything after the lone "--" is passed to this script.)

    Tune the CONFIG block below, then re-run. Set EXPORT_DIR to auto-write one
    FBX per asset (great for dropping into Unity). Leave it "" to just build the
    scene so you can sculpt/tweak by hand first.

TESTED AGAINST: Blender 4.x (works on 3.6+ with minor material-name fallbacks).
"""

import bpy
import bmesh
import random
import os
import sys
from math import radians, pi
from mathutils import Vector

# ============================ CONFIG ============================
CONFIG = {
    "seed": 20260712,          # change for a totally different rock set
    "clear_scene": True,       # wipe existing objects before generating
    "export_dir": "",          # e.g. "D:/work/OneButtonSubmission/Assets/environment/generated"
                               # empty string = build in-scene only, no FBX export

    # how many of each asset to create
    "counts": {
        "boulders": 6,
        "ledges": 4,
        "cliffs": 1,
        "ground_chunks": 2,
        "bg_ridges": 3,        # background parallax mountains
    },

    "style": {
        "faceted": True,       # True = flat-shaded low-poly (Getting Over It look)
        #                        False = smooth organic rocks
        "rock_light": (0.34, 0.31, 0.28),   # base rock color (linear RGB)
        "rock_dark":  (0.10, 0.09, 0.085),  # crevice / shadow color
        "bg_color":   (0.16, 0.17, 0.22),   # distant ridge color (cool, desaturated)
        "edge_bevel": 0.06,    # rounded edge width on ledges (0 = sharp)
    },

    # FBX export tuning for Unity (Y-up, 1 unit = 1 m)
    "export": {
        "apply_transform": True,
    },
}
# ===============================================================


# ---------- small utilities ----------

def log(msg):
    print(f"[GOI-GEN] {msg}")


def parse_cli_export_override():
    """Allow: blender --background --python thisfile.py -- --export <dir>"""
    argv = sys.argv
    if "--" in argv:
        extra = argv[argv.index("--") + 1:]
        if "--export" in extra:
            i = extra.index("--export")
            if i + 1 < len(extra):
                return extra[i + 1]
    return None


def clear_scene():
    """Remove all objects and orphan mesh/material/texture data."""
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.textures):
        for db in list(block):
            if db.users == 0:
                block.remove(db)


def activate(obj):
    """Deselect everything, then select + make `obj` active (needed for ops)."""
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def get_collection(name):
    """Get or create a named collection linked under the scene root."""
    coll = bpy.data.collections.get(name)
    if coll is None:
        coll = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(coll)
    return coll


def move_to_collection(obj, coll_name):
    coll = get_collection(coll_name)
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    coll.objects.link(obj)


def apply_transform(obj, location=False, rotation=True, scale=True):
    activate(obj)
    bpy.ops.object.transform_apply(location=location, rotation=rotation, scale=scale)


def recalc_normals(obj):
    activate(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')


def shade(obj, faceted):
    activate(obj)
    if faceted:
        bpy.ops.object.shade_flat()
    else:
        bpy.ops.object.shade_smooth()


def add_displace(obj, tex_type, strength, scale, rng):
    """Attach a Displace modifier driven by a fresh procedural texture."""
    tex = bpy.data.textures.new(f"{obj.name}_{tex_type}", type=tex_type)
    if hasattr(tex, "noise_scale"):
        tex.noise_scale = scale
    if hasattr(tex, "noise_depth"):
        tex.noise_depth = rng.randint(1, 3)
    if tex_type == 'VORONOI' and hasattr(tex, "distance_metric"):
        tex.distance_metric = 'DISTANCE'
    mod = obj.modifiers.new(name=f"Disp_{tex_type}", type='DISPLACE')
    mod.texture = tex
    mod.strength = strength
    mod.mid_level = 0.5
    mod.texture_coords = 'LOCAL'


def apply_all_modifiers(obj):
    activate(obj)
    for mod in list(obj.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except RuntimeError as e:
            log(f"  could not apply modifier {mod.name}: {e}")


def _set_input(node, names, value):
    """Set the first matching input by name (handles Blender version renames)."""
    for n in names:
        if n in node.inputs:
            node.inputs[n].default_value = value
            return True
    return False


# ---------- materials ----------

def make_rock_material(name, light, dark, roughness=0.9):
    """Procedural rock: object-space noise -> color ramp -> base color, plus bump."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nodes, links = nt.nodes, nt.links
    nodes.clear()

    out = nodes.new('ShaderNodeOutputMaterial'); out.location = (600, 0)
    bsdf = nodes.new('ShaderNodeBsdfPrincipled'); bsdf.location = (300, 0)
    coord = nodes.new('ShaderNodeTexCoord'); coord.location = (-800, 0)
    big = nodes.new('ShaderNodeTexNoise'); big.location = (-560, 120)
    fine = nodes.new('ShaderNodeTexNoise'); fine.location = (-560, -180)
    ramp = nodes.new('ShaderNodeValToRGB'); ramp.location = (-300, 120)
    bump = nodes.new('ShaderNodeBump'); bump.location = (0, -220)

    big.inputs['Scale'].default_value = 3.5
    big.inputs['Detail'].default_value = 6.0
    fine.inputs['Scale'].default_value = 18.0
    fine.inputs['Detail'].default_value = 8.0

    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (*dark, 1.0)
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[1].color = (*light, 1.0)

    _set_input(bsdf, ['Roughness'], roughness)
    _set_input(bsdf, ['Specular IOR Level', 'Specular'], 0.15)
    bump.inputs['Strength'].default_value = 0.35

    links.new(coord.outputs['Object'], big.inputs['Vector'])
    links.new(coord.outputs['Object'], fine.inputs['Vector'])
    links.new(big.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    links.new(fine.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return mat


def make_flat_material(name, color, roughness=1.0):
    """Simple matte material for distant background ridges."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        _set_input(bsdf, ['Base Color'], (*color, 1.0))
        _set_input(bsdf, ['Roughness'], roughness)
        _set_input(bsdf, ['Specular IOR Level', 'Specular'], 0.0)
    return mat


def assign_material(obj, mat):
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)


# ---------- asset builders ----------

def create_boulder(index, location, mat, faceted, rng):
    subdiv = 2 if faceted else 3
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = f"Rock_{index:02d}"

    obj.scale = (rng.uniform(0.8, 1.5), rng.uniform(0.7, 1.2), rng.uniform(0.8, 1.3))
    obj.rotation_euler = (rng.uniform(0, pi), rng.uniform(0, pi), rng.uniform(0, pi))
    apply_transform(obj)

    add_displace(obj, 'VORONOI', strength=rng.uniform(0.30, 0.55), scale=rng.uniform(0.4, 0.9), rng=rng)
    add_displace(obj, 'CLOUDS', strength=rng.uniform(0.10, 0.22), scale=rng.uniform(0.2, 0.5), rng=rng)
    apply_all_modifiers(obj)

    recalc_normals(obj)
    shade(obj, faceted)
    assign_material(obj, mat)
    move_to_collection(obj, "GOI_Rocks")
    return obj


def create_ledge(index, location, mat, faceted, bevel, rng):
    bpy.ops.mesh.primitive_cube_add(size=2.0, location=location)
    obj = bpy.context.active_object
    obj.name = f"Ledge_{index:02d}"

    obj.scale = (rng.uniform(1.6, 3.0), rng.uniform(0.35, 0.6), rng.uniform(1.0, 1.6))
    apply_transform(obj)

    sub = obj.modifiers.new(name="Sub", type='SUBSURF')
    sub.subdivision_type = 'SIMPLE'
    sub.levels = 3
    sub.render_levels = 3
    apply_all_modifiers(obj)

    add_displace(obj, 'CLOUDS', strength=rng.uniform(0.12, 0.24), scale=rng.uniform(0.3, 0.6), rng=rng)
    apply_all_modifiers(obj)

    if bevel > 0.0:
        bev = obj.modifiers.new(name="Bevel", type='BEVEL')
        bev.width = bevel
        bev.segments = 2
        bev.limit_method = 'ANGLE'
        bev.angle_limit = radians(40)
        apply_all_modifiers(obj)

    recalc_normals(obj)
    shade(obj, faceted)
    assign_material(obj, mat)
    move_to_collection(obj, "GOI_Ledges")
    return obj


def create_cliff(index, location, mat, faceted, rng):
    # a tall vertical slab, heavily displaced -> craggy wall
    bpy.ops.mesh.primitive_cube_add(size=2.0, location=location)
    obj = bpy.context.active_object
    obj.name = f"Cliff_{index:02d}"
    obj.scale = (rng.uniform(2.5, 4.0), rng.uniform(5.0, 8.0), 1.2)
    apply_transform(obj)

    sub = obj.modifiers.new(name="Sub", type='SUBSURF')
    sub.subdivision_type = 'SIMPLE'
    sub.levels = 4
    apply_all_modifiers(obj)

    add_displace(obj, 'VORONOI', strength=rng.uniform(0.5, 0.9), scale=rng.uniform(0.5, 1.1), rng=rng)
    add_displace(obj, 'CLOUDS', strength=rng.uniform(0.2, 0.4), scale=rng.uniform(0.2, 0.5), rng=rng)
    apply_all_modifiers(obj)

    recalc_normals(obj)
    shade(obj, faceted)
    assign_material(obj, mat)
    move_to_collection(obj, "GOI_Cliffs")
    return obj


def create_ground_chunk(index, location, mat, faceted, rng):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = f"Ground_{index:02d}"
    obj.scale = (rng.uniform(6.0, 10.0), rng.uniform(3.0, 5.0), 1.0)
    apply_transform(obj)

    sub = obj.modifiers.new(name="Sub", type='SUBSURF')
    sub.subdivision_type = 'SIMPLE'
    sub.levels = 5
    apply_all_modifiers(obj)

    add_displace(obj, 'CLOUDS', strength=rng.uniform(0.25, 0.5), scale=rng.uniform(0.15, 0.4), rng=rng)
    apply_all_modifiers(obj)

    recalc_normals(obj)
    shade(obj, faceted)
    assign_material(obj, mat)
    move_to_collection(obj, "GOI_Ground")
    return obj


def create_bg_ridge(index, mat, rng):
    """Flat, dark, low-poly mountain silhouette placed behind the play plane."""
    depth = -8.0 - index * 6.0            # push each layer further back in Z
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(rng.uniform(-3, 3), 6.0, depth))
    obj = bpy.context.active_object
    obj.name = f"BGRidge_{index:02d}"
    obj.rotation_euler = (radians(90), 0, 0)   # stand it up to face the camera
    obj.scale = (18.0 + index * 6.0, 10.0 + index * 3.0, 1.0)
    apply_transform(obj)

    sub = obj.modifiers.new(name="Sub", type='SUBSURF')
    sub.subdivision_type = 'SIMPLE'
    sub.levels = 4
    apply_all_modifiers(obj)

    add_displace(obj, 'CLOUDS', strength=rng.uniform(2.0, 3.5), scale=rng.uniform(0.15, 0.3), rng=rng)
    apply_all_modifiers(obj)

    shade(obj, faceted=True)
    # fade distant layers slightly cooler/darker
    fade = 0.75 ** index
    faded = tuple(c * fade for c in mat_color_of(mat))
    assign_material(obj, make_flat_material(f"BG_Mat_{index:02d}", faded))
    move_to_collection(obj, "GOI_Background")
    return obj


def mat_color_of(mat):
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    if bsdf and 'Base Color' in bsdf.inputs:
        c = bsdf.inputs['Base Color'].default_value
        return (c[0], c[1], c[2])
    return (0.16, 0.17, 0.22)


# ---------- export ----------

def export_fbx(obj, out_dir, apply_xf):
    os.makedirs(out_dir, exist_ok=True)
    activate(obj)
    # export at origin so Unity gets a clean pivot; restore afterwards
    saved = obj.location.copy()
    obj.location = (0.0, 0.0, 0.0)
    path = os.path.join(out_dir, f"{obj.name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=apply_xf,
        object_types={'MESH'},
        mesh_smooth_type='FACE',
        axis_forward='-Z',
        axis_up='Y',
    )
    obj.location = saved
    log(f"  exported {path}")


# ---------- main ----------

def main():
    cfg = CONFIG
    style = cfg["style"]
    counts = cfg["counts"]
    rng = random.Random(cfg["seed"])

    export_dir = parse_cli_export_override() or cfg["export_dir"]

    log(f"seed={cfg['seed']} faceted={style['faceted']} export={'ON -> ' + export_dir if export_dir else 'off'}")

    if cfg["clear_scene"]:
        clear_scene()

    bpy.context.scene.cursor.location = (0, 0, 0)

    rock_mat = make_rock_material("GOI_Rock", style["rock_light"], style["rock_dark"])
    bg_mat = make_flat_material("GOI_BG", style["bg_color"])
    faceted = style["faceted"]
    bevel = style["edge_bevel"]

    created = []

    # lay assets out in a tidy row on X so you can see/select them
    x = 0.0
    def next_x(step):
        nonlocal x
        loc = x
        x += step
        return loc

    for i in range(counts["boulders"]):
        created.append(create_boulder(i, (next_x(3.5), 0, 0), rock_mat, faceted, rng))
    for i in range(counts["ledges"]):
        created.append(create_ledge(i, (next_x(5.0), 0, -3), rock_mat, faceted, bevel, rng))
    for i in range(counts["cliffs"]):
        created.append(create_cliff(i, (next_x(9.0), 0, -6), rock_mat, faceted, rng))
    for i in range(counts["ground_chunks"]):
        created.append(create_ground_chunk(i, (next_x(12.0), -2, -9), rock_mat, faceted, rng))
    for i in range(counts["bg_ridges"]):
        created.append(create_bg_ridge(i, bg_mat, rng))

    log(f"created {len(created)} assets across collections GOI_*")

    if export_dir:
        log("exporting FBX (one per asset)...")
        for obj in created:
            export_fbx(obj, export_dir, cfg["export"]["apply_transform"])
        log("export complete.")
    else:
        log("no export_dir set — scene built only. Set CONFIG['export_dir'] to write FBX.")


if __name__ == "__main__":
    main()
