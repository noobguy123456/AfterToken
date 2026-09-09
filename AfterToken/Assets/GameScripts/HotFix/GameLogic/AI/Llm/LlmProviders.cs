using System;

namespace GameLogic.AI.Llm
{
    /// <summary>厂商 API 协议风格。</summary>
    public enum LlmApiStyle
    {
        /// <summary>OpenAI 兼容：POST {endpoint}/chat/completions，Bearer 鉴权。</summary>
        OpenAI,
        /// <summary>Anthropic 原生：POST {endpoint}/v1/messages，x-api-key + anthropic-version 头。</summary>
        Anthropic,
    }

    /// <summary>厂商预设：玩家只选厂商 + 粘 API Key，endpoint/模型由预设带出。</summary>
    public sealed class LlmProviderPreset
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Endpoint;
        public readonly string DefaultModel;
        public readonly LlmApiStyle Style;

        public LlmProviderPreset(string id, string displayName, string endpoint, string defaultModel, LlmApiStyle style)
        {
            Id = id;
            DisplayName = displayName;
            Endpoint = endpoint;
            DefaultModel = defaultModel;
            Style = style;
        }
    }

    /// <summary>
    /// 内置厂商预设表。预设只带出 endpoint；DefaultModel 仅作输入框占位提示（型号迭代快，不设默认值）。
    /// 玩家选 Custom 可完全自填（自托管/代理端点）。
    /// </summary>
    public static class LlmProviders
    {
        public const string CustomId = "custom";

        public static readonly LlmProviderPreset[] Presets =
        {
            new LlmProviderPreset("deepseek", "DeepSeek", "https://api.deepseek.com", "deepseek-chat", LlmApiStyle.OpenAI),
            new LlmProviderPreset("kimi", "Kimi (Moonshot)", "https://api.moonshot.cn/v1", "moonshot-v1-8k", LlmApiStyle.OpenAI),
            new LlmProviderPreset("claude", "Claude (Anthropic)", "https://api.anthropic.com", "claude-3-5-sonnet-latest", LlmApiStyle.Anthropic),
            new LlmProviderPreset("chatgpt", "ChatGPT (OpenAI)", "https://api.openai.com/v1", "gpt-4o-mini", LlmApiStyle.OpenAI),
        };

        /// <summary>按 id 查预设，查不到返回 null（即 Custom）。</summary>
        public static LlmProviderPreset Find(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return null;
            }
            foreach (var p in Presets)
            {
                if (string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>
        /// 旧配置没有 provider 字段时的推断：endpoint 命中预设域名则归该厂商，否则 Custom。
        /// </summary>
        public static string InferProviderId(string endpoint, string model)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                foreach (var p in Presets)
                {
                    if (endpoint.StartsWith(p.Endpoint, StringComparison.OrdinalIgnoreCase))
                    {
                        return p.Id;
                    }
                }
            }
            return CustomId;
        }

        /// <summary>解析最终协议风格：优先 provider 预设，无预设按 endpoint 推断，兜底 OpenAI 兼容。</summary>
        public static LlmApiStyle ResolveStyle(string providerId, string endpoint)
        {
            var preset = Find(providerId);
            if (preset != null)
            {
                return preset.Style;
            }
            if (!string.IsNullOrEmpty(endpoint)
                && endpoint.IndexOf("anthropic.com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LlmApiStyle.Anthropic;
            }
            return LlmApiStyle.OpenAI;
        }
    }
}
