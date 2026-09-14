using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// AI 队友信息面板：从聊天窗 Info 按钮或靠近队友按交互键打开。
    /// 显示队友名字、好感档位（名称 + 人格描述）、好感进度与记忆列表；
    /// "赠送礼物"按钮切换到赠礼子面板，列出仓库中可赠送的物品，点击即送一件。
    /// 赠礼链路：扣物品 → 加好感（飘字事件自动触发）→ 写礼物记忆（随机判定不外露）
    /// → 好奇飘字（无论记忆写入成败都显示）→ gift_receive bark → 刷新面板。
    /// Prefab：Assets/AssetRaw/UI/CompanionInfoUI/CompanionInfoUI.prefab
    /// （静态结构在 Prefab，记忆/礼物列表项运行时生成）。
    /// </summary>
    [Window(UILayer.UI, "CompanionInfoUI", true)]
    public class CompanionInfoUI : UIWindow
    {
        private const int CompanionId = CompanionAffinitySystem.DefaultCompanionId;

        private RectTransform _infoRoot;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _tierText;
        private TextMeshProUGUI _tierDescText;
        private TextMeshProUGUI _affinityText;
        private TextMeshProUGUI _memoryLabelText;
        private RectTransform _memoryListRoot;
        private Button _giftButton;
        private Button _closeButton;

        private RectTransform _giftPanel;
        private RectTransform _giftListRoot;
        private TextMeshProUGUI _giftEmptyText;

        private readonly List<GameObject> _memoryItems = new List<GameObject>();
        private readonly List<GameObject> _giftItems = new List<GameObject>();

        // 记忆列表低频刷新：内容指纹不变时不重建（聊天也会写记忆）
        private float _refreshTimer;
        private const float RefreshInterval = 0.5f;
        private string _memoryFingerprint = "";

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _infoRoot = FindChildComponent<RectTransform>("m_rect_InfoRoot");
            _titleText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Title");
            _nameText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Name");
            _tierText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Tier");
            _tierDescText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_TierDesc");
            _affinityText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Affinity");
            _memoryLabelText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_MemoryLabel");
            _memoryListRoot = FindChildComponent<RectTransform>("m_rect_InfoRoot/m_rect_Panel/m_rect_MemoryList");
            _giftButton = FindChildComponent<Button>("m_rect_InfoRoot/m_rect_Panel/m_btn_Gift");
            _closeButton = FindChildComponent<Button>("m_rect_InfoRoot/m_rect_Panel/m_btn_Close");
            _giftPanel = FindChildComponent<RectTransform>("m_rect_InfoRoot/m_rect_Panel/m_rect_GiftPanel");
            _giftListRoot = FindChildComponent<RectTransform>("m_rect_InfoRoot/m_rect_Panel/m_rect_GiftPanel/m_rect_GiftList");
            _giftEmptyText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_rect_GiftPanel/m_text_GiftEmpty");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();

            if (GraphicRaycaster != null)
            {
                GraphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            // 框架根 Canvas 的 CanvasScaler 已统一为 1920x1080 横屏参考分辨率，本界面按 1920x1080 像素设计

            Loc.Bind(_titleText, "ui.companion.info.title");
            Loc.Bind(_memoryLabelText, "ui.companion.info.memories");
            Loc.Bind(_giftEmptyText, "ui.companion.gift.empty");
            var giftBtnLabel = _giftButton != null ? _giftButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            Loc.Bind(giftBtnLabel, "ui.companion.info.gift");
            var closeBtnLabel = _closeButton != null ? _closeButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            Loc.Bind(closeBtnLabel, "ui.companion.info.close");
            var giftTitle = _giftPanel != null
                ? _giftPanel.Find("m_text_GiftTitle")?.GetComponent<TextMeshProUGUI>() : null;
            Loc.Bind(giftTitle, "ui.companion.gift.title");

            _giftButton?.onClick.AddListener(ToggleGiftPanel);
            _closeButton?.onClick.AddListener(() => Close());

            if (_giftPanel != null)
            {
                _giftPanel.gameObject.SetActive(false);
            }

            RefreshAll();
        }

        protected override void OnDestroy()
        {
            ClearItems(_memoryItems);
            ClearItems(_giftItems);
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            // 低频刷新好感/档位/记忆（聊天等其它来源也可能变动）
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                RefreshAffinity();
                RebuildMemoryListIfChanged();
            }
        }

        /// <summary>全量刷新（打开/赠礼后）：好感区 + 记忆列表 + 赠礼列表（若子面板开着）。</summary>
        private void RefreshAll()
        {
            RefreshAffinity();
            RebuildMemoryList(true);
            if (_giftPanel != null && _giftPanel.gameObject.activeSelf)
            {
                RebuildGiftList();
            }
        }

        /// <summary>刷新名字、档位名 + 人格描述、好感进度。</summary>
        private void RefreshAffinity()
        {
            if (_nameText != null)
            {
                _nameText.text = GetCompanionName();
            }

            int exp = CompanionAffinitySystem.GetExp(CompanionId);
            int tier = CompanionAffinitySystem.GetTier(CompanionId);
            var tierCfg = CompanionAffinityConfigMgr.Instance.GetTier(tier);

            if (_tierText != null)
            {
                _tierText.text = tierCfg != null ? Loc.Get(tierCfg.NameKey) : $"T{tier}";
            }
            if (_tierDescText != null)
            {
                _tierDescText.text = tierCfg != null ? Loc.Get(tierCfg.DescKey) : "";
            }

            if (_affinityText != null)
            {
                int maxTier = CompanionAffinityConfigMgr.Instance.MaxTier;
                string progress;
                if (tier >= maxTier)
                {
                    progress = Loc.Get("ui.companion.info.max_tier");
                }
                else
                {
                    var nextCfg = CompanionAffinityConfigMgr.Instance.GetTier(tier + 1);
                    string nextName = nextCfg != null ? Loc.Get(nextCfg.NameKey) : $"T{tier + 1}";
                    int threshold = nextCfg != null ? nextCfg.Threshold : 0;
                    progress = Loc.Get("ui.companion.info.next_tier", nextName, exp, threshold);
                }
                _affinityText.text = $"{Loc.Get("ui.companion.info.affinity")}: {exp}\n{progress}";
            }
        }

        private static string GetCompanionName()
        {
            var cfg = CompanionConfigMgr.Instance.Get(CompanionId);
            return cfg != null ? Loc.Get(cfg.NameKey) : "Companion";
        }

        /// <summary>记忆列表重建（空列表显示"还没有记忆"占位行）。</summary>
        private void RebuildMemoryList(bool force)
        {
            if (_memoryListRoot == null) return;

            var memories = CompanionMemorySystem.GetAllMemories(CompanionId);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < memories.Count; i++)
            {
                sb.Append(memories[i].type).Append('|').Append(memories[i].content).Append(';');
            }
            string fingerprint = sb.ToString();
            if (!force && fingerprint == _memoryFingerprint)
            {
                return;
            }
            _memoryFingerprint = fingerprint;

            ClearItems(_memoryItems);

            if (memories.Count == 0)
            {
                _memoryItems.Add(CreateTextRow(_memoryListRoot, Loc.Get("ui.companion.info.memories.empty"),
                    new Color(0.55f, 0.58f, 0.64f)));
                return;
            }

            // 新记忆显示在最上面
            for (int i = memories.Count - 1; i >= 0; i--)
            {
                string line = CompanionMemorySystem.FormatForDisplay(memories[i]);
                _memoryItems.Add(CreateTextRow(_memoryListRoot, line, Color.white));
            }
        }

        private void RebuildMemoryListIfChanged()
        {
            RebuildMemoryList(false);
        }

        private static GameObject CreateTextRow(RectTransform parent, string text, Color color)
        {
            var go = new GameObject("MemoryRow");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380f, 26f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = color;
            tmp.raycastTarget = false;
            return go;
        }

        #region 赠礼子面板

        private void ToggleGiftPanel()
        {
            if (_giftPanel == null) return;
            bool show = !_giftPanel.gameObject.activeSelf;
            _giftPanel.gameObject.SetActive(show);
            if (show)
            {
                RebuildGiftList();
            }
        }

        /// <summary>
        /// 列出仓库中可赠送的物品（affinityValue > 0 且 companionAccept == 1 且持有数 > 0）。
        /// 点击即送一件，不做二次确认。
        /// </summary>
        private void RebuildGiftList()
        {
            ClearItems(_giftItems);
            if (_giftListRoot == null) return;

            // 按物品 ID 聚合仓库堆叠
            var counts = new Dictionary<int, int>();
            var order = new List<int>();
            var stacks = Warehouse.Items;
            for (int i = 0; i < stacks.Count; i++)
            {
                int id = stacks[i].ItemId;
                if (!counts.ContainsKey(id))
                {
                    counts[id] = 0;
                    order.Add(id);
                }
                counts[id] += stacks[i].Count;
            }

            int shown = 0;
            for (int i = 0; i < order.Count; i++)
            {
                int itemId = order[i];
                int count = counts[itemId];
                if (count <= 0) continue;

                var cfg = ItemConfigMgr.Instance.Get(itemId);
                if (cfg == null || cfg.AffinityValue <= 0 || cfg.CompanionAccept == 0) continue;

                string label = $"{ItemConfigMgr.Instance.GetName(itemId)} x{count}   {Loc.Get("ui.companion.gift.affinity", cfg.AffinityValue)}";
                int captured = itemId;
                _giftItems.Add(CreateGiftRow(label, () => GiveGift(captured)));
                shown++;
            }

            if (_giftEmptyText != null)
            {
                _giftEmptyText.gameObject.SetActive(shown == 0);
            }
        }

        private GameObject CreateGiftRow(string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("GiftRow");
            go.transform.SetParent(_giftListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 44f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 15;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
            text.raycastTarget = false;
            return go;
        }

        /// <summary>
        /// 赠送一件礼物：扣物品 → 加好感（飘字事件自动触发）→ 写礼物记忆 →
        /// 好奇飘字（无论记忆随机判定成败都显示，不暴露判定）→ gift_receive bark → 刷新。
        /// </summary>
        private void GiveGift(int itemId)
        {
            if (!InventorySystem.TryConsumeItem(itemId, 1))
            {
                return;
            }

            CompanionAffinitySystem.AddGift(itemId, CompanionId);

            CompanionMemorySystem.TryRecord(CompanionId,
                CompanionMemoryRuleConfigMgr.TypeGift,
                CompanionMemorySystem.MakeGiftContent(itemId, 1));

            var companion = CompanionSystem.Instance != null ? CompanionSystem.Instance.Companion : null;
            if (companion != null)
            {
                WorldFloatText.Show(Loc.Get("companion.memory.hint.gift", GetCompanionName()),
                    companion.transform.position, new Color(1f, 0.65f, 0.85f));
            }

            CompanionSystem.Instance?.Brain?.SayLocal("gift_receive", bypassCooldown: true);

            RefreshAll();
        }

        #endregion

        private static void ClearItems(List<GameObject> items)
        {
            foreach (var go in items)
            {
                if (go != null) Object.Destroy(go);
            }
            items.Clear();
        }
    }
}
