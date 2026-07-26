using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 建筑摆放系统：处理点击空地建造建筑的逻辑。
    /// </summary>
    public class BuildingPlacementSystem : MonoBehaviour
    {
        private Camera _mainCamera;
        private GameObject _previewInstance;
        private int _selectedBuildingConfigId = -1;
        private bool _isPlacing;
        private BuildingSystem _buildingSystem;
        private LayerMask _groundLayer;
        private float _gridSize = 2f;

        public bool IsPlacing => _isPlacing;

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
            if (!_isPlacing) return;

            UpdatePreviewPosition();

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

            _selectedBuildingConfigId = buildingConfigId;
            _isPlacing = true;
            CreatePreview(buildingConfigId);
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
        }

        private void CreatePreview(int buildingConfigId)
        {
            var cfg = BuildingConfigMgr.Instance.Get(buildingConfigId);
            if (cfg == null) return;

            _previewInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _previewInstance.transform.localScale = new Vector3(2f, 2f, 2f);
            var renderer = _previewInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0f, 1f, 0f, 0.5f);
            }
        }

        private void UpdatePreviewPosition()
        {
            if (_previewInstance == null || _mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
            {
                Vector3 position = SnapToGrid(hit.point);
                _previewInstance.transform.position = position;

                // 检查是否可以放置
                bool canPlace = CanPlaceAt(position);
                var renderer = _previewInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
                }
            }
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float x = Mathf.Round(position.x / _gridSize) * _gridSize;
            float z = Mathf.Round(position.z / _gridSize) * _gridSize;
            return new Vector3(x, 0f, z);
        }

        private bool CanPlaceAt(Vector3 position)
        {
            // 检查该位置是否已有建筑
            Collider[] colliders = Physics.OverlapBox(position, new Vector3(_gridSize * 0.4f, 1f, _gridSize * 0.4f));
            foreach (var collider in colliders)
            {
                if (collider.GetComponent<BuildingEntity>() != null)
                {
                    return false;
                }
            }
            return true;
        }

        private void TryPlaceBuilding()
        {
            if (_previewInstance == null) return;

            Vector3 position = _previewInstance.transform.position;
            if (!CanPlaceAt(position))
            {
                Log.Warning("[BuildingPlacementSystem] 该位置无法放置建筑");
                return;
            }

            if (_buildingSystem == null)
            {
                Log.Error("[BuildingPlacementSystem] BuildingSystem 未初始化");
                return;
            }

            if (_buildingSystem.TryBuild(_selectedBuildingConfigId, position, out int instanceId))
            {
                Log.Info($"[BuildingPlacementSystem] 建筑 {_selectedBuildingConfigId} 放置成功，实例 ID: {instanceId}");
            }

            CancelPlacement();
        }
    }
}
