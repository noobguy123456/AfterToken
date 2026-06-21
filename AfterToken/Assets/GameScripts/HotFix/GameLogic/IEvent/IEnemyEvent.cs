using TEngine;
namespace GameLogic
{
    /// <summary>
    /// 敌人事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IEnemyEvent
    {
        void OnEnemySpawned(int enemyId, int configId);
        void OnEnemyDied(int enemyId);
    }
}
