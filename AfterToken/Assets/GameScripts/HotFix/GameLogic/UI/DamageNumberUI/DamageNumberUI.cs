using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

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
        private Text _textTemplate;
        private RectTransform _rootRect;

        protected override void ScriptGenerator()
        {
            _textTemplate = FindChildComponent<Text>("m_text_Template");
            _rootRect = rectTransform;
        }
        #endregion

        [Header("飘字动画")]
        [SerializeField] private int _poolSize = 16;
        [SerializeField] private float _fadeDuration = 0.8f;
        [SerializeField] private float _moveOffset = 40f;

        private static DamageNumberUI _instance;
        private readonly Queue<Text> _pool = new Queue<Text>();
        private readonly List<ActiveNumber> _activeNumbers = new List<ActiveNumber>();

        private class ActiveNumber
        {
            public Text Text;
            public float Timer;
            public Vector2 StartPos;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            _instance = this;
            // 兜底：无论Prefab中模板是否隐藏，都确保模板节点 inactive。
            var templateTransform = rectTransform?.Find("m_text_Template");
            templateTransform?.gameObject.SetActive(false);
            InitializePool();
            // 用独立 MonoBehaviour 驱动飘字动画，避免 UIWindow.OnUpdate 被 UI 栈显隐规则中断。
            var updater = gameObject.GetComponent<DamageNumberUpdater>();
            if (updater == null) updater = gameObject.AddComponent<DamageNumberUpdater>();
            updater.Owner = this;
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

            Text txt;
            if (_pool.Count > 0)
            {
                txt = _pool.Dequeue();
            }
            else
            {
                txt = Object.Instantiate(_textTemplate, _rootRect, false);
            }

            txt.gameObject.SetActive(true);
            txt.text = damage.ToString();
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
            }
        }

        private void ReturnToPool(Text txt)
        {
            txt.gameObject.SetActive(false);
            _pool.Enqueue(txt);
        }

        /// <summary>
        /// 独立驱动飘字动画的 MonoBehaviour，避免被 TEngine UI 栈显隐中断。
        /// </summary>
        private class DamageNumberUpdater : MonoBehaviour
        {
            public DamageNumberUI Owner;

            private void Update()
            {
                Owner?.UpdateNumbers(Time.deltaTime);
            }
        }
    }
}
