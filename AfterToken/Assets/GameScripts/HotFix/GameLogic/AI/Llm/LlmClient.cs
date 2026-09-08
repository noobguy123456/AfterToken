using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace GameLogic.AI.Llm
{
    /// <summary>LLM 请求结果（失败不抛异常）。</summary>
    public readonly struct LlmResult
    {
        public readonly bool Ok;
        public readonly string Text;
        public readonly string Error;
        /// <summary>本次请求 token 用量（端点上报告 usage 时填充，否则 0）。</summary>
        public readonly int TokensUsed;

        public LlmResult(bool ok, string text, string error, int tokensUsed = 0)
        {
            Ok = ok;
            Text = text;
            Error = error;
            TokensUsed = tokensUsed;
        }

        public static LlmResult Fail(string error) => new LlmResult(false, null, error);
    }

    /// <summary>
    /// LLM HTTP 客户端：OpenAI 兼容 POST {endpoint}/chat/completions（非流式）。
    /// 纯网络封装，不知道任何玩法概念；配置见 <see cref="LlmConfig"/>。
    /// 流式 SSE 留扩展位（MVP 不需要）。
    /// </summary>
    public class LlmClient
    {
        private readonly LlmConfig _config;

        public LlmClient(LlmConfig config)
        {
            _config = config;
        }

        public bool IsConfigured => _config != null && _config.IsValid;

        /// <summary>
        /// 发起一次对话补全。超时/网络错误/HTTP 错误均返回 Ok=false，不抛异常。
        /// </summary>
        public async UniTask<LlmResult> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            if (!IsConfigured)
            {
                return LlmResult.Fail("not_configured");
            }

            string url = _config.endpoint.TrimEnd('/') + "/chat/completions";
            string body = BuildRequestBody(systemPrompt, userPrompt);

            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + _config.GetApiKey());
            req.timeout = (int)Math.Ceiling(_config.EffectiveTimeout);

            try
            {
                await req.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return LlmResult.Fail("cancelled");
            }
            catch (Exception e)
            {
                return LlmResult.Fail("exception:" + e.Message);
            }

            if (req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.DataProcessingError)
            {
                return LlmResult.Fail("network:" + req.error);
            }
            if (req.result == UnityWebRequest.Result.ProtocolError)
            {
                return LlmResult.Fail($"http_{req.responseCode}");
            }

            return ParseResponse(req.downloadHandler.text);
        }

        private string BuildRequestBody(string systemPrompt, string userPrompt)
        {
            // response_format json_object：OpenAI 兼容端点普遍支持；不支持的端点会忽略该字段，
            // 契约约束同时写在 system prompt 里双保险。
            var root = new JObject
            {
                ["model"] = _config.model,
                ["temperature"] = _config.EffectiveTemperature,
                ["max_tokens"] = 120,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JObject { ["role"] = "user", ["content"] = userPrompt },
                },
                ["response_format"] = new JObject { ["type"] = "json_object" },
            };
            return root.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static LlmResult ParseResponse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return LlmResult.Fail("empty_response");
            }

            try
            {
                var root = JObject.Parse(json);
                var content = root["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return LlmResult.Fail("empty_content");
                }
                int tokens = root["usage"]?["total_tokens"]?.Value<int>() ?? 0;
                return new LlmResult(true, content, null, tokens);
            }
            catch (Exception e)
            {
                return LlmResult.Fail("parse:" + e.Message);
            }
        }
    }
}
