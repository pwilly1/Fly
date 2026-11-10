"""Generate the stylized bird meshes and wire them into FlightPrototype.

Run from the repository root. The meshes are ordinary OBJ assets; the scene
contains editable GameObjects for every piece and both wing pivots.
"""

from __future__ import annotations

import math
import re
import uuid
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Fly" / "Assets"
MESH_DIR = ASSETS / "Resources" / "BirdArt"
MATERIAL_DIR = ASSETS / "Materials" / "Bird"
SCENE = ASSETS / "Scenes" / "FlightPrototype.unity"
SOURCE_MATERIAL = ASSETS / "Materials" / "PrototypeBird.mat"

COLORS = {
    "rust": (0.63, 0.20, 0.17),
    "feather": (0.76, 0.30, 0.23),
    "shadow": (0.34, 0.18, 0.20),
    "cream": (0.91, 0.76, 0.57),
    "orange": (0.94, 0.40, 0.06),
    "eye": (0.035, 0.025, 0.035),
    "glint": (1.0, 0.94, 0.81),
}


class Mesh:
    def __init__(self):
        self.vertices = []
        self.faces = []

    def vertex(self, point):
        self.vertices.append(point)
        return len(self.vertices)

    def face(self, a, b, c):
        self.faces.append((a, b, c))

    def ellipsoid(self, center, radius, around=12, vertical=7):
        rings = []
        for j in range(1, vertical):
            latitude = -math.pi / 2 + math.pi * j / vertical
            ring = []
            for i in range(around):
                longitude = 2 * math.pi * i / around
                ring.append(self.vertex((
                    center[0] + radius[0] * math.cos(latitude) * math.cos(longitude),
                    center[1] + radius[1] * math.sin(latitude),
                    center[2] + radius[2] * math.cos(latitude) * math.sin(longitude),
                )))
            rings.append(ring)
        bottom = self.vertex((center[0], center[1] - radius[1], center[2]))
        top = self.vertex((center[0], center[1] + radius[1], center[2]))
        for i in range(around):
            self.face(bottom, rings[0][i], rings[0][(i + 1) % around])
            self.face(rings[-1][(i + 1) % around], rings[-1][i], top)
        for lower, upper in zip(rings, rings[1:]):
            for i in range(around):
                k = (i + 1) % around
                self.face(lower[i], upper[i], lower[k])
                self.face(lower[k], upper[i], upper[k])

    def feather(self, base, tip, width, thickness=0.035):
        """A pointed, faceted feather with an upper ridge and lower face."""
        dx, dz = tip[0] - base[0], tip[2] - base[2]
        length = math.hypot(dx, dz)
        across = (-dz / length, 0, dx / length)
        sections = []
        for fraction, scale in ((0, 0.55), (0.25, 1), (0.55, 0.85), (0.82, 0.48), (1, 0.02)):
            midpoint = tuple(base[k] + (tip[k] - base[k]) * fraction for k in range(3))
            left = self.vertex((midpoint[0] + across[0] * width * scale,
                                midpoint[1], midpoint[2] + across[2] * width * scale))
            right = self.vertex((midpoint[0] - across[0] * width * scale,
                                 midpoint[1], midpoint[2] - across[2] * width * scale))
            ridge = self.vertex((midpoint[0], midpoint[1] + thickness * scale, midpoint[2]))
            underside = self.vertex((midpoint[0], midpoint[1] - thickness * scale, midpoint[2]))
            sections.append((left, ridge, right, underside))
        for a, b in zip(sections, sections[1:]):
            for i in range(4):
                nxt = (i + 1) % 4
                self.face(a[i], a[nxt], b[i])
                self.face(a[nxt], b[nxt], b[i])

    def cone(self, base, tip, radius, sides=7):
        rings = []
        for j, center in enumerate((base, tip)):
            scale = 1 if j == 0 else 0.035
            rings.append([self.vertex((center[0] + math.cos(2 * math.pi * i / sides) * radius * scale,
                                       center[1] + math.sin(2 * math.pi * i / sides) * radius * scale,
                                       center[2])) for i in range(sides)])
        for i in range(sides):
            k = (i + 1) % sides
            self.face(rings[0][i], rings[0][k], rings[1][i])
            self.face(rings[0][k], rings[1][k], rings[1][i])

    def write_obj(self, path):
        with path.open("w", newline="\n") as output:
            output.write("# Fly stylized bird mesh\ns off\n")
            for x, y, z in self.vertices:
                output.write(f"v {x:.5f} {y:.5f} {z:.5f}\n")
            for a, b, c in self.faces:
                output.write(f"f {a} {b} {c}\n")

    def append_faces(self, source, face_indices):
        offset = len(self.vertices)
        self.vertices.extend(source.vertices)
        self.faces.extend(tuple(vertex + offset for vertex in source.faces[i])
                          for i in face_indices)


def make_meshes():
    meshes = {}
    body = Mesh()
    belly = Mesh()
    torso = Mesh()
    torso.ellipsoid((0, -0.06, 0), (0.43, 0.35, 0.70), 14, 8)
    head = Mesh()
    head.ellipsoid((0, 0.34, 0.52), (0.34, 0.34, 0.35), 12, 7)
    for source, cream_region in (
        (torso, lambda y, z: z > -0.10 + 0.5 * y),
        (head, lambda y, z: z > 0.60 + 0.5 * (y - 0.25)),
    ):
        red_faces, cream_faces = [], []
        for i, face in enumerate(source.faces):
            center_y = sum(source.vertices[v - 1][1] for v in face) / 3
            center_z = sum(source.vertices[v - 1][2] for v in face) / 3
            (cream_faces if cream_region(center_y, center_z) else red_faces).append(i)
        body.append_faces(source, red_faces)
        belly.append_faces(source, cream_faces)
    meshes["body"] = (body, "rust")
    meshes["belly"] = (belly, "cream")

    beak = Mesh()
    beak.cone((0, 0.23, 0.88), (0, 0.18, 1.16), 0.12, 8)
    meshes["beak"] = (beak, "orange")

    eyes = Mesh()
    highlights = Mesh()
    for side in (-1, 1):
        eyes.ellipsoid((side * 0.286, 0.42, 0.662), (0.091, 0.092, 0.087), 10, 6)
        highlights.ellipsoid((side * 0.323, 0.457, 0.705), (0.022, 0.025, 0.020), 8, 5)
    meshes["eyes"] = (eyes, "eye")
    meshes["eye_glints"] = (highlights, "glint")

    tail = Mesh()
    for i in range(-2, 3):
        tail.feather((i * 0.05, -0.12, -0.54),
                     (i * 0.16, -0.47, -1.34 - (2 - abs(i)) * 0.06), 0.12, 0.045)
    meshes["tail"] = (tail, "shadow")

    feet = Mesh()
    for side in (-1, 1):
        x = side * 0.22
        feet.cone((x, -0.30, 0.06), (x, -0.61, 0.08), 0.05)
        for toe in (-1, 0, 1):
            feet.cone((x, -0.59, 0.10), (x + toe * 0.085, -0.61, 0.28), 0.033)
    meshes["feet"] = (feet, "orange")

    for side, label in ((-1, "left"), (1, "right")):
        wing = Mesh()
        wing.feather((0, 0, 0.12), (side * 1.08, 0.05, -0.23), 0.28, 0.085)
        wing.feather((side * 0.23, 0.01, 0.12), (side * 1.49, 0.03, -0.54), 0.25, 0.07)
        meshes[f"{label}_wing"] = (wing, "rust")

        primaries = Mesh()
        for i in range(5):
            x = 0.39 + i * 0.16
            primaries.feather((side * x, 0.035, -0.07 - i * 0.045),
                              (side * (0.66 + i * 0.23), 0.04, -0.79 - i * 0.07),
                              0.105, 0.034)
        meshes[f"{label}_primaries"] = (primaries, "shadow")

        coverts = Mesh()
        for i in range(5):
            x = 0.13 + i * 0.15
            coverts.feather((side * x, 0.09, 0.10 - i * 0.04),
                            (side * (x + 0.31), 0.10, -0.31 - i * 0.045),
                            0.115, 0.039)
        meshes[f"{label}_coverts"] = (coverts, "feather")
    return meshes


def guid_for(name):
    return uuid.uuid5(uuid.NAMESPACE_URL, f"fly-bird-art/{name}").hex


def write_assets(meshes):
    MESH_DIR.mkdir(parents=True, exist_ok=True)
    MATERIAL_DIR.mkdir(parents=True, exist_ok=True)
    for folder in (MESH_DIR.parent, MESH_DIR, MATERIAL_DIR):
        meta = folder.with_name(folder.name + ".meta")
        if not meta.exists():
            meta.write_text(f"fileFormatVersion: 2\nguid: {guid_for(str(folder.relative_to(ASSETS)))}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")

    material_source = SOURCE_MATERIAL.read_text(encoding="utf-8-sig")
    for name, (r, g, b) in COLORS.items():
        material = material_source.replace("m_Name: PrototypeBird", f"m_Name: Bird {name.title()}")
        material = re.sub(r"(- _BaseColor: )\{[^}]+\}", rf"\g<1>{{r: {r}, g: {g}, b: {b}, a: 1}}", material)
        material = re.sub(r"(- _Color: )\{[^}]+\}", rf"\g<1>{{r: {r}, g: {g}, b: {b}, a: 1}}", material)
        material = material.replace("- _Cull: 2", "- _Cull: 0")
        path = MATERIAL_DIR / f"Bird_{name}.mat"
        path.write_text(material, encoding="utf-8", newline="\n")
        path.with_name(path.name + ".meta").write_text(f"fileFormatVersion: 2\nguid: {guid_for('material-' + name)}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")

    for name, (mesh, _) in meshes.items():
        path = MESH_DIR / f"{name}.obj"
        mesh.write_obj(path)
        path.with_name(path.name + ".meta").write_text(f"fileFormatVersion: 2\nguid: {guid_for('mesh-' + name)}\nModelImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")


def game_object(go_id, transform_id, name, components=()):
    component_refs = "\n".join(f"  - component: {{fileID: {value}}}" for value in (transform_id, *components))
    return f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{component_refs}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""


def transform(go_id, transform_id, parent_id, children=(), position=(0, 0, 0)):
    child_refs = "\n" + "\n".join(f"  - {{fileID: {child}}}" for child in children) if children else " []"
    return f"""--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {position[0]}, y: {position[1]}, z: {position[2]}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:{child_refs}
  m_Father: {{fileID: {parent_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def wire_scene(meshes):
    scene = SCENE.read_text(encoding="utf-8-sig")
    if "m_Name: Bird Body" in scene:
        return  # Meshes and materials were refreshed; the scene is already wired.

    # Keep the existing visual pivot, collision capsule, and banking behavior.
    scene = scene.replace(
        "m_LocalRotation: {x: 0.7071068, y: 0, z: 0, w: 0.7071068}\n  m_LocalPosition: {x: 0, y: 0, z: 0}",
        "m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}", 1)
    scene = scene.replace("  m_Name: Bird placeholder\n", "  m_Name: Bird Visual\n", 1)
    marker = "--- !u!23 &168868444\nMeshRenderer:\n"
    idx = scene.index(marker) + len(marker)
    scene = scene[:idx] + scene[idx:].replace("  m_Enabled: 1", "  m_Enabled: 0", 1)

    renderer_template = re.search(r"--- !u!23 &168868444\nMeshRenderer:\n.*?(?=--- !u!)", scene, re.S).group()
    filter_template = re.search(r"--- !u!33 &168868446\nMeshFilter:\n.*?(?=--- !u!)", scene, re.S).group()
    next_id = 3000000000
    def ids():
        nonlocal next_id
        values = tuple(range(next_id, next_id + 4))
        next_id += 10
        return values

    docs = []
    visual_children = []
    mesh_nodes = []
    wing_pivots = {}
    for label, side in (("left", -1), ("right", 1)):
        go, tr, _, _ = ids()
        wing_pivots[label] = tr
        visual_children.append(tr)
        parts = [f"{label}_wing", f"{label}_primaries", f"{label}_coverts"]
        child_transforms = []
        for part in parts:
            child_go, child_tr, renderer, mesh_filter = ids()
            child_transforms.append(child_tr)
            mesh_nodes.append((part, child_go, child_tr, renderer, mesh_filter, tr))
        docs.extend((game_object(go, tr, f"{label.title()} Wing Pivot"),
                     transform(go, tr, 168868443, child_transforms,
                               (side * 0.34, 0.16, 0.14))))

    display = {"body": "Bird Body", "belly": "Cream Belly", "beak": "Beak",
               "eyes": "Eyes", "eye_glints": "Eye Highlights", "tail": "Tail Feathers",
               "feet": "Feet"}
    for part in ("body", "belly", "beak", "eyes", "eye_glints", "tail", "feet"):
        go, tr, renderer, mesh_filter = ids()
        visual_children.append(tr)
        mesh_nodes.append((part, go, tr, renderer, mesh_filter, 168868443))

    for part, go, tr, renderer, mesh_filter, parent in mesh_nodes:
        name = display.get(part, part.replace("_", " ").title())
        material = meshes[part][1]
        docs.append(game_object(go, tr, name, (renderer, mesh_filter)))
        docs.append(transform(go, tr, parent))
        rendered = renderer_template.replace("&168868444", f"&{renderer}")
        rendered = rendered.replace("m_GameObject: {fileID: 168868442}", f"m_GameObject: {{fileID: {go}}}")
        rendered = re.sub(r"guid: 4ce9f6f300f6dd14190b04f538638eca", f"guid: {guid_for('material-' + material)}", rendered)
        rendered = rendered.replace("  m_Enabled: 0", "  m_Enabled: 1")
        docs.append(rendered)
        filtered = filter_template.replace("&168868446", f"&{mesh_filter}")
        filtered = filtered.replace("m_GameObject: {fileID: 168868442}", f"m_GameObject: {{fileID: {go}}}")
        # Unity assigns model subasset IDs during import; the binder resolves
        # each imported mesh by its Resources path instead of guessing an ID.
        filtered = re.sub(r"m_Mesh: \{[^}]+\}", "m_Mesh: {fileID: 0}", filtered)
        docs.append(filtered)

    visual_transform = re.search(r"--- !u!4 &168868443\nTransform:\n.*?(?=--- !u!)", scene, re.S).group()
    new_children = "  m_Children:\n" + "\n".join(f"  - {{fileID: {child}}}" for child in visual_children)
    scene = scene.replace(visual_transform, visual_transform.replace("  m_Children: []", new_children))

    # Component is on the existing visual object; the two pivots stay editable.
    visual_go = re.search(r"--- !u!1 &168868442\nGameObject:\n.*?(?=--- !u!)", scene, re.S).group()
    scene = scene.replace(visual_go, visual_go.replace("  - component: {fileID: 168868444}",
                         "  - component: {fileID: 168868444}\n  - component: {fileID: 3000000990}\n  - component: {fileID: 3000000991}"))
    docs.append(f"""--- !u!114 &3000000990
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 168868442}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid_for('BirdWingMotion.cs')}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::BirdWingMotion
  leftWing: {{fileID: {wing_pivots['left']}}}
  rightWing: {{fileID: {wing_pivots['right']}}}
""")
    docs.append(f"""--- !u!114 &3000000991
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 168868442}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid_for('BirdMeshBinder.cs')}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::BirdMeshBinder
""")

    roots = "--- !u!1660057539 &9223372036854775807"
    scene = scene.replace(roots, "".join(docs) + roots)
    SCENE.write_text(scene, encoding="utf-8", newline="\n")


if __name__ == "__main__":
    generated = make_meshes()
    write_assets(generated)
    wire_scene(generated)
    print(f"Generated {len(generated)} bird mesh parts and scene objects")
