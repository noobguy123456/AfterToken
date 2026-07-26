namespace GameLogic
{
    /// <summary>
    /// 运行时生产实例数据。
    /// </summary>
    public class ProductionInstance
    {
        public int InstanceId;
        public int ConfigId;
        public int BuildingInstanceId;
        public float Progress;
        public int OutputItemId;
        public int OutputCount;

        public ProductionInstance(int instanceId, int configId, int buildingInstanceId, int outputItemId, int outputCount)
        {
            InstanceId = instanceId;
            ConfigId = configId;
            BuildingInstanceId = buildingInstanceId;
            Progress = 0f;
            OutputItemId = outputItemId;
            OutputCount = outputCount;
        }
    }
}
