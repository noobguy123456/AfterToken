using System.Collections.Generic;
using GameLogic.Loot;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 战利品容器面板（开箱 UI）。
    /// 显示箱内道具格子，点击格子拿取单个，Take All 一键全拿。
    /// 不暂停游戏（搜打撤开箱有risk）；Esc/E/关闭按钮或走出触发区关闭。
    /// </summary>
    [Window(UILayer.Top, location: "LootContainerUI", fullScreen: false)]
    public class LootContainerUI : UIWindow
    {
        /// <summary>
        /// 开箱不暂停游戏（若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值）。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 1f;

        private TextMeshProUGUI _titleText;
        private RectTransform _slotRoot;
        private GameObject _slotTemplate;
        private Button _takeAllButton;
        private Button _closeButton;

        private readonly List<ItemSlotWidget> _slots = new List<ItemSlotWidget>();

        private LootContainerEntity _container;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Title");
            _slotRoot = FindChildComponent<RectTransform>("m_img_Background/m_rect_SlotRoot");
            _slotTemplate = FindChild("m_img_Background/m_rect_SlotRoot/m_item_Slot")?.gameObject;
            _takeAllButton = FindChildComponent<Button>("m_img_Background/m_btn_TakeAll");
            _closeButton = FindChildComponent<Button>("m_img_Background/m_btn_Close");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _container = UserDatas != null && UserDatas.Length > 0 ? UserDatas[0] as LootContainerEntity : null;
            Refresh();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();

            if (_takeAllButton != null)
            {
                _takeAllButton.onClick.RemoveAllListeners();
                _takeAllButton.onClick.AddListener(OnTakeAllClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => GameModule.UI.CloseUI<LootContainerUI>());
            }
        }

        protected override void OnDestroy()
        {
            ItemTooltipUI.HideTooltip();
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            _container = null;
            base.OnDestroy();
        }

        private void OnTakeAllClicked()
        {
            if (_container == null) return;
            LootContainerSystem.Instance?.TakeAll(_container);
            Refresh();
        }

        private void OnSlotClicked(ItemSlotWidget slot)
        {
            if (_container == null || !slot.HasItem) return;

            int index = _slots.IndexOf(slot);
            if (index < 0) return;

            LootContainerSystem.Instance?.TryTake(_container, index);
            Refresh();
        }

        private void Refresh()
        {
            if (_titleText != null)
            {
                _titleText.text = "Container";
            }

            if (_slotRoot == null || _slotTemplate == null) return;

            var contents = _container != null ? _container.Contents : null;
            int count = contents?.Count ?? 0;

            while (_slots.Count < count)
            {
                var widget = CreateWidgetByPrefab<ItemSlotWidget>(_slotTemplate, _slotRoot);
                if (widget == null) break;
                widget.gameObject.SetActive(true);
                widget.Clicked = OnSlotClicked;
                _slots.Add(widget);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < count)
                {
                    _slots[i].gameObject.SetActive(true);
                    _slots[i].SetItem(contents[i]);
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
