using TEngine;
namespace GameLogic
{
    /// <summary>
    /// 战斗事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IBattleEvent
    {
        void OnEntityDamaged(DamageInfo damageInfo);
        void OnEntityKilled(int attackerId, int targetId);
        void OnPickupCollected(int pickupId, int collectorId);
    }
}
