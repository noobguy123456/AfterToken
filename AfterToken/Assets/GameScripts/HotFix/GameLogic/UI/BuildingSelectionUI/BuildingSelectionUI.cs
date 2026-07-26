using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 建筑选择界面：显示可建造的建筑列表，玩家选择建筑后进入摆放模式。
    /// </summary>
    [Window(UILayer.UI, "TestUI", true)]
    public class BuildingSelectionUI : UIWindow
    {
        private RectTransform _buildingListRoot;
        private Button _closeButton;
        private readonly List<GameObject> _buildingItems = new List<GameObject>();
        private BuildingPlacementSystem _placementSystem;

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
        }

        protected override void OnDestroy()
        {
            ClearBuildingItems();
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
            CreateText("Select Building", new Vector2(0, 400), 36, TextAlignmentOptions.Center);

            // 创建建筑列表
            _buildingListRoot = CreateBuildingList(new Vector2(0, 0), new Vector2(600, 600));

            // 创建关闭按钮
            _closeButton = CreateButton("Close", new Vector2(0, -350), () => Close());

            RefreshBuildingList();
        }

        private TextMeshProUGUI CreateText(string content, Vector2 position, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text_" + content);
            go.transform.SetParent(Canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 50);

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
            rect.sizeDelta = new Vector2(200, 50);

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
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return btn;
        }

        private RectTransform CreateBuildingList(Vector2 position, Vector2 size)
        {
            var go = new GameObject("BuildingList");
            go.transform.SetParent(Canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 创建 viewport
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

            // 创建 content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(10, 0);
            contentRect.offsetMax = new Vector2(-10, 0);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            return contentRect;
        }

        private void RefreshBuildingList()
        {
            if (_buildingListRoot == null) return;

            ClearBuildingItems();

            var allBuildings = BuildingConfigMgr.Instance.GetAll();
            foreach (var cfg in allBuildings)
            {
                CreateBuildingItem(cfg);
            }
        }

        private void CreateBuildingItem(GameConfig.cfg.Building cfg)
        {
            var go = new GameObject($"Building_{cfg.Id}");
            go.transform.SetParent(_buildingListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560, 100);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(() => SelectBuilding(cfg.Id));

            // 建筑名称
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.offsetMin = new Vector2(10, -30);
            nameRect.offsetMax = new Vector2(-10, -10);

            var nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.text = cfg.Name;
            nameText.fontSize = 20;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.color = Color.white;

            // 建筑描述
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(go.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.offsetMin = new Vector2(10, 10);
            descRect.offsetMax = new Vector2(-10, -30);

            var descText = descGo.AddComponent<TextMeshProUGUI>();
            descText.text = $"{cfg.Desc}\nCost: {cfg.BuildCostGold}G | Time: {cfg.BuildTime}s";
            descText.fontSize = 14;
            descText.alignment = TextAlignmentOptions.Left;
            descText.color = new Color(0.8f, 0.8f, 0.8f);

            _buildingItems.Add(go);
        }

        private void SelectBuilding(int configId)
        {
            if (_placementSystem == null)
            {
                var simRoot = SingletonSystem.GetGameObject("SimulationRoot");
                if (simRoot != null)
                {
                    _placementSystem = simRoot.GetComponent<BuildingPlacementSystem>();
                }
            }

            if (_placementSystem != null)
            {
                _placementSystem.StartPlacement(configId);
                Close();
            }
            else
            {
                Log.Error("[BuildingSelectionUI] BuildingPlacementSystem 未找到");
            }
        }

        private void ClearBuildingItems()
        {
            foreach (var go in _buildingItems)
            {
                if (go != null) Object.Destroy(go);
            }
            _buildingItems.Clear();
        }
    }
}
