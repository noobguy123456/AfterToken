using TEngine;
using UnityEngine;
using Building = GameConfig.cfg.Building;

namespace GameLogic
{
    /// <summary>
    /// 建筑摆放系统：处理点击空地建造建筑的逻辑。
    /// 位置吸附到全局 1m 基础网格（见 MapGrid），占地冲突由 BuildingSystem 的格子占用表判定。
    /// </summary>
    public class BuildingPlacementSystem : MonoBehaviour
    {
        private Camera _mainCamera;
        private GameObject _previewInstance;
        private GameObject _gridLines;
        private int _selectedBuildingConfigId = -1;
        private int _footprintX = 1;
        private int _footprintZ = 1;
        private int _rotationY; // 摆放朝向（0/90/180/270），R 键旋转
        private bool _isPlacing;
        private BuildingSystem _buildingSystem;
        private LayerMask _groundLayer;

        // 摆放模式网格线的覆盖半径（与 Simulation 地面 50x50m 对齐）
        private const int GridExtent = 25;

        public bool IsPlacing => _isPlacing;

        /// <summary>当前朝向下的有效占地（旋转 90/270 度时 X/Z 对调）。</summary>
        private void GetEffectiveFootprint(out int fx, out int fz)
        {
            bool swapped = _rotationY % 180 != 0;
            fx = swapped ? _footprintZ : _footprintX;
            fz = swapped ? _footprintX : _footprintZ;
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            _groundLayer = LayerMask.GetMask("Default");
        }

        public void Initialize(BuildingSystem buildingSystem)
        {
            _buildingSystem = buildingSystem;
        }

        private void Update()
        {
            if (Time.frameCount % 15 == 0) Log.Info($"[hb] Placement f={Time.frameCount}");
            if (!_isPlacing) return;

            UpdatePreviewPosition();

            if (Input.GetKeyDown(KeyCode.R))
            {
                // R 键旋转 90 度（影响占地 X/Z 与预览朝向）
                _rotationY = (_rotationY + 90) % 360;
                UpdatePreviewPosition();
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceBuilding();
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        public void StartPlacement(int buildingConfigId)
        {
            if (_isPlacing)
            {
                CancelPlacement();
            }

            var cfg = BuildingConfigMgr.Instance.Get(buildingConfigId);
            if (cfg == null)
            {
                Log.Error($"[BuildingPlacementSystem] 找不到建筑配置 {buildingConfigId}");
                return;
            }

            _selectedBuildingConfigId = buildingConfigId;
            _footprintX = cfg.FootprintX;
            _footprintZ = cfg.FootprintZ;
            _rotationY = 0;
            _isPlacing = true;
            CreatePreview(cfg);
            CreateGridLines();
        }

        public void CancelPlacement()
        {
            _isPlacing = false;
            _selectedBuildingConfigId = -1;
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
            if (_gridLines != null)
            {
                Destroy(_gridLines);
                _gridLines = null;
            }
        }

        private void CreatePreview(Building cfg)
        {
            // 预览与场景实体共用占位模型，尺寸即真实占地
            _previewInstance = BuildingModelFactory.CreatePlaceholder(cfg.BuildingType, cfg.FootprintX, cfg.FootprintZ);
            // 预览不参与射线检测，避免自身挡住指向地面的射线
            SetLayerRecursively(_previewInstance, 2); // Ignore Raycast
            TintPreview(true);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void TintPreview(bool canPlace)
        {
            if (_previewInstance == null) return;
            Color color = canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            foreach (var renderer in _previewInstance.GetComponentsInChildren<Renderer>())
            {
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        /// <summary>
        /// 摆放模式下显示 1m 基础网格线（仅摆放模式显示，退出即销毁）。
        /// 线画在格子边界（格中心位于 1m 整数倍坐标，边界在 x.5），与建筑实际占地对齐。
        /// </summary>
        private void CreateGridLines()
        {
            _gridLines = new GameObject("PlacementGrid");
            float length = GridExtent * 2f * MapGrid.BaseCellSize;
            var lineColor = new Color(1f, 1f, 1f, 0.6f);
            for (int i = -GridExtent; i < GridExtent; i++)
            {
                float offset = (i + 0.5f) * MapGrid.BaseCellSize;
                AddGridLine(new Vector3(offset, 0.02f, 0f), new Vector3(0.04f, 0.02f, length), lineColor);
                AddGridLine(new Vector3(0f, 0.02f, offset), new Vector3(length, 0.02f, 0.04f), lineColor);
            }
        }

        private void AddGridLine(Vector3 localPosition, Vector3 scale, Color color)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GridLine";
            line.transform.SetParent(_gridLines.transform, false);
            line.transform.localPosition = localPosition;
            line.transform.localScale = scale;
            var renderer = line.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            // 网格线不参与物理
            var collider = line.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void UpdatePreviewPosition()
        {
            if (_previewInstance == null || _mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                GetEffectiveFootprint(out int fx, out int fz);
                Vector3 position = MapGrid.Snap(hit.point, fx, fz);
                _previewInstance.transform.position = position;
                _previewInstance.transform.rotation = Quaternion.Euler(0f, _rotationY, 0f);
                TintPreview(CanPlaceAt(position));
            }
        }

        /// <summary>
        /// 完整可放置校验（占地/重复/资源），与 BuildingSystem.TryBuild 判定一致。
        /// </summary>
        private bool CanPlaceAt(Vector3 snappedPosition)
        {
            if (_buildingSystem == null) return false;
            return _buildingSystem.CanBuild(_selectedBuildingConfigId, snappedPosition, _rotationY, out _);
        }

        private void TryPlaceBuilding()
        {
            if (_previewInstance == null) return;

            if (_buildingSystem == null)
            {
                Log.Error("[BuildingPlacementSystem] BuildingSystem 未初始化");
                return;
            }

            Vector3 position = _previewInstance.transform.position;
            if (!_buildingSystem.CanBuild(_selectedBuildingConfigId, position, _rotationY, out string reason))
            {
                // 放置失败：飘字提示原因，保持摆放模式不取消
                Log.Warning($"[BuildingPlacementSystem] 无法放置：{reason}");
                ShowFloatText(reason, position);
                return;
            }

            if (_buildingSystem.TryBuild(_selectedBuildingConfigId, position, _rotationY, out int instanceId))
            {
                Log.Info($"[BuildingPlacementSystem] 建筑 {_selectedBuildingConfigId} 放置成功，实例 ID: {instanceId}");
                // 放置成功同样保持选中（连续摆放），仅刷新预览染色（同类型已存在会转红）
                UpdatePreviewPosition();
            }
        }

        /// <summary>
        /// 在指定世界位置显示飘字提示（上升 + 淡出，自动销毁）。
        /// 用 legacy Text（OS 字体回退可显中文；项目 TMP 字库为 Latin-only，中文会显示方框）。
        /// </summary>
        private void ShowFloatText(string message, Vector3 worldPos)
        {
            var go = new GameObject("PlacementFloatText");
            go.transform.position = worldPos + Vector3.up * 2.5f;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 80f);
            rect.localScale = Vector3.one * 0.01f; // 世界空间 UI 标准缩放（600px ≈ 6m 宽）

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 56;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.35f, 0.3f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            FloatTextAnim.Attach(go, text, _mainCamera);
        }

        /// <summary>飘字动画：上飘 + 面向相机 + 生命周期结束自动销毁。</summary>
        private class FloatTextAnim : MonoBehaviour
        {
            private UnityEngine.UI.Text _text;
            private Camera _cam;
            private float _timer;
            private const float LifeTime = 1.2f;

            public static void Attach(GameObject go, UnityEngine.UI.Text text, Camera cam)
            {
                var anim = go.AddComponent<FloatTextAnim>();
                anim._text = text;
                anim._cam = cam;
            }

            private void Update()
            {
                _timer += Time.deltaTime;
                transform.position += Vector3.up * (0.8f * Time.deltaTime);
                if (_cam != null)
                {
                    transform.LookAt(_cam.transform);
                    transform.Rotate(0f, 180f, 0f); // 翻转让文字正面朝相机
                }
                if (_text != null)
                {
                    Color c = _text.color;
                    c.a = Mathf.Clamp01(1f - _timer / LifeTime);
                    _text.color = c;
                }
                if (_timer >= LifeTime)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
