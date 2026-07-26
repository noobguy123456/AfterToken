using Cysharp.Threading.Tasks;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 建筑场景实体：负责加载和显示 3D 建筑模型，显示建筑状态。
    /// </summary>
    public class BuildingEntity : MonoBehaviour
    {
        private int _instanceId;
        private int _configId;
        private GameObject _modelInstance;
        private Renderer[] _renderers;
        private Color _originalColor = Color.white;
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

            try
            {
                _modelInstance = await GameModule.Resource.LoadGameObjectAsync(modelAddress, transform);
                if (_modelInstance != null)
                {
                    _renderers = _modelInstance.GetComponentsInChildren<Renderer>();
                    CacheOriginalColor();
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
            // 创建默认 3D 模型（Cube）
            _modelInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _modelInstance.transform.SetParent(transform, false);
            _modelInstance.transform.localPosition = Vector3.zero;
            _modelInstance.transform.localScale = new Vector3(2f, 2f, 2f);
            _renderers = _modelInstance.GetComponentsInChildren<Renderer>();
            CacheOriginalColor();
        }

        private void CacheOriginalColor()
        {
            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null)
            {
                _originalColor = _renderers[0].material.color;
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
            _labelInstance.transform.localPosition = new Vector3(0f, 2.5f, 0f); // 放置在建筑上方

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
            if (_renderers == null) return;

            Color targetColor = state switch
            {
                BuildingState.Building => Color.Lerp(_buildingColor, _originalColor, progress),
                BuildingState.Upgrading => Color.Lerp(_upgradingColor, _originalColor, progress),
                _ => _originalColor,
            };

            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = targetColor;
                }
            }
        }

        public void SetSelected(bool selected)
        {
            if (_renderers == null) return;

            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    if (selected)
                    {
                        renderer.material.color = Color.yellow;
                    }
                    else
                    {
                        renderer.material.color = _originalColor;
                    }
                }
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
                GameModule.Resource.UnloadAsset(_modelInstance);
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
