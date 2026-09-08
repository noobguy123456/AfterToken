using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 特效播放句柄。支持提前 Stop；到期或被强制回收后自动失效。
    /// 句柄对象由 EffectSystem 内部池化复用，外部不要缓存长期引用。
    /// </summary>
    public class EffectInstance
    {
        internal EffectSystem.EffectPool Pool;
        internal GameObject Go;
        internal EffectDriver Driver;
        internal Transform FollowTarget;
        internal float EndTime;
        internal bool Stopped;

        /// <summary>
        /// 是否仍在播放。
        /// </summary>
        public bool IsPlaying => !Stopped && Go != null;

        /// <summary>
        /// 提前结束并回收特效。
        /// </summary>
        public void Stop()
        {
            if (Stopped)
            {
                return;
            }
            Stopped = true;
            EffectSystem.Instance?.StopInternal(this);
        }

        internal void Setup(EffectSystem.EffectPool pool, GameObject go, EffectDriver driver, Transform followTarget, float endTime)
        {
            Pool = pool;
            Go = go;
            Driver = driver;
            FollowTarget = followTarget;
            EndTime = endTime;
            Stopped = false;
        }

        internal void Reset()
        {
            Pool = null;
            Go = null;
            Driver = null;
            FollowTarget = null;
            EndTime = 0f;
            Stopped = false;
        }
    }
}
