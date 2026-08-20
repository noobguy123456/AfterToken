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
    /// Prefab：Assets/AssetRaw/UI/SimulationMainUI/SimulationMainUI.prefab（静态结构在 Prefab，列表项运行时生成）。
    /// </summary>
    [Window(UILayer.UI, "SimulationMainUI", true)]
    public class SimulationMainUI : UIWindow
    {
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _levelText;
        private RectTransform _simRoot;
        private RectTransform _panelRoot;
        private RectTransform _buildingListRoot;
        private RectTransform _orderListRoot;
        private Button _pauseButton;
        private Button _normalButton;
        private Button _fastButton;
        private Button _panelButton;
        private Button _deployButton;
        private Button _closeButton;
        private Button _buildButton;
        private Button _backButton;
        private bool _panelVisible;

        private readonly List<GameObject> _buildingItems = new List<GameObject>();
        private readonly List<GameObject> _orderItems = new List<GameObject>();

        private float _refreshTimer;
        private const float REFRESH_INTERVAL = 0.5f;
        private SimulationSystem _cachedSimulationSystem;

        protected override void ScriptGenerator()
        {
            _simRoot = FindChildComponent<RectTransform>("m_rect_SimRoot");
            _goldText = FindChildComponent<TextMeshProUGUI>("m_rect_SimRoot/m_rect_HudBar/m_text_Gold");
            _levelText = FindChildComponent<TextMeshProUGUI>("m_rect_SimRoot/m_rect_HudBar/m_text_Level");
            _timeText = FindChildComponent<TextMeshProUGUI>("m_rect_SimRoot/m_rect_HudBar/m_text_Time");
            _pauseButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_HudBar/m_btn_Pause");
            _normalButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_HudBar/m_btn_Normal");
            _fastButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_HudBar/m_btn_Fast");
            _panelButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_HudBar/m_btn_Panel");
            _deployButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_HudBar/m_btn_Deploy");

            _panelRoot = FindChildComponent<RectTransform>("m_rect_SimRoot/m_rect_Panel");
            _closeButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_Panel/m_btn_Close");
            _buildButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_Panel/m_btn_Build");
            _backButton = FindChildComponent<Button>("m_rect_SimRoot/m_rect_Panel/m_btn_Back");
            _buildingListRoot = FindChildComponent<RectTransform>("m_rect_SimRoot/m_rect_Panel/m_scroll_BuildingList/Viewport/m_rect_Content");
            _orderListRoot = FindChildComponent<RectTransform>("m_rect_SimRoot/m_rect_Panel/m_scroll_OrderList/Viewport/m_rect_Content");
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

            // 框架根 Canvas 的 CanvasScaler 已统一为 1920x1080 横屏参考分辨率，本界面按 1920x1080 像素设计，无需额外缩放

            BindButtons();
            RegisterSimulationEvents();

            SetPanelVisible(false);
            RefreshBuildingList();
            RefreshOrderList();
        }

        protected override void OnDestroy()
        {
            ClearListItems();
            // 注意：不要在子类调用 RemoveAllUIEvent()，UIWindow.InternalDestroy 已统一释放，重复调用会触发内存池二次释放异常
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void BindButtons()
        {
            _pauseButton?.onClick.AddListener(() => SetSpeed(ESimSpeed.Pause));
            _normalButton?.onClick.AddListener(() => SetSpeed(ESimSpeed.Normal));
            _fastButton?.onClick.AddListener(() => SetSpeed(ESimSpeed.Fast));
            _panelButton?.onClick.AddListener(() => SetPanelVisible(!_panelVisible));
            // Deploy：打开基地内选关窗口（LobbyUI 复用为关卡选择面板）
            _deployButton?.onClick.AddListener(() => GameModule.UI.ShowUIAsync<LobbyUI>());
            _closeButton?.onClick.AddListener(() => SetPanelVisible(false));
            _buildButton?.onClick.AddListener(OpenBuildingSelection);
            _backButton?.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureMainMenu>());
        }

        /// <summary>管理面板当前是否可见（供 ESC 关闭链查询）。</summary>
        public bool IsPanelVisible => _panelVisible;

        /// <summary>展开管理面板（供外部调用，如摆放模式 ESC/右键退回）。</summary>
        public void OpenManagementPanel()
        {
            SetPanelVisible(true);
        }

        /// <summary>收起管理面板（ESC 关闭链调用）。</summary>
        public void CloseManagementPanel()
        {
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            _panelVisible = visible;
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(visible);
            }
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
            text.text = $"{cfg.Name} (Lv{cfg.MaxLevel})  [{count}/{maxCount}]\nCost: {cfg.BuildCostGold}G{BuildUnlockHint(cfg)}";
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
        /// 数量上限的提升途径提示（第三行，与 TbBuilding 解锁字段对应；购买途径由 Unlock 按钮价格体现，不在此行重复）。
        /// </summary>
        private static string BuildUnlockHint(GameConfig.cfg.Building cfg)
        {
            var sb = new System.Text.StringBuilder();
            if (cfg.MaxCountUpgradeLevel > 0)
            {
                sb.Append("+1 slot at building Lv").Append(cfg.MaxCountUpgradeLevel).Append("   ");
            }
            if (cfg.MaxCountPerPlayerLevel > 0)
            {
                sb.Append("+").Append(cfg.MaxCountPerPlayerLevel).Append(" slot per player Lv");
            }
            return sb.Length > 0 ? "\n" + sb.ToString().TrimEnd() : string.Empty;
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
                string reason = "System not ready";
                if (simSystem?.Building != null && simSystem.Building.TryPurchaseSlot(configId, out reason))
                {
                    RefreshBuildingList();
                }
                else
                {
                    Log.Warning($"[SimulationMainUI] Unlock slot failed: {reason}");
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
                Log.Error("[SimulationMainUI] BuildingPlacementSystem not found");
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
