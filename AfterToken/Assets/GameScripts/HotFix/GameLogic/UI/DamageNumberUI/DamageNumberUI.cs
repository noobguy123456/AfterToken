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
        [SerializeField] private int _poolSize = 16;
        [SerializeField] private float _fadeDuration = 0.8f;
        [SerializeField] private float _moveOffset = 40f;

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
            FixFullScreenCanvas();
            _instance = this;
            InitializePool();
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

        private void ShowInternal(int damage, Vector2 screenPos, bool isCritical)
        {
            if (_textTemplate == null || _rootRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRect, screenPos, null, out var localPos)) return;

            TextMeshProUGUI txt;
            if (_pool.Count > 0)
            {
                txt = _pool.Dequeue();
            }
            else
            {
                txt = Object.Instantiate(_textTemplate, _rootRect, false);
            }

            txt.gameObject.SetActive(true);
            txt.text = GetDamageText(damage);
            txt.color = isCritical ? new Color(1, 0.5f, 0, 1) : Color.white;
            txt.fontSize = isCritical ? 32 : 24;

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

        private void ReturnToPool(TextMeshProUGUI txt)
        {
            txt.gameObject.SetActive(false);
            _pool.Enqueue(txt);
        }
    }
}
