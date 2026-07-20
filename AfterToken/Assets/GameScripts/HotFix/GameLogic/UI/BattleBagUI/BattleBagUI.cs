using System.Collections.Generic;
using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗内临时背包面板（B 键开关）。
    /// 显示当前容量/最大容量与道具格子。
    /// </summary>
    [Window(UILayer.Top, location: "BattleBagUI", fullScreen: false)]
    public class BattleBagUI : UIWindow
    {
        private TextMeshProUGUI _capacityText;
        private RectTransform _slotRoot;
        private GameObject _slotTemplate;

        private readonly List<ItemSlotWidget> _slots = new List<ItemSlotWidget>();

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _capacityText = FindChildComponent<TextMeshProUGUI>("m_text_Capacity");
            _slotRoot = FindChildComponent<RectTransform>("m_rect_SlotRoot");
            _slotTemplate = FindChild("m_rect_SlotRoot/m_item_Slot")?.gameObject;
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            Refresh();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<int, int>(IItemEvent_Event.OnTempInventoryChanged, OnTempInventoryChanged);
        }

        protected override void OnDestroy()
        {
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void OnTempInventoryChanged(int usedSlots, int maxSlots)
        {
            Refresh();
        }

        private void Refresh()
        {
            var items = RunInventory.Items;

            if (_capacityText != null)
            {
                _capacityText.text = $"Capacity: {RunInventory.UsedSlots}/{RunInventory.MaxSlots}";
            }

            if (_slotRoot == null || _slotTemplate == null)
            {
                return;
            }

            // 格子数量与背包内容对齐
            while (_slots.Count < items.Count)
            {
                var widget = CreateWidgetByPrefab<ItemSlotWidget>(_slotTemplate, _slotRoot);
                if (widget == null)
                {
                    break;
                }
                widget.gameObject.SetActive(true);
                _slots.Add(widget);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                bool active = i < items.Count;
                _slots[i].gameObject.SetActive(active);
                if (active)
                {
                    _slots[i].SetItem(items[i]);
                }
            }
        }
    }
}
