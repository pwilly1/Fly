"""Render quick offline views of the generated bird meshes for inspection."""

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from build_bird_art import COLORS, MESH_DIR


def load_mesh(path):
    vertices, faces = [], []
    for line in path.read_text().splitlines():
        if line.startswith("v "):
            vertices.append(tuple(map(float, line.split()[1:])))
        elif line.startswith("f "):
            faces.append(tuple(int(i) - 1 for i in line.split()[1:]))
    return np.array(vertices), faces


def render(view, out):
    img = Image.new("RGB", (600, 600), (122, 177, 205))
    painter = ImageDraw.Draw(img)
    all_faces = []
    for path in MESH_DIR.glob("*.obj"):
        name = path.stem
        color_name = ("cream" if name == "belly" else "orange" if name in ("beak", "feet")
                      else "eye" if name == "eyes" else "glint" if name == "eye_glints"
                      else "shadow" if name == "tail" or "primaries" in name
                      else "feather" if "coverts" in name else "rust")
        vertices, faces = load_mesh(path)
        if name.startswith("left_"):
            vertices += (-0.34, 0.16, 0.14)
        elif name.startswith("right_"):
            vertices += (0.34, 0.16, 0.14)
        if view == "rear":
            screen = vertices[:, (0, 1)]
            depth = vertices[:, 2]
            light = np.array([-0.35, 0.7, -0.45])
        elif view == "front":
            screen = vertices[:, (0, 1)] * (-1, 1)
            depth = -vertices[:, 2]
            light = np.array([0.35, 0.7, 0.45])
        else:
            screen = vertices[:, (2, 1)] * (-1, 1)
            depth = -vertices[:, 0]
            light = np.array([0.6, 0.7, 0.2])
        for face in faces:
            points = vertices[list(face)]
            normal = np.cross(points[1] - points[0], points[2] - points[0])
            magnitude = np.linalg.norm(normal)
            if magnitude < 1e-8:
                continue
            brightness = 0.78 + 0.22 * abs(float(np.dot(normal / magnitude, light)))
            rgb = tuple(int(min(255, component * brightness * 255)) for component in COLORS[color_name])
            projected = [tuple((screen[i] * (205, -205) + (300, 315)).astype(int)) for i in face]
            all_faces.append((float(np.mean(depth[list(face)])), projected, rgb))
    for _, points, color in sorted(all_faces, reverse=True):
        painter.polygon(points, fill=color)
    img.save(out)


if __name__ == "__main__":
    output = Path(__file__).resolve().parents[1] / "bird-preview.png"
    render("rear", output)
    print(output)
