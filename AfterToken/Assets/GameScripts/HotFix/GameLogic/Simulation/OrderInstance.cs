namespace GameLogic
{
    /// <summary>
    /// 运行时订单实例数据。
    /// </summary>
    public class OrderInstance
    {
        public int InstanceId;
        public int ConfigId;
        public float RemainingTime;

        public OrderInstance(int instanceId, int configId, float timeLimit)
        {
            InstanceId = instanceId;
            ConfigId = configId;
            RemainingTime = timeLimit;
        }
    }
}
