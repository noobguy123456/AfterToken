using System.Collections.Generic;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 建筑选择界面：显示可建造的建筑列表，玩家选择建筑后进入摆放模式。
    /// Prefab：Assets/AssetRaw/UI/BuildingSelectionUI/BuildingSelectionUI.prefab（静态结构在 Prefab，列表项运行时生成）。
    /// </summary>
    [Window(UILayer.UI, "BuildingSelectionUI", true)]
    public class BuildingSelectionUI : UIWindow
    {
        private RectTransform _simRoot;
        private RectTransform _buildingListRoot;
        private Button _closeButton;
        private readonly List<GameObject> _buildingItems = new List<GameObject>();
        private BuildingPlacementSystem _placementSystem;

        protected override void ScriptGenerator()
        {
            _simRoot = FindChildComponent<RectTransform>("m_rect_SimRoot");
            _buildingListRoot = FindChildComponent<RectTransform>("m_rect_SimRoot/m_scroll_List/Viewport/m_rect_Content");
            _closeButton = FindChildComponent<Button>("m_rect_SimRoot/m_btn_Close");
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

            _closeButton?.onClick.AddListener(() => Close());
            RefreshBuildingList();
        }

        protected override void OnDestroy()
        {
            ClearBuildingItems();
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
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
                Log.Error("[BuildingSelectionUI] BuildingPlacementSystem not found");
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
