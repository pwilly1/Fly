from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math
import uuid

# Pillow is needed only to regenerate the artwork.
OUT = Path(__file__).resolve().parent.parent / 'Fly/Assets/UI'
OUT.mkdir(parents=True, exist_ok=True)
SCALE = 3
NAVY = (38, 84, 101, 255)
CREAM = (255, 247, 222, 255)


def new_image(width, height):
    return Image.new('RGBA', (width * SCALE, height * SCALE), (0, 0, 0, 0))


def box(draw, bounds, radius, fill):
    draw.rounded_rectangle(tuple(v * SCALE for v in bounds), radius * SCALE, fill=fill)


def ellipse(draw, bounds, fill):
    draw.ellipse(tuple(v * SCALE for v in bounds), fill=fill)


def mask_to_layer(mask, color):
    layer = Image.new('RGBA', mask.size, color)
    layer.putalpha(mask)
    return layer


def finish_shape(mask, fill, border=NAVY, shadow=True):
    image = Image.new('RGBA', mask.size, (0, 0, 0, 0))
    if shadow:
        offset = Image.new('L', mask.size, 0)
        offset.paste(mask, (0, 8 * SCALE))
        shadow_mask = offset.filter(ImageFilter.GaussianBlur(10 * SCALE)).point(lambda a: int(a * 0.30))
        image.alpha_composite(mask_to_layer(shadow_mask, (30, 71, 88, 255)))
    outline = mask.filter(ImageFilter.MaxFilter(5 * SCALE))
    image.alpha_composite(mask_to_layer(outline, border))
    image.alpha_composite(mask_to_layer(mask, fill))
    return image


def save(name, image):
    image = image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.LANCZOS)
    image.save(OUT / f'{name}.png', optimize=True)
    meta = OUT / f'{name}.png.meta'
    if not meta.exists():
        meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n', encoding='utf-8')


def cloud_pill(width, height):
    mask = Image.new('L', (width * SCALE, height * SCALE), 0)
    d = ImageDraw.Draw(mask)
    box(d, (29, 42, width - 28, height - 22), 48, 255)
    ellipse(d, (65, 24, 166, 117), 255)
    ellipse(d, (134, 11, 269, 118), 255)
    ellipse(d, (width - 244, 20, width - 114, 120), 255)
    ellipse(d, (width - 152, 32, width - 48, 124), 255)
    image = finish_shape(mask, CREAM)
    draw = ImageDraw.Draw(image)
    box(draw, (62, height - 34, width - 64, height - 29), 3, (230, 219, 183, 180))
    return image


def cloud_card():
    w, h = 650, 600
    mask = Image.new('L', (w * SCALE, h * SCALE), 0)
    d = ImageDraw.Draw(mask)
    box(d, (53, 82, 597, 554), 82, 255)
    ellipse(d, (82, 44, 275, 235), 255)
    ellipse(d, (205, 21, 432, 222), 255)
    ellipse(d, (373, 48, 570, 232), 255)
    ellipse(d, (104, 448, 276, 575), 255)
    ellipse(d, (377, 449, 548, 576), 255)
    image = finish_shape(mask, CREAM)
    draw = ImageDraw.Draw(image)
    box(draw, (107, 177, 543, 185), 4, (239, 217, 166, 190))
    return image


def retry_button():
    w, h = 420, 120
    mask = Image.new('L', (w * SCALE, h * SCALE), 0)
    box(ImageDraw.Draw(mask), (16, 15, w - 16, h - 23), 46, 255)
    image = finish_shape(mask, (236, 142, 111, 255))
    draw = ImageDraw.Draw(image)
    box(draw, (45, 30, w - 45, 40), 5, (255, 194, 162, 210))
    return image


def star():
    image = new_image(100, 100)
    points = []
    for i in range(10):
        angle = -math.pi / 2 + i * math.pi / 5
        radius = 39 if i % 2 == 0 else 18
        points.append(((50 + math.cos(angle) * radius) * SCALE, (50 + math.sin(angle) * radius) * SCALE))
    draw = ImageDraw.Draw(image)
    draw.polygon(points, fill=NAVY)
    inner = []
    for i in range(10):
        angle = -math.pi / 2 + i * math.pi / 5
        radius = 33 if i % 2 == 0 else 15
        inner.append(((50 + math.cos(angle) * radius) * SCALE, (50 + math.sin(angle) * radius) * SCALE))
    draw.polygon(inner, fill=(250, 204, 91, 255))
    return image


def stopwatch():
    image = new_image(100, 100)
    d = ImageDraw.Draw(image)
    box(d, (42, 4, 58, 18), 4, NAVY)
    box(d, (46, 9, 54, 19), 2, CREAM)
    ellipse(d, (12, 16, 88, 92), NAVY)
    ellipse(d, (19, 23, 81, 85), CREAM)
    ellipse(d, (46, 47, 54, 55), NAVY)
    d.line([(50 * SCALE, 51 * SCALE), (50 * SCALE, 31 * SCALE)], fill=NAVY, width=5 * SCALE)
    d.line([(50 * SCALE, 51 * SCALE), (64 * SCALE, 60 * SCALE)], fill=NAVY, width=5 * SCALE)
    return image


def medal(name, inner, outer):
    image = new_image(220, 250)
    d = ImageDraw.Draw(image)
    d.polygon([(49*SCALE, 70*SCALE), (92*SCALE, 107*SCALE), (84*SCALE, 244*SCALE), (44*SCALE, 220*SCALE)], fill=(95, 175, 189, 255))
    d.polygon([(171*SCALE, 70*SCALE), (128*SCALE, 107*SCALE), (136*SCALE, 244*SCALE), (176*SCALE, 220*SCALE)], fill=(236, 142, 111, 255))
    rays = []
    for i in range(24):
        angle = -math.pi / 2 + i * math.pi / 12
        radius = 88 if i % 2 == 0 else 75
        rays.append(((110 + math.cos(angle)*radius)*SCALE, (108 + math.sin(angle)*radius)*SCALE))
    d.polygon(rays, fill=NAVY)
    rays_inner = []
    for i in range(24):
        angle = -math.pi / 2 + i * math.pi / 12
        radius = 80 if i % 2 == 0 else 69
        rays_inner.append(((110 + math.cos(angle)*radius)*SCALE, (108 + math.sin(angle)*radius)*SCALE))
    d.polygon(rays_inner, fill=outer)
    ellipse(d, (47, 45, 173, 171), NAVY)
    ellipse(d, (54, 52, 166, 164), inner)
    ellipse(d, (68, 66, 152, 150), outer)
    star_points = []
    for i in range(10):
        angle = -math.pi / 2 + i * math.pi / 5
        radius = 39 if i % 2 == 0 else 18
        star_points.append(((110 + math.cos(angle)*radius)*SCALE, (108 + math.sin(angle)*radius)*SCALE))
    d.polygon(star_points, fill=CREAM)
    return image


save('cloud_progress', cloud_pill(600, 180))
save('cloud_results', cloud_card())
save('retry_button', retry_button())
save('star', star())
save('stopwatch', stopwatch())
save('medal_gold', medal('gold', (255, 226, 120, 255), (241, 184, 68, 255)))
save('medal_silver', medal('silver', (232, 241, 246, 255), (170, 198, 215, 255)))
save('medal_bronze', medal('bronze', (234, 184, 130, 255), (182, 119, 77, 255)))
print('Generated 8 UI sprites')
