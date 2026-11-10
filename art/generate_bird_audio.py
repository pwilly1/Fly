"""Generate the small original flight sounds used by the Unity prototype."""

import math
import random
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 44_100
OUTPUT = Path(__file__).resolve().parents[1] / 'Fly/Assets/Resources/BirdSounds'
OUTPUT.mkdir(parents=True, exist_ok=True)


def write_wav(name, samples, level):
    peak = max(abs(sample) for sample in samples)
    scale = level / peak
    pcm = struct.pack('<' + 'h' * len(samples), *(
        int(max(-1.0, min(1.0, sample * scale)) * 32767)
        for sample in samples
    ))
    with wave.open(str(OUTPUT / name), 'wb') as sound:
        sound.setnchannels(1)
        sound.setsampwidth(2)
        sound.setframerate(SAMPLE_RATE)
        sound.writeframes(pcm)
    print(name, f'{len(samples) / SAMPLE_RATE:.2f}s', f'peak={level:.2f}')


def wing_flap():
    rng = random.Random(428)
    samples = []
    high = low = 0.0
    duration = 0.46
    for index in range(int(SAMPLE_RATE * duration)):
        time = index / SAMPLE_RATE
        first_sweep = math.exp(-((time - 0.105) / 0.067) ** 2)
        second_sweep = math.exp(-((time - 0.29) / 0.095) ** 2)
        cutoff = 500 + 1800 * first_sweep + 750 * second_sweep
        noise = rng.uniform(-1.0, 1.0)
        high += (1 - math.exp(-2 * math.pi * cutoff / SAMPLE_RATE)) * (noise - high)
        low += (1 - math.exp(-2 * math.pi * 110 / SAMPLE_RATE)) * (noise - low)
        whoosh = (high - low) * (first_sweep + 0.52 * second_sweep)
        thump = math.sin(2 * math.pi * 82 * (time - 0.07)) * math.exp(-24 * max(0, time - 0.07))
        if time < 0.07:
            thump = 0
        samples.append(whoosh + 0.18 * thump)
    write_wav('WingFlap.wav', samples, 0.68)


def flight_wind():
    rng = random.Random(907)
    duration = 4.0
    length = int(SAMPLE_RATE * duration)
    overlap = int(SAMPLE_RATE * 0.5)
    airy = rumble = 0.0
    raw = []
    for index in range(length + overlap):
        time = index / SAMPLE_RATE
        noise = rng.uniform(-1.0, 1.0)
        airy += (1 - math.exp(-2 * math.pi * 1100 / SAMPLE_RATE)) * (noise - airy)
        rumble += (1 - math.exp(-2 * math.pi * 85 / SAMPLE_RATE)) * (noise - rumble)
        gust = 0.72 + 0.15 * math.sin(2 * math.pi * time / 2)
        gust += 0.11 * math.sin(2 * math.pi * time / 4 + 1.1)
        raw.append((airy - rumble) * gust)

    samples = raw[:length]
    for index in range(overlap):
        blend = index / overlap
        samples[index] = raw[length + index] * (1 - blend) + raw[index] * blend
    seam = samples[0] - samples[-1]
    seam_fade = int(SAMPLE_RATE * 0.02)
    for index in range(seam_fade):
        blend = (index + 1) / seam_fade
        samples[length - seam_fade + index] += seam * blend * blend
    write_wav('FlightWind.wav', samples, 0.55)
    print('wind loop seam', abs(samples[0] - samples[-1]))


wing_flap()
flight_wind()
