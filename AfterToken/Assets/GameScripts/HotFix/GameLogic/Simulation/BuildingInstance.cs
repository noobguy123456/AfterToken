namespace GameLogic
{
    /// <summary>
    /// 运行时建筑实例数据。
    /// </summary>
    public class BuildingInstance
    {
        public int InstanceId;
        public int ConfigId;
        public int Level;
        public BuildingState State;
        public float Progress;
        public int[] ProductionSlots;

        public BuildingInstance(int instanceId, int configId, int level, int productionSlotCount)
        {
            InstanceId = instanceId;
            ConfigId = configId;
            Level = level;
            State = BuildingState.Building;
            Progress = 0f;
            ProductionSlots = new int[productionSlotCount];
        }
    }
}
