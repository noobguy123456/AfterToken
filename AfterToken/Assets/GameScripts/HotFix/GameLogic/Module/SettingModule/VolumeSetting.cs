using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 音量设置（主音量/音乐/音效，0..1）。
    /// 持久化由 SaveSystem 接管；旧框架层 PlayerPrefs 音量（Constant.Setting.*）在首次读取时一次性导入。
    /// 写入时立即应用到 AudioModule 并落盘。
    /// </summary>
    public static class VolumeSetting
    {
        /// <summary>
        /// 旧 PlayerPrefs 键（TEngine Constant.Setting.*），仅用于迁移，不再写入。
        /// </summary>
        private const string LEGACY_MUSIC_KEY = "Setting.MusicVolume";
        private const string LEGACY_SOUND_KEY = "Setting.SoundVolume";

        private const float DEFAULT_VALUE = 1f;
        private const float MIN_VALUE = 0f;
        private const float MAX_VALUE = 1f;

        private static float? _cachedMaster;
        private static float? _cachedMusic;
        private static float? _cachedSound;

        /// <summary>
        /// 主音量（AudioListener.volume）。
        /// </summary>
        public static float Master
        {
            get
            {
                if (!_cachedMaster.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    // 主音量此前无持久化，无存档时用默认值
                    _cachedMaster = d.masterVolumeInitialized ? d.masterVolume : DEFAULT_VALUE;
                }
                return Mathf.Clamp(_cachedMaster.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedMaster = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.masterVolumeInitialized = true;
                d.masterVolume = _cachedMaster.Value;
                SaveSystem.Flush();

                ApplyMaster(_cachedMaster.Value);
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// 音乐音量（AudioMixer 的 MusicVolume 参数）。
        /// </summary>
        public static float Music
        {
            get
            {
                if (!_cachedMusic.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    // 无存档时尝试导入旧框架层 PlayerPrefs 值，否则用默认值
                    _cachedMusic = d.musicVolumeInitialized
                        ? d.musicVolume
                        : PlayerPrefs.GetFloat(LEGACY_MUSIC_KEY, DEFAULT_VALUE);
                }
                return Mathf.Clamp(_cachedMusic.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedMusic = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.musicVolumeInitialized = true;
                d.musicVolume = _cachedMusic.Value;
                SaveSystem.Flush();

                ApplyMusic(_cachedMusic.Value);
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// 音效音量（AudioMixer 的 SoundVolume 参数）。
        /// </summary>
        public static float Sound
        {
            get
            {
                if (!_cachedSound.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    _cachedSound = d.soundVolumeInitialized
                        ? d.soundVolume
                        : PlayerPrefs.GetFloat(LEGACY_SOUND_KEY, DEFAULT_VALUE);
                }
                return Mathf.Clamp(_cachedSound.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedSound = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.soundVolumeInitialized = true;
                d.soundVolume = _cachedSound.Value;
                SaveSystem.Flush();

                ApplySound(_cachedSound.Value);
                OnChanged?.Invoke();
            }
        }

        public static float Min => MIN_VALUE;
        public static float Max => MAX_VALUE;
        public static float Default => DEFAULT_VALUE;

        /// <summary>
        /// 音量变动事件（设置面板等 UI 刷新用）。
        /// </summary>
        public static event Action OnChanged;

        /// <summary>
        /// 启动/槽位切换时把存档中的音量读回应用到 AudioModule。
        /// </summary>
        public static void ApplyAll()
        {
            ApplyMaster(Master);
            ApplyMusic(Music);
            ApplySound(Sound);
        }

        private static void ApplyMaster(float value)
        {
            if (GameModule.Audio != null)
            {
                GameModule.Audio.Volume = value;
            }
        }

        private static void ApplyMusic(float value)
        {
            if (GameModule.Audio != null)
            {
                GameModule.Audio.MusicVolume = value;
            }
        }

        private static void ApplySound(float value)
        {
            if (GameModule.Audio != null)
            {
                GameModule.Audio.SoundVolume = value;
            }
        }

        /// <summary>
        /// 失效缓存（存档槽位切换时由 SaveSystem 调用），下次访问从新槽位重读。
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedMaster = null;
            _cachedMusic = null;
            _cachedSound = null;
        }
    }
}
