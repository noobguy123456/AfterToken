using TEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友好感度事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ICompanionAffinityEvent
    {
        /// <summary>好感度变化（delta 可为 0 的刷新场景不发）。</summary>
        void OnAffinityChanged(int companionId, int delta, int total);

        /// <summary>好感度档位提升（newTier 为新档位 1-4）。</summary>
        void OnAffinityTierUp(int companionId, int newTier);
    }
}
