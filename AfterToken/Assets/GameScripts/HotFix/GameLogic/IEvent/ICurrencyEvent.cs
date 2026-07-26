using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 货币变化事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ICurrencyEvent
    {
        void OnGoldChanged(long currentGold);
        void OnDiamondChanged(long currentDiamond);
        void OnEnergyChanged(int currentEnergy, int maxEnergy);
    }
}
