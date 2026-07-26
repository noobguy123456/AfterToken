using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家档案事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPlayerProfileEvent
    {
        void OnPlayerLevelUp(int newLevel);
        void OnExpChanged(int currentExp, int maxExp);
    }
}
