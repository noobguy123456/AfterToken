using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 命中反馈与受击方向指示系统。
    /// 只负责事件订阅与逻辑计算，具体 UI 显示由 HitFeedbackUI 处理。
    /// </summary>
    public class HitFeedbackSystem : MonoBehaviour
    {
        public static HitFeedbackSystem Instance { get; private set; }

        [Header("伤害指示器")]

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private void Awake()
        {
            Instance = this;

            _eventMgr.AddEvent<float, float>(IHitFeedbackEvent_Event.OnDamageIndicator, OnDamageIndicator);
            _eventMgr.AddEvent<bool>(IHitFeedbackEvent_Event.OnHitTarget, OnHitTarget);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;
        }

        private void OnDamageIndicator(float angle, float duration)
        {
            HitFeedbackUI.Instance?.ShowDamageIndicator(angle);
        }

        private void OnHitTarget(bool isCritical)
        {
            HitFeedbackUI.Instance?.ShowHitMarker(isCritical);
        }
    }
}
