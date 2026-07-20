using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 掉落系统。
    /// 监听敌人死亡事件，按掉落表掷点并在死亡位置生成掉落物（PickupEntity）。
    /// </summary>
    public class DropSystem : MonoBehaviour
    {
        public static DropSystem Instance { get; private set; }

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private void Awake()
        {
            Instance = this;
            _eventMgr.AddEvent<int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;
        }

        private void OnEnemyDied(int enemyId)
        {
            // 事件触发时敌人实体尚未销毁，可从注册表取到配置 ID 与死亡位置
            if (!EnemyRegistry.TryGet(enemyId, out var enemy) || enemy == null)
            {
                return;
            }

            int configId = enemy.ConfigId;
            Vector2 position = enemy.transform.position;
            var drops = DropConfigMgr.Instance.GetDropsForEnemy(configId);
            if (drops.Count == 0)
            {
                return;
            }

            foreach (var drop in drops)
            {
                // dropRate 单位为万分之一（10000 = 必掉）
                if (Random.Range(0, 10000) >= drop.DropRate)
                {
                    continue;
                }

                int count = Random.Range(drop.MinCount, drop.MaxCount + 1);
                if (count <= 0)
                {
                    continue;
                }

                Vector2 offset = Random.insideUnitCircle * 0.5f;
                PickupEntity.Spawn(drop.ItemId, count, position + offset);
            }
        }
    }
}
