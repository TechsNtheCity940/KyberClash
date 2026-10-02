import os, wave, struct
import numpy as np

OUT = os.path.dirname(os.path.abspath(__file__))
SR = 44100
rng = np.random.default_rng(1234)

def write(name, samples):
    samples = np.clip(samples, -1.0, 1.0)
    samples = (samples * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(samples.tobytes())
    print("wrote", name, len(samples))

def env(n, a=0.005, r=0.05, kind="lin"):
    t = np.arange(n) / SR
    dur = n / SR
    e = np.ones(n)
    ai = int(SR * a); ri = int(SR * r)
    if ai > 0:
        e[:ai] = np.linspace(0, 1, ai)
    if ri > 0:
        e[-ri:] = np.linspace(1, 0, ri)
    return e

def sine(freq, dur, a=0.005, r=0.05, vol=0.6):
    n = int(SR * dur)
    t = np.arange(n) / SR
    return np.sin(2 * np.pi * freq * t) * vol * env(n, a, r)

def tri(freq, dur, a=0.005, r=0.05, vol=0.6):
    n = int(SR * dur)
    t = np.arange(n) / SR
    v = 2 * np.abs(2 * (freq * t - np.floor(freq * t + 0.5))) - 1
    return v * vol * env(n, a, r)

def square(freq, dur, a=0.005, r=0.05, vol=0.6):
    n = int(SR * dur)
    t = np.arange(n) / SR
    v = np.where(np.sin(2 * np.pi * freq * t) >= 0, 1.0, -1.0)
    return v * vol * env(n, a, r)

def noise(dur, vol=0.5, lp=0.0, a=0.003, r=0.05):
    n = int(SR * dur)
    x = rng.uniform(-1, 1, n)
    if lp > 0:
        b, a_ = [1 - lp], [1, -lp]
        x = np.clip(x, -1, 1)
        # simple one-pole
        y = np.zeros(n)
        prev = 0.0
        for i in range(n):
            prev = lp * x[i] + (1 - lp) * prev
            y[i] = prev
        x = y
    return x * vol * env(n, a, r)

def sweep(f0, f1, dur, vol=0.4, kind="sine"):
    n = int(SR * dur)
    t = np.arange(n) / SR
    p = np.linspace(0, 1, n)
    f = f0 * (1 - p) + f1 * p
    if kind == "sine":
        v = np.sin(2 * np.pi * f * t)
    else:
        v = 2 * np.abs(2 * (f * t - np.floor(f * t + 0.5))) - 1
    return v * vol * env(n, 0.01, 0.08)

def mix(*lists):
    n = max(len(l) for l in lists)
    out = np.zeros(n)
    for l in lists:
        out[:len(l)] += l
    peak = max(1e-6, np.max(np.abs(out)))
    if peak > 1.0:
        out = out / peak
    return out

# SFX
write("swing", sweep(900, 320, 0.18, vol=0.35, kind="tri"))
write("hit", mix(noise(0.12, vol=0.7, lp=0.4), sine(120, 0.12, r=0.08, vol=0.6)))
write("perfectparry", mix(sine(1320, 0.35, r=0.2, vol=0.5), sine(1980, 0.30, a=0.01, r=0.2, vol=0.35)))
write("parry", mix(square(740, 0.15, r=0.1, vol=0.5), noise(0.08, vol=0.3, lp=0.6)))
write("land", sine(90, 0.15, r=0.1, vol=0.7))
write("dash", sweep(500, 1400, 0.20, vol=0.3, kind="sine"))
write("jump", sweep(300, 620, 0.12, vol=0.4, kind="sine"))
write("grab", square(420, 0.10, r=0.05, vol=0.5))
write("throw", sweep(260, 900, 0.22, vol=0.5, kind="tri"))
write("ringout_ko", sweep(700, 70, 0.6, vol=0.5, kind="sine"))
write("ui_confirm", sine(880, 0.08, r=0.04, vol=0.5))
write("match_win", mix(sine(523, 0.8, r=0.4, vol=0.4), sine(659, 0.8, a=0.05, r=0.4, vol=0.3), sine(784, 0.8, a=0.1, r=0.4, vol=0.3)))
write("match_draw", sine(330, 0.5, r=0.3, vol=0.45))
write("clash", mix(square(1100, 0.20, r=0.12, vol=0.45), noise(0.10, vol=0.35, lp=0.7)))

def music(name, root, scale, bpm, bars, vol=0.30):
    beat = 60.0 / bpm
    dur = beat * 4 * bars
    total = int(SR * dur)
    out = np.zeros(total)
    step = int(SR * beat / 2)
    idx = 0
    t0 = 0
    while t0 < total:
        note = scale[idx % len(scale)]
        f = root * (2 ** (note / 12.0))
        seg = int(SR * beat / 2)
        tt = np.arange(seg) / SR
        v = (np.sin(2 * np.pi * f * tt) + 0.3 * np.sin(4 * np.pi * f * tt)) * vol * env(seg, 0.02, 0.1)
        end = min(t0 + seg, total)
        out[t0:end] += v[:end - t0]
        t0 += step
        idx += 1
    peak = max(1e-6, np.max(np.abs(out)))
    out = out / peak * 0.9
    write(name, out)

music("music_title", 220.0, [0, 3, 7, 10, 12, 7, 3], 92, 8)
music("music_character_select", 196.0, [0, 4, 7, 11, 12, 7, 4], 100, 8)
music("music_gameplay_battle", 165.0, [0, 3, 5, 7, 10, 12, 10, 7], 124, 12)

print("DONE")
