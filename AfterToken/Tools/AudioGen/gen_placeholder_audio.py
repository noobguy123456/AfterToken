# -*- coding: utf-8 -*-
"""生成占位音频（16bit WAV，纯 stdlib）到 Assets/AssetRaw/Audios/。
后续音频美术替换同名文件即可无缝切换（YooAsset 按文件名寻址）。
用法：python Tools/AudioGen/gen_placeholder_audio.py
"""
import math
import os
import random
import struct
import wave

SR = 44100
ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "AssetRaw", "Audios")

rng = random.Random(42)


def write_wav(rel_path, samples):
    path = os.path.join(ROOT, rel_path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    peak = max(0.01, max(abs(s) for s in samples))
    scale = 0.85 / peak
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(
            struct.pack("<h", int(max(-1.0, min(1.0, s * scale)) * 32767))
            for s in samples))
    print("wrote", rel_path, f"{len(samples)/SR:.2f}s")


def silence(n):
    return [0.0] * n


def sine(freq, n, vol=1.0):
    return [math.sin(2 * math.pi * freq * i / SR) * vol for i in range(n)]


def env_exp(samples, decay):
    return [s * math.exp(-decay * i / SR) for i, s in enumerate(samples)]


def loop_fade(samples, fade_sec=0.05):
    """首尾交叉淡化，保证循环点不爆音。"""
    f = int(SR * fade_sec)
    out = list(samples)
    for i in range(f):
        t = i / f
        out[i] *= t
        out[-1 - i] *= t
    return out


def chord(freqs, n, vol=0.2):
    out = [0.0] * n
    for f in freqs:
        s = sine(f, n, vol)
        for i in range(n):
            out[i] += s[i]
    return out


# ── BGM（循环）──

def gen_bgm_mainmenu():
    """慢速 pad 和弦循环 8s：Am - F - C - G 各 2s。"""
    chords = [
        [220.0, 261.63, 329.63],   # Am
        [174.61, 220.0, 261.63],   # F
        [130.81, 164.81, 196.0],   # C
        [196.0, 246.94, 293.66],   # G
    ]
    seg = SR * 2
    out = []
    for c in chords:
        s = chord(c, seg, 0.16)
        # 缓慢呼吸包络
        for i in range(seg):
            s[i] *= 0.6 + 0.4 * math.sin(math.pi * i / seg)
        out.extend(s)
    write_wav("BGM/bgm_mainmenu.wav", loop_fade(out, 0.3))


def gen_bgm_base():
    """经营场景：温和琶音 8s。"""
    notes = [261.63, 329.63, 392.0, 523.25, 392.0, 329.63]
    seg = SR // 2  # 0.5s 一个音
    out = []
    for _ in range(8):
        for f in notes:
            s = env_exp(sine(f, seg, 0.22), 4.0)
            out.extend(s)
    write_wav("BGM/bgm_base.wav", loop_fade(out[: SR * 8], 0.3))


def gen_bgm_battle():
    """战斗安全态：中速低音脉冲 8s。"""
    out = []
    beat = SR // 2
    for bar in range(16):
        f = 55.0 if bar % 2 == 0 else 65.41
        s = env_exp(sine(f, beat, 0.5), 8.0)
        # 加一点泛音
        h = env_exp(sine(f * 2, beat, 0.15), 10.0)
        out.extend([a + b for a, b in zip(s, h)])
    write_wav("BGM/bgm_battle.wav", loop_fade(out, 0.2))


def gen_bgm_combat():
    """战斗交战态：快速鼓点脉冲 4s。"""
    out = []
    beat = SR // 4
    for bar in range(16):
        if bar % 4 == 2:
            # 军鼓：噪声 burst
            s = env_exp([rng.uniform(-1, 1) * 0.4 for _ in range(beat)], 25.0)
        else:
            s = env_exp(sine(60.0, beat, 0.55), 12.0)
        out.extend(s)
    write_wav("BGM/bgm_combat.wav", loop_fade(out, 0.15))


# ── SFX ──

def noise_burst(n, vol=0.8):
    return [rng.uniform(-1, 1) * vol for _ in range(n)]


def gen_fire(name, decay, body_freq):
    n = int(SR * 0.25)
    noise = env_exp(noise_burst(n, 0.7), decay)
    body = env_exp(sine(body_freq, n, 0.35), decay * 0.8)
    write_wav(f"SFX/{name}.wav", [a + b for a, b in zip(noise, body)])


def gen_sfx():
    gen_fire("sfx_fire_pistol", 40.0, 220.0)
    gen_fire("sfx_fire_rifle", 30.0, 180.0)
    gen_fire("sfx_fire_smg", 45.0, 260.0)
    gen_fire("sfx_fire_rocket", 12.0, 90.0)

    # 换弹：两段 click
    c1 = env_exp(sine(1800, int(SR * 0.05), 0.5), 200.0)
    c2 = env_exp(sine(1200, int(SR * 0.07), 0.5), 150.0)
    write_wav("SFX/sfx_reload.wav", c1 + silence(int(SR * 0.15)) + c2)

    # 脚步：低通感 thump（低频正弦+少量噪声）
    n = int(SR * 0.12)
    thump = env_exp(sine(75.0, n, 0.6), 60.0)
    hiss = env_exp(noise_burst(n, 0.06), 80.0)
    write_wav("SFX/sfx_footstep.wav", [a + b for a, b in zip(thump, hiss)])

    # 爆炸：低频噪声轰
    n = int(SR * 0.8)
    boom = env_exp(noise_burst(n, 0.9), 6.0)
    sub = env_exp(sine(45.0, n, 0.7), 5.0)
    write_wav("SFX/sfx_explosion.wav", [a + b for a, b in zip(boom, sub)])

    # 拾取：上行双音
    t1 = env_exp(sine(660.0, int(SR * 0.08), 0.35), 40.0)
    t2 = env_exp(sine(990.0, int(SR * 0.12), 0.35), 30.0)
    write_wav("SFX/sfx_pickup.wav", t1 + t2)

    # UI click
    write_wav("SFX/sfx_ui_click.wav", env_exp(sine(1500.0, int(SR * 0.04), 0.4), 250.0))


# ── Voice 占位 ──

def gen_voice_blip():
    """对话占位语音：短促双 blip。"""
    b1 = env_exp(sine(440.0, int(SR * 0.06), 0.3), 90.0)
    b2 = env_exp(sine(550.0, int(SR * 0.08), 0.3), 70.0)
    write_wav("Voice/voice_blip.wav", b1 + b2)


if __name__ == "__main__":
    gen_bgm_mainmenu()
    gen_bgm_base()
    gen_bgm_battle()
    gen_bgm_combat()
    gen_sfx()
    gen_voice_blip()
    print("all done")
