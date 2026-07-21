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
    }
}
