using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 模拟经营事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ISimulationEvent
    {
        void OnBuildingCompleted(int buildingId, int instanceId, int level);
        void OnBuildingUpgraded(int buildingId, int instanceId, int level);
        void OnProductionStarted(int productionId, int instanceId);
        void OnProductionFinished(int productionId, int instanceId, int itemId, int count);
        void OnOrderGenerated(int orderId, int orderInstanceId);
        void OnOrderCompleted(int orderId, int orderInstanceId);
        void OnSimulationTimeAdvanced(float deltaTime, float totalTime);
        void OnSimulationSpeedChanged(ESimSpeed speed);
    }
}
