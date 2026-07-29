using System.Collections.Generic;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 战斗内临时背包面板（B 键开关）。
    /// 显示当前容量/最大容量与道具格子。
    /// </summary>
    [Window(UILayer.Top, location: "BattleBagUI", fullScreen: false)]
    public class BattleBagUI : UIWindow
    {
        /// <summary>
        /// 背包打开时暂停游戏进程（不影响声音）。
        /// 若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        private TextMeshProUGUI _capacityText;
        private RectTransform _slotRoot;
        private GameObject _slotTemplate;
        private Button _closeButton;
        private Button _sortButton;

        private readonly List<ItemSlotWidget> _slots = new List<ItemSlotWidget>();

        /// <summary>
        /// 当前选中的格子（绿色高亮 + 放大），null 表示无选中。
        /// </summary>
        private ItemSlotWidget _selectedSlot;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），内容节点均在 m_img_Background 下，必须写完整路径。
            _capacityText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Capacity");
            _slotRoot = FindChildComponent<RectTransform>("m_img_Background/m_rect_SlotRoot");
            _slotTemplate = FindChild("m_img_Background/m_rect_SlotRoot/m_item_Slot")?.gameObject;
            _closeButton = FindChildComponent<Button>("m_img_Background/m_btn_Close");
            _sortButton = FindChildComponent<Button>("m_img_Background/m_btn_Sort");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);
            Refresh();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int, int>(IItemEvent_Event.OnTempInventoryChanged, OnTempInventoryChanged);

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<BattleBagUI>());
            }

            if (_sortButton != null)
            {
                _sortButton.onClick.RemoveAllListeners();
                _sortButton.onClick.AddListener(RunInventory.Organize);
            }
        }

        protected override void OnDestroy()
        {
            ItemTooltipUI.HideTooltip();
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void OnTempInventoryChanged(int usedSlots, int maxSlots)
        {
            Refresh();
        }

        private void Refresh()
        {
            // 背包内容刷新后格子与道具的对应关系可能变化，直接清除选中态避免误标
            ClearSelection();

            var items = RunInventory.Items;
            var maxSlots = RunInventory.MaxSlots;

            if (_capacityText != null)
            {
                _capacityText.text = $"Capacity: {RunInventory.UsedSlots}/{maxSlots}";
            }

            if (_slotRoot == null || _slotTemplate == null)
            {
                return;
            }

            // 格子数量始终与最大容量对齐，空槽位显示为空
            while (_slots.Count < maxSlots)
            {
                var widget = CreateWidgetByPrefab<ItemSlotWidget>(_slotTemplate, _slotRoot);
                if (widget == null)
                {
                    break;
                }
                widget.gameObject.SetActive(true);
                widget.Clicked = OnSlotClicked;
                _slots.Add(widget);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                bool hasItem = i < items.Count;
                _slots[i].gameObject.SetActive(true);
                if (hasItem)
                {
                    _slots[i].SetItem(items[i]);
                }
                else
                {
                    _slots[i].SetEmpty();
                }
            }
        }

        /// <summary>
        /// 格子点击：再次点击已选中的格子取消选中；空槽位点击只清除当前选中。
        /// </summary>
        private void OnSlotClicked(ItemSlotWidget slot)
        {
            if (_selectedSlot == slot)
            {
                ClearSelection();
                return;
            }

            ClearSelection();

            if (slot.HasItem)
            {
                _selectedSlot = slot;
                _selectedSlot.SetSelected(true);
            }
        }

        private void ClearSelection()
        {
            if (_selectedSlot != null)
            {
                _selectedSlot.SetSelected(false);
                _selectedSlot = null;
            }
        }
    }
}
