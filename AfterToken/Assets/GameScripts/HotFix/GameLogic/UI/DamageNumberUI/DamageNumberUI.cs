using System.Collections.Generic;
using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 伤害数字飘字 UI。
    /// 作为全屏 UI 打开后，通过静态 Show 方法在命中位置生成飘字。
    /// </summary>
    [Window(UILayer.UI, location: "DamageNumberUI")]
    public class DamageNumberUI : UIWindow
    {
        #region 脚本工具生成的代码
        private TextMeshProUGUI _textTemplate;
        private RectTransform _rootRect;

        protected override void ScriptGenerator()
        {
            _textTemplate = FindChildComponent<TextMeshProUGUI>("m_text_Template");
            _rootRect = rectTransform;
        }
        #endregion

        [Header("飘字动画")]
        private int _poolSize;
        private float _fadeDuration;
        private float _moveOffset;
        private Color _normalColor;
        private Color _criticalColor;
        private int _normalFontSize;
        private int _criticalFontSize;

        private static DamageNumberUI _instance;
        private readonly Queue<TextMeshProUGUI> _pool = new Queue<TextMeshProUGUI>();
        private readonly List<ActiveNumber> _activeNumbers = new List<ActiveNumber>();

        // struct 避免每个飘字产生一次堆分配；列表元素修改后需写回（见 UpdateNumbers）。
        private struct ActiveNumber
        {
            public TextMeshProUGUI Text;
            public float Timer;
            public Vector2 StartPos;
        }

        private const int MAX_CACHED_DAMAGE = 999;
        private static readonly string[] _damageTextCache = new string[MAX_CACHED_DAMAGE + 1];

        /// <summary>
        /// 获取伤害数值文本。常见数值走缓存，避免每次命中都产生字符串分配。
        /// </summary>
        private static string GetDamageText(int damage)
        {
            if ((uint)damage <= MAX_CACHED_DAMAGE)
            {
                return _damageTextCache[damage] ??= damage.ToString();
            }
            return damage.ToString();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            LoadUiConfig();
            FixFullScreenCanvas();
            _instance = this;
            InitializePool();
        }

        private void LoadUiConfig()
        {
            try
            {
                var uiConfig = ConfigSystem.Instance?.Tables?.TbUiConfig?.GetOrDefault(1);
                if (uiConfig != null)
                {
                    _poolSize = uiConfig.DamageNumberPoolSize;
                    _fadeDuration = uiConfig.DamageNumberFadeDuration;
                    _moveOffset = uiConfig.DamageNumberMoveOffset;
                    _normalColor = ToUnityColor(uiConfig.DamageNumberNormalColor);
                    _criticalColor = ToUnityColor(uiConfig.DamageNumberCriticalColor);
                    _normalFontSize = uiConfig.DamageNumberNormalFontSize;
                    _criticalFontSize = uiConfig.DamageNumberCriticalFontSize;
                    return;
                }
            }
            catch
            {
                // ignored
            }

            // 配置缺失时的兜底值。
            _poolSize = 16;
            _fadeDuration = 0.8f;
            _moveOffset = 40f;
            _normalColor = Color.white;
            _criticalColor = new Color(1, 0.5f, 0, 1);
            _normalFontSize = 24;
            _criticalFontSize = 32;
        }

        private static Color ToUnityColor(GameConfig.cfg.Color c)
        {
            if (c == null) return Color.white;
            return new Color(c.R, c.G, c.B, c.A);
        }

        protected override void OnDestroy()
        {
            _instance = null;
            base.OnDestroy();
        }

        private void InitializePool()
        {
            if (_textTemplate == null) return;
            _textTemplate.gameObject.SetActive(false);
            for (int i = 0; i < _poolSize; i++)
            {
                var txt = Object.Instantiate(_textTemplate, _rootRect, false);
                txt.gameObject.SetActive(false);
                _pool.Enqueue(txt);
            }
        }

        /// <summary>
        /// 显示伤害数字。
        /// </summary>
        public static void Show(int damage, Vector2 screenPos, bool isCritical = false)
        {
            _instance?.ShowInternal(damage, screenPos, isCritical);
        }

        /// <summary>
        /// 在指定父节点的局部坐标处显示伤害数字（狙击镜镜窗内飘字用）。
        /// 与 Show 共用对象池与飘字动画，仅父节点与坐标系不同。
        /// </summary>
        public static void ShowLocal(RectTransform parent, int damage, Vector2 localPos, bool isCritical = false)
        {
            if (parent == null) return;
            _instance?.SpawnNumber(parent, damage, localPos, isCritical);
        }

        private void ShowInternal(int damage, Vector2 screenPos, bool isCritical)
        {
            if (_textTemplate == null || _rootRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRect, screenPos, null, out var localPos)) return;
            SpawnNumber(_rootRect, damage, localPos, isCritical);
        }

        private void SpawnNumber(RectTransform parent, int damage, Vector2 localPos, bool isCritical)
        {
            if (_textTemplate == null || parent == null) return;

            TextMeshProUGUI txt;
            if (_pool.Count > 0)
            {
                txt = _pool.Dequeue();
                // 池内节点默认挂在 _rootRect 下，镜内飘字需换父到镜窗容器
                txt.transform.SetParent(parent, false);
            }
            else
            {
                txt = Object.Instantiate(_textTemplate, parent, false);
            }

            txt.gameObject.SetActive(true);
            txt.text = GetDamageText(damage);
            txt.color = isCritical ? _criticalColor : _normalColor;
            txt.fontSize = isCritical ? _criticalFontSize : _normalFontSize;

            var rt = txt.rectTransform;
            rt.anchoredPosition = localPos;
            rt.localScale = Vector3.one;

            _activeNumbers.Add(new ActiveNumber
            {
                Text = txt,
                Timer = 0f,
                StartPos = localPos,
            });
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UpdateNumbers(Time.deltaTime);
        }

        internal void UpdateNumbers(float delta)
        {
            for (int i = _activeNumbers.Count - 1; i >= 0; i--)
            {
                var item = _activeNumbers[i];
                // 镜内飘字会随狙击镜窗口一起销毁（关镜时），此处剔除已销毁的条目，
                // 避免访问已销毁的 TextMeshProUGUI 抛 MissingReferenceException
                if (item.Text == null)
                {
                    _activeNumbers.RemoveAt(i);
                    continue;
                }
                item.Timer += delta;
                float t = item.Timer / _fadeDuration;
                if (t >= 1f)
                {
                    ReturnToPool(item.Text);
                    _activeNumbers.RemoveAt(i);
                    continue;
                }

                var rt = item.Text.rectTransform;
                rt.anchoredPosition = item.StartPos + Vector2.up * _moveOffset * t;

                var c = item.Text.color;
                c.a = 1 - t;
                item.Text.color = c;

                // ActiveNumber 为 struct，修改 Timer 后必须写回列表，否则飘字永不结束。
                _activeNumbers[i] = item;
            }
        }

        /// <summary>
        /// 外部驱动飘字更新（狙击镜打开时本窗口被框架隐藏、OnUpdate 停走，
        /// 由 SniperScopeUI.OnUpdate 代调；窗口可见时跳过避免双重推进）。
        /// </summary>
        public static void TickExternal(float delta)
        {
            if (_instance != null && !_instance.Visible)
            {
                _instance.UpdateNumbers(delta);
            }
        }

        private void ReturnToPool(TextMeshProUGUI txt)
        {
            // 镜内飘字用完后归位到本窗口根节点，保持池内节点父节点一致
            if (_rootRect != null && txt.transform.parent != _rootRect)
            {
                txt.transform.SetParent(_rootRect, false);
            }
            txt.gameObject.SetActive(false);
            _pool.Enqueue(txt);
        }
    }
}
