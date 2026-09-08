using System;
using System.IO;
using TEngine;
using UnityEngine;

namespace GameLogic.AI.Llm
{
    /// <summary>
    /// LLM 本地配置（OpenAI 兼容端点）。
    /// 从 UserSettings/llm_config.json 读取（UserSettings/ 已 gitignore，key 不会入库）；
    /// 模板见项目根 llm_config.example.json。M4 将接入设置面板输入。
    /// </summary>
    [Serializable]
    public class LlmConfig
    {
        /// <summary>OpenAI 兼容服务根地址（不含 /chat/completions）。</summary>
        public string endpoint;
        public string apiKey;
        public string model;
        /// <summary>请求超时（秒），0/负数用默认值 8。</summary>
        public float timeoutSeconds = 8f;

        public const float DefaultTimeout = 8f;

        /// <summary>三个必填项齐全才视为已配置。</summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(model);

        public float EffectiveTimeout => timeoutSeconds > 0.1f ? timeoutSeconds : DefaultTimeout;

        private static string ConfigPath =>
            Path.Combine(Application.dataPath, "../UserSettings/llm_config.json");

        private static LlmConfig _cache;

        /// <summary>加载配置（带缓存）。文件缺失/损坏返回空配置（IsValid=false）。</summary>
        public static LlmConfig Load()
        {
            if (_cache != null)
            {
                return _cache;
            }

            _cache = new LlmConfig();
            try
            {
                if (File.Exists(ConfigPath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), _cache);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[LlmConfig] 读取 llm_config.json 失败，按未配置处理: {e.Message}");
                _cache = new LlmConfig();
            }
            return _cache;
        }

        /// <summary>热重载（M4 设置面板保存后调用）。</summary>
        public static void Reload()
        {
            _cache = null;
        }
    }
}
