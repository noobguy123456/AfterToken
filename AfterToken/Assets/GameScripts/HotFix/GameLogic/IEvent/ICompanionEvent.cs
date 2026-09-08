using TEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友事件接口。
    /// 由 CompanionSystem / 队友 FSM 发出，字幕 UI、小地图等订阅。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ICompanionEvent
    {
        /// <summary>
        /// 队友说了一句话（本地台词或 LLM 生成，字幕 UI 不关心来源）。
        /// </summary>
        void OnCompanionSay(string speaker, string text);

        /// <summary>
        /// 队友 FSM 状态切换（Follow/Hold/PingMove/Engage/Retreat/Dead）。
        /// </summary>
        void OnCompanionStateChanged(string stateName, string previousStateName);

        /// <summary>
        /// 跟随开关切换（G 键）：true = 跟随玩家，false = 原地驻守。
        /// </summary>
        void OnCompanionFollowToggled(bool followEnabled);
    }
}
