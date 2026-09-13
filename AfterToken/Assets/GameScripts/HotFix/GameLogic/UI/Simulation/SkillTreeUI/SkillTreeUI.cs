using System.Collections.Generic;
using System.Text;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Skill = GameConfig.cfg.Skill;
using ESkillEffect = GameConfig.cfg.ESkillEffect;

namespace GameLogic
{
    /// <summary>
    /// 技能树界面：经营场景技能操作台 NPC（consoleType=1）打开。
    /// 左侧三分支页签（战斗/生存/经营），中间按 cfg.Row/Col 网格摆节点（运行时生成），右侧选中节点详情 + 升级。
    /// 节点着色：满级=金、可升级=亮绿、已学但暂不可升=蓝、被前置锁定=灰；有前置的节点画连线。
    /// 升级走 SkillSystem.TryUpgrade，失败原因（已本地化）红字显示 3 秒后消失（同 BuildingInfoUI._failText 模式）。
    /// Prefab：Assets/AssetRaw/UI/SkillTreeUI/SkillTreeUI.prefab（静态结构在 Prefab，节点与连线运行时生成）。
    /// </summary>
    [Window(UILayer.UI, "SkillTreeUI", true)]
    public class SkillTreeUI : UIWindow
    {
        // ---- 分支常量（与配置表 branch 字段一致） ----
        private const int BranchCombat = 1;
        private const int BranchSurvival = 2;
        private const int BranchEconomy = 3;

        // ---- 节点摆位（1920x1080 设计分辨率） ----
        private const float NodeSize = 110f;
        private const float NodeSpacing = 160f;

        // ---- 节点状态着色 ----
        private static readonly Color MaxedColor = new Color(0.95f, 0.78f, 0.2f, 1f);
        private static readonly Color AvailableColor = new Color(0.3f, 0.72f, 0.45f, 1f);
        private static readonly Color LearnedColor = new Color(0.3f, 0.45f, 0.7f, 1f);
        private static readonly Color LockedColor = new Color(0.32f, 0.32f, 0.36f, 1f);
        private static readonly Color LineUnlockedColor = new Color(0.95f, 0.78f, 0.2f, 0.8f);
        private static readonly Color LineLockedColor = new Color(0.4f, 0.4f, 0.45f, 0.8f);
        private static readonly Color TabNormalColor = new Color(0.14f, 0.16f, 0.19f, 1f);
        private static readonly Color TabSelectedColor = new Color(0.16f, 0.55f, 0.38f, 1f);

        private TextMeshProUGUI _titleText;
        private Button _closeButton;
        private Button _tabCombatButton;
        private Button _tabSurvivalButton;
        private Button _tabEconomyButton;
        private RectTransform _nodeRoot;

        // 详情面板
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _descText;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _effectText;
        private TextMeshProUGUI _costText;
        private Button _upgradeButton;
        private TextMeshProUGUI _upgradeLabel;

        // 升级失败原因（红色，3 秒后自动消失）
        private TextMeshProUGUI _failText;
        private float _failTimer;
        private const float FailTextDuration = 3f;

        private int _currentBranch = BranchCombat;
        private int _selectedSkillId;

        /// <summary>当前分支已生成的节点按钮（skillId → 节点），刷新状态/连线时按 id 索引。</summary>
        private readonly Dictionary<int, NodeWidget> _nodes = new Dictionary<int, NodeWidget>();
        /// <summary>前置连线（skillId → 线 Image），skillId 为有前置的那个节点。</summary>
        private readonly List<KeyValuePair<int, Image>> _lines = new List<KeyValuePair<int, Image>>();

        private sealed class NodeWidget
        {
            public Button Button;
            public Image Image;
            public Outline Outline;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI LevelText;
            public Vector2 Position;
        }

        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_text_Title");
            _closeButton = FindChildComponent<Button>("m_rect_Root/m_rect_Panel/m_btn_Close");
            _tabCombatButton = FindChildComponent<Button>("m_rect_Root/m_rect_Panel/m_rect_TabRoot/m_btn_TabCombat");
            _tabSurvivalButton = FindChildComponent<Button>("m_rect_Root/m_rect_Panel/m_rect_TabRoot/m_btn_TabSurvival");
            _tabEconomyButton = FindChildComponent<Button>("m_rect_Root/m_rect_Panel/m_rect_TabRoot/m_btn_TabEconomy");
            _nodeRoot = FindChildComponent<RectTransform>("m_rect_Root/m_rect_Panel/m_rect_NodeRoot");
            _nameText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_text_Name");
            _descText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_text_Desc");
            _levelText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_text_Level");
            _effectText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_text_Effect");
            _costText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_text_Cost");
            _upgradeButton = FindChildComponent<Button>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_btn_Upgrade");
            _upgradeLabel = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_btn_Upgrade/m_text_Label");
            _failText = FindChildComponent<TextMeshProUGUI>("m_rect_Root/m_rect_Panel/m_rect_DetailPanel/m_text_Fail");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();

            if (GraphicRaycaster != null)
            {
                GraphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            // 框架根 Canvas 的 CanvasScaler 已统一为 1920x1080 横屏参考分辨率，本界面按 1920x1080 像素设计，无需额外缩放

            Loc.Bind(_titleText, "ui.skill.title");
            Loc.Bind(_tabCombatButton?.transform.Find("m_text_Label")?.GetComponent<TextMeshProUGUI>(), "ui.skill.branch.combat");
            Loc.Bind(_tabSurvivalButton?.transform.Find("m_text_Label")?.GetComponent<TextMeshProUGUI>(), "ui.skill.branch.survival");
            Loc.Bind(_tabEconomyButton?.transform.Find("m_text_Label")?.GetComponent<TextMeshProUGUI>(), "ui.skill.branch.economy");
            if (_upgradeLabel != null)
            {
                Loc.Bind(_upgradeLabel, "ui.skill.upgrade");
            }

            _closeButton?.onClick.AddListener(() => Close());
            _tabCombatButton?.onClick.AddListener(() => ShowBranch(BranchCombat));
            _tabSurvivalButton?.onClick.AddListener(() => ShowBranch(BranchSurvival));
            _tabEconomyButton?.onClick.AddListener(() => ShowBranch(BranchEconomy));
            _upgradeButton?.onClick.AddListener(TryUpgradeSelected);

            ShowBranch(BranchCombat);
        }

        protected override void OnDestroy()
        {
            ClearNodes();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            // 失败原因到时自动清除
            if (_failTimer > 0f)
            {
                _failTimer -= Time.deltaTime;
                if (_failTimer <= 0f && _failText != null)
                {
                    _failText.text = "";
                }
            }
        }

        // ---- 页签 ----

        /// <summary>切换分支：重建节点区，默认选中该分支第一个节点。</summary>
        private void ShowBranch(int branch)
        {
            _currentBranch = branch;
            UpdateTabHighlight();
            RebuildNodes();

            var skills = SkillConfigMgr.Instance.GetByBranch(branch);
            _selectedSkillId = skills.Count > 0 ? skills[0].Id : 0;
            RefreshNodeStates();
            RefreshDetail();
        }

        private void UpdateTabHighlight()
        {
            SetTabColor(_tabCombatButton, _currentBranch == BranchCombat);
            SetTabColor(_tabSurvivalButton, _currentBranch == BranchSurvival);
            SetTabColor(_tabEconomyButton, _currentBranch == BranchEconomy);
        }

        private static void SetTabColor(Button button, bool selected)
        {
            var image = button != null ? button.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = selected ? TabSelectedColor : TabNormalColor;
            }
        }

        // ---- 节点区 ----

        /// <summary>Row/Col 网格映射到节点区局部坐标（整树在节点区居中）。</summary>
        private static Vector2 GetNodePosition(Skill cfg, int maxRow, int maxCol)
        {
            return new Vector2((cfg.Col - maxCol * 0.5f) * NodeSpacing, (maxRow * 0.5f - cfg.Row) * NodeSpacing);
        }

        private void RebuildNodes()
        {
            ClearNodes();
            if (_nodeRoot == null) return;

            var skills = SkillConfigMgr.Instance.GetByBranch(_currentBranch);
            if (skills.Count == 0) return;

            int maxRow = 0;
            int maxCol = 0;
            foreach (var cfg in skills)
            {
                if (cfg.Row > maxRow) maxRow = cfg.Row;
                if (cfg.Col > maxCol) maxCol = cfg.Col;
            }

            foreach (var cfg in skills)
            {
                CreateNode(cfg, maxRow, maxCol);
            }

            // 连线在节点之后生成，渲染在节点下层
            foreach (var cfg in skills)
            {
                if (cfg.PrereqSkillId > 0 && _nodes.TryGetValue(cfg.PrereqSkillId, out var from)
                    && _nodes.TryGetValue(cfg.Id, out var to))
                {
                    CreateLine(cfg.Id, from.Position, to.Position);
                }
            }
        }

        private void CreateNode(Skill cfg, int maxRow, int maxCol)
        {
            var go = new GameObject($"Node_{cfg.Id}");
            go.transform.SetParent(_nodeRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(NodeSize, NodeSize);
            rect.anchoredPosition = GetNodePosition(cfg, maxRow, maxCol);

            var image = go.AddComponent<Image>();
            image.color = LockedColor;

            // 选中态描边（默认关）
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = false;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            int skillId = cfg.Id;
            button.onClick.AddListener(() => SelectNode(skillId));

            var nameText = CreateNodeText(go.transform, "Name", new Vector2(0f, 18f), new Vector2(NodeSize + 40f, 40f), 13);
            var levelText = CreateNodeText(go.transform, "Level", new Vector2(0f, -34f), new Vector2(NodeSize, 24f), 12);

            _nodes[cfg.Id] = new NodeWidget
            {
                Button = button,
                Image = image,
                Outline = outline,
                NameText = nameText,
                LevelText = levelText,
                Position = rect.anchoredPosition,
            };
        }

        private static TextMeshProUGUI CreateNodeText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>前置连线：细 Image 旋转拉伸，两端缩进一个节点宽度避免压住按钮文字。</summary>
        private void CreateLine(int skillId, Vector2 from, Vector2 to)
        {
            var go = new GameObject("Line_" + skillId);
            go.transform.SetParent(_nodeRoot, false);
            go.transform.SetAsFirstSibling();
            var rect = go.AddComponent<RectTransform>();

            Vector2 dir = to - from;
            float length = dir.magnitude;
            Vector2 mid = (from + to) * 0.5f;
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(Mathf.Max(0f, length - NodeSize), 5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var image = go.AddComponent<Image>();
            image.color = LineLockedColor;
            image.raycastTarget = false;
            _lines.Add(new KeyValuePair<int, Image>(skillId, image));
        }

        /// <summary>刷新全部节点的着色/文本/选中描边与连线颜色（升级成功或打开时调用）。</summary>
        private void RefreshNodeStates()
        {
            foreach (var kv in _nodes)
            {
                var cfg = SkillConfigMgr.Instance.Get(kv.Key);
                var widget = kv.Value;
                if (cfg == null) continue;

                int level = SkillSystem.GetLevel(cfg.Id);
                widget.NameText.text = Loc.Get(cfg.NameKey);
                widget.LevelText.text = Loc.Get("ui.skill.level", level, cfg.MaxLevel);
                widget.Image.color = GetNodeColor(cfg, level);
                widget.Outline.enabled = cfg.Id == _selectedSkillId;
            }

            // 连线：前置已满级 = 金色，否则灰
            foreach (var pair in _lines)
            {
                var cfg = SkillConfigMgr.Instance.Get(pair.Key);
                if (cfg == null || pair.Value == null) continue;
                var prereq = SkillConfigMgr.Instance.Get(cfg.PrereqSkillId);
                bool unlocked = prereq != null && SkillSystem.GetLevel(prereq.Id) >= prereq.MaxLevel;
                pair.Value.color = unlocked ? LineUnlockedColor : LineLockedColor;
            }
        }

        private static Color GetNodeColor(Skill cfg, int level)
        {
            if (level >= cfg.MaxLevel)
            {
                return MaxedColor;
            }
            // 前置未满足 = 锁定灰
            if (cfg.PrereqSkillId > 0)
            {
                var prereq = SkillConfigMgr.Instance.Get(cfg.PrereqSkillId);
                if (prereq != null && SkillSystem.GetLevel(prereq.Id) < prereq.MaxLevel)
                {
                    return LockedColor;
                }
            }
            // 材料/金币够 = 亮绿可升级；已学过但暂不够 = 蓝
            return SkillSystem.CanUpgrade(cfg.Id, out _) ? AvailableColor
                : level > 0 ? LearnedColor
                : LockedColor;
        }

        private void SelectNode(int skillId)
        {
            if (_selectedSkillId == skillId) return;
            _selectedSkillId = skillId;
            RefreshNodeStates();
            RefreshDetail();
        }

        // ---- 详情面板 ----

        private void RefreshDetail()
        {
            var cfg = SkillConfigMgr.Instance.Get(_selectedSkillId);
            if (cfg == null)
            {
                if (_nameText != null) _nameText.text = "";
                if (_descText != null) _descText.text = "";
                if (_levelText != null) _levelText.text = "";
                if (_effectText != null) _effectText.text = "";
                if (_costText != null) _costText.text = "";
                if (_upgradeButton != null) _upgradeButton.interactable = false;
                return;
            }

            int level = SkillSystem.GetLevel(cfg.Id);
            if (_nameText != null) _nameText.text = Loc.Get(cfg.NameKey);
            if (_descText != null) _descText.text = Loc.Get(cfg.DescKey);
            if (_levelText != null) _levelText.text = Loc.Get("ui.skill.level", level, cfg.MaxLevel);

            if (_effectText != null)
            {
                var sb = new StringBuilder();
                sb.Append(Loc.Get("ui.skill.effect_per_level", FormatEffectValue(cfg.EffectType, cfg.EffectValue)));
                sb.Append('\n');
                sb.Append(Loc.Get("ui.skill.effect_current", level > 0
                    ? FormatEffectValue(cfg.EffectType, level * cfg.EffectValue)
                    : "0"));
                _effectText.text = sb.ToString();
            }

            if (_costText != null)
            {
                _costText.text = FormatCost(cfg);
            }

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = level < cfg.MaxLevel;
            }
        }

        /// <summary>消耗明细：标题 + 逐项材料（名字 x数量）+ 金币。</summary>
        private static string FormatCost(Skill cfg)
        {
            var sb = new StringBuilder();
            sb.Append(Loc.Get("ui.skill.cost_title"));
            bool any = false;
            if (cfg.CostItems != null)
            {
                foreach (var item in cfg.CostItems)
                {
                    sb.Append('\n').Append(ItemConfigMgr.Instance.GetName(item.Id)).Append(" x").Append(item.Num);
                    any = true;
                }
            }
            if (cfg.CostGold > 0)
            {
                sb.Append('\n').Append(Loc.Get("ui.saveslot.gold")).Append(" x").Append(cfg.CostGold);
                any = true;
            }
            if (!any)
            {
                sb.Append('\n').Append(Loc.Get("ui.sim.free"));
            }
            return sb.ToString();
        }

        /// <summary>效果值格式化：百分比类显示 ±X%，加算类显示 +N；缩减类（换弹/闪避耗耐）为负号。</summary>
        private static string FormatEffectValue(ESkillEffect type, float value)
        {
            switch (type)
            {
                case ESkillEffect.DamagePct:
                case ESkillEffect.MoveSpeedPct:
                case ESkillEffect.GoldGainPct:
                case ESkillEffect.ExpGainPct:
                case ESkillEffect.ProductionSpeedPct:
                    return $"+{(value * 100f).ToString("F0")}%";
                case ESkillEffect.ReloadTimePct:
                case ESkillEffect.DodgeCostPct:
                    return $"-{(value * 100f).ToString("F0")}%";
                default:
                    return $"+{value.ToString("F0")}";
            }
        }

        // ---- 升级 ----

        private void TryUpgradeSelected()
        {
            if (_selectedSkillId <= 0) return;
            if (SkillSystem.TryUpgrade(_selectedSkillId, out var reason))
            {
                // 成功：刷新节点状态与详情
                RefreshNodeStates();
                RefreshDetail();
            }
            else
            {
                ShowFailReason(reason);
            }
        }

        private void ShowFailReason(string reason)
        {
            if (_failText == null) return;
            _failText.text = reason;
            _failTimer = FailTextDuration;
        }

        private void ClearNodes()
        {
            foreach (var kv in _nodes)
            {
                if (kv.Value.Button != null) Object.Destroy(kv.Value.Button.gameObject);
            }
            _nodes.Clear();
            foreach (var pair in _lines)
            {
                if (pair.Value != null) Object.Destroy(pair.Value.gameObject);
            }
            _lines.Clear();
        }
    }
}
