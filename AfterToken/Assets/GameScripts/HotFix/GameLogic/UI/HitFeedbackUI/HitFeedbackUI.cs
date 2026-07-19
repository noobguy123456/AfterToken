using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 命中反馈与受击方向指示 UI。
    /// 由 HitFeedbackSystem 驱动，负责显示命中标记和 8 方向受击指示。
    /// 属于战斗 HUD 层级（UILayer.UI），必须低于设置/死亡等弹窗（UILayer.Top），避免遮挡交互界面。
    /// </summary>
    [Window(UILayer.UI, location: "HitFeedbackUI", fullScreen: false)]
    public class HitFeedbackUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Image[] _directionIndicators = new Image[8];
        private Image _hitMarkerTemplate;
        private RectTransform _indicatorRoot;

        protected override void ScriptGenerator()
        {
            for (int i = 0; i < 8; i++)
            {
                _directionIndicators[i] = FindChildComponent<Image>($"m_rect_IndicatorRoot/m_img_Indicator_{i}");
            }
            _hitMarkerTemplate = FindChildComponent<Image>("m_rect_IndicatorRoot/m_img_HitMarker");
            _indicatorRoot = FindChildComponent<RectTransform>("m_rect_IndicatorRoot");
        }
        #endregion

        [Header("伤害指示器")]
        [SerializeField] private float _indicatorFadeSpeed = 2f;

        [Header("命中标记")]
        [SerializeField] private int _hitMarkerPoolSize = 8;
        [SerializeField] private float _hitMarkerDuration = 0.15f;
        [SerializeField] private float _hitMarkerSize = 32f;

        public static HitFeedbackUI Instance { get; private set; }

        private readonly Queue<Image> _hitMarkerPool = new Queue<Image>();
        private readonly List<ActiveHitMarker> _activeHitMarkers = new List<ActiveHitMarker>();

        private class ActiveHitMarker
        {
            public Image Image;
            public float Timer;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            Instance = this;
            InitializeIndicators();
            InitializeHitMarkerPool();
        }

        protected override void OnDestroy()
        {
            Instance = null;
            base.OnDestroy();
        }

        private void InitializeIndicators()
        {
            for (int i = 0; i < _directionIndicators.Length; i++)
            {
                if (_directionIndicators[i] != null)
                {
                    _directionIndicators[i].color = new Color(1, 0, 0, 0);
                }
            }
        }

        private void InitializeHitMarkerPool()
        {
            if (_hitMarkerTemplate == null || _indicatorRoot == null) return;
            _hitMarkerTemplate.gameObject.SetActive(false);

            for (int i = 0; i < _hitMarkerPoolSize; i++)
            {
                var marker = CreateHitMarker();
                if (marker != null) _hitMarkerPool.Enqueue(marker);
            }
        }

        private Image CreateHitMarker()
        {
            if (_hitMarkerTemplate == null || _indicatorRoot == null) return null;
            var marker = Object.Instantiate(_hitMarkerTemplate, _indicatorRoot, false);
            marker.gameObject.SetActive(false);
            marker.rectTransform.sizeDelta = new Vector2(_hitMarkerSize, _hitMarkerSize);
            return marker;
        }

        private int CountBoundIndicators()
        {
            int count = 0;
            for (int i = 0; i < _directionIndicators.Length; i++)
            {
                if (_directionIndicators[i] != null) count++;
            }
            return count;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UpdateHitMarkers();
            UpdateDamageIndicators();
        }

        private void UpdateHitMarkers()
        {
            float delta = Time.deltaTime;
            for (int i = _activeHitMarkers.Count - 1; i >= 0; i--)
            {
                var item = _activeHitMarkers[i];
                item.Timer += delta;
                float t = item.Timer / _hitMarkerDuration;
                if (t >= 1f)
                {
                    ReturnHitMarker(item.Image);
                    _activeHitMarkers.RemoveAt(i);
                    continue;
                }

                var c = item.Image.color;
                c.a = 1 - t;
                item.Image.color = c;
            }
        }

        private void UpdateDamageIndicators()
        {
            for (int i = 0; i < _directionIndicators.Length; i++)
            {
                var img = _directionIndicators[i];
                if (img == null) continue;

                var c = img.color;
                if (c.a > 0)
                {
                    c.a -= _indicatorFadeSpeed * Time.deltaTime;
                    if (c.a < 0) c.a = 0;
                    img.color = c;
                }
            }
        }

        private void ReturnHitMarker(Image marker)
        {
            if (marker == null) return;
            marker.gameObject.SetActive(false);
            _hitMarkerPool.Enqueue(marker);
        }

        /// <summary>
        /// 显示受击方向指示。
        /// </summary>
        /// <param name="angle">攻击来源角度，0-360，0 表示正前方。</param>
        public void ShowDamageIndicator(float angle)
        {
            int index = Mathf.RoundToInt(angle / 45f) % 8;
            if (index >= 0 && index < _directionIndicators.Length && _directionIndicators[index] != null)
            {
                _directionIndicators[index].color = new Color(1, 0, 0, 1);
            }
        }

        /// <summary>
        /// 在目标屏幕位置显示命中标记。
        /// </summary>
        /// <param name="isCritical">是否暴击/弱点。</param>
        /// <param name="screenPos">目标在屏幕上的位置。</param>
        public void ShowHitMarker(bool isCritical, Vector2 screenPos)
        {
            if (_indicatorRoot == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_indicatorRoot, screenPos, null, out var localPos)) return;

            Image marker;
            if (_hitMarkerPool.Count > 0)
            {
                marker = _hitMarkerPool.Dequeue();
            }
            else
            {
                marker = CreateHitMarker();
                if (marker == null) return;
            }

            marker.gameObject.SetActive(true);
            marker.color = isCritical ? new Color(1, 0.5f, 0, 1) : new Color(1, 0, 0, 1);
            marker.rectTransform.anchoredPosition = localPos;

            _activeHitMarkers.Add(new ActiveHitMarker
            {
                Image = marker,
                Timer = 0f,
            });
        }
    }
}
