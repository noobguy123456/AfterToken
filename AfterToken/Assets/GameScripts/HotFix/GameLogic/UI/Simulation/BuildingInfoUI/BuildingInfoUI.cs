using System.Collections.Generic;
using System.Text;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 建筑信息面板：点击场景建筑打开。
    /// 左侧产出（进行中队列 + 可生产配方），右侧建筑名 + 模型快照 + 升级。
    /// 模型快照用 RenderTexture 一次性拍摄（占位模型很轻，性能无压力；正式模型若变重可换静态原画）。
    /// Prefab：Assets/AssetRaw/UI/BuildingInfoUI/BuildingInfoUI.prefab（静态结构在 Prefab，配方列表项运行时生成）。
    /// </summary>
    [Window(UILayer.UI, "BuildingInfoUI", true)]
    public class BuildingInfoUI : UIWindow
    {
        /// <summary>待显示的建筑实例 ID：重复 ShowUIAsync 已开窗口时靠它切换目标。</summary>
        public static int PendingInstanceId = -1;

        private int _instanceId = -1;
        private SimulationSystem _simSystem;

        private RectTransform _infoRoot;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _stateText;
        private TextMeshProUGUI _queueText;
        private TextMeshProUGUI _upgradeText;
        private RectTransform _recipeListRoot;
        private RawImage _previewImage;
        private Button _upgradeButton;
        private Button _closeButton;
        private RenderTexture _previewRT;
        private readonly List<GameObject> _recipeItems = new List<GameObject>();

        // 升级失败原因（红色，3 秒后自动消失）
        private TextMeshProUGUI _failText;
        private float _failTimer;
        private const float FailTextDuration = 3f;

        private float _refreshTimer;
        private const float RefreshInterval = 0.25f;

        protected override void ScriptGenerator()
        {
            _infoRoot = FindChildComponent<RectTransform>("m_rect_InfoRoot");
            _titleText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Title");
            _stateText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_State");
            _queueText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Queue");
            _recipeListRoot = FindChildComponent<RectTransform>("m_rect_InfoRoot/m_rect_Panel/m_rect_RecipeList");
            _previewImage = FindChildComponent<RawImage>("m_rect_InfoRoot/m_rect_Panel/m_rimg_Preview");
            _upgradeButton = FindChildComponent<Button>("m_rect_InfoRoot/m_rect_Panel/m_btn_Upgrade");
            _upgradeText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_UpgradeCost");
            _failText = FindChildComponent<TextMeshProUGUI>("m_rect_InfoRoot/m_rect_Panel/m_text_Fail");
            _closeButton = FindChildComponent<Button>("m_rect_InfoRoot/m_rect_Panel/m_btn_Close");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();

            if (GraphicRaycaster != null)
            {
                GraphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }

            // 框架根 Canvas 带 CanvasScaler（参考 750x1334 按宽适配，1920x1080 下放大 2.56 倍），
            // 本界面按 1920x1080 像素设计，反向缩放保持设计尺寸
            var rootCanvas = rectTransform.GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas != null && rootCanvas.scaleFactor > 0f && _infoRoot != null)
            {
                _infoRoot.localScale = Vector3.one / rootCanvas.scaleFactor;
            }

            _simSystem = GetSimulationSystem();
            _instanceId = ResolvePendingInstanceId();
            if (_instanceId <= 0 && UserDatas != null && UserDatas.Length > 0 && UserDatas[0] is int userDataId)
            {
                _instanceId = userDataId;
            }

            _upgradeButton?.onClick.AddListener(TryUpgrade);
            _closeButton?.onClick.AddListener(() => Close());

            RefreshAll();
        }

        protected override void OnDestroy()
        {
            ClearRecipeItems();
            ReleasePreviewRT();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            // 窗口已开时点击另一栋建筑：切换目标
            int pending = ResolvePendingInstanceId();
            if (pending != _instanceId && pending > 0)
            {
                _instanceId = pending;
                RefreshAll();
            }

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                RefreshDynamic();
            }

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

        private static int ResolvePendingInstanceId()
        {
            int id = PendingInstanceId;
            PendingInstanceId = -1;
            return id;
        }

        private static SimulationSystem GetSimulationSystem()
        {
            var root = SingletonSystem.GetGameObject("SimulationRoot");
            return root != null ? root.GetComponent<SimulationSystem>() : null;
        }

        /// <summary>全量刷新（切换目标/打开时）：标题、快照、配方列表、动态区。</summary>
        private void RefreshAll()
        {
            CaptureModelPreview();
            RebuildRecipeList();
            RefreshDynamic();
        }

        /// <summary>高频刷新：标题/状态/队列/升级花费。</summary>
        private void RefreshDynamic()
        {
            var building = _simSystem?.Building?.GetBuilding(_instanceId);
            var cfg = building != null ? BuildingConfigMgr.Instance.Get(building.ConfigId) : null;
            if (building == null || cfg == null)
            {
                if (_titleText != null) _titleText.text = "Building not found";
                return;
            }

            if (_titleText != null) _titleText.text = $"{cfg.Name}  Lv{building.Level}";

            string stateText = building.State switch
            {
                BuildingState.Building => $"Constructing... {building.Progress * 100f:F0}%",
                BuildingState.Upgrading => $"Upgrading... {building.Progress * 100f:F0}%",
                _ => "Idle",
            };
            if (_stateText != null) _stateText.text = stateText;

            if (_queueText != null)
            {
                var sb = new StringBuilder();
                int running = 0;
                if (_simSystem.Production != null)
                {
                    foreach (var p in _simSystem.Production.Productions)
                    {
                        if (p.BuildingInstanceId != _instanceId) continue;
                        running++;
                        sb.Append(ItemConfigMgr.Instance.GetName(p.OutputItemId))
                            .Append(" x").Append(p.OutputCount)
                            .Append("   ").Append((p.Progress * 100f).ToString("F0")).Append("%\n");
                    }
                }
                if (running == 0)
                {
                    sb.Append("(no production)");
                }
                _queueText.text = sb.ToString().TrimEnd();
            }

            if (_upgradeText != null)
            {
                _upgradeText.text = building.Level >= cfg.MaxLevel
                    ? "Max level"
                    : $"Cost: {cfg.UpgradeCostGold}G + items";
            }
        }

        private void RebuildRecipeList()
        {
            ClearRecipeItems();
            var building = _simSystem?.Building?.GetBuilding(_instanceId);
            if (building == null || _recipeListRoot == null) return;

            var recipes = ProductionConfigMgr.Instance.GetByBuildingId(building.ConfigId);
            foreach (var recipe in recipes)
            {
                CreateRecipeItem(recipe);
            }
        }

        private void CreateRecipeItem(GameConfig.cfg.Production recipe)
        {
            var go = new GameObject($"Recipe_{recipe.Id}");
            go.transform.SetParent(_recipeListRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 64f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.16f, 0.24f, 0.95f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-90f, 0f);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = $"{recipe.Name} (req Lv{recipe.LevelRequired})\n{FormatItems(recipe.InputItems)} -> {ItemConfigMgr.Instance.GetName(recipe.OutputItemId)} x{recipe.OutputCount}, {recipe.ProductionTime}s";
            text.fontSize = 13;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            var btn = CreateItemButton(rect, "Start", new Vector2(168f, 0f), () => TryStartProduction(recipe.Id));
            var btnRect = btn.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(76f, 40f);

            _recipeItems.Add(go);
        }

        private void TryStartProduction(int productionId)
        {
            if (_simSystem?.Production != null
                && _simSystem.Production.TryStartProduction(_instanceId, productionId, out _))
            {
                RefreshDynamic();
            }
        }

        private void TryUpgrade()
        {
            if (_simSystem?.Building == null) return;
            if (_simSystem.Building.TryUpgrade(_instanceId))
            {
                RefreshDynamic();
            }
            else if (!_simSystem.Building.CanUpgrade(_instanceId, out var reason))
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

        /// <summary>
        /// 用临时相机把建筑模型拍到 RenderTexture（只拍一帧）。
        /// 拍摄时把实体临时切到独立层，避免画面里混入地面与其他建筑。
        /// </summary>
        private void CaptureModelPreview()
        {
            ReleasePreviewRT();
            if (_previewImage == null) return;

            var entity = _simSystem?.Building?.GetBuildingEntity(_instanceId);
            if (entity == null)
            {
                _previewImage.texture = null;
                return;
            }

            var renderers = entity.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                _previewImage.texture = null;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                if (r != null) bounds.Encapsulate(r.bounds);
            }

            const int previewLayer = 30;
            SetLayerRecursively(entity.gameObject, previewLayer);
            try
            {
                _previewRT = new RenderTexture(256, 256, 16);
                var camGo = new GameObject("BuildingPreviewCamera");
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 1f);
                cam.cullingMask = 1 << previewLayer;
                cam.fieldOfView = 30f;

                float dist = bounds.extents.magnitude * 2.2f + 1f;
                cam.transform.position = bounds.center + new Vector3(1f, 0.9f, -1f).normalized * dist;
                cam.transform.LookAt(bounds.center);
                cam.targetTexture = _previewRT;
                cam.Render();
                cam.targetTexture = null;
                Object.Destroy(camGo);

                _previewImage.texture = _previewRT;
            }
            finally
            {
                // 实体原本都在 Default 层（BuildingSystem 创建，未改过层）
                SetLayerRecursively(entity.gameObject, 0);
            }
        }

        private void ReleasePreviewRT()
        {
            if (_previewRT != null)
            {
                _previewRT.Release();
                Object.Destroy(_previewRT);
                _previewRT = null;
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static string FormatItems(IReadOnlyList<GameConfig.cfg.ItemExchange> items)
        {
            if (items == null || items.Count == 0) return "(free)";
            var sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(ItemConfigMgr.Instance.GetName(items[i].Id)).Append(" x").Append(items[i].Num);
            }
            return sb.ToString();
        }

        /// <summary>配方列表项内的小按钮（动态内容专用，静态结构一律放 Prefab）。</summary>
        private Button CreateItemButton(RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(140f, 44f);

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

        private void ClearRecipeItems()
        {
            foreach (var go in _recipeItems)
            {
                if (go != null) Object.Destroy(go);
            }
            _recipeItems.Clear();
        }
    }
}
