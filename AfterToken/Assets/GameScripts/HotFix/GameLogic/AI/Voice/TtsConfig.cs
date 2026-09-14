using System;
using System.IO;
using GameLogic.AI.Llm;
using TEngine;
using UnityEngine;

namespace GameLogic.AI.Voice
{
    /// <summary>按 speakerId 覆盖默认声音参数；JsonUtility 使用数组以兼容 IL2CPP。</summary>
    [Serializable]
    public class TtsVoiceProfile
    {
        public string speakerId;
        public string voice;
        [TextArea] public string instructions;
        public float speed;
    }

    /// <summary>
    /// 云端 TTS 配置。运行时文件位于 persistentDataPath/tts_config.json；
    /// 编辑器兼容读取 UserSettings/tts_config.json 并自动迁移。API Key 加密落盘。
    /// </summary>
    [Serializable]
    public class TtsConfig
    {
        public bool enabled;
        /// <summary>API 根地址，例如 https://api.openai.com/v1；也可直接填写 /audio/speech 完整地址。</summary>
        public string endpoint;
        public string apiKey;
        public string apiKeyEncrypted;
        public string model = "gpt-4o-mini-tts";
        public string voice = "coral";
        [TextArea] public string instructions =
            "Speak in an original adult female voice. Quiet, restrained and distant; soft breath, precise pronunciation, low emotional intensity, subtle melancholy. Do not imitate any existing character or performer.";
        public float speed = 0.9f;
        public float timeoutSeconds = 20f;
        public bool cacheEnabled = true;
        public TtsVoiceProfile[] profiles;

        public const float DefaultTimeout = 20f;
        public const float DefaultSpeed = 0.9f;

        public static string LastError { get; private set; }

        public bool IsValid => enabled
            && !string.IsNullOrWhiteSpace(endpoint)
            && !string.IsNullOrWhiteSpace(GetApiKey())
            && !string.IsNullOrWhiteSpace(model)
            && !string.IsNullOrWhiteSpace(voice);

        public float EffectiveTimeout => timeoutSeconds > 0.1f ? timeoutSeconds : DefaultTimeout;
        public float EffectiveSpeed => Mathf.Clamp(speed > 0.01f ? speed : DefaultSpeed, 0.25f, 4f);

        public string GetApiKey()
        {
            if (!string.IsNullOrEmpty(apiKeyEncrypted))
            {
                string decrypted = SecretStore.Decrypt(apiKeyEncrypted);
                if (!string.IsNullOrEmpty(decrypted))
                {
                    return decrypted;
                }
                if (string.IsNullOrEmpty(apiKey))
                {
                    return null;
                }
            }
            return apiKey;
        }

        public TtsVoiceProfile ResolveProfile(string speakerId)
        {
            if (profiles == null || string.IsNullOrEmpty(speakerId))
            {
                return null;
            }
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && string.Equals(profiles[i].speakerId, speakerId, StringComparison.OrdinalIgnoreCase))
                {
                    return profiles[i];
                }
            }
            return null;
        }

        private static string ConfigPath => Path.Combine(Application.persistentDataPath, "tts_config.json");
        private static string LegacyConfigPath => Path.Combine(Application.dataPath, "../UserSettings/tts_config.json");
        private static TtsConfig _cache;

        public static TtsConfig Load()
        {
            if (_cache != null)
            {
                return _cache;
            }

            _cache = new TtsConfig();
            bool migrated = false;
            try
            {
#if UNITY_EDITOR
                // 开发期始终优先工程内的 gitignored 配置，方便调 voice/instructions/speed 后重进 Play 即生效。
                if (File.Exists(LegacyConfigPath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(LegacyConfigPath), _cache);
                    migrated = true;
                }
                else
#endif
                if (File.Exists(ConfigPath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), _cache);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[TtsConfig] 读取配置失败，按未启用处理: {e.Message}");
                _cache = new TtsConfig();
            }

            if ((!string.IsNullOrEmpty(_cache.apiKey) && string.IsNullOrEmpty(_cache.apiKeyEncrypted)
                 && SecretStore.IsAvailable) || migrated)
            {
                Save(_cache);
            }
            return _cache;
        }

        public static bool Save(TtsConfig config)
        {
            LastError = null;
            if (config == null)
            {
                LastError = "config is null";
                return false;
            }

            string plainKey = config.GetApiKey();
            if (!string.IsNullOrEmpty(plainKey))
            {
                string encrypted = SecretStore.Encrypt(plainKey);
                if (encrypted == null)
                {
                    LastError = "device encryption unavailable";
                    return false;
                }
                config.apiKeyEncrypted = encrypted;
                config.apiKey = null;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, true));
                _cache = config;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Log.Warning($"[TtsConfig] 保存配置失败: {e.Message}");
                return false;
            }
        }

        public static void Reload()
        {
            _cache = null;
        }
    }
}
