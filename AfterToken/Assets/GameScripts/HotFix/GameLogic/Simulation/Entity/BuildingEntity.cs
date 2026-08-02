using Cysharp.Threading.Tasks;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Building = GameConfig.cfg.Building;
using EBuildingType = GameConfig.cfg.EBuildingType;

namespace GameLogic
{
    /// <summary>
    /// 建筑场景实体：负责加载和显示 3D 建筑模型，显示建筑状态。
    /// </summary>
    public class BuildingEntity : MonoBehaviour
    {
        private int _instanceId;
        private int _configId;
        private Building _cfg;
        private GameObject _modelInstance;
        private bool _modelFromResource; // 模型是否来自资源加载（决定销毁时走 UnloadAsset 还是 Destroy）
        private Renderer[] _renderers;
        private Color[] _originalColors; // 逐 renderer 缓存，多方块占位模型各有颜色
        private Color _buildingColor = new Color(1f, 0.5f, 0.5f, 0.7f);
        private Color _upgradingColor = new Color(0.5f, 0.5f, 1f, 0.7f);
        private GameObject _labelInstance;
        private TextMeshProUGUI _labelText;
        private Camera _mainCamera;

        public int InstanceId => _instanceId;
        public int ConfigId => _configId;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        public async UniTask InitializeAsync(int instanceId, int configId, Vector3 position)
        {
            _instanceId = instanceId;
            _configId = configId;
            transform.position = position;

            var cfg = BuildingConfigMgr.Instance.Get(configId);
            if (cfg == null)
            {
                Log.Error($"[BuildingEntity] 找不到建筑配置 {configId}");
                return;
            }
            _cfg = cfg;

            await LoadModelAsync(cfg.Icon);
            CreateLabel(cfg.Name, 1);
            UpdateState(BuildingState.Building, 0f);
        }

        private async UniTask LoadModelAsync(string modelAddress)
        {
            if (string.IsNullOrEmpty(modelAddress))
            {
                Log.Warning($"[BuildingEntity] 建筑 {_configId} 未配置模型地址，使用默认模型");
                CreateDefaultModel();
                return;
            }

            // 资源地址无效（占位配置）时直接用占位模型，避免资源模块报 ERROR
            if (!GameModule.Resource.CheckLocationValid(modelAddress))
            {
                CreateDefaultModel();
                return;
            }

            try
            {
                _modelInstance = await GameModule.Resource.LoadGameObjectAsync(modelAddress, transform);
                if (_modelInstance != null)
                {
                    _modelFromResource = true;
                    _renderers = _modelInstance.GetComponentsInChildren<Renderer>();
                    CacheOriginalColors();
                }
                else
                {
                    // 资源加载返回 null（占位地址无对应资源）时回退到占位模型
                    Log.Warning($"[BuildingEntity] 建筑 {_configId} 模型资源为空 {modelAddress}，使用默认模型");
                    CreateDefaultModel();
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[BuildingEntity] 加载建筑模型失败 {modelAddress}: {e.Message}");
                CreateDefaultModel();
            }
        }

        private void CreateDefaultModel()
        {
            // 正式模型资源到位前，使用按类型拼装的占位方块模型（尺寸由配置表 footprint 决定）
            int fx = _cfg != null ? _cfg.FootprintX : 2;
            int fz = _cfg != null ? _cfg.FootprintZ : 2;
            var type = _cfg != null ? _cfg.BuildingType : EBuildingType.Workshop;
            _modelInstance = BuildingModelFactory.CreatePlaceholder(type, fx, fz);
            _modelInstance.transform.SetParent(transform, false);
            _modelInstance.transform.localPosition = Vector3.zero;
            _renderers = _modelInstance.GetComponentsInChildren<Renderer>();
            CacheOriginalColors();
        }

        private void CacheOriginalColors()
        {
            if (_renderers == null)
            {
                _originalColors = null;
                return;
            }
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalColors[i] = _renderers[i] != null ? _renderers[i].material.color : Color.white;
            }
        }

        /// <summary>
        /// 创建建筑标签（World Space UI）。
        /// </summary>
        public void CreateLabel(string buildingName, int level)
        {
            // 创建 Canvas（World Space）
            _labelInstance = new GameObject("BuildingLabel");
            _labelInstance.transform.SetParent(transform, false);
            // 标签高度按建筑类型抬高，避免嵌入占位模型顶部
            float labelHeight = _cfg != null ? BuildingModelFactory.GetLabelHeight(_cfg.BuildingType) : 2.5f;
            _labelInstance.transform.localPosition = new Vector3(0f, labelHeight, 0f);

            var canvas = _labelInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera;

            var canvasRect = _labelInstance.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 0.5f);

            // 创建背景
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(_labelInstance.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.7f);

            // 创建文本
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_labelInstance.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _labelText = textGo.AddComponent<TextMeshProUGUI>();
            _labelText.text = $"{buildingName} Lv{level}";
            _labelText.fontSize = 0.3f;
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.color = Color.white;
        }

        /// <summary>
        /// 更新建筑标签文本。
        /// </summary>
        public void UpdateLabel(string buildingName, int level)
        {
            if (_labelText != null)
            {
                _labelText.text = $"{buildingName} Lv{level}";
            }
        }

        public void UpdateState(BuildingState state, float progress)
        {
            if (_renderers == null || _originalColors == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                Color original = _originalColors[i];
                _renderers[i].material.color = state switch
                {
                    BuildingState.Building => Color.Lerp(_buildingColor, original, progress),
                    BuildingState.Upgrading => Color.Lerp(_upgradingColor, original, progress),
                    _ => original,
                };
            }
        }

        public void SetSelected(bool selected)
        {
            if (_renderers == null || _originalColors == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].material.color = selected ? Color.yellow : _originalColors[i];
            }
        }

        private void LateUpdate()
        {
            // 使标签始终面向相机
            if (_labelInstance != null && _mainCamera != null)
            {
                _labelInstance.transform.LookAt(_mainCamera.transform);
                _labelInstance.transform.Rotate(0f, 180f, 0f); // 翻转，使文本正面朝向相机
            }
        }

        private void OnDestroy()
        {
            if (_modelInstance != null)
            {
                // 资源加载的模型走资源卸载；代码拼装的占位模型直接销毁
                if (_modelFromResource)
                {
                    GameModule.Resource.UnloadAsset(_modelInstance);
                }
                else
                {
                    Destroy(_modelInstance);
                }
                _modelInstance = null;
            }

            if (_labelInstance != null)
            {
                Destroy(_labelInstance);
                _labelInstance = null;
            }
        }
    }
}
