using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 建筑系统：建造、升级、拆除与状态推进。
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        private readonly List<BuildingInstance> _buildings = new List<BuildingInstance>();
        private readonly Dictionary<int, BuildingEntity> _buildingEntities = new Dictionary<int, BuildingEntity>();
        private int _nextInstanceId = 1;
        private GameEventMgr _eventMgr;
        private Transform _buildingRoot;

        public IReadOnlyList<BuildingInstance> Buildings => _buildings;

        private void Awake()
        {
            _eventMgr = new GameEventMgr();
            _eventMgr.AddEvent<float, float>(ISimulationEvent_Event.OnSimulationTimeAdvanced, OnTimeAdvanced);
        }

        private void OnDestroy()
        {
            _eventMgr?.Clear();
        }

        public void Initialize(Transform buildingRoot)
        {
            _buildingRoot = buildingRoot;
        }

        public bool TryBuild(int configId, out int instanceId)
        {
            return TryBuild(configId, Vector3.zero, out instanceId);
        }

        public bool TryBuild(int configId, Vector3 position, out int instanceId)
        {
            instanceId = 0;
            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null)
            {
                Log.Warning($"[BuildingSystem] 找不到建筑配置 {configId}");
                return false;
            }

            // 检查是否已存在相同建筑
            foreach (var b in _buildings)
            {
                if (b.ConfigId == configId)
                {
                    Log.Warning($"[BuildingSystem] 建筑 {cfg.Name} 已存在，无法重复建造");
                    return false;
                }
            }

            if (!CurrencySystem.HasGold(cfg.BuildCostGold))
            {
                Log.Warning($"[BuildingSystem] 金币不足，无法建造 {cfg.Name}");
                return false;
            }

            if (!InventorySystem.HasItems(cfg.BuildCostItems))
            {
                Log.Warning($"[BuildingSystem] 材料不足，无法建造 {cfg.Name}");
                return false;
            }

            // 先创建实例，再扣除资源，避免资源扣除后实例创建失败
            instanceId = _nextInstanceId++;
            var building = new BuildingInstance(instanceId, configId, 1, cfg.ProductionSlotCount);

            CurrencySystem.TryConsumeGold(cfg.BuildCostGold);
            InventorySystem.TryConsumeItems(cfg.BuildCostItems);

            _buildings.Add(building);

            // 创建场景实体
            CreateBuildingEntityAsync(building, position).Forget();

            return true;
        }

        private async UniTaskVoid CreateBuildingEntityAsync(BuildingInstance building, Vector3 position)
        {
            if (_buildingRoot == null)
            {
                Log.Warning("[BuildingSystem] BuildingRoot 未设置，无法创建建筑实体");
                return;
            }

            var go = new GameObject($"Building_{building.InstanceId}");
            go.transform.SetParent(_buildingRoot, false);
            var entity = go.AddComponent<BuildingEntity>();
            await entity.InitializeAsync(building.InstanceId, building.ConfigId, position);
            _buildingEntities[building.InstanceId] = entity;
        }

        public bool TryUpgrade(int instanceId)
        {
            var building = GetBuilding(instanceId);
            if (building == null || building.State != BuildingState.Idle)
            {
                return false;
            }

            var cfg = BuildingConfigMgr.Instance.Get(building.ConfigId);
            if (cfg == null || building.Level >= cfg.MaxLevel)
            {
                return false;
            }

            if (!CurrencySystem.HasGold(cfg.UpgradeCostGold))
            {
                Log.Warning($"[BuildingSystem] 金币不足，无法升级 {cfg.Name}");
                return false;
            }

            if (!InventorySystem.HasItems(cfg.UpgradeCostItems))
            {
                Log.Warning($"[BuildingSystem] 材料不足，无法升级 {cfg.Name}");
                return false;
            }

            CurrencySystem.TryConsumeGold(cfg.UpgradeCostGold);
            InventorySystem.TryConsumeItems(cfg.UpgradeCostItems);

            building.State = BuildingState.Upgrading;
            building.Progress = 0f;

            // 更新场景实体状态
            if (_buildingEntities.TryGetValue(instanceId, out var entity))
            {
                entity.UpdateState(BuildingState.Upgrading, 0f);
            }

            return true;
        }

        public bool TryDemolish(int instanceId)
        {
            var building = GetBuilding(instanceId);
            if (building == null || building.State != BuildingState.Idle)
            {
                return false;
            }

            // 销毁场景实体
            if (_buildingEntities.TryGetValue(instanceId, out var entity))
            {
                Destroy(entity.gameObject);
                _buildingEntities.Remove(instanceId);
            }

            _buildings.Remove(building);
            return true;
        }

        public BuildingInstance GetBuilding(int instanceId)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i].InstanceId == instanceId)
                {
                    return _buildings[i];
                }
            }
            return null;
        }

        public BuildingEntity GetBuildingEntity(int instanceId)
        {
            _buildingEntities.TryGetValue(instanceId, out var entity);
            return entity;
        }

        public bool IsSlotAvailable(int instanceId, int slotIndex)
        {
            var building = GetBuilding(instanceId);
            if (building == null || building.State != BuildingState.Idle)
            {
                return false;
            }
            if (slotIndex < 0 || slotIndex >= building.ProductionSlots.Length)
            {
                return false;
            }
            return building.ProductionSlots[slotIndex] == 0;
        }

        public bool TryOccupySlot(int instanceId, int slotIndex, int productionInstanceId)
        {
            if (!IsSlotAvailable(instanceId, slotIndex))
            {
                return false;
            }
            var building = GetBuilding(instanceId);
            building.ProductionSlots[slotIndex] = productionInstanceId;
            return true;
        }

        public void ReleaseSlot(int instanceId, int slotIndex)
        {
            var building = GetBuilding(instanceId);
            if (building == null || slotIndex < 0 || slotIndex >= building.ProductionSlots.Length)
            {
                return;
            }
            building.ProductionSlots[slotIndex] = 0;
        }

        public int FindFreeSlot(int instanceId)
        {
            var building = GetBuilding(instanceId);
            if (building == null || building.State != BuildingState.Idle)
            {
                return -1;
            }
            for (int i = 0; i < building.ProductionSlots.Length; i++)
            {
                if (building.ProductionSlots[i] == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private void OnTimeAdvanced(float deltaTime, float totalTime)
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                var building = _buildings[i];
                var cfg = BuildingConfigMgr.Instance.Get(building.ConfigId);
                if (cfg == null)
                {
                    continue;
                }

                if (building.State == BuildingState.Building)
                {
                    building.Progress += deltaTime / cfg.BuildTime;
                    if (building.Progress >= 1f)
                    {
                        building.Progress = 1f;
                        building.State = BuildingState.Idle;
                        UpdateBuildingEntityState(building);
                        GameEvent.Get<ISimulationEvent>().OnBuildingCompleted(building.ConfigId, building.InstanceId, building.Level);
                    }
                    else
                    {
                        UpdateBuildingEntityState(building);
                    }
                }
                else if (building.State == BuildingState.Upgrading)
                {
                    building.Progress += deltaTime / cfg.UpgradeTime;
                    if (building.Progress >= 1f)
                    {
                        building.Progress = 1f;
                        building.Level++;
                        building.State = BuildingState.Idle;
                        UpdateBuildingEntityState(building);
                        GameEvent.Get<ISimulationEvent>().OnBuildingUpgraded(building.ConfigId, building.InstanceId, building.Level);
                    }
                    else
                    {
                        UpdateBuildingEntityState(building);
                    }
                }
            }
        }

        private void UpdateBuildingEntityState(BuildingInstance building)
        {
            if (_buildingEntities.TryGetValue(building.InstanceId, out var entity))
            {
                entity.UpdateState(building.State, building.Progress);
                
                // 更新建筑标签（等级变化时）
                var cfg = BuildingConfigMgr.Instance.Get(building.ConfigId);
                if (cfg != null)
                {
                    entity.UpdateLabel(cfg.Name, building.Level);
                }
            }
        }

        public void Clear()
        {
            foreach (var entity in _buildingEntities.Values)
            {
                if (entity != null)
                {
                    Destroy(entity.gameObject);
                }
            }
            _buildingEntities.Clear();
            _buildings.Clear();
            _nextInstanceId = 1;
        }
    }
}
