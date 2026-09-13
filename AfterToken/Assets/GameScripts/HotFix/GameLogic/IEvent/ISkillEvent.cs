using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 技能树事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ISkillEvent
    {
        /// <summary>技能升级成功（newLevel 为升级后的等级）。</summary>
        void OnSkillUpgraded(int skillId, int newLevel);
    }
}
