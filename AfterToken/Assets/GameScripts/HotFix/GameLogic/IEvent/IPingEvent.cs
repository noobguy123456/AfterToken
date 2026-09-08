using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 标点类型（APEX 式上下文标点）。
    /// </summary>
    public enum PingType
    {
        None = 0,
        /// <summary>地面/障碍：前往该点。</summary>
        Move = 1,
        /// <summary>掉落物/容器：前往拾取点。</summary>
        Loot = 2,
        /// <summary>敌人：集火标记目标。</summary>
        Attack = 3,
    }

    /// <summary>
    /// 标点事件接口。
    /// 由 PingSystem 发出，AI 队友、小地图、音效（未来）等订阅。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPingEvent
    {
        /// <summary>
        /// 玩家创建标点。targetId 仅 Attack 类型有效（敌人 InstanceID），其余为 0。
        /// </summary>
        void OnPingCreated(PingType type, Vector2 pos, int targetId);

        /// <summary>
        /// 标点被清除（过期/被新标点覆盖/已被消费）。
        /// </summary>
        void OnPingCleared();
    }
}
