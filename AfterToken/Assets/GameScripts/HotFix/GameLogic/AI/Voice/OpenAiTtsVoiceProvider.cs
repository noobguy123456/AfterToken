using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TEngine;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLogic.AI.Voice
{
    /// <summary>
    /// OpenAI Speech API 兼容 TTS：POST /audio/speech，固定请求 WAV；
    /// 内存 + persistentDataPath/VoiceCache 双层缓存，失败返回 null 由 AudioSystem 回退 blip。
    /// </summary>
    public sealed class OpenAiTtsVoiceProvider : IVoiceProvider
    {
        private const int MaxInputChars = 1000;
        private readonly TtsConfig _config;
        private readonly Dictionary<string, AudioClip> _memoryCache = new Dictionary<string, AudioClip>();
        private readonly string _cacheDirectory;

        public OpenAiTtsVoiceProvider(TtsConfig config)
        {
            _config = config;
            _cacheDirectory = Path.Combine(Application.persistentDataPath, "VoiceCache");
        }

        public async UniTask<AudioClip> RequestClipAsync(string speakerId, string text, CancellationToken ct)
        {
            if (_config == null || !_config.IsValid || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            text = text.Trim();
            if (text.Length > MaxInputChars)
            {
                text = text.Substring(0, MaxInputChars);
            }

            var profile = _config.ResolveProfile(speakerId);
            string voice = !string.IsNullOrWhiteSpace(profile?.voice) ? profile.voice : _config.voice;
            string instructions = !string.IsNullOrWhiteSpace(profile?.instructions)
                ? profile.instructions : _config.instructions;
            float speed = profile != null && profile.speed > 0.01f
                ? Mathf.Clamp(profile.speed, 0.25f, 4f) : _config.EffectiveSpeed;
            string key = ComputeCacheKey(speakerId, text, voice, instructions, speed);

            if (_memoryCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            string cachePath = Path.Combine(_cacheDirectory, key + ".wav");
            if (_config.cacheEnabled && File.Exists(cachePath))
            {
                try
                {
                    var clip = WavAudioClipDecoder.Decode(File.ReadAllBytes(cachePath), "tts_" + key);
                    if (clip != null)
                    {
                        _memoryCache[key] = clip;
                        return clip;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[TTS] 读取缓存失败，将重新请求: {e.Message}");
                }
            }

            string url = BuildSpeechUrl(_config.endpoint);
            string body = BuildRequestBody(text, voice, instructions, speed);
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "audio/wav");
            request.SetRequestHeader("Authorization", "Bearer " + _config.GetApiKey());
            request.timeout = Mathf.CeilToInt(_config.EffectiveTimeout);

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception e)
            {
                Log.Warning($"[TTS] 请求异常: {e.Message}");
                return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Log.Warning($"[TTS] 请求失败: HTTP {request.responseCode}, {request.error}");
                return null;
            }

            byte[] wav = request.downloadHandler.data;
            var result = WavAudioClipDecoder.Decode(wav, "tts_" + key);
            if (result == null)
            {
                Log.Warning("[TTS] 服务返回的内容不是受支持的 WAV。");
                return null;
            }

            _memoryCache[key] = result;
            if (_config.cacheEnabled)
            {
                try
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    File.WriteAllBytes(cachePath, wav);
                }
                catch (Exception e)
                {
                    // 缓存失败不影响本次播放。
                    Log.Warning($"[TTS] 写入缓存失败: {e.Message}");
                }
            }
            return result;
        }

        private string BuildRequestBody(string text, string voice, string instructions, float speed)
        {
            var root = new JObject
            {
                ["model"] = _config.model,
                ["input"] = text,
                ["voice"] = voice,
                ["response_format"] = "wav",
                ["speed"] = speed,
            };
            if (!string.IsNullOrWhiteSpace(instructions))
            {
                root["instructions"] = instructions;
            }
            return root.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static string BuildSpeechUrl(string endpoint)
        {
            string url = endpoint.TrimEnd('/');
            return url.EndsWith("/audio/speech", StringComparison.OrdinalIgnoreCase)
                ? url : url + "/audio/speech";
        }

        private string ComputeCacheKey(string speakerId, string text, string voice, string instructions, float speed)
        {
            string material = "v1|" + _config.endpoint + "|" + _config.model + "|" + voice + "|"
                              + speed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "|"
                              + instructions + "|" + speakerId + "|" + text;
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(material));
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
