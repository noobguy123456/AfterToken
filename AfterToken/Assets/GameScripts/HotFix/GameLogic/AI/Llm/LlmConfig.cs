using System;
using System.IO;
using TEngine;
using UnityEngine;

namespace GameLogic.AI.Llm
{
    /// <summary>
    /// LLM 本地配置（OpenAI 兼容端点）。
    /// 存于 Application.persistentDataPath/llm_config.json（全平台可写，编辑器下也在系统用户目录，不入库）；
    /// 旧位置 UserSettings/llm_config.json 仅做读取迁移。
    /// apiKey 以 <see cref="SecretStore"/> 加密落盘（密文含魔数前缀），
    /// 其余字段明文 JSON。读取兼容旧全明文文件，保存时自动升级为密文。
    /// 模板见项目根 llm_config.example.json。
    /// </summary>
    [Serializable]
    public class LlmConfig
    {
        /// <summary>厂商预设 id（见 <see cref="LlmProviders"/>），空/custom 表示自填端点。</summary>
        public string provider;
        /// <summary>OpenAI 兼容服务根地址（不含 /chat/completions；Anthropic 为 API 根）。</summary>
        public string endpoint;
        /// <summary>明文文件里的 key 字段；密文模式下此字段留空，key 存 apiKeyEncrypted。</summary>
        public string apiKey;
        /// <summary>加密后的 key（带 SecretStore 魔数前缀）。</summary>
        public string apiKeyEncrypted;
        public string model;
        /// <summary>请求超时（秒），0/负数用默认值 8。</summary>
        public float timeoutSeconds = 8f;
        /// <summary>采样温度，0/负数用默认值 0.4（人格稳定优先，压随机性）。</summary>
        public float temperature = 0.4f;
        /// <summary>M5 操控模式：fsm=本地状态机（默认），llm=AI 开车。</summary>
        public string controlMode = "fsm";

        public const float DefaultTimeout = 8f;
        public const float DefaultTemperature = 0.4f;

        /// <summary>最近一次保存失败的原因（UI 反馈用）；成功时清空。</summary>
        public static string LastError { get; private set; }

        /// <summary>三个必填项齐全才视为已配置。</summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(GetApiKey()) && !string.IsNullOrEmpty(model);

        public float EffectiveTimeout => timeoutSeconds > 0.1f ? timeoutSeconds : DefaultTimeout;
        public float EffectiveTemperature => temperature > 0.001f ? temperature : DefaultTemperature;

        /// <summary>是否使用 LLM 操控通道（M5）。</summary>
        public bool IsLlmControl => string.Equals(controlMode, "llm", StringComparison.OrdinalIgnoreCase);

        /// <summary>生效的厂商 id（旧配置无 provider 字段时按 endpoint 推断）。</summary>
        public string EffectiveProviderId =>
            !string.IsNullOrEmpty(provider) ? provider : LlmProviders.InferProviderId(endpoint, model);

        /// <summary>生效的协议风格（OpenAI 兼容 / Anthropic 原生）。</summary>
        public LlmApiStyle ApiStyle => LlmProviders.ResolveStyle(provider, endpoint);

        /// <summary>取明文 key：优先解密 apiKeyEncrypted，失败/为空回退明文 apiKey 字段。</summary>
        public string GetApiKey()
        {
            if (!string.IsNullOrEmpty(apiKeyEncrypted))
            {
                string decrypted = SecretStore.Decrypt(apiKeyEncrypted);
                if (!string.IsNullOrEmpty(decrypted))
                {
                    return decrypted;
                }
                // 解密失败（换机/刷机/数据损坏）：不回退明文字段（此时本应为空），按未配置处理
                if (string.IsNullOrEmpty(apiKey))
                {
                    return null;
                }
            }
            return apiKey;
        }

        // persistentDataPath 是全平台唯一保证可写的位置（编辑器/Windows 包/安卓 APK 内 dataPath 均不可写）。
        // 旧路径（工程 UserSettings）只做读取迁移，不再写入。
        private static string ConfigPath =>
            Path.Combine(Application.persistentDataPath, "llm_config.json");

        private static string LegacyConfigPath =>
            Path.Combine(Application.dataPath, "../UserSettings/llm_config.json");

        private static LlmConfig _cache;

        /// <summary>加载配置（带缓存）。文件缺失/损坏/解密失败返回对应降级配置。</summary>
        public static LlmConfig Load()
        {
            if (_cache != null)
            {
                return _cache;
            }

            _cache = new LlmConfig();
            bool migratedFromLegacy = false;
            try
            {
                if (File.Exists(ConfigPath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), _cache);
                }
                else if (File.Exists(LegacyConfigPath))
                {
                    // 旧位置（工程 UserSettings）读取后迁移到 persistentDataPath
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(LegacyConfigPath), _cache);
                    migratedFromLegacy = true;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[LlmConfig] 读取 llm_config.json 失败，按未配置处理: {e.Message}");
                _cache = new LlmConfig();
            }

            // 旧明文 key 自动迁移为密文（下次 Load 即走加密路径）；旧位置文件同步迁移
            if ((!string.IsNullOrEmpty(_cache.apiKey) && string.IsNullOrEmpty(_cache.apiKeyEncrypted)
                && SecretStore.IsAvailable) || migratedFromLegacy)
            {
                Save(_cache);
            }
            return _cache;
        }

        /// <summary>
        /// 保存配置：apiKey 加密进 apiKeyEncrypted 并清空明文字段后写盘。
        /// 加密不可用时不写文件（避免静默降级为明文存储），返回 false。
        /// </summary>
        public static bool Save(LlmConfig config)
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
                    Log.Error("[LlmConfig] 设备不支持密钥派生，拒绝明文落盘，配置未保存。");
                    return false;
                }
                config.apiKeyEncrypted = encrypted;
                config.apiKey = null;
            }

            try
            {
                // persistentDataPath 目录在打包/Android 上可能不存在，必须先建
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, prettyPrint: true));
                _cache = config;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Log.Error($"[LlmConfig] 写入 llm_config.json 失败: {e.Message}");
                return false;
            }
        }

        /// <summary>热重载（设置面板保存后由 Save 内部已刷新缓存，外部强制重读用）。</summary>
        public static void Reload()
        {
            _cache = null;
        }
    }
}
