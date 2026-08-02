using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 模拟经营主界面。
    /// 渲染方式：挂在框架 UIRoot（Screen Space - Overlay）下，不占场景、Scene 视图不可见、
    /// 不随场景相机倾斜；UI 特效后续用序列帧或 UICamera 特效层（见 docs/Proposal/ui/ui-render-architecture.md）；
    /// 建筑头顶牌子为 World Space（见 BuildingEntity.CreateLabel）。
    /// 布局：顶部常驻 HUD 条（金币/等级/时间/时间控制）+ 管理面板（默认隐藏，Tab 切换）。
    /// 本期使用代码动态创建 UI，后续替换为正式 Prefab。
    /// </summary>
    [Window(UILayer.UI, "TestUI", true)]
    public class SimulationMainUI : UIWindow
    {
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _levelText;
        private GameObject _uiRootGo;
        private RectTransform _buildRoot;
        private RectTransform _hudRoot;
        private RectTransform _panelRoot;
        private RectTransform _buildingListRoot;
        private RectTransform _orderListRoot;
        private Button _pauseButton;
        private Button _normalButton;
        private Button _fastButton;
        private Button _backButton;
        private bool _panelVisible;

        private readonly List<GameObject> _buildingItems = new List<GameObject>();
        private readonly List<GameObject> _orderItems = new List<GameObject>();

        private float _refreshTimer;
        private const float REFRESH_INTERVAL = 0.5f;
        private SimulationSystem _cachedSimulationSystem;

        protected override void ScriptGenerator()
        {
            // 动态创建 UI，不依赖 Prefab 节点
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();

            // 禁用 GraphicRaycaster 的 Block Raycasts，避免拦截场景中的鼠标输入
            if (GraphicRaycaster != null)
            {
                GraphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            // UI 容器挂在框架 UIRoot（Overlay）下：纯 RectTransform 不带 Canvas，渲染随窗口自身画布，
            // 不占场景、不随场景相机倾斜（修复旧版自建 SSC Canvas 挂 Main Camera 导致的面板倾斜/陷地）
            _uiRootGo = new GameObject("SimulationUIRoot", typeof(RectTransform));
            _uiRootGo.transform.SetParent(rectTransform, false);
            _buildRoot = (RectTransform)_uiRootGo.transform;
            // 容器按设计分辨率 1920x1080 固定尺寸居中（反向缩放后 1 单位 = 1 屏幕像素，
            // 顶锚 HUD 与中心面板布局坐标都按像素直写，无需换算）
            _buildRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _buildRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _buildRoot.pivot = new Vector2(0.5f, 0.5f);
            _buildRoot.anchoredPosition = Vector2.zero;
            _buildRoot.sizeDelta = new Vector2(1920f, 1080f);

            // 框架根 Canvas 带 CanvasScaler（参考 750x1334 按宽适配，1920x1080 下放大 2.56 倍），
            // 本界面布局按 1920x1080 像素设计，反向缩放保持设计尺寸（正式 Prefab 化后移除）
            var rootCanvas = rectTransform.GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas != null && rootCanvas.scaleFactor > 0f)
            {
                _buildRoot.localScale = Vector3.one / rootCanvas.scaleFactor;
            }

            TEngine.Log.Info("[SimulationMainUI] OnCreate BuildUI begin");
            BuildUI();
            TEngine.Log.Info("[SimulationMainUI] OnCreate RegisterSimulationEvents begin");
            RegisterSimulationEvents();
            TEngine.Log.Info("[SimulationMainUI] OnCreate done");
        }

        protected override void OnDestroy()
        {
            ClearListItems();
            // 注意：不要在子类调用 RemoveAllUIEvent()，UIWindow.InternalDestroy 已统一释放，重复调用会触发内存池二次释放异常
            if (_uiRootGo != null)
            {
                Object.Destroy(_uiRootGo);
                _uiRootGo = null;
            }
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void BuildUI()
        {
            if (_buildRoot == null) return;

            BuildHud();
            BuildPanel();
            SetPanelVisible(false);

            RefreshBuildingList();
            RefreshOrderList();
        }

        /// <summary>
        /// 顶部常驻 HUD 条：金币 / 等级 / 时间 / 时间控制 / 面板提示。
        /// </summary>
        private void BuildHud()
        {
            var go = new GameObject("HudBar");
            go.transform.SetParent(_buildRoot, false);
            _hudRoot = go.AddComponent<RectTransform>();
            _hudRoot.anchorMin = new Vector2(0f, 1f);
            _hudRoot.anchorMax = new Vector2(1f, 1f);
            _hudRoot.pivot = new Vector2(0.5f, 0.5f);
            _hudRoot.sizeDelta = new Vector2(0f, 56f);
            _hudRoot.anchoredPosition = new Vector2(0f, -28f);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);
            // 背景不拦截点击（按钮自身会拦截）
            bg.raycastTarget = false;

            _goldText = CreateText("Gold: 0", _hudRoot, new Vector2(-550f, 0f), 22, TextAlignmentOptions.Left);
            _levelText = CreateText("Lv: 1", _hudRoot, new Vector2(-300f, 0f), 22, TextAlignmentOptions.Left);
            _timeText = CreateText("Time: 0s", _hudRoot, new Vector2(-50f, 0f), 22, TextAlignmentOptions.Left);

            _pauseButton = CreateButton("Pause", _hudRoot, new Vector2(220f, 0f), () => SetSpeed(ESimSpeed.Pause));
            _normalButton = CreateButton("1x", _hudRoot, new Vector2(370f, 0f), () => SetSpeed(ESimSpeed.Normal));
            _fastButton = CreateButton("2x", _hudRoot, new Vector2(520f, 0f), () => SetSpeed(ESimSpeed.Fast));

            CreateButton("Panel (Tab)", _hudRoot, new Vector2(740f, 0f), () => SetPanelVisible(!_panelVisible));
        }

        /// <summary>
        /// 管理面板：建筑列表 / 订单列表 / 建造与升级 / 返回主菜单。默认隐藏，Tab 切换。
        /// </summary>
        private void BuildPanel()
        {
            var go = new GameObject("ManagementPanel");
            go.transform.SetParent(_buildRoot, false);
            _panelRoot = go.AddComponent<RectTransform>();
            _panelRoot.anchoredPosition = Vector2.zero;
            _panelRoot.sizeDelta = new Vector2(1100f, 750f);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            CreateText("Management", _panelRoot, new Vector2(0f, 335f), 32, TextAlignmentOptions.Center);

            // 文本框宽 400，左对齐时位置需偏移半宽才能对齐列表左边缘
            CreateText("Buildings", _panelRoot, new Vector2(-320f, 290f), 24, TextAlignmentOptions.Left);
            _buildingListRoot = CreateScrollList(_panelRoot, new Vector2(-280f, -15f), new Vector2(480f, 550f));

            CreateText("Orders", _panelRoot, new Vector2(240f, 290f), 24, TextAlignmentOptions.Left);
            _orderListRoot = CreateScrollList(_panelRoot, new Vector2(280f, -15f), new Vector2(480f, 550f));

            CreateButton("Build", _panelRoot, new Vector2(-380f, -330f), () => OpenBuildingSelection());
            CreateButton("Upgrade", _panelRoot, new Vector2(-220f, -330f), () => TryUpgradeSelected());
            _backButton = CreateButton("Back to Menu", _panelRoot, new Vector2(380f, -330f), () => GameApp.ChangeProcedure<ProcedureMainMenu>());
        }

        private void SetPanelVisible(bool visible)
        {
            _panelVisible = visible;
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(visible);
            }
        }

        private TextMeshProUGUI CreateText(string content, RectTransform parent, Vector2 position, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 50);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string label, RectTransform parent, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(140, 40);

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
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            return btn;
        }

        private RectTransform CreateScrollList(RectTransform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject("ScrollList");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.16f, 0.24f, 0.95f);

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 创建 viewport（RectMask2D 裁剪，无需模板缓冲；Mask 在此渲染模式下会导致内容全部不可见）
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(go.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();

            // 创建 content（实际放置列表项的容器）
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(10, 0);
            contentRect.offsetMax = new Vector2(-10, 0);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            return contentRect;
        }

        private void RegisterSimulationEvents()
        {
            AddUIEvent<long>(ICurrencyEvent_Event.OnGoldChanged, OnGoldChanged);
            AddUIEvent<int, int>(IPlayerProfileEvent_Event.OnExpChanged, OnExpChanged);
            AddUIEvent<int, int, int>(ISimulationEvent_Event.OnBuildingCompleted, OnBuildingChanged);
            AddUIEvent<int, int, int>(ISimulationEvent_Event.OnBuildingUpgraded, OnBuildingChanged);
            AddUIEvent<int, int>(ISimulationEvent_Event.OnOrderGenerated, OnOrderChanged);
            AddUIEvent<int, int>(ISimulationEvent_Event.OnOrderCompleted, OnOrderChanged);
            AddUIEvent<int, int, int, int>(ISimulationEvent_Event.OnProductionFinished, OnProductionFinished);
        }

        private void SetSpeed(ESimSpeed speed)
        {
            var simSystem = GetSimulationSystem();
            simSystem?.SimTime?.SetSpeed(speed);
        }

        private void OpenBuildingSelection()
        {
            GameModule.UI.ShowUIAsync<BuildingSelectionUI>();
        }

        private void TryUpgradeSelected()
        {
            // TODO: 实现建筑选中逻辑，当前默认升级第一个建筑
            var simSystem = GetSimulationSystem();
            if (simSystem?.Building != null && simSystem.Building.Buildings.Count > 0)
            {
                simSystem.Building.TryUpgrade(simSystem.Building.Buildings[0].InstanceId);
                RefreshBuildingList();
            }
        }

        private SimulationSystem GetSimulationSystem()
        {
            if (_cachedSimulationSystem == null)
            {
                var root = SingletonSystem.GetGameObject("SimulationRoot");
                _cachedSimulationSystem = root?.GetComponent<SimulationSystem>();
            }
            return _cachedSimulationSystem;
        }

        private void OnGoldChanged(long gold)
        {
            if (_goldText != null)
            {
                _goldText.text = $"Gold: {gold}";
            }
        }

        private void OnExpChanged(int exp, int maxExp)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Lv: {PlayerProfileSystem.Level} ({exp}/{maxExp})";
            }
        }

        private void OnBuildingChanged(int buildingId, int instanceId, int level)
        {
            RefreshBuildingList();
        }

        private void OnOrderChanged(int orderId, int instanceId)
        {
            RefreshOrderList();
        }

        private void OnProductionFinished(int productionId, int instanceId, int itemId, int count)
        {
            RefreshBuildingList();
            RefreshOrderList();
        }

        protected override void OnUpdate()
        {
            if (Time.frameCount % 15 == 0) TEngine.Log.Info($"[hb] SimMainUI f={Time.frameCount}");

            // Tab 切换管理面板
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SetPanelVisible(!_panelVisible);
            }

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= REFRESH_INTERVAL)
            {
                _refreshTimer = 0f;
                RefreshTimeDisplay();
            }
        }

        private void RefreshTimeDisplay()
        {
            var simSystem = GetSimulationSystem();
            if (simSystem?.SimTime != null && _timeText != null)
            {
                _timeText.text = $"Time: {simSystem.SimTime.CurrentTime:F0}s";
            }
        }

        private void RefreshBuildingList()
        {
            if (_buildingListRoot == null) return;

            ClearListItems(_buildingItems);

            var simSystem = GetSimulationSystem();
            if (simSystem?.Building == null) return;

            // 显示可建造列表
            var allBuildings = BuildingConfigMgr.Instance.GetAll();
            foreach (var cfg in allBuildings)
            {
                CreateBuildingItem(cfg);
            }

            // 显示已建造列表
            foreach (var building in simSystem.Building.Buildings)
            {
                CreateBuildingInstanceItem(building);
            }
        }

        private void CreateBuildingItem(GameConfig.cfg.Building cfg)
        {
            var simSystem = GetSimulationSystem();

            var go = new GameObject($"Building_{cfg.Id}");
            go.transform.SetParent(_buildingListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 80);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.22f, 0.3f, 0.95f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(() => EnterPlacement(cfg.Id));

            // 可购买栏位时右侧放解锁按钮，文本让出宽度
            long slotPrice = simSystem?.Building != null ? simSystem.Building.GetSlotPrice(cfg.Id) : 0;
            bool canUnlock = slotPrice > 0;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(canUnlock ? -110 : -10, 0);

            int count = simSystem?.Building != null ? simSystem.Building.CountByConfig(cfg.Id) : 0;
            int maxCount = simSystem?.Building != null ? simSystem.Building.GetMaxCount(cfg.Id) : cfg.MaxCount;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = $"{cfg.Name} (Lv{cfg.MaxLevel})  [{count}/{maxCount}]\nCost: {cfg.BuildCostGold}G";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            if (canUnlock)
            {
                CreateUnlockSlotButton(go.transform, cfg.Id, slotPrice);
            }

            _buildingItems.Add(go);
        }

        /// <summary>
        /// 建筑列表项右侧的解锁栏位按钮：花费金币永久提升该类型数量上限 +1（价格线性递增）。
        /// </summary>
        private void CreateUnlockSlotButton(Transform parent, int configId, long price)
        {
            var go = new GameObject("UnlockSlot");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-10f, 0f);
            rect.sizeDelta = new Vector2(90f, 40f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.35f, 0.3f, 0.15f, 0.95f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(() =>
            {
                var simSystem = GetSimulationSystem();
                string reason = "系统未就绪";
                if (simSystem?.Building != null && simSystem.Building.TryPurchaseSlot(configId, out reason))
                {
                    RefreshBuildingList();
                }
                else
                {
                    Log.Warning($"[SimulationMainUI] 解锁栏位失败：{reason}");
                }
            });

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = $"Unlock\n{price}G";
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void CreateBuildingInstanceItem(BuildingInstance building)
        {
            var cfg = BuildingConfigMgr.Instance.Get(building.ConfigId);
            if (cfg == null) return;

            var go = new GameObject($"BuildingInst_{building.InstanceId}");
            go.transform.SetParent(_buildingListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 80);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.32f, 0.25f, 0.95f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            string stateText = building.State switch
            {
                BuildingState.Building => $"Building... {building.Progress * 100:F0}%",
                BuildingState.Upgrading => $"Upgrading... {building.Progress * 100:F0}%",
                _ => "Idle",
            };
            text.text = $"{cfg.Name} Lv{building.Level}\n{stateText}";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            _buildingItems.Add(go);
        }

        /// <summary>
        /// 点击可建造建筑：关闭管理面板并进入摆放模式，由玩家自己选位置放置。
        /// </summary>
        private void EnterPlacement(int configId)
        {
            var simRoot = SingletonSystem.GetGameObject("SimulationRoot");
            var placement = simRoot != null ? simRoot.GetComponent<BuildingPlacementSystem>() : null;
            if (placement == null)
            {
                Log.Error("[SimulationMainUI] BuildingPlacementSystem 未找到");
                return;
            }

            SetPanelVisible(false);
            placement.StartPlacement(configId);
        }

        private void RefreshOrderList()
        {
            if (_orderListRoot == null) return;

            ClearListItems(_orderItems);

            var simSystem = GetSimulationSystem();
            if (simSystem?.Order == null) return;

            foreach (var order in simSystem.Order.Orders)
            {
                CreateOrderItem(order);
            }
        }

        private void CreateOrderItem(OrderInstance order)
        {
            var cfg = OrderConfigMgr.Instance.Get(order.ConfigId);
            if (cfg == null) return;

            var go = new GameObject($"Order_{order.InstanceId}");
            go.transform.SetParent(_orderListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 80);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.34f, 0.27f, 0.2f, 0.95f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(() => TryDeliverOrder(order.InstanceId));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            string itemsText = "";
            foreach (var item in cfg.RequiredItems)
            {
                itemsText += $"{ItemConfigMgr.Instance.GetName(item.Id)}x{item.Num} ";
            }
            text.text = $"Order #{order.InstanceId}\nNeed: {itemsText}\nReward: {cfg.RewardGold}G";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            _orderItems.Add(go);
        }

        private void TryDeliverOrder(int orderInstanceId)
        {
            var simSystem = GetSimulationSystem();
            simSystem?.Order?.TryDeliverOrder(orderInstanceId);
            RefreshOrderList();
        }

        private void ClearListItems()
        {
            ClearListItems(_buildingItems);
            ClearListItems(_orderItems);
        }

        private void ClearListItems(List<GameObject> list)
        {
            foreach (var go in list)
            {
                if (go != null) Object.Destroy(go);
            }
            list.Clear();
        }
    }
}
