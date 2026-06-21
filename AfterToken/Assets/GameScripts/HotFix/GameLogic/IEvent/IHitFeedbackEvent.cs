using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 受击/命中反馈事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IHitFeedbackEvent
    {
        /// <summary>
        /// 受到伤害指示器（CS 风格 8 方向）。
        /// </summary>
        /// <param name="angle">攻击来源角度（0-360，0 为正前方/上方）。</param>
        /// <param name="duration">显示持续时间。</param>
        void OnDamageIndicator(float angle, float duration);

        /// <summary>
        /// 命中目标反馈。
        /// </summary>
        /// <param name="isCritical">是否暴击/弱点。</param>
        void OnHitTarget(bool isCritical);

        /// <summary>
        /// 火箭锁定完成。
        /// </summary>
        /// <param name="targetId">锁定目标 ID。</param>
        void OnTargetLocked(int targetId);
    }
}
