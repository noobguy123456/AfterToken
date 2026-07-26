using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 订单配置管理器。
    /// </summary>
    public class OrderConfigMgr
    {
        private static OrderConfigMgr _instance;
        public static OrderConfigMgr Instance => _instance ??= new OrderConfigMgr();

        public Order Get(int orderId)
        {
            return ConfigSystem.Instance.Tables.TbOrder.GetOrDefault(orderId);
        }

        public System.Collections.Generic.IReadOnlyList<Order> GetAll()
        {
            return ConfigSystem.Instance.Tables.TbOrder.DataList;
        }
    }
}
