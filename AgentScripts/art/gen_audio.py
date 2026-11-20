"""Generates placeholder sound effects into Assets/Audio (mono 22.05 kHz WAV).

Run: python AgentScripts/art/gen_audio.py   (needs numpy)
"""
import os, wave
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Audio")
os.makedirs(OUT, exist_ok=True)
SR = 22050
rng = np.random.default_rng(11)


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, attack=0.005, decay=0.1):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    return a * np.exp(-x / decay)


def lowpass(x, k):
    k = max(1, int(k))
    return np.convolve(x, np.ones(k) / k, mode="same")


def highpass(x, k):
    return x - lowpass(x, k)


def save(name, x, peak=0.9):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1
    x = x / m * peak
    data = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())


def footstep():
    # chappal slap (sharp, mid) + short shuffle drag
    n = int(SR * 0.32)
    slap = highpass(rng.normal(0, 1, n), 6) * env(n, 0.001, 0.025)
    drag = lowpass(rng.normal(0, 1, n), 4) * np.concatenate([np.zeros(int(SR * 0.04)),
                                                            env(n - int(SR * 0.04), 0.03, 0.07)]) * 0.35
    thump = np.sin(2 * np.pi * 110 * t(0.32)) * env(n, 0.002, 0.04) * 0.5
    save("footstep", slap + drag + thump)


def crunch():
    n = int(SR * 0.16)
    x = np.zeros(n)
    for _ in range(14):
        p = rng.integers(0, n - 400)
        L = rng.integers(80, 400)
        x[p:p + L] += highpass(rng.normal(0, 1, L), 3) * np.exp(-np.arange(L) / (L / 4)) * rng.uniform(0.4, 1)
    x += lowpass(rng.normal(0, 1, n), 10) * env(n, 0.002, 0.05) * 0.6
    save("crunch", x, 0.7)


def knock():
    out = np.zeros(int(SR * 0.75))
    for i, st in enumerate((0.0, 0.2, 0.4)):
        n = int(SR * 0.2)
        k = np.sin(2 * np.pi * 140 * t(0.2)) * env(n, 0.001, 0.035)
        k += lowpass(rng.normal(0, 1, n), 3) * env(n, 0.0005, 0.01) * 0.6
        p = int(SR * st)
        out[p:p + n] += k
    save("knock", out)


def glass():
    n = int(SR * 1.4)
    x = highpass(rng.normal(0, 1, n), 2) * env(n, 0.001, 0.12)
    tt = t(1.4)
    for _ in range(40):
        f = rng.uniform(2500, 7500)
        st = rng.uniform(0, 0.6)
        e = np.where(tt > st, np.exp(-(tt - st) / rng.uniform(0.05, 0.3)), 0)
        x += np.sin(2 * np.pi * f * tt) * e * rng.uniform(0.1, 0.4)
    save("glass_break", x)


def whoosh():
    n = int(SR * 0.6)
    noise = rng.normal(0, 1, n)
    k = np.linspace(30, 3, n)
    y = np.zeros(n)
    acc = 0.0
    for i in range(n):  # one-pole lowpass with sweeping cutoff
        a = 1 / k[i]
        acc += a * (noise[i] - acc)
        y[i] = acc
    y *= np.sin(np.linspace(0, np.pi, n)) ** 2
    save("whoosh", y, 0.8)


def sting():
    tt = t(0.7)
    x = np.zeros_like(tt)
    for f in (196, 233, 277):
        x += np.sign(np.sin(2 * np.pi * f * tt)) * 0.3 + np.sin(2 * np.pi * f * 2 * tt) * 0.2
    x *= env(len(tt), 0.01, 0.35)
    save("sting", lowpass(x, 3), 0.7)


def door_close():
    n = int(SR * 0.5)
    x = np.sin(2 * np.pi * 80 * t(0.5)) * env(n, 0.001, 0.08)
    x += lowpass(rng.normal(0, 1, n), 4) * env(n, 0.001, 0.03)
    save("door_close", x)


def jump():
    tt = t(0.35)
    f = np.linspace(300, 900, len(tt))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(tt), 0.01, 0.15)
    save("jump", x, 0.6)


def praise():
    out = []
    for f in (784, 988, 1175):
        tt = t(0.12)
        out.append((np.sin(2 * np.pi * f * tt) + 0.3 * np.sin(2 * np.pi * 2 * f * tt)) * env(len(tt), 0.005, 0.08))
    save("praise", np.concatenate(out), 0.6)


def ambient():
    sec = 8.0
    tt = t(sec)
    n = len(tt)
    fan = lowpass(rng.normal(0, 1, n), 40) * 0.6
    fan *= 1 + 0.25 * np.sin(2 * np.pi * 2.0 * tt)  # blade swish (2 Hz divides the loop evenly)
    hum = 0.08 * np.sin(2 * np.pi * 50 * tt)
    cr = np.zeros(n)
    for st in np.arange(0.3, sec - 0.5, 0.9):
        for k in range(3):
            p = int(SR * (st + k * 0.06))
            L = int(SR * 0.035)
            tc = np.arange(L) / SR
            cr[p:p + L] += np.sin(2 * np.pi * 4400 * tc) * np.sin(np.pi * tc / tc[-1]) * 0.12
    x = fan + hum + cr
    # seamless loop: crossfade the tail into the head
    fade = int(SR * 0.5)
    w = np.linspace(0, 1, fade)
    x[:fade] = x[:fade] * w + x[-fade:] * (1 - w)
    x = x[:-fade]
    save("ambient", x, 0.5)


if __name__ == "__main__":
    footstep(); crunch(); knock(); glass(); whoosh(); sting(); door_close(); jump(); praise(); ambient()
    print("ok", sorted(os.listdir(OUT)))
