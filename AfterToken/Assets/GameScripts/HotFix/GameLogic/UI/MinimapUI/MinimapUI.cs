using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 小地图 UI（传统 2D 平面缩略图样式）。
    /// 显示 <see cref="MinimapSystem"/> 烘焙的静态地图纹理——通过 RawImage.uvRect
    /// 平移一个以玩家为中心的观察窗口（钳制在地图范围内，永不显示场景外区域）；
    /// 玩家与敌人以平面图标投影在窗口内。
    /// 层级用 Tips：压过狙击镜（Top）的灰色蒙版，开镜时小地图不变灰。
    /// </summary>
    [Window(UILayer.Tips, location: "MinimapUI", fullScreen: false)]
    public class MinimapUI : UIWindow
    {
        /// <summary>
        /// 小地图面板边长（像素），需与 prefab 中 m_raw_Map 尺寸一致。
        /// </summary>
        private const float MapPanelSize = 220f;

        /// <summary>
        /// 大地图面板边长（像素，屏幕居中显示）。
        /// </summary>
        private const float BigMapPanelSize = 636f;

        /// <summary>
        /// 观察窗口占整幅地图的比例（0.45 ≈ 显示玩家周围约半张地图的跨度）。
        /// 大地图模式显示整幅地图。
        /// </summary>
        private const float WindowFraction = 0.45f;

        /// <summary>
        /// 大地图模式是否打开（InputSystem.IsMenuUIOpen 读取，用于打开时屏蔽射击/瞄准）。
        /// </summary>
        public static bool IsBigMapOpen { get; private set; }

        private bool _bigMapMode;
        private float _innerSize = MapPanelSize;
        private RectTransform _panelRect;

        private RawImage _mapImage;
        private RectTransform _iconRoot;
        private Image _enemyDotTemplate;
        private RectTransform _playerMarker;
        private GameObject _panelGo;

        private readonly List<Image> _enemyDotPool = new();
        private Image _companionDot;
        private Image _pingMarker;

        private static readonly Color CompanionDotColor = new Color(0.3f, 1f, 0.4f);
        private static readonly Color PingMarkerColor = new Color(1f, 0.6f, 0.1f);

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _mapImage = FindChildComponent<RawImage>("m_rect_Panel/m_raw_Map");
            _iconRoot = FindChildComponent<RectTransform>("m_rect_Panel/m_rect_IconRoot");
            _enemyDotTemplate = FindChildComponent<Image>("m_rect_Panel/m_rect_IconRoot/m_img_EnemyDot");
            _playerMarker = FindChildComponent<RectTransform>("m_rect_Panel/m_rect_IconRoot/m_img_Player");
            var panel = FindChild("m_rect_Panel");
            _panelGo = panel != null ? panel.gameObject : null;
            _panelRect = panel as RectTransform;
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            if (_enemyDotTemplate != null)
            {
                _enemyDotTemplate.gameObject.SetActive(false);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // 其它菜单类窗口（背包/开箱/纸条/设置）打开时隐藏小地图面板：
            // 小地图挂在 Tips 层压过狙击镜蒙版，若仍显示会盖住设置面板的按钮。
            // 大地图模式本身也算"菜单打开"（屏蔽射击），所以这里只看其它窗口
            bool windowMenuOpen = InputSystem.IsWindowMenuOpen();
            if (_bigMapMode && windowMenuOpen)
            {
                // 其它菜单压上来时自动退出大地图
                SetBigMapMode(false);
            }
            if (_panelGo != null && _panelGo.activeSelf == windowMenuOpen)
            {
                _panelGo.SetActive(!windowMenuOpen);
            }

            // M 键切换大地图模式（其它菜单打开时不响应）
            if (!windowMenuOpen && Input.GetKeyDown(KeyBindingSetting.GetKey(KeyBindAction.Map)))
            {
                SetBigMapMode(!_bigMapMode);
            }

            var minimap = MinimapSystem.Instance;
            if (minimap == null || !minimap.IsReady) return;

            // RenderTexture 为运行时对象，绑定一次即可
            if (_mapImage != null && _mapImage.texture == null && minimap.MapTexture != null)
            {
                _mapImage.texture = minimap.MapTexture;
            }

            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null) return;

            // 以玩家为中心的观察窗口，钳制在地图有效 UV 区间内
            Vector2 playerUv = minimap.WorldToMapUv(player.transform.position);
            Rect window = CalcWindow(minimap, playerUv, _bigMapMode);
            if (_mapImage != null)
            {
                _mapImage.uvRect = window;
            }

            // 玩家标记：窗口内投影（靠近地图边缘时不再居中，符合传统小地图行为）
            if (_playerMarker != null && TryMapUvToPanel(playerUv, window, _innerSize, out Vector2 playerPos))
            {
                _playerMarker.anchoredPosition = playerPos;
            }

            UpdateEnemyDots(minimap, window);
            UpdateCompanionDot(minimap, window);
            UpdatePingMarker(minimap, window);
        }

        /// <summary>
        /// 队友绿点（复用敌人圆点模板染色）。
        /// </summary>
        private void UpdateCompanionDot(MinimapSystem minimap, Rect window)
        {
            var companion = CompanionSystem.Instance != null ? CompanionSystem.Instance.Companion : null;
            if (companion == null || !companion.gameObject.activeInHierarchy || companion.IsDead)
            {
                if (_companionDot != null && _companionDot.gameObject.activeSelf)
                {
                    _companionDot.gameObject.SetActive(false);
                }
                return;
            }

            if (!TryMapUvToPanel(minimap.WorldToMapUv(companion.transform.position), window, _innerSize, out Vector2 pos))
            {
                if (_companionDot != null && _companionDot.gameObject.activeSelf)
                {
                    _companionDot.gameObject.SetActive(false);
                }
                return;
            }

            if (_companionDot == null)
            {
                _companionDot = Object.Instantiate(_enemyDotTemplate, _iconRoot);
                _companionDot.name = "m_img_CompanionDot";
                _companionDot.color = CompanionDotColor;
            }
            if (!_companionDot.gameObject.activeSelf)
            {
                _companionDot.gameObject.SetActive(true);
            }
            ((RectTransform)_companionDot.transform).anchoredPosition = pos;
        }

        /// <summary>
        /// 标点图标（橙色菱形：模板旋转 45°）。
        /// </summary>
        private void UpdatePingMarker(MinimapSystem minimap, Rect window)
        {
            var ping = PingSystem.Instance;
            if (ping == null || !ping.HasActivePing
                || !TryMapUvToPanel(minimap.WorldToMapUv(ping.ActivePingPos.ToWorld(0f)), window, _innerSize, out Vector2 pos))
            {
                if (_pingMarker != null && _pingMarker.gameObject.activeSelf)
                {
                    _pingMarker.gameObject.SetActive(false);
                }
                return;
            }

            if (_pingMarker == null)
            {
                _pingMarker = Object.Instantiate(_enemyDotTemplate, _iconRoot);
                _pingMarker.name = "m_img_PingMarker";
                _pingMarker.color = PingMarkerColor;
                _pingMarker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            if (!_pingMarker.gameObject.activeSelf)
            {
                _pingMarker.gameObject.SetActive(true);
            }
            ((RectTransform)_pingMarker.transform).anchoredPosition = pos;
        }

        /// <summary>
        /// 切换大地图模式：面板从右上角小图变成屏幕居中大地图，视野扩到整幅地图。
        /// </summary>
        private void SetBigMapMode(bool big)
        {
            if (_bigMapMode == big) return;
            _bigMapMode = big;
            IsBigMapOpen = big;

            if (_panelRect == null) return;
            if (big)
            {
                _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                _panelRect.pivot = new Vector2(0.5f, 0.5f);
                _panelRect.anchoredPosition = Vector2.zero;
                _panelRect.sizeDelta = new Vector2(BigMapPanelSize + 12f, BigMapPanelSize + 12f);
                _innerSize = BigMapPanelSize;
            }
            else
            {
                // 恢复 prefab 右上角布局
                _panelRect.anchorMin = _panelRect.anchorMax = Vector2.one;
                _panelRect.pivot = Vector2.one;
                _panelRect.anchoredPosition = new Vector2(-16f, -16f);
                _panelRect.sizeDelta = new Vector2(MapPanelSize + 12f, MapPanelSize + 12f);
                _innerSize = MapPanelSize;
            }
        }

        /// <summary>
        /// 计算以玩家为中心、钳制在地图范围内的观察窗口（UV 空间）。
        /// 大地图模式显示整幅地图。
        /// </summary>
        private static Rect CalcWindow(MinimapSystem minimap, Vector2 playerUv, bool bigMap)
        {
            Vector2 uvMin = minimap.MapUvMin;
            Vector2 uvMax = minimap.MapUvMax;
            float fraction = bigMap ? 1f : WindowFraction;
            float winW = Mathf.Min(fraction, uvMax.x - uvMin.x);
            float winH = Mathf.Min(fraction, uvMax.y - uvMin.y);
            float x = Mathf.Clamp(playerUv.x - winW * 0.5f, uvMin.x, uvMax.x - winW);
            float y = Mathf.Clamp(playerUv.y - winH * 0.5f, uvMin.y, uvMax.y - winH);
            return new Rect(x, y, winW, winH);
        }

        /// <summary>
        /// 地图 UV → 面板本地坐标。返回 false 表示在观察窗口外（应隐藏）。
        /// </summary>
        private static bool TryMapUvToPanel(Vector2 uv, Rect window, float panelSize, out Vector2 pos)
        {
            pos = default;
            if (uv.x < window.xMin || uv.x > window.xMax || uv.y < window.yMin || uv.y > window.yMax)
            {
                return false;
            }
            pos = (new Vector2((uv.x - window.x) / window.width, (uv.y - window.y) / window.height)
                   - new Vector2(0.5f, 0.5f)) * panelSize;
            return true;
        }

        /// <summary>
        /// 按敌人注册表维护红点池并刷新位置。
        /// </summary>
        private void UpdateEnemyDots(MinimapSystem minimap, Rect window)
        {
            if (_enemyDotTemplate == null || _iconRoot == null) return;

            int index = 0;
            foreach (var enemy in EnemyRegistry.All)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                if (!TryMapUvToPanel(minimap.WorldToMapUv(enemy.transform.position), window, _innerSize, out Vector2 pos)) continue;

                Image dot = GetDot(index++);
                if (dot == null) break;

                var rt = (RectTransform)dot.transform;
                rt.anchoredPosition = pos;
            }

            // 隐藏多余红点
            for (int i = index; i < _enemyDotPool.Count; i++)
            {
                if (_enemyDotPool[i].gameObject.activeSelf)
                {
                    _enemyDotPool[i].gameObject.SetActive(false);
                }
            }
        }

        private Image GetDot(int index)
        {
            while (_enemyDotPool.Count <= index)
            {
                var dot = Object.Instantiate(_enemyDotTemplate, _iconRoot);
                dot.gameObject.SetActive(true);
                _enemyDotPool.Add(dot);
            }

            var img = _enemyDotPool[index];
            if (!img.gameObject.activeSelf)
            {
                img.gameObject.SetActive(true);
            }
            return img;
        }

        protected override void OnDestroy()
        {
            // 静态状态必须在窗口销毁时复位，否则离开战斗后 IsMenuUIOpen 恒为 true
            _bigMapMode = false;
            IsBigMapOpen = false;
            base.OnDestroy();
        }
    }
}
