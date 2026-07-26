using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 模拟经营主界面。
    /// 本期使用代码动态创建 UI，后续替换为正式 Prefab。
    /// </summary>
    [Window(UILayer.UI, "TestUI", true)]
    public class SimulationMainUI : UIWindow
    {
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _levelText;
        private RectTransform _buildingListRoot;
        private RectTransform _orderListRoot;
        private Button _pauseButton;
        private Button _normalButton;
        private Button _fastButton;
        private Button _backButton;

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
            
            BuildUI();
            RegisterSimulationEvents();
        }

        protected override void OnDestroy()
        {
            ClearListItems();
            RemoveAllUIEvent();
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        private void BuildUI()
        {
            var canvas = Canvas;
            if (canvas == null) return;

            // 清空 TestUI 原有内容
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if (child != null) Object.Destroy(child.gameObject);
            }

            // 创建标题
            CreateText("Simulation", new Vector2(0, 400), 48, TextAlignmentOptions.Center);

            // 顶部资源栏
            _goldText = CreateText("Gold: 0", new Vector2(-400, 350), 24, TextAlignmentOptions.Left);
            _levelText = CreateText("Lv: 1", new Vector2(0, 350), 24, TextAlignmentOptions.Center);
            _timeText = CreateText("Time: 0s", new Vector2(400, 350), 24, TextAlignmentOptions.Right);

            // 时间控制按钮
            _pauseButton = CreateButton("Pause", new Vector2(-300, 280), () => SetSpeed(ESimSpeed.Pause));
            _normalButton = CreateButton("1x", new Vector2(-150, 280), () => SetSpeed(ESimSpeed.Normal));
            _fastButton = CreateButton("2x", new Vector2(0, 280), () => SetSpeed(ESimSpeed.Fast));
            _backButton = CreateButton("Back to Menu", new Vector2(400, -400), () => GameApp.ChangeProcedure<ProcedureMainMenu>());

            // 建筑操作按钮
            CreateButton("Build", new Vector2(-600, 280), () => OpenBuildingSelection());
            CreateButton("Upgrade", new Vector2(-450, 280), () => TryUpgradeSelected());

            // 建筑列表标题
            CreateText("Buildings", new Vector2(-600, 200), 32, TextAlignmentOptions.Left);
            _buildingListRoot = CreateScrollList(new Vector2(-600, -50), new Vector2(400, 500));

            // 订单列表标题
            CreateText("Orders", new Vector2(200, 200), 32, TextAlignmentOptions.Left);
            _orderListRoot = CreateScrollList(new Vector2(200, -50), new Vector2(400, 500));

            RefreshBuildingList();
            RefreshOrderList();
        }

        private TextMeshProUGUI CreateText(string content, Vector2 position, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(Canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 50);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private Button CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(Canvas.transform, false);
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

            return btn;
        }

        private RectTransform CreateScrollList(Vector2 position, Vector2 size)
        {
            var go = new GameObject("ScrollList");
            go.transform.SetParent(Canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 创建 viewport（带 Mask，用于裁剪内容）
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(go.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.clear;

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
            var go = new GameObject($"Building_{cfg.Id}");
            go.transform.SetParent(_buildingListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 80);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(() => TryBuild(cfg.Id));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = $"{cfg.Name} (Lv{cfg.MaxLevel})\nCost: {cfg.BuildCostGold}G";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            _buildingItems.Add(go);
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
            image.color = new Color(0.2f, 0.25f, 0.2f, 0.9f);

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

        private void TryBuild(int configId)
        {
            var simSystem = GetSimulationSystem();
            simSystem?.Building?.TryBuild(configId, out _);
            RefreshBuildingList();
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
            image.color = new Color(0.25f, 0.2f, 0.15f, 0.9f);

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
