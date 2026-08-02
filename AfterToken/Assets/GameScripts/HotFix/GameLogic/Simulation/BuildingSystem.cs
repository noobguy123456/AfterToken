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
        // 网格占用表：格子坐标 → 占用实例（格子坐标定义见 MapGrid，全局统一 1m 基础格）
        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();
        private readonly Dictionary<int, List<Vector2Int>> _buildingCells = new Dictionary<int, List<Vector2Int>>();
        // 已购买的数量栏位（configId → 栏位数），内存态，持久化由 save-system 统一实现
        private readonly Dictionary<int, int> _purchasedSlots = new Dictionary<int, int>();
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
            return TryBuild(configId, Vector3.zero, 0f, out instanceId);
        }

        public bool TryBuild(int configId, Vector3 position, out int instanceId)
        {
            return TryBuild(configId, position, 0f, out instanceId);
        }

        /// <summary>
        /// 建造建筑。rotationY 为朝向（0/90/180/270），旋转 90/270 度时占地 X/Z 对调。
        /// </summary>
        public bool TryBuild(int configId, Vector3 position, float rotationY, out int instanceId)
        {
            instanceId = 0;
            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null)
            {
                Log.Warning($"[BuildingSystem] 找不到建筑配置 {configId}");
                return false;
            }

            if (!CanBuild(configId, position, rotationY, out string reason))
            {
                Log.Warning($"[BuildingSystem] 无法建造 {cfg.Name}：{reason}");
                return false;
            }

            // 吸附到网格并登记占地（全局统一 1m 基础格，旋转时占地 X/Z 对调）
            ComputeFootprint(cfg, position, rotationY, out Vector3 snapped, out var cells);

            // 先创建实例，再扣除资源，避免资源扣除后实例创建失败
            instanceId = _nextInstanceId++;
            var building = new BuildingInstance(instanceId, configId, 1, cfg.ProductionSlotCount);

            CurrencySystem.TryConsumeGold(cfg.BuildCostGold);
            InventorySystem.TryConsumeItems(cfg.BuildCostItems);

            _buildings.Add(building);
            _buildingCells[instanceId] = cells;
            foreach (var cell in cells)
            {
                _occupiedCells.Add(cell);
            }

            // 创建场景实体
            CreateBuildingEntityAsync(building, snapped, rotationY).Forget();

            return true;
        }

        /// <summary>
        /// 校验指定位置能否建造（占地/数量上限/金币/材料），失败原因写入 reason（供摆放预览染色与飘字提示）。
        /// </summary>
        public bool CanBuild(int configId, Vector3 position, float rotationY, out string reason)
        {
            reason = null;
            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null)
            {
                reason = "配置错误";
                return false;
            }

            ComputeFootprint(cfg, position, rotationY, out _, out var cells);
            if (!IsAreaFree(cells))
            {
                reason = "当前位置无法放置";
                return false;
            }

            if (CountByConfig(configId) >= GetMaxCount(configId))
            {
                reason = "数量已达上限";
                return false;
            }

            if (!CurrencySystem.HasGold(cfg.BuildCostGold))
            {
                reason = "金币不足";
                return false;
            }

            if (!InventorySystem.HasItems(cfg.BuildCostItems))
            {
                reason = "材料不足";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 同类建筑当前数量上限 = 基础值(maxCount) + 玩家等级解锁(maxCountPerPlayerLevel)
        /// + 升级解锁(同类每有 1 座达到 maxCountUpgradeLevel 上限 +1) + 已购买栏位。
        /// 三种方式并存，配置方法见 building.xlsx 字段备注。
        /// </summary>
        public int GetMaxCount(int configId)
        {
            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null)
            {
                return 0;
            }

            int max = cfg.MaxCount;

            if (cfg.MaxCountPerPlayerLevel > 0)
            {
                max += Mathf.Max(0, PlayerProfileSystem.Level - 1) * cfg.MaxCountPerPlayerLevel;
            }

            if (cfg.MaxCountUpgradeLevel > 0)
            {
                foreach (var b in _buildings)
                {
                    if (b.ConfigId == configId && b.Level >= cfg.MaxCountUpgradeLevel)
                    {
                        max++;
                    }
                }
            }

            if (_purchasedSlots.TryGetValue(configId, out int slots))
            {
                max += slots;
            }

            return max;
        }

        /// <summary>同类建筑当前已建数量。</summary>
        public int CountByConfig(int configId)
        {
            int count = 0;
            foreach (var b in _buildings)
            {
                if (b.ConfigId == configId)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 购买下一个数量栏位的金币价格 = maxCountSlotBaseCost + 已购数 * maxCountSlotCostGrow。
        /// 返回 0 表示该建筑不可购买栏位。
        /// </summary>
        public long GetSlotPrice(int configId)
        {
            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null || cfg.MaxCountSlotBaseCost <= 0)
            {
                return 0;
            }
            _purchasedSlots.TryGetValue(configId, out int purchased);
            return cfg.MaxCountSlotBaseCost + (long)purchased * cfg.MaxCountSlotCostGrow;
        }

        /// <summary>购买一个数量栏位（永久提升该类型上限 +1）。</summary>
        public bool TryPurchaseSlot(int configId, out string reason)
        {
            reason = null;
            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null)
            {
                reason = "配置错误";
                return false;
            }

            long price = GetSlotPrice(configId);
            if (price <= 0)
            {
                reason = "该建筑不可解锁";
                return false;
            }

            if (!CurrencySystem.HasGold(price))
            {
                reason = "金币不足";
                return false;
            }

            CurrencySystem.TryConsumeGold(price);
            _purchasedSlots.TryGetValue(configId, out int purchased);
            _purchasedSlots[configId] = purchased + 1;
            Log.Info($"[BuildingSystem] 购买栏位：{cfg.Name} 数量上限提升至 {GetMaxCount(configId)}（花费 {price}G）");
            return true;
        }

        /// <summary>计算吸附后的位置与占地格子（旋转 90/270 度时占地 X/Z 对调）。</summary>
        private static void ComputeFootprint(GameConfig.cfg.Building cfg, Vector3 position, float rotationY, out Vector3 snapped, out List<Vector2Int> cells)
        {
            bool swapped = Mathf.RoundToInt(rotationY) % 180 != 0;
            int fx = swapped ? cfg.FootprintZ : cfg.FootprintX;
            int fz = swapped ? cfg.FootprintX : cfg.FootprintZ;
            snapped = MapGrid.Snap(position, fx, fz);
            cells = MapGrid.GetFootprintCells(snapped, fx, fz);
        }

        /// <summary>
        /// 检查一组格子是否全部空闲（供摆放预览与建造校验共用）。
        /// </summary>
        public bool IsAreaFree(List<Vector2Int> cells)
        {
            foreach (var cell in cells)
            {
                if (_occupiedCells.Contains(cell))
                {
                    return false;
                }
            }
            return true;
        }

        private async UniTaskVoid CreateBuildingEntityAsync(BuildingInstance building, Vector3 position, float rotationY = 0f)
        {
            if (_buildingRoot == null)
            {
                Log.Warning("[BuildingSystem] BuildingRoot 未设置，无法创建建筑实体");
                return;
            }

            var go = new GameObject($"Building_{building.InstanceId}");
            go.transform.SetParent(_buildingRoot, false);
            go.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
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

            // 释放占地格子
            if (_buildingCells.TryGetValue(instanceId, out var cells))
            {
                foreach (var cell in cells)
                {
                    _occupiedCells.Remove(cell);
                }
                _buildingCells.Remove(instanceId);
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
            if (Time.frameCount % 15 == 0) Log.Info($"[hb] Building f={Time.frameCount}");
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
            _purchasedSlots.Clear();
            _nextInstanceId = 1;
        }
    }
}
