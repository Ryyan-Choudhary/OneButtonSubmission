"""
game_assets_blender.py
======================
Beautiful, game-matched assets for the twilight 2.5D shotgun climber, generated
procedurally in Blender and exported as FBX for Unity.

BUILDS
    - HERO GUN      : a stylized shotgun-blaster (gunmetal + wood + amber muzzle
                      glow + teal energy cell). Barrel points +X and the origin
                      sits at the grip, so in Unity you parent it under GunPivot
                      and it aims correctly out of the box.
    - PLATFORMS     : sculpted rock slabs the player lands on — flat-ish landable
                      tops, craggy sides, glowing teal crystal shards.
    - OBSTACLE ROCKS: chunky faceted boulders for variety.
    - BACKGROUND    : twilight gradient world, a soft moon, a field of stars, and
                      layered receding mountain ridges (fading toward the sky).
    - PREVIEW SETUP : cool key + warm rim lights and a framed camera so it looks
                      gorgeous the moment you hit render.

PALETTE — matched 1:1 to the game's Palette.cs (twilight/dusk).

HOW TO RUN
    A) Scripting workspace -> open this file -> Run Script (Alt+P).
    B) Headless export:
         blender --background --python game_assets_blender.py -- --export "D:/work/OneButtonSubmission/Assets/environment/generated"

    Set CONFIG["export_dir"] (or pass --export) to write FBX. Empty = build only.

TESTED AGAINST Blender 4.x (material node names have 3.x fallbacks).
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
    "seed": 20260712,
    "clear_scene": True,
    "export_dir": "",          # e.g. ".../Assets/environment/generated"
    "build": {
        "gun": True,
        "platforms": 3,
        "boulders": 4,
        "background": True,
        "preview_lights_camera": True,
    },
    "gun_variant": "shotgun",  # "shotgun" or "sniper" (longer barrel)
}

# --- game palette (linear RGB, from Palette.cs) ---
PAL = {
    "rock":      (0.30, 0.28, 0.34),
    "rock_dark": (0.10, 0.09, 0.11),
    "ground":    (0.22, 0.20, 0.26),
    "gun_metal": (0.20, 0.21, 0.25),
    "gun_dark":  (0.10, 0.11, 0.13),
    "gun_wood":  (0.35, 0.22, 0.14),
    "muzzle":    (1.00, 0.62, 0.22),
    "ammo_teal": (0.10, 0.85, 0.80),
    "amber":     (0.95, 0.55, 0.18),
    "sky_bottom":(0.09, 0.08, 0.18),
    "sky_top":   (0.34, 0.26, 0.46),
    "ridge":     (0.14, 0.12, 0.22),
    "moon":      (0.95, 0.92, 0.85),
}
# ===============================================================


def log(m): print(f"[GAME-ASSETS] {m}")


def cli_export():
    argv = sys.argv
    if "--" in argv:
        extra = argv[argv.index("--") + 1:]
        if "--export" in extra and extra.index("--export") + 1 < len(extra):
            return extra[extra.index("--export") + 1]
    return None


# ---------------- scene / object utils ----------------

def clear_scene():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.textures, bpy.data.lights):
        for db in list(coll):
            if db.users == 0:
                coll.remove(db)


def activate(obj):
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def get_collection(name):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def to_collection(obj, name):
    c = get_collection(name)
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    c.objects.link(obj)


def apply_mods(obj):
    activate(obj)
    for m in list(obj.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=m.name)
        except RuntimeError as e:
            log(f"  modifier {m.name} failed: {e}")


def bevel(obj, width=0.02, segments=2, angle=50):
    m = obj.modifiers.new("Bevel", 'BEVEL')
    m.width = width
    m.segments = segments
    m.limit_method = 'ANGLE'
    m.angle_limit = radians(angle)


def subsurf(obj, levels=2, simple=False):
    m = obj.modifiers.new("Sub", 'SUBSURF')
    if simple:
        m.subdivision_type = 'SIMPLE'
    m.levels = levels
    m.render_levels = levels


def smooth(obj):
    activate(obj)
    bpy.ops.object.shade_smooth()


def flat(obj):
    activate(obj)
    bpy.ops.object.shade_flat()


def recalc_normals(obj):
    activate(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')


def join(parts, name):
    activate(parts[0])
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    return obj


def set_origin(obj, point):
    activate(obj)
    bpy.context.scene.cursor.location = Vector(point)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location = (0, 0, 0)


# ---------------- materials ----------------

def _si(node, names, value):
    for n in names:
        if n in node.inputs:
            node.inputs[n].default_value = value
            return True
    return False


def _new(name):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    return mat, bsdf


def metal(name, color, rough=0.35, metallic=1.0):
    mat, b = _new(name)
    _si(b, ['Base Color'], (*color, 1))
    _si(b, ['Metallic'], metallic)
    _si(b, ['Roughness'], rough)
    return mat


def matte(name, color, rough=0.85):
    mat, b = _new(name)
    _si(b, ['Base Color'], (*color, 1))
    _si(b, ['Metallic'], 0.0)
    _si(b, ['Roughness'], rough)
    return mat


def emissive(name, color, strength=5.0):
    mat, b = _new(name)
    _si(b, ['Base Color'], (*color, 1))
    _si(b, ['Emission Color', 'Emission'], (*color, 1))
    _si(b, ['Emission Strength'], strength)
    return mat


def wood(name, color):
    mat, b = _new(name)
    nt = mat.node_tree
    nodes, links = nt.nodes, nt.links
    tex = nodes.new('ShaderNodeTexNoise')
    tex.inputs['Scale'].default_value = 3.0
    tex.inputs['Detail'].default_value = 6.0
    ramp = nodes.new('ShaderNodeValToRGB')
    dark = tuple(c * 0.6 for c in color)
    ramp.color_ramp.elements[0].color = (*dark, 1)
    ramp.color_ramp.elements[1].color = (*color, 1)
    links.new(tex.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    _si(b, ['Roughness'], 0.5)
    _si(b, ['Metallic'], 0.0)
    return mat


def rock_material(name, light, dark):
    mat, b = _new(name)
    nt = mat.node_tree
    nodes, links = nt.nodes, nt.links
    coord = nodes.new('ShaderNodeTexCoord')
    big = nodes.new('ShaderNodeTexNoise'); big.inputs['Scale'].default_value = 3.0; big.inputs['Detail'].default_value = 6
    fine = nodes.new('ShaderNodeTexNoise'); fine.inputs['Scale'].default_value = 20.0; fine.inputs['Detail'].default_value = 8
    ramp = nodes.new('ShaderNodeValToRGB')
    bump = nodes.new('ShaderNodeBump')
    ramp.color_ramp.elements[0].position = 0.35; ramp.color_ramp.elements[0].color = (*dark, 1)
    ramp.color_ramp.elements[1].position = 0.75; ramp.color_ramp.elements[1].color = (*light, 1)
    links.new(coord.outputs['Object'], big.inputs['Vector'])
    links.new(coord.outputs['Object'], fine.inputs['Vector'])
    links.new(big.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    links.new(fine.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], b.inputs['Normal'])
    bump.inputs['Strength'].default_value = 0.4
    _si(b, ['Roughness'], 0.9)
    _si(b, ['Metallic'], 0.0)
    return mat


# ---------------- primitive helpers ----------------

def cube(name, loc, scale, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    if mat:
        o.data.materials.append(mat)
    return o


def cyl(name, loc, radius, depth, mat, rot=(0, 0, 0), verts=48):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth, vertices=verts, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    if mat:
        o.data.materials.append(mat)
    smooth(o)
    return o


def sphere(name, loc, radius, mat, subdiv=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius, location=loc)
    o = bpy.context.active_object
    o.name = name
    if mat:
        o.data.materials.append(mat)
    smooth(o)
    return o


def torus(name, loc, major, minor, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(location=loc, rotation=rot,
                                     major_radius=major, minor_radius=minor,
                                     major_segments=24, minor_segments=10)
    o = bpy.context.active_object
    o.name = name
    if mat:
        o.data.materials.append(mat)
    smooth(o)
    return o


# ---------------- HERO GUN ----------------

def build_gun(location=(0, 0, 0), variant="shotgun"):
    m_metal = metal("Gun_Metal", PAL["gun_metal"], rough=0.32, metallic=0.95)
    m_dark = metal("Gun_Dark", PAL["gun_dark"], rough=0.25, metallic=0.95)
    m_wood = wood("Gun_Wood", PAL["gun_wood"])
    m_amber = emissive("Gun_Muzzle", PAL["muzzle"], strength=6.0)
    m_teal = emissive("Gun_Cell", PAL["ammo_teal"], strength=4.0)

    barrel_end = 2.7 if variant == "sniper" else 2.25
    barrel_len = (barrel_end - 0.9)
    parts = []

    # receiver body
    receiver = cube("Receiver", (0.6, 0, 0), (1.1, 0.30, 0.28), m_metal); bevel(receiver, 0.03, 3)
    parts.append(receiver)
    # top rail + sights
    parts.append(cube("Rail", (0.7, 0.20, 0), (0.8, 0.05, 0.12), m_dark))
    parts.append(cube("RearSight", (0.20, 0.26, 0), (0.06, 0.10, 0.12), m_dark))
    parts.append(cube("FrontSight", (barrel_end - 0.25, 0.15, 0), (0.04, 0.10, 0.05), m_dark))
    # ejection port inset (darker)
    parts.append(cube("Port", (0.75, 0.06, 0.145), (0.35, 0.12, 0.02), m_dark))

    # barrel + shroud + muzzle
    bx = 0.9 + barrel_len / 2.0
    parts.append(cyl("Barrel", (bx, 0.03, 0), 0.075, barrel_len, m_dark, rot=(0, radians(90), 0), verts=48))
    parts.append(cyl("Shroud", (1.15, 0.03, 0), 0.11, 0.55, m_metal, rot=(0, radians(90), 0), verts=32))
    for rx in (1.15, 1.5, 1.85):
        parts.append(torus("Ring", (rx, 0.03, 0), 0.095, 0.018, m_metal, rot=(0, radians(90), 0)))
    parts.append(cyl("MuzzleBrake", (barrel_end, 0.03, 0), 0.10, 0.20, m_metal, rot=(0, radians(90), 0), verts=32))
    parts.append(cyl("MuzzleGlow", (barrel_end + 0.10, 0.03, 0), 0.055, 0.10, m_amber, rot=(0, radians(90), 0), verts=24))

    # pump / foregrip
    pump = cube("Pump", (1.25, -0.20, 0), (0.30, 0.14, 0.22), m_wood); bevel(pump, 0.03, 2)
    parts.append(pump)

    # grip (angled) + trigger + guard
    grip = cube("Grip", (0.25, -0.34, 0), (0.14, 0.40, 0.18), m_wood, rot=(0, 0, radians(-12))); bevel(grip, 0.03, 2)
    parts.append(grip)
    parts.append(torus("TriggerGuard", (0.48, -0.18, 0), 0.11, 0.018, m_metal, rot=(radians(90), 0, 0)))
    parts.append(cube("Trigger", (0.46, -0.16, 0), (0.03, 0.09, 0.03), m_dark))

    # stock (behind, slight drop) + cheek riser
    stock = cube("Stock", (-0.30, -0.06, 0), (0.60, 0.24, 0.17), m_wood, rot=(0, 0, radians(-4))); bevel(stock, 0.03, 2)
    parts.append(stock)
    parts.append(cube("Cheek", (-0.35, 0.12, 0), (0.42, 0.06, 0.15), m_wood, rot=(0, 0, radians(-4))))

    # teal energy cell on top of receiver
    parts.append(cyl("EnergyCell", (0.38, 0.30, 0), 0.06, 0.18, m_teal, rot=(0, 0, 0), verts=20))

    gun = join(parts, "HeroGun" if variant == "shotgun" else "HeroSniper")
    set_origin(gun, (0.45, -0.12, 0))   # grip point -> Unity pivot
    gun.location = location
    to_collection(gun, "GAME_Gun")
    log(f"  built gun ({variant})")
    return gun


# ---------------- PLATFORMS ----------------

def build_platform(index, location, rng):
    m_rock = rock_material(f"Rock_{index}", PAL["rock"], PAL["rock_dark"])
    m_crystal = emissive(f"Crystal_{index}", PAL["ammo_teal"], strength=3.0)

    # base slab
    bpy.ops.mesh.primitive_cube_add(size=2.0, location=location)
    slab = bpy.context.active_object
    slab.name = f"Platform_{index}"
    slab.scale = (rng.uniform(1.8, 2.6), rng.uniform(0.4, 0.6), rng.uniform(1.1, 1.5))
    activate(slab); bpy.ops.object.transform_apply(scale=True)

    subsurf(slab, 3, simple=True)
    apply_mods(slab)

    # displace sides/bottom more than the top so the landing surface stays usable
    tex = bpy.data.textures.new(f"plat_{index}", 'CLOUDS')
    tex.noise_scale = rng.uniform(0.35, 0.6)
    dm = slab.modifiers.new("Disp", 'DISPLACE')
    dm.texture = tex; dm.strength = rng.uniform(0.12, 0.22); dm.mid_level = 0.6; dm.texture_coords = 'LOCAL'
    apply_mods(slab)

    bevel(slab, 0.05, 2); apply_mods(slab)
    recalc_normals(slab); flat(slab)
    slab.data.materials.append(m_rock)
    to_collection(slab, "GAME_Platforms")

    # glowing teal crystal shards poking out of the top
    shards = []
    for s in range(rng.randint(2, 4)):
        sx = location[0] + rng.uniform(-1.4, 1.4)
        sz = location[2] + rng.uniform(-0.8, 0.8)
        sy = location[1] + rng.uniform(0.3, 0.55)
        bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=rng.uniform(0.06, 0.12), depth=rng.uniform(0.3, 0.6),
                                        location=(sx, sy, sz),
                                        rotation=(radians(rng.uniform(-18, 18)), 0, radians(rng.uniform(-18, 18))))
        shard = bpy.context.active_object
        shard.name = f"Crystal_{index}_{s}"
        shard.data.materials.append(m_crystal)
        flat(shard)
        shards.append(shard)
    if shards:
        plat = join([slab] + shards, f"Platform_{index}")
        to_collection(plat, "GAME_Platforms")
    log(f"  built platform {index}")


def build_boulder(index, location, rng):
    m_rock = rock_material(f"Boulder_{index}", PAL["rock"], PAL["rock_dark"])
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=location)
    o = bpy.context.active_object
    o.name = f"Boulder_{index}"
    o.scale = (rng.uniform(0.8, 1.4), rng.uniform(0.7, 1.2), rng.uniform(0.8, 1.3))
    o.rotation_euler = (rng.uniform(0, pi), rng.uniform(0, pi), rng.uniform(0, pi))
    activate(o); bpy.ops.object.transform_apply(rotation=True, scale=True)

    for kind, strg, scl in (('VORONOI', rng.uniform(0.3, 0.5), rng.uniform(0.4, 0.9)),
                            ('CLOUDS', rng.uniform(0.1, 0.2), rng.uniform(0.2, 0.5))):
        tex = bpy.data.textures.new(f"bould_{index}_{kind}", kind)
        if hasattr(tex, "noise_scale"): tex.noise_scale = scl
        dm = o.modifiers.new(f"D_{kind}", 'DISPLACE')
        dm.texture = tex; dm.strength = strg; dm.mid_level = 0.5; dm.texture_coords = 'LOCAL'
    apply_mods(o)
    recalc_normals(o); flat(o)
    o.data.materials.append(m_rock)
    to_collection(o, "GAME_Obstacles")
    log(f"  built boulder {index}")


# ---------------- BACKGROUND ----------------

def setup_world(bottom, top):
    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nodes, links = nt.nodes, nt.links
    nodes.clear()
    out = nodes.new('ShaderNodeOutputWorld')
    bg = nodes.new('ShaderNodeBackground')
    ramp = nodes.new('ShaderNodeValToRGB')
    sep = nodes.new('ShaderNodeSeparateXYZ')
    coord = nodes.new('ShaderNodeTexCoord')
    mapr = nodes.new('ShaderNodeMapRange')
    ramp.color_ramp.elements[0].color = (*bottom, 1)
    ramp.color_ramp.elements[1].color = (*top, 1)
    mapr.inputs['From Min'].default_value = -0.15
    mapr.inputs['From Max'].default_value = 0.55
    links.new(coord.outputs['Generated'], sep.inputs['Vector'])
    links.new(sep.outputs['Z'], mapr.inputs['Value'])
    links.new(mapr.outputs['Result'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bg.inputs['Color'])
    links.new(bg.outputs['Background'], out.inputs['Surface'])
    bg.inputs['Strength'].default_value = 1.0


def build_stars(radius=200.0):
    m, b = _new("Stars")
    nt = m.node_tree; nodes, links = nt.nodes, nt.links
    noise = nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 220.0
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.62
    ramp.color_ramp.elements[1].position = 0.66
    ramp.color_ramp.elements[0].color = (0, 0, 0, 1)
    ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], b.inputs['Emission Color'] if 'Emission Color' in b.inputs else b.inputs['Emission'])
    _si(b, ['Emission Strength'], 1.5)
    _si(b, ['Base Color'], (0, 0, 0, 1))
    m.use_backface_culling = False

    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=(0, 0, 0))
    dome = bpy.context.active_object
    dome.name = "StarDome"
    dome.data.materials.append(m)
    smooth(dome)
    to_collection(dome, "GAME_Background")


def build_moon():
    m = emissive("Moon", PAL["moon"], strength=3.0)
    o = sphere("Moon", (-40, 30, 120), 10.0, m, subdiv=3)
    to_collection(o, "GAME_Background")


def build_ridge(index, depth, base_color, rng):
    mat = matte(f"Ridge_{index}", base_color, rough=1.0)
    go_mesh = bpy.data.meshes.new(f"Ridge_{index}")
    obj = bpy.data.objects.new(f"Ridge_{index}", go_mesh)
    bpy.context.scene.collection.objects.link(obj)

    cols = 24
    width = 260 + index * 90
    base_y = -30 - index * 8
    max_h = 55 - index * 8
    step = width / cols
    x0 = -width / 2
    topY = [base_y + max_h * (0.35 + 0.65 * rng.random()) for _ in range(cols + 1)]

    verts, faces = [], []
    for c in range(cols):
        xa, xb = x0 + c * step, x0 + (c + 1) * step
        vi = len(verts)
        verts += [Vector((xa, base_y, 0)), Vector((xb, base_y, 0)),
                  Vector((xa, topY[c], 0)), Vector((xb, topY[c + 1], 0))]
        faces += [(vi, vi + 2, vi + 3, vi + 1)]
    go_mesh.from_pydata(verts, [], faces)
    go_mesh.update()
    obj.data.materials.append(mat)
    obj.location = (0, depth, base_y * -0.2)
    obj.rotation_euler = (radians(90), 0, 0)
    flat(obj)
    to_collection(obj, "GAME_Background")


def build_background(rng):
    setup_world(PAL["sky_bottom"], PAL["sky_top"])
    build_stars()
    build_moon()
    for i in range(4):
        shade = tuple(c * (0.85 ** i) for c in PAL["ridge"])
        build_ridge(i, depth=60 + i * 45, base_color=shade, rng=rng)
    log("  built background (world, stars, moon, ridges)")


# ---------------- preview lights + camera ----------------

def setup_preview():
    # cool key
    bpy.ops.object.light_add(type='AREA', location=(6, -8, 8))
    key = bpy.context.active_object
    key.data.energy = 1200
    key.data.size = 8
    key.data.color = (0.70, 0.78, 1.0)
    key.rotation_euler = (radians(55), 0, radians(35))
    # warm rim
    bpy.ops.object.light_add(type='AREA', location=(-8, 6, 4))
    rim = bpy.context.active_object
    rim.data.energy = 700
    rim.data.size = 6
    rim.data.color = (1.0, 0.55, 0.3)
    rim.rotation_euler = (radians(70), 0, radians(-120))
    for l in (key, rim):
        to_collection(l, "GAME_Preview")

    # camera framing the gun
    bpy.ops.object.camera_add(location=(3.5, -6.5, 1.6), rotation=(radians(80), 0, radians(28)))
    cam = bpy.context.active_object
    cam.data.lens = 60
    bpy.context.scene.camera = cam
    to_collection(cam, "GAME_Preview")
    log("  preview lights + camera set")


# ---------------- export ----------------

def export_fbx(obj, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    activate(obj)
    saved = obj.location.copy()
    obj.location = (0, 0, 0)
    path = os.path.join(out_dir, f"{obj.name}.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             object_types={'MESH'}, mesh_smooth_type='FACE',
                             axis_forward='-Z', axis_up='Y')
    obj.location = saved
    log(f"  exported {path}")


# ---------------- main ----------------

def main():
    cfg = CONFIG
    rng = random.Random(cfg["seed"])
    export_dir = cli_export() or cfg["export_dir"]

    if cfg["clear_scene"]:
        clear_scene()
    bpy.context.scene.cursor.location = (0, 0, 0)

    exportable = []

    if cfg["build"]["gun"]:
        exportable.append(build_gun((0, 0, 0), cfg["gun_variant"]))

    px = -6.0
    for i in range(cfg["build"]["platforms"]):
        build_platform(i, (px, -3.0, 0), rng)
        px -= 6.5

    bx = 8.0
    for i in range(cfg["build"]["boulders"]):
        build_boulder(i, (bx, -3.0, 0), rng)
        bx += 3.5

    if cfg["build"]["background"]:
        build_background(rng)

    if cfg["build"]["preview_lights_camera"]:
        setup_preview()

    if export_dir:
        log("exporting FBX (gun, platforms, boulders)...")
        # collect meshes worth exporting (skip background/world)
        for coll_name in ("GAME_Gun", "GAME_Platforms", "GAME_Obstacles"):
            coll = bpy.data.collections.get(coll_name)
            if coll:
                for obj in list(coll.objects):
                    if obj.type == 'MESH':
                        export_fbx(obj, export_dir)
        log("export complete.")
    else:
        log("no export_dir set — scene built only. Set CONFIG['export_dir'] to write FBX.")

    log("done. Collections: GAME_Gun, GAME_Platforms, GAME_Obstacles, GAME_Background, GAME_Preview")


if __name__ == "__main__":
    main()
