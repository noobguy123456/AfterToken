using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 小地图系统。
    /// 战斗入场后对整张地图做一次正交俯视烘焙（one-shot bake），
    /// 生成一张静态 2D 平面缩略图（RenderTexture）供 <see cref="MinimapUI"/> 显示。
    /// 相机烘焙后即停用，不逐帧渲染 3D 场景。
    /// </summary>
    public class MinimapSystem : MonoBehaviour
    {
        public static MinimapSystem Instance { get; private set; }

        private const int TextureSize = 512;
        private const float CameraHeight = 50f;
        /// <summary>
        /// 找不到战斗边界时的兜底视野半径（米）。
        /// </summary>
        private const float FallbackHalfExtent = 30f;

        private Camera _mapCamera;
        private RenderTexture _mapTexture;
        private Vector3 _mapCenter;
        private float _halfExtent = FallbackHalfExtent;
        private Vector2 _uvMin;
        private Vector2 _uvMax = Vector2.one;
        private bool _baked;

        /// <summary>
        /// 小地图静态纹理（MinimapUI 绑定到 RawImage）。
        /// </summary>
        public RenderTexture MapTexture => _mapTexture;

        /// <summary>
        /// 是否已完成烘焙。
        /// </summary>
        public bool IsReady => _baked;

        /// <summary>
        /// 地图实际范围在纹理中的 UV 区间（方形纹理按长边等比覆盖，短边两侧为背景区）。
        /// </summary>
        public Vector2 MapUvMin => _uvMin;
        public Vector2 MapUvMax => _uvMax;

        private void Awake()
        {
            Instance = this;

            _mapTexture = new RenderTexture(TextureSize, TextureSize, 16)
            {
                name = "MinimapRT",
                antiAliasing = 1
            };

            var go = new GameObject("MinimapCamera");
            go.transform.SetParent(transform, false);
            // 北向朝上：从正上方垂直俯视，世界 +Z 对应纹理上方
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _mapCamera = go.AddComponent<Camera>();
            _mapCamera.orthographic = true;
            _mapCamera.targetTexture = _mapTexture;
            _mapCamera.clearFlags = CameraClearFlags.SolidColor;
            _mapCamera.backgroundColor = new Color(0.08f, 0.1f, 0.09f, 1f);
            // 剔除 UI 层，避免界面元素被小地图相机透视重渲
            _mapCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            _mapCamera.allowHDR = false;
            // 只在烘焙帧手动渲染一次，平时保持停用
            _mapCamera.enabled = false;
        }

        private void Start()
        {
            Bake();
        }

        /// <summary>
        /// 对整张地图做一次俯视烘焙。
        /// 以 <see cref="BattleBoundary"/> 的地面包围盒为地图范围；无边界时以玩家出生点为中心的兜底范围。
        /// </summary>
        private void Bake()
        {
            if (_baked) return;
            _baked = true;

            if (BattleBoundary.HasBounds)
            {
                Bounds b = BattleBoundary.Bounds;
                _mapCenter = b.center;
                _halfExtent = Mathf.Max(b.extents.x, b.extents.z);
            }
            else
            {
                var player = PlayerSystem.Instance?.GetPlayerEntity();
                Vector3 p = player != null ? player.transform.position : Vector3.zero;
                _mapCenter = new Vector3(p.x, 0f, p.z);
                _halfExtent = FallbackHalfExtent;
                Log.Warning("[MinimapSystem] 战斗边界未初始化，小地图使用兜底范围烘焙");
            }
            if (_halfExtent < 0.01f) _halfExtent = FallbackHalfExtent;

            _mapCamera.transform.position = new Vector3(_mapCenter.x, CameraHeight, _mapCenter.z);
            _mapCamera.orthographicSize = _halfExtent;

            // 无光照平面化烘焙：用内置 Unlit/Color 替换 shader 渲一次，
            // 材质按 _Color 平色输出，不带光影/阴影，呈"真地图"平面感。
            // 打包注意：Unlit/Color 需加入 Graphics Settings 的 Always Included Shaders。
            var flatShader = Shader.Find("Unlit/Color");
            if (flatShader != null)
            {
                _mapCamera.SetReplacementShader(flatShader, "");
            }
            else
            {
                Log.Warning("[MinimapSystem] 找不到 Unlit/Color，小地图回退为带光影烘焙");
            }
            _mapCamera.Render();
            if (flatShader != null)
            {
                _mapCamera.ResetReplacementShader();
            }

            // 地图实际范围对应的 UV 区间（显示窗口只在该区间内平移，永不露出场景外）
            if (BattleBoundary.HasBounds)
            {
                Bounds b = BattleBoundary.Bounds;
                _uvMin = WorldToMapUv(b.min);
                _uvMax = WorldToMapUv(b.max);
            }
            else
            {
                _uvMin = Vector2.zero;
                _uvMax = Vector2.one;
            }
        }

        /// <summary>
        /// 世界坐标 → 静态地图纹理 UV（可能超出 [0,1]，表示在地图范围外）。
        /// </summary>
        public Vector2 WorldToMapUv(Vector3 worldPos)
        {
            float inv = 0.5f / _halfExtent;
            return new Vector2(
                (worldPos.x - _mapCenter.x) * inv + 0.5f,
                (worldPos.z - _mapCenter.z) * inv + 0.5f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_mapCamera != null && _mapCamera.targetTexture != null)
            {
                _mapCamera.targetTexture = null;
            }
            if (_mapTexture != null)
            {
                _mapTexture.Release();
                Destroy(_mapTexture);
                _mapTexture = null;
            }
        }
    }
}
