using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 语音提供器（M-future TTS 预留接口）。
    /// 实现方按说话人 + 文本异步合成/拉取 AudioClip；返回 null 表示失败（调用方回退占位音）。
    /// 注册点：<see cref="AudioSystem.VoiceProvider"/>。
    /// </summary>
    public interface IVoiceProvider
    {
        /// <summary>
        /// 请求一条语音。speakerId 建议用本地化呼号或人设 id（如 "exusiai"）；
        /// 实现方自行做缓存/限流/断联降级。
        /// </summary>
        UniTask<AudioClip> RequestClipAsync(string speakerId, string text, CancellationToken ct);
    }
}
