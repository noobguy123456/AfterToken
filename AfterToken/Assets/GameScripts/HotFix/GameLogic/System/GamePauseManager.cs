using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏时间缩放管理器。
    /// 通过 Time.timeScale 暂停/减缓游戏进程，不影响声音播放。
    /// 支持多个时间缩放请求叠加，最终取最小值（最慢的时间缩放优先）。
    /// </summary>
    public static class GamePauseManager
    {
        private static readonly List<float> _timeScaleRequests = new List<float>();

        /// <summary>
        /// 推送一个时间缩放请求。
        /// UI 面板打开、武器轮盘打开等场景均可调用。
        /// </summary>
        /// <param name="timeScale">目标时间缩放，0=暂停，1=正常。</param>
        public static void PushTimeScale(float timeScale)
        {
            _timeScaleRequests.Add(Mathf.Clamp01(timeScale));
            Apply();
        }

        /// <summary>
        /// 弹出一个时间缩放请求。
        /// 与 PushTimeScale 成对调用。
        /// </summary>
        public static void PopTimeScale()
        {
            if (_timeScaleRequests.Count > 0)
            {
                _timeScaleRequests.RemoveAt(_timeScaleRequests.Count - 1);
            }
            Apply();
        }

        /// <summary>
        /// 清空所有时间缩放请求并恢复正常时间。
        /// 仅在流程切换等需要强制恢复的场景调用（兜底防泄漏），日常使用请保持 Push/Pop 成对。
        /// </summary>
        public static void Reset()
        {
            _timeScaleRequests.Clear();
            Time.timeScale = 1f;
        }

        private static void Apply()
        {
            if (_timeScaleRequests.Count == 0)
            {
                Time.timeScale = 1f;
                return;
            }

            float min = _timeScaleRequests[0];
            for (int i = 1; i < _timeScaleRequests.Count; i++)
            {
                if (_timeScaleRequests[i] < min)
                {
                    min = _timeScaleRequests[i];
                }
            }

            Time.timeScale = min;
        }
    }
}
