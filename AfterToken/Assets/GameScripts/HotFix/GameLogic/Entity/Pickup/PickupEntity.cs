using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 世界掉落物实体。
    /// 玩家触碰后尝试拾取进临时背包；背包满时保留在地上，可稍后拾取。
    /// 代码自建占位视觉（占位 sprite + 稀有度染色），无需 Prefab。
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class PickupEntity : MonoBehaviour
    {
        private int _itemId;
        private int _count;

        /// <summary>
        /// 在指定位置生成一个掉落物。
        /// </summary>
        public static PickupEntity Spawn(int itemId, int count, Vector2 position)
        {
            var go = new GameObject($"Pickup_{itemId}");
            go.transform.position = position;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            var pickup = go.AddComponent<PickupEntity>();
            pickup.Initialize(itemId, count);
            return pickup;
        }

        private void Initialize(int itemId, int count)
        {
            _itemId = itemId;
            _count = count;

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            visualGo.transform.localScale = Vector3.one * 0.5f;

            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            renderer.color = RarityColors.Get(ItemConfigMgr.Instance.GetQuality(itemId));
            renderer.sortingOrder = 2;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (RunInventory.TryAdd(_itemId, _count))
            {
                GameEvent.Get<IItemEvent>().OnItemPickedUp(_itemId, _count);
                Destroy(gameObject);
            }
            // 背包已满时保留在地上；RunInventory 内部已广播 OnInventoryFull
        }
    }
}
