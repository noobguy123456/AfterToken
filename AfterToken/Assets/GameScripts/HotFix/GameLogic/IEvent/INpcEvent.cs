using TEngine;

namespace GameLogic
{
    /// <summary>
    /// NPC 事件接口。
    /// NpcSystem 发送，对话系统等叙事消费者接收。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface INpcEvent
    {
        /// <summary>
        /// 玩家与 NPC 触发交谈（E 键）。
        /// 对话系统落地前由 NpcSystem 打日志占位。
        /// </summary>
        void OnNpcTalked(int npcId);
    }
}
