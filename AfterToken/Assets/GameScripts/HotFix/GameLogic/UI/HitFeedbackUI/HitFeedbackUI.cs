using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 命中反馈与受击方向指示 UI。
    /// 由 HitFeedbackSystem 驱动，负责显示命中标记和 8 方向受击指示。
    /// </summary>
    [Window(UILayer.System, location: "HitFeedbackUI", fullScreen: true)]
    public class HitFeedbackUI : UIWindow
    {
        #region 脚本工具生成的代码
        private Image[] _directionIndicators = new Image[8];
        private Image _hitMarker;

        protected override void ScriptGenerator()
        {
            for (int i = 0; i < 8; i++)
            {
                _directionIndicators[i] = FindChildComponent<Image>($"m_rect_IndicatorRoot/m_img_Indicator_{i}");
            }
            _hitMarker = FindChildComponent<Image>("m_rect_IndicatorRoot/m_img_HitMarker");
        }
        #endregion

        [Header("伤害指示器")]
        [SerializeField] private float _indicatorFadeSpeed = 2f;

        public static HitFeedbackUI Instance { get; private set; }

        private float _hitMarkerTimer;
        private float _hitMarkerDuration;

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            Instance = this;
            InitializeIndicators();
            Log.Debug($"[HitFeedbackUI] 节点绑定: HitMarker={_hitMarker != null}, Indicators={CountBoundIndicators()}");
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
            if (_hitMarker != null)
            {
                _hitMarker.color = new Color(1, 0, 0, 0);
            }
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

            // 命中标记淡出
            if (_hitMarker != null && _hitMarkerTimer < _hitMarkerDuration)
            {
                _hitMarkerTimer += Time.deltaTime;
                float alpha = 1 - (_hitMarkerTimer / _hitMarkerDuration);
                var color = _hitMarker.color;
                color.a = alpha;
                _hitMarker.color = color;
            }

            // 伤害指示器淡出
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
        /// 显示命中标记。
        /// </summary>
        public void ShowHitMarker(bool isCritical = false)
        {
            if (_hitMarker != null)
            {
                _hitMarker.color = isCritical ? new Color(1, 0.5f, 0, 1) : new Color(1, 0, 0, 1);
                _hitMarkerTimer = 0;
                _hitMarkerDuration = 0.15f;
            }
        }
    }
}
