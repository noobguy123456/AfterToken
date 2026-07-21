using System.Collections.Generic;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 玩家仓库面板（大厅进入）。
    /// 显示仓库全部道具与容量。
    /// </summary>
    [Window(UILayer.Top, location: "WarehouseUI", fullScreen: true)]
    public class WarehouseUI : UIWindow
    {
        private TextMeshProUGUI _capacityText;
        private RectTransform _slotRoot;
        private GameObject _slotTemplate;
        private Button _closeButton;

        private readonly List<ItemSlotWidget> _slots = new List<ItemSlotWidget>();

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），内容节点均在 m_img_Background 下，必须写完整路径。
            _capacityText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Capacity");
            _slotRoot = FindChildComponent<RectTransform>("m_img_Background/m_rect_SlotRoot");
            _slotTemplate = FindChild("m_img_Background/m_rect_SlotRoot/m_item_Slot")?.gameObject;
            _closeButton = FindChildComponent<Button>("m_img_Background/m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<WarehouseUI>());
            }

            Refresh();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(IItemEvent_Event.OnWarehouseChanged, OnWarehouseChanged);
        }

        protected override void OnDestroy()
        {
            ItemTooltipUI.HideTooltip();
            base.OnDestroy();
        }

        private void OnWarehouseChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            var items = Warehouse.Items;

            if (_capacityText != null)
            {
                _capacityText.text = $"Capacity: {Warehouse.UsedSlots}/{Warehouse.MaxSlots}";
            }

            if (_slotRoot == null || _slotTemplate == null)
            {
                return;
            }

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
