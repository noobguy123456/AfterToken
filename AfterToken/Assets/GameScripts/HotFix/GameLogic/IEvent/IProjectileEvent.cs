using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 子弹事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IProjectileEvent
    {
        void OnProjectileCreated(int projectileId, Vector3 position);
        void OnProjectileHit(int projectileId, GameObject target);
        void OnProjectileDestroyed(int projectileId);
    }
}
