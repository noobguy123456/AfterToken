using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 订单系统：订单生成、交付、刷新与奖励发放。
    /// </summary>
    public class OrderSystem : MonoBehaviour
    {
        private readonly List<OrderInstance> _orders = new List<OrderInstance>();
        private int _nextInstanceId = 1;
        private float _refreshTimer;
        private GameEventMgr _eventMgr;

        public IReadOnlyList<OrderInstance> Orders => _orders;

        private void Awake()
        {
            UnityEngine.Random.InitState(System.Environment.TickCount);
            _eventMgr = new GameEventMgr();
            _eventMgr.AddEvent<float, float>(ISimulationEvent_Event.OnSimulationTimeAdvanced, OnTimeAdvanced);
        }

        private void OnDestroy()
        {
            _eventMgr?.Clear();
        }

        private void Start()
        {
            GenerateInitialOrders();
        }

        private void GenerateInitialOrders()
        {
            int maxCount = SimTimeConfigMgr.Instance.MaxOrderCount;
            for (int i = 0; i < maxCount; i++)
            {
                GenerateRandomOrder();
            }
        }

        public bool TryDeliverOrder(int orderInstanceId)
        {
            var order = GetOrder(orderInstanceId);
            if (order == null)
            {
                return false;
            }

            var cfg = OrderConfigMgr.Instance.Get(order.ConfigId);
            if (cfg == null)
            {
                return false;
            }

            if (!InventorySystem.HasItems(cfg.RequiredItems))
            {
                Log.Warning($"[OrderSystem] 库存不足，无法交付订单 {order.ConfigId}");
                return false;
            }

            // 先扣除物品，再发放奖励，避免奖励发放失败后物品未扣除
            InventorySystem.TryConsumeItems(cfg.RequiredItems);
            CurrencySystem.AddGold(cfg.RewardGold);
            InventorySystem.AddItems(cfg.RewardItems);
            PlayerProfileSystem.AddExp(cfg.RewardExp);

            _orders.Remove(order);
            GameEvent.Get<ISimulationEvent>().OnOrderCompleted(order.ConfigId, order.InstanceId);
            return true;
        }

        public OrderInstance GetOrder(int orderInstanceId)
        {
            for (int i = 0; i < _orders.Count; i++)
            {
                if (_orders[i].InstanceId == orderInstanceId)
                {
                    return _orders[i];
                }
            }
            return null;
        }

        private void OnTimeAdvanced(float deltaTime, float totalTime)
        {
            if (Time.frameCount % 15 == 0) Log.Info($"[hb] Order f={Time.frameCount}");
            _refreshTimer += deltaTime;
            if (_refreshTimer >= SimTimeConfigMgr.Instance.OrderRefreshInterval)
            {
                _refreshTimer = 0f;
                TryRefreshOrders();
            }

            for (int i = _orders.Count - 1; i >= 0; i--)
            {
                var order = _orders[i];
                if (order.RemainingTime > 0f)
                {
                    order.RemainingTime -= deltaTime;
                    if (order.RemainingTime <= 0f)
                    {
                        _orders.RemoveAt(i);
                    }
                }
            }
        }

        private void TryRefreshOrders()
        {
            int maxCount = SimTimeConfigMgr.Instance.MaxOrderCount;
            // 防御：配置为空或权重全 0 导致无法生成新订单时，避免 while 死循环卡死主线程
            int guard = maxCount * 4;
            while (_orders.Count < maxCount && guard-- > 0)
            {
                int before = _orders.Count;
                GenerateRandomOrder();
                if (_orders.Count == before)
                {
                    break;
                }
            }
        }

        private void GenerateRandomOrder()
        {
            var allOrders = OrderConfigMgr.Instance.GetAll();
            if (allOrders == null || allOrders.Count == 0)
            {
                return;
            }

            int totalWeight = 0;
            foreach (var o in allOrders)
            {
                totalWeight += o.Weight;
            }

            int random = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;
            foreach (var o in allOrders)
            {
                current += o.Weight;
                if (random < current)
                {
                    int instanceId = _nextInstanceId++;
                    var order = new OrderInstance(instanceId, o.Id, o.TimeLimit);
                    _orders.Add(order);
                    GameEvent.Get<ISimulationEvent>().OnOrderGenerated(o.Id, instanceId);
                    return;
                }
            }
        }

        public void Clear()
        {
            _orders.Clear();
            _nextInstanceId = 1;
            _refreshTimer = 0f;
        }
    }
}
