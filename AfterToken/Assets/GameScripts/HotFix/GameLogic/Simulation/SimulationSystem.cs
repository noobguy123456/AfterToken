using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 经营总控：初始化并协调各经营子系统。
    /// </summary>
    public class SimulationSystem : MonoBehaviour
    {
        private SimTimeSystem _simTimeSystem;
        private BuildingSystem _buildingSystem;
        private ProductionSystem _productionSystem;
        private OrderSystem _orderSystem;
        private BuildingPlacementSystem _placementSystem;
        private Transform _buildingRoot;

        public SimTimeSystem SimTime => _simTimeSystem;
        public BuildingSystem Building => _buildingSystem;
        public ProductionSystem Production => _productionSystem;
        public OrderSystem Order => _orderSystem;
        public BuildingPlacementSystem Placement => _placementSystem;

        private void Awake()
        {
            // 创建建筑根节点
            var buildingRootGo = new GameObject("BuildingRoot");
            buildingRootGo.transform.SetParent(transform, false);
            _buildingRoot = buildingRootGo.transform;

            _simTimeSystem = gameObject.AddComponent<SimTimeSystem>();
            _buildingSystem = gameObject.AddComponent<BuildingSystem>();
            _productionSystem = gameObject.AddComponent<ProductionSystem>();
            _orderSystem = gameObject.AddComponent<OrderSystem>();
            _placementSystem = gameObject.AddComponent<BuildingPlacementSystem>();

            _buildingSystem.Initialize(_buildingRoot);
            _productionSystem.Initialize(_buildingSystem);
            _placementSystem.Initialize(_buildingSystem);
        }

        public void Enter()
        {
            _simTimeSystem?.Resume();
        }

        public void Leave()
        {
            _simTimeSystem?.Pause();
            _placementSystem?.CancelPlacement();
            _buildingSystem?.Clear();
            _productionSystem?.Clear();
            _orderSystem?.Clear();
        }
    }
}
