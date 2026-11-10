"""Build a stylized robin in Blender and render review views.

Run with Blender 5.2: blender -b --factory-startup --python art/build_robin.py
"""

import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
RENDERS = ROOT / "previews"
RENDERS.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)


def material(name, color, roughness=0.8, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


rust = material("Robin | warm rust", (0.61, 0.19, 0.16))
head_red = material("Robin | head red", (0.74, 0.27, 0.20))
cover_red = material("Robin | upper feather red", (0.70, 0.25, 0.19))
shadow_red = material("Robin | shaded red", (0.45, 0.16, 0.18))
primary_dark = material("Robin | plum primary feathers", (0.21, 0.12, 0.17))
primary_mid = material("Robin | muted wing tips", (0.34, 0.18, 0.21))
cream = material("Robin | warm cream", (0.95, 0.78, 0.59))
cream_light = material("Robin | face cream", (1.0, 0.87, 0.68))
orange = material("Robin | beak and feet", (0.94, 0.41, 0.065), roughness=0.54)
orange_light = material("Robin | beak highlight", (1.0, 0.59, 0.12), roughness=0.55)
eye_rim = material("Robin | dark eye rim", (0.18, 0.075, 0.065), roughness=0.55)
eye_black = material("Robin | glossy eyes", (0.012, 0.009, 0.012), roughness=0.13)
eye_glint = material("Robin | eye glints", (1.0, 0.96, 0.84), roughness=0.08)


bird = bpy.data.objects.new("Robin | visual root", None)
bpy.context.collection.objects.link(bird)


def mesh_object(name, vertices, faces, mat, parent=bird, bevel=0):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.data.materials.append(mat)
    if bevel:
        modifier = obj.modifiers.new("Soft modeled edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.affect = "EDGES"
        normal = obj.modifiers.new("Weighted surface normals", "WEIGHTED_NORMAL")
        normal.keep_sharp = True
    return obj


def sculpted_sphere(name, position, scale, mats, cream_selector=None, segments=24, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=position)
    obj = bpy.context.object
    obj.name = name
    obj.parent = bird
    for vert in obj.data.vertices:
        x, y, z = vert.co
        vert.co = (x * scale[0], y * scale[1], z * scale[2])
    for mat in mats:
        obj.data.materials.append(mat)
    if cream_selector:
        for poly in obj.data.polygons:
            center = sum((obj.data.vertices[i].co for i in poly.vertices), Vector()) / len(poly.vertices)
            if cream_selector(center):
                poly.material_index = 1
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


# The cream is assigned to faces of the same meshes, which leaves a clean seam.
body = sculpted_sphere(
    "Body | rust back and cream breast", (0, -0.05, 0.93), (0.51, 0.69, 0.50),
    (rust, cream), lambda p: p.y > -0.19 + 0.46 * p.z,
    segments=32, rings=20,
)
head = sculpted_sphere(
    "Head | red crown and cream cheeks", (0, 0.42, 1.41), (0.37, 0.37, 0.36),
    (head_red, cream_light), lambda p: p.y > 0.04 + 0.55 * p.z,
    segments=28, rings=18,
)


def ball(name, pos, scale, mat, parent=bird, segments=16, rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.scale = scale
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


for side, label in ((-1, "L"), (1, "R")):
    ball(f"Eye rim {label}", (side * 0.298, 0.638, 1.49), (0.117, 0.105, 0.118), eye_rim)
    ball(f"Eye pupil {label}", (side * 0.333, 0.708, 1.504), (0.086, 0.071, 0.086), eye_black)
    ball(f"Eye glint {label}", (side * 0.366, 0.765, 1.548), (0.021, 0.014, 0.021), eye_glint,
         segments=10, rings=7)


# A two-piece wedge beak reads as a bird beak from all three views.
mesh_object("Upper beak", [
    (-0.13, 0.73, 1.38), (0.13, 0.73, 1.38), (0, 0.73, 1.49),
    (-0.11, 0.82, 1.34), (0.11, 0.82, 1.34), (0, 1.19, 1.30),
], [(0, 1, 2), (0, 3, 5, 2), (2, 5, 4, 1), (0, 1, 4, 3), (3, 4, 5)], orange_light)
mesh_object("Lower beak", [
    (-0.105, 0.77, 1.34), (0.105, 0.77, 1.34),
    (-0.075, 0.86, 1.27), (0.075, 0.86, 1.27), (0, 1.15, 1.28),
], [(0, 1, 3, 2), (0, 2, 4), (1, 4, 3), (2, 3, 4)], orange)


def leaf(name, base, tip, width, thickness, mat, parent=bird, bend=0.0):
    """A smooth feather with a tapered outline, raised center vein and rounded tip."""
    base, tip = Vector(base), Vector(tip)
    direction = tip - base
    sideways = Vector((-direction.y, direction.x, 0)).normalized()
    verts = []
    profiles = ((0, 0.32), (0.18, 0.86), (0.42, 1.0), (0.68, 0.83),
                (0.86, 0.52), (0.96, 0.23), (1, 0.02))
    for t, span in profiles:
        center = base.lerp(tip, t)
        center.z += math.sin(math.pi * t) * bend
        left = center + sideways * width * span
        right = center - sideways * width * span
        ridge = center + Vector((0, 0, thickness * span))
        underside = center - Vector((0, 0, thickness * 0.45 * span))
        verts.extend((tuple(left), tuple(ridge), tuple(right), tuple(underside)))
    faces = []
    for i in range(len(profiles) - 1):
        for edge in range(4):
            a = i * 4 + edge
            b = i * 4 + (edge + 1) % 4
            c = (i + 1) * 4 + (edge + 1) % 4
            d = (i + 1) * 4 + edge
            faces.append((a, b, c, d))
    obj = mesh_object(name, verts, faces, mat, parent)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


wing_pivots = []
for side, label in ((-1, "Left"), (1, "Right")):
    pivot = bpy.data.objects.new(f"{label} wing | animation pivot", None)
    bpy.context.collection.objects.link(pivot)
    pivot.parent = bird
    pivot.location = (side * 0.39, -0.10, 1.19)
    wing_pivots.append(pivot)

    # Broad, hand-shaped wing rather than an elongated sphere.
    outline = [
        (0.0, 0.20), (0.30, 0.35), (0.77, 0.34), (1.13, 0.20),
        (1.48, -0.03), (1.59, -0.22), (1.39, -0.42),
        (1.04, -0.59), (0.58, -0.60), (0.15, -0.39),
    ]
    top = [(side * x, y, 0.09 - 0.06 * x) for x, y in outline]
    bottom = [(x, y, z - 0.12) for x, y, z in top]
    center_top = (side * 0.95, -0.13, 0.15)
    center_bottom = (side * 0.95, -0.13, -0.14)
    vertices = top + bottom + [center_top, center_bottom]
    n = len(outline)
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append((2 * n, i, j))
        faces.append((2 * n + 1, n + j, n + i))
        faces.append((i, n + i, n + j, j))
    wing = mesh_object(f"{label} wing | solid shape", vertices, faces, rust, pivot, bevel=0.035)

    # Dark flight feathers create the scalloped trailing edge and a broad span.
    for i in range(7):
        x = 0.38 + 0.22 * i
        leaf(f"{label} primary {i + 1}",
             (side * x, -0.22 - 0.034 * i, 0.09 - 0.01 * i),
             (side * (x + 0.17 + 0.09 * i), -0.94 + 0.022 * i, -0.03 - 0.018 * i),
             0.155 - 0.006 * i, 0.048, primary_mid if i < 2 else primary_dark,
             pivot, bend=0.03)
    for i in range(6):
        x = 0.16 + 0.235 * i
        leaf(f"{label} red covert {i + 1}",
             (side * x, 0.16 - 0.023 * i, 0.16 - 0.012 * i),
             (side * (x + 0.27), -0.38 - 0.025 * i, 0.115 - 0.014 * i),
             0.155, 0.025, cover_red if i % 2 == 0 else shadow_red,
             pivot, bend=0.025)


# Long fan-shaped tail feathers remain distinct in rear view.
for row, z in enumerate((1.20, 1.02)):
    for i in range(-2, 3):
        leaf(f"Back covert row {row + 1} feather {i + 3}",
             (i * 0.13, -0.56 - 0.08 * row, z),
             (i * 0.15, -0.72 - 0.06 * row, z - 0.20),
             0.075, 0.012, cover_red if (i + row) % 2 else rust,
             bend=0.01)

for i in range(-3, 4):
    leaf(f"Tail feather {i + 4}",
         (i * 0.045, -0.58, 0.77),
         (i * 0.115, -1.69 + 0.06 * abs(i), 0.46),
         0.105, 0.045, primary_dark if abs(i) > 1 else primary_mid,
         bend=0.04)


def tube(name, start, end, radius, mat, vertices=8):
    direction = Vector(end) - Vector(start)
    midpoint = (Vector(start) + Vector(end)) / 2
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius * 0.58,
                                    depth=direction.length, location=midpoint)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.parent = bird
    obj.data.materials.append(mat)
    return obj


for side, label in ((-1, "L"), (1, "R")):
    x = side * 0.23
    tube(f"Leg {label}", (x, 0.00, 0.56), (x, 0.03, 0.27), 0.036, orange)
    for toe in (-1, 0, 1):
        tube(f"Toe {label} {toe + 2}", (x, 0.06, 0.28),
             (x + toe * 0.095, 0.27, 0.24), 0.026, orange)


# Keep the model's animation on separate pivots for later Unity integration.
for pivot, side in zip(wing_pivots, (-1, 1)):
    for frame, lift in ((1, -12), (7, 25), (13, -43), (20, 12), (25, -12)):
        pivot.rotation_euler = (0, math.radians(-side * lift), 0)
        pivot.keyframe_insert(data_path="rotation_euler", frame=frame)
    if pivot.animation_data and pivot.animation_data.action:
        pivot.animation_data.action.name = f"Robin flap | {pivot.name}"
bpy.context.scene.frame_end = 25
bpy.context.scene.frame_set(1)


world = bpy.data.worlds.new("Soft sky studio")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.41, 0.57, 0.68, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.45


def area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy, data.shape, data.size, data.color = energy, "DISK", size, color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector((0, 0, 1.1)) - obj.location).to_track_quat("-Z", "Y").to_euler()


area_light("Key | warm", (3, 4, 5), 260, 4, (1.0, 0.86, 0.73))
area_light("Fill | sky", (-4, -3, 3), 170, 5, (0.73, 0.86, 1.0))
area_light("Rim | soft", (2, -4, 4), 180, 3, (1.0, 0.90, 0.72))


camera_data = bpy.data.cameras.new("Preview camera")
camera = bpy.data.objects.new("Preview camera", camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = 4.7

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 24
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"

for name, position in (
    ("rear", (0, -5.5, 2.15)),
    ("side", (5.5, -0.2, 1.75)),
    ("front", (0, 5.5, 1.8)),
    ("three_quarter", (4.5, 4.5, 2.55)),
):
    camera.location = position
    camera.rotation_euler = (Vector((0, 0, 1.0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(RENDERS / f"robin_{name}.png")
    bpy.ops.render.render(write_still=True)

scene.render.filepath = ""
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "Robin_Draft.blend"))

# Keep the file in the art workspace until the appearance is approved.
bpy.ops.object.select_all(action="DESELECT")
for obj in (bird, *bird.children_recursive):
    if obj.type in {"MESH", "EMPTY"}:
        obj.select_set(True)
bpy.context.view_layer.objects.active = bird
bpy.ops.export_scene.fbx(filepath=str(ROOT / "Robin_Draft.fbx"), use_selection=True,
                         add_leaf_bones=False, bake_anim=True, bake_anim_use_nla_strips=False,
                         bake_anim_use_all_actions=False)
