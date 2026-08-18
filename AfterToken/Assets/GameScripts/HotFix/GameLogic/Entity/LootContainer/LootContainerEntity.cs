using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Loot
{
    /// <summary>
    /// 战利品容器实体（搜打撤开箱点）。
    /// 挂载在场景中的容器对象上，负责触发区检测、占位视觉与箱内道具状态。
    /// 箱内道具在首次打开时按 TbLootContainer 权重表掷点生成，拿空后变为已开状态。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LootContainerEntity : MonoBehaviour
    {
        [SerializeField] private int _lootTableId = 1;
        [SerializeField] private SpriteRenderer _visualRenderer;

        /// <summary>
        /// 单次开箱掷点次数（每种道具按权重独立抽取）。
        /// </summary>
        private const int RollTimes = 3;

        private static readonly Color ClosedColor = new Color(0.72f, 0.5f, 0.25f, 1f);
        private static readonly Color OpenedColor = new Color(0.35f, 0.35f, 0.35f, 0.8f);

        private readonly List<ItemStack> _contents = new List<ItemStack>();
        private bool _rolled;
        private bool _opened;

        public int LootTableId => _lootTableId;
        public bool IsOpened => _opened;
        public bool PlayerInside { get; private set; }

        /// <summary>
        /// 箱内剩余道具（首开时生成，之后为拿取后的剩余）。
        /// </summary>
        public IReadOnlyList<ItemStack> Contents => _contents;

        private void Awake()
        {
            var collider = GetComponent<BoxCollider>();
            collider.isTrigger = true;
            if (collider.size == Vector3.one)
            {
                // 默认 1m 立方体触发区太小，放宽到 2m 便于交互
                collider.size = new Vector3(2f, 2f, 2f);
            }

            EnsureVisualRenderer();
            UpdateVisual();
        }

        private void OnEnable()
        {
            LootContainerRegistry.Register(this);
        }

        private void OnDisable()
        {
            LootContainerRegistry.Unregister(this);
        }

        private void EnsureVisualRenderer()
        {
            if (_visualRenderer != null) return;
            _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_visualRenderer != null) return;

            // 占位视觉：0.9m 平铺方块（与敌人一致的 X+90° 平躺渲染），1m 网格对齐
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            visualGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visualGo.transform.localScale = Vector3.one * 0.9f;
            // 抬高 5cm，避免与地面（y=0）共面 z-fighting 闪烁
            visualGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _visualRenderer = visualGo.AddComponent<SpriteRenderer>();
            _visualRenderer.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            _visualRenderer.sortingOrder = 1;
        }

        /// <summary>
        /// 首次打开时按权重表生成箱内道具（每种抽到的道具合并为一格堆叠）。
        /// </summary>
        public void EnsureContentsRolled()
        {
            if (_rolled) return;
            _rolled = true;

            var rows = LootContainerConfigMgr.Instance.GetRowsForContainer(_lootTableId);
            if (rows.Count == 0) return;

            int totalWeight = 0;
            foreach (var row in rows)
            {
                totalWeight += Mathf.Max(0, row.Weight);
            }
            if (totalWeight <= 0) return;

            for (int i = 0; i < RollTimes; i++)
            {
                int roll = Random.Range(0, totalWeight);
                foreach (var row in rows)
                {
                    roll -= Mathf.Max(0, row.Weight);
                    if (roll >= 0) continue;

                    int count = Random.Range(row.MinCount, row.MaxCount + 1);
                    if (count > 0)
                    {
                        AddStack(row.ItemId, count);
                    }
                    break;
                }
            }
        }

        private void AddStack(int itemId, int count)
        {
            for (int i = 0; i < _contents.Count; i++)
            {
                if (_contents[i].ItemId != itemId) continue;
                var stack = _contents[i];
                stack.Count += count;
                _contents[i] = stack;
                return;
            }
            _contents.Add(new ItemStack(itemId, count));
        }

        /// <summary>
        /// 移除指定格的道具（拿取成功后调用）。
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _contents.Count) return;
            _contents.RemoveAt(index);
            if (_contents.Count == 0)
            {
                SetOpened();
            }
        }

        /// <summary>
        /// 标记为已开（拿空）：视觉变灰，不再可交互。
        /// </summary>
        public void SetOpened()
        {
            if (_opened) return;
            _opened = true;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_visualRenderer == null) return;
            _visualRenderer.color = _opened ? OpenedColor : ClosedColor;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = true;
            LootContainerSystem.Instance?.OnPlayerEnteredContainer(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = false;
            LootContainerSystem.Instance?.OnPlayerExitedContainer(this);
        }
    }
}
