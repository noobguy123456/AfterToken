using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 生产系统：生产队列、配方消耗与产出结算。
    /// </summary>
    public class ProductionSystem : MonoBehaviour
    {
        private readonly List<ProductionInstance> _productions = new List<ProductionInstance>();
        private int _nextInstanceId = 1;
        private GameEventMgr _eventMgr;
        private BuildingSystem _buildingSystem;

        public IReadOnlyList<ProductionInstance> Productions => _productions;

        private void Awake()
        {
            _eventMgr = new GameEventMgr();
            _eventMgr.AddEvent<float, float>(ISimulationEvent_Event.OnSimulationTimeAdvanced, OnTimeAdvanced);
        }

        private void OnDestroy()
        {
            _eventMgr?.Clear();
        }

        public void Initialize(BuildingSystem buildingSystem)
        {
            _buildingSystem = buildingSystem;
        }

        public bool TryStartProduction(int buildingInstanceId, int productionId, out int productionInstanceId)
        {
            productionInstanceId = 0;
            if (_buildingSystem == null)
            {
                Log.Error("[ProductionSystem] BuildingSystem 未初始化");
                return false;
            }

            var building = _buildingSystem.GetBuilding(buildingInstanceId);
            if (building == null || building.State != BuildingState.Idle)
            {
                return false;
            }

            var cfg = ProductionConfigMgr.Instance.Get(productionId);
            if (cfg == null)
            {
                Log.Warning($"[ProductionSystem] 找不到生产配方 {productionId}");
                return false;
            }

            if (cfg.BuildingId != building.ConfigId)
            {
                Log.Warning($"[ProductionSystem] 建筑类型不匹配，无法生产 {cfg.Name}");
                return false;
            }

            if (building.Level < cfg.LevelRequired)
            {
                Log.Warning($"[ProductionSystem] 建筑等级不足，无法生产 {cfg.Name}");
                return false;
            }

            if (!InventorySystem.HasItems(cfg.InputItems))
            {
                Log.Warning($"[ProductionSystem] 材料不足，无法生产 {cfg.Name}");
                return false;
            }

            int slotIndex = _buildingSystem.FindFreeSlot(buildingInstanceId);
            if (slotIndex < 0)
            {
                Log.Warning($"[ProductionSystem] 生产队列已满");
                return false;
            }

            // 先创建实例并占用槽位，再扣除材料，避免材料扣除后实例创建失败
            productionInstanceId = _nextInstanceId++;
            var production = new ProductionInstance(productionInstanceId, productionId, buildingInstanceId, cfg.OutputItemId, cfg.OutputCount);
            _productions.Add(production);
            _buildingSystem.TryOccupySlot(buildingInstanceId, slotIndex, productionInstanceId);

            InventorySystem.TryConsumeItems(cfg.InputItems);

            GameEvent.Get<ISimulationEvent>().OnProductionStarted(productionId, productionInstanceId);
            return true;
        }

        public ProductionInstance GetProduction(int productionInstanceId)
        {
            for (int i = 0; i < _productions.Count; i++)
            {
                if (_productions[i].InstanceId == productionInstanceId)
                {
                    return _productions[i];
                }
            }
            return null;
        }

        private void OnTimeAdvanced(float deltaTime, float totalTime)
        {
            for (int i = _productions.Count - 1; i >= 0; i--)
            {
                var production = _productions[i];
                var cfg = ProductionConfigMgr.Instance.Get(production.ConfigId);
                if (cfg == null)
                {
                    _productions.RemoveAt(i);
                    continue;
                }

                production.Progress += deltaTime / cfg.ProductionTime;
                if (production.Progress >= 1f)
                {
                    production.Progress = 1f;
                    CompleteProduction(production);
                    _productions.RemoveAt(i);
                }
            }
        }

        private void CompleteProduction(ProductionInstance production)
        {
            InventorySystem.AddItem(production.OutputItemId, production.OutputCount);
            GameEvent.Get<ISimulationEvent>().OnProductionFinished(production.ConfigId, production.InstanceId, production.OutputItemId, production.OutputCount);
        }

        public void Clear()
        {
            _productions.Clear();
            _nextInstanceId = 1;
        }
    }
}
