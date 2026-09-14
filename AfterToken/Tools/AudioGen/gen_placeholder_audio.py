# -*- coding: utf-8 -*-
"""生成项目 BGM/SFX 的可循环原型音频（16bit WAV，纯 stdlib）。

BGM 采用“废土科幻 + 俯视射击 + 基地经营”的声音方向：模拟合成器 pad、
低频机械脉冲、短促 pluck 和轻量工业鼓组。它们是可直接放入游戏的音乐原型，
后续替换为正式作曲版本时保持同名即可无缝切换（YooAsset 按文件名寻址）。
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


def tone(freq, n, vol=0.2, harmonics=(1.0, 0.35, 0.12), attack=0.02, release=0.18):
    """带泛音和首尾包络的合成器音色。"""
    out = []
    attack_n = max(1, int(SR * attack))
    release_n = max(1, int(SR * release))
    for i in range(n):
        value = sum(a * math.sin(2 * math.pi * freq * h * i / SR)
                    for h, a in enumerate(harmonics, 1))
        a = min(1.0, i / attack_n)
        r = min(1.0, (n - i) / release_n)
        out.append(value * vol * a * r)
    return out


def add_at(dst, src, start):
    for i, value in enumerate(src):
        if start + i < len(dst):
            dst[start + i] += value


def noise_hat(n, vol=0.08):
    """短促的高频噪声，用作工业 hi-hat。"""
    return [rng.uniform(-1.0, 1.0) * vol * math.exp(-22.0 * i / SR)
            for i in range(n)]


# ── BGM（循环）──

def gen_bgm_mainmenu():
    """主菜单：Am-F-C-G，冷静、克制，带远处的无线电闪烁感。"""
    length = SR * 16
    out = [0.0] * length
    chords = [[110.0, 164.81, 220.0], [87.31, 130.81, 174.61],
              [65.41, 98.0, 130.81], [98.0, 146.83, 196.0]]
    for bar, chord_notes in enumerate(chords * 2):
        start = bar * SR * 2
        for freq in chord_notes:
            add_at(out, tone(freq, SR * 2, 0.075, (1.0, 0.25, 0.08), 0.4, 0.5), start)
        for step, freq in enumerate([440.0, 523.25, 659.25, 523.25]):
            add_at(out, tone(freq, SR // 2, 0.07, (1.0, 0.12), 0.01, 0.12),
                   start + step * SR // 2)
    write_wav("BGM/bgm_mainmenu.wav", loop_fade(out, 0.4))


def gen_bgm_base():
    """基地：温暖的 C 大调琶音，像修复旧设备后重新亮起的灯。"""
    length = SR * 16
    out = [0.0] * length
    progression = [[130.81, 164.81, 196.0], [98.0, 130.81, 164.81],
                   [110.0, 146.83, 196.0], [87.31, 130.81, 174.61]]
    pattern = [0, 1, 2, 1, 0, 1, 2, 1]
    for bar, chord_notes in enumerate(progression * 2):
        start = bar * SR * 2
        for step, idx in enumerate(pattern):
            freq = chord_notes[idx] * (2.0 if step in (3, 7) else 1.0)
            add_at(out, tone(freq, SR // 4, 0.12, (1.0, 0.18), 0.01, 0.12),
                   start + step * SR // 4)
        add_at(out, tone(chord_notes[0], SR * 2, 0.045, (1.0, 0.2), 0.3, 0.4), start)
    write_wav("BGM/bgm_base.wav", loop_fade(out, 0.4))


def gen_bgm_battle():
    """战斗安全态：100 BPM 的低频机械脉冲，留出枪声和脚步的空间。"""
    length = SR * 16
    out = [0.0] * length
    beat = int(SR * 0.6)
    roots = [55.0, 55.0, 65.41, 49.0]
    for step in range(32):
        start = step * beat // 2
        root = roots[(step // 8) % 4]
        add_at(out, tone(root, beat // 2, 0.22, (1.0, 0.2, 0.08), 0.005, 0.18), start)
        if step % 4 == 2:
            add_at(out, tone(root * 2, beat // 3, 0.08, (1.0, 0.15), 0.005, 0.08), start)
    write_wav("BGM/bgm_battle.wav", loop_fade(out, 0.25))


def gen_bgm_combat():
    """交战态：更快的工业鼓组，低频和噪声脉冲制造紧迫感。"""
    length = SR * 8
    out = [0.0] * length
    step = int(SR * 0.25)
    for i in range(32):
        start = i * step
        if i % 4 in (0, 2):
            add_at(out, tone(48.0, step // 2, 0.28, (1.0, 0.3, 0.1), 0.002, 0.08), start)
        if i % 4 == 2:
            add_at(out, noise_hat(step // 2, 0.13), start)
        elif i % 2 == 1:
            add_at(out, noise_hat(step // 3, 0.07), start)
        if i % 8 == 6:
            add_at(out, env_exp(noise_burst(step // 2, 0.18), 30.0), start)
    write_wav("BGM/bgm_combat.wav", loop_fade(out, 0.18))


# ── SFX ──

def noise_burst(n, vol=0.8):
    return [rng.uniform(-1, 1) * vol for _ in range(n)]


def gen_weapon_fire(name, duration, noise_vol, noise_decay, body_freq, body_vol,
                    body_decay, crack_freq=None, crack_vol=0.0):
    """生成一发带枪响、枪身冲击和可选高频 crack 的武器音。"""
    n = int(SR * duration)
    noise = env_exp(noise_burst(n, noise_vol), noise_decay)
    body = env_exp(sine(body_freq, n, body_vol), body_decay)
    out = [a + b for a, b in zip(noise, body)]
    if crack_freq:
        crack = env_exp(sine(crack_freq, n, crack_vol), noise_decay * 1.2)
        out = [a + b for a, b in zip(out, crack)]
    write_wav(f"SFX/{name}.wav", out)


def gen_fire_pistol():
    # 干脆的中频枪身冲击，尾部极短，适合单发点击。
    gen_weapon_fire("sfx_fire_pistol", 0.20, 0.62, 52.0, 185.0, 0.42, 42.0,
                    crack_freq=1550.0, crack_vol=0.16)


def gen_fire_smg():
    # 更亮、更短促的机械枪声，连续播放时颗粒感清晰。
    gen_weapon_fire("sfx_fire_smg", 0.14, 0.54, 68.0, 265.0, 0.34, 55.0,
                    crack_freq=2150.0, crack_vol=0.12)


def gen_fire_sniper():
    # 狙击枪：前端高频 crack + 厚重枪身 + 较长空气尾。
    n = int(SR * 0.72)
    crack = env_exp(noise_burst(n, 0.9), 34.0)
    body = env_exp(sine(92.0, n, 0.7), 5.2)
    ring = env_exp(sine(330.0, n, 0.24), 7.0)
    high = env_exp(sine(1750.0, n, 0.22), 22.0)
    write_wav("SFX/sfx_fire_sniper.wav", [a + b + c + d for a, b, c, d in zip(crack, body, ring, high)])


def gen_fire_rocket():
    # RPG：低频发射冲击叠加长一些的推进器呼啸，不替代爆炸音。
    n = int(SR * 1.05)
    blast = env_exp(noise_burst(n, 0.92), 5.0)
    sub = env_exp(sine(42.0, n, 0.82), 3.2)
    motor = env_exp(sine(118.0, n, 0.3), 2.8)
    write_wav("SFX/sfx_fire_rocket.wav", [a + b + c for a, b, c in zip(blast, sub, motor)])


def gen_sfx():
    gen_fire_pistol()
    gen_fire_smg()
    gen_fire_sniper()
    gen_fire_rocket()
    # 兼容现有 Rifle 配置；正式步枪音仍保留独立资源名。
    gen_weapon_fire("sfx_fire_rifle", 0.25, 0.66, 38.0, 205.0, 0.42, 34.0,
                    crack_freq=1800.0, crack_vol=0.14)

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
