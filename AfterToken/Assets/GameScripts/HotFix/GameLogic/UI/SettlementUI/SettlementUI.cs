using System.Collections.Generic;
using TMPro;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 撤离结算画面：成功撤离后由 <see cref="Portal.PortalSystem.ExtractToBase"/> 打开，
    /// 展示关卡、meta 奖励（金币/经验）与战利品网格（RunInventory 快照）。
    /// 打开期间暂停游戏（TimeScaleWhenVisible=0）；确认按钮关闭本窗并切回经营流程。
    /// 不注册进任何 ESC 关窗链——必须点确认按钮，避免流程悬置。
    /// </summary>
    [Window(UILayer.Top, location: "SettlementUI", fullScreen: true)]
    public class SettlementUI : UIWindow
    {
        /// <summary>结算画面打开时暂停游戏进程。</summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _rewardsLabelText;
        private TextMeshProUGUI _rewardsText;
        private TextMeshProUGUI _lootLabelText;
        private TextMeshProUGUI _lootValueText;
        private TextMeshProUGUI _lootEmptyText;
        private RectTransform _slotRoot;
        private GameObject _slotTemplate;
        private Button _confirmButton;

        private readonly List<ItemSlotWidget> _slots = new List<ItemSlotWidget>();

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），内容节点均在 m_img_Background 下，必须写完整路径。
            _titleText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Title");
            _levelText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Level");
            _rewardsLabelText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_RewardsLabel");
            _rewardsText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_Rewards");
            _lootLabelText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_LootLabel");
            _lootValueText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_LootValue");
            _lootEmptyText = FindChildComponent<TextMeshProUGUI>("m_img_Background/m_text_LootEmpty");
            _slotRoot = FindChildComponent<RectTransform>("m_img_Background/m_rect_SlotRoot");
            _slotTemplate = FindChild("m_img_Background/m_rect_SlotRoot/m_item_Slot")?.gameObject;
            _confirmButton = FindChildComponent<Button>("m_img_Background/m_btn_Confirm");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            CrosshairUpdater.Instance?.SetVisible(false);

            Loc.Bind(_titleText, "ui.settlement.title");
            Loc.Bind(_rewardsLabelText, "ui.settlement.rewards");
            Loc.Bind(_lootLabelText, "ui.settlement.loot");
            Loc.Bind(FindChildComponent<TextMeshProUGUI>("m_img_Background/m_btn_Confirm/m_text_Label"), "ui.settlement.confirm");

            Populate(UserData as SettlementData);
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        protected override void OnDestroy()
        {
            ItemTooltipUI.HideTooltip();
            CrosshairUpdater.Instance?.SetVisible(true);
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void Populate(SettlementData data)
        {
            if (data == null)
            {
                Log.Warning("[SettlementUI] 打开时未携带 SettlementData");
                return;
            }

            if (_levelText != null)
            {
                _levelText.text = Loc.Get("ui.settlement.level", data.LevelId);
            }

            if (_rewardsText != null)
            {
                string summary = data.MetaReward?.ToString();
                _rewardsText.text = string.IsNullOrEmpty(summary)
                    ? Loc.Get("ui.settlement.rewards.empty")
                    : summary;
            }

            var loot = data.LootItems;
            bool hasLoot = loot != null && loot.Count > 0;

            if (_lootValueText != null)
            {
                _lootValueText.gameObject.SetActive(hasLoot);
                if (hasLoot)
                {
                    _lootValueText.text = Loc.Get("ui.settlement.loot.value", data.LootValueTotal);
                }
            }

            if (_lootEmptyText != null)
            {
                _lootEmptyText.gameObject.SetActive(!hasLoot);
                if (!hasLoot)
                {
                    _lootEmptyText.text = Loc.Get("ui.settlement.loot.empty");
                }
            }

            BuildLootGrid(hasLoot ? loot : null);
        }

        private void BuildLootGrid(List<ItemStack> loot)
        {
            if (_slotRoot == null || _slotTemplate == null)
            {
                return;
            }

            int count = loot?.Count ?? 0;
            while (_slots.Count < count)
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
                bool active = i < count;
                _slots[i].gameObject.SetActive(active);
                if (active)
                {
                    _slots[i].SetItem(loot[i]);
                }
            }
        }

        /// <summary>
        /// 确认返回基地：ChangeProcedure 内部会 CloseAll（含本窗）并复位暂停状态。
        /// </summary>
        private void OnConfirmClicked()
        {
            GameApp.ChangeProcedure<ProcedureSimulation>();
        }
    }
}
