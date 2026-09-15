using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        /// <summary>语音音量默认值略低于主音量，避免盖过 BGM。</summary>
        private const float DEFAULT_VOICE_VALUE = 0.8f;
        private const float MIN_VALUE = 0f;
        private const float MAX_VALUE = 1f;
        private const int SAVE_DEBOUNCE_MILLISECONDS = 300;

        private static float? _cachedMaster;
        private static float? _cachedMusic;
        private static float? _cachedSound;
        private static float? _cachedVoicePlayer;
        private static float? _cachedVoiceNpc;
        private static CancellationTokenSource _saveDebounceCts;

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
                ScheduleFlush();

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
                ScheduleFlush();

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
                ScheduleFlush();

                ApplySound(_cachedSound.Value);
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// 玩家语音音量（AudioSystem 语音子通道逐 agent 应用，不走 mixer）。
        /// </summary>
        public static float VoicePlayer
        {
            get
            {
                if (!_cachedVoicePlayer.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    _cachedVoicePlayer = d.voicePlayerVolumeInitialized ? d.voicePlayerVolume : DEFAULT_VOICE_VALUE;
                }
                return Mathf.Clamp(_cachedVoicePlayer.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedVoicePlayer = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.voicePlayerVolumeInitialized = true;
                d.voicePlayerVolume = _cachedVoicePlayer.Value;
                ScheduleFlush();

                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// NPC 语音音量（AudioSystem 语音子通道逐 agent 应用，不走 mixer）。
        /// </summary>
        public static float VoiceNpc
        {
            get
            {
                if (!_cachedVoiceNpc.HasValue)
                {
                    var d = SaveSystem.Data.settings;
                    _cachedVoiceNpc = d.voiceNpcVolumeInitialized ? d.voiceNpcVolume : DEFAULT_VOICE_VALUE;
                }
                return Mathf.Clamp(_cachedVoiceNpc.Value, MIN_VALUE, MAX_VALUE);
            }
            set
            {
                _cachedVoiceNpc = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);

                var d = SaveSystem.Data.settings;
                d.voiceNpcVolumeInitialized = true;
                d.voiceNpcVolume = _cachedVoiceNpc.Value;
                ScheduleFlush();

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
            // Voice 组 mixer 固定 1：语音音量由 AudioSystem 按子通道逐 agent 应用（VoicePlayer/VoiceNpc）
            if (GameModule.Audio != null)
            {
                GameModule.Audio.VoiceVolume = 1f;
                GameModule.Audio.VoiceEnable = true;
            }
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
                // 新设置体系由 0..1 滑条表达静音，不继承旧 PlayerPrefs 的 muted 开关。
                GameModule.Audio.MusicEnable = true;
                GameModule.Audio.MusicVolume = value;
            }
        }

        private static void ApplySound(float value)
        {
            if (GameModule.Audio != null)
            {
                GameModule.Audio.SoundEnable = true;
                GameModule.Audio.UISoundEnable = true;
                GameModule.Audio.SoundVolume = value;
                // 当前设置页没有单独的 UI 音量，按钮音效应跟随“音效音量”。
                GameModule.Audio.UISoundVolume = value;
            }
        }

        private static void ScheduleFlush()
        {
            _saveDebounceCts?.Cancel();
            var cts = new CancellationTokenSource();
            _saveDebounceCts = cts;
            FlushAfterDelayAsync(cts).Forget();
        }

        private static async UniTaskVoid FlushAfterDelayAsync(CancellationTokenSource cts)
        {
            try
            {
                await UniTask.Delay(SAVE_DEBOUNCE_MILLISECONDS, ignoreTimeScale: true,
                    cancellationToken: cts.Token);
                SaveSystem.Flush();
            }
            catch (OperationCanceledException)
            {
                // 新滑条值覆盖旧请求，最终值会由新的延迟任务统一落盘。
            }
            finally
            {
                if (ReferenceEquals(_saveDebounceCts, cts))
                {
                    _saveDebounceCts = null;
                }
                cts.Dispose();
            }
        }

        /// <summary>设置页关闭或应用退出时立即提交尚未落盘的音量。</summary>
        public static void FlushPending()
        {
            var cts = _saveDebounceCts;
            if (cts == null)
            {
                return;
            }

            _saveDebounceCts = null;
            cts.Cancel();
            SaveSystem.Flush();
        }

        /// <summary>
        /// 失效缓存（存档槽位切换时由 SaveSystem 调用），下次访问从新槽位重读。
        /// </summary>
        public static void InvalidateCache()
        {
            _saveDebounceCts?.Cancel();
            _saveDebounceCts = null;
            _cachedMaster = null;
            _cachedMusic = null;
            _cachedSound = null;
            _cachedVoicePlayer = null;
            _cachedVoiceNpc = null;
        }
    }
}
