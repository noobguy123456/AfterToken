using UnityEngine;

namespace GameLogic.Narrative
{
    /// <summary>
    /// 叙事小纸条实体（场景 collectible）。
    /// 挂载在场景中的纸条对象上，负责触发区检测与占位视觉。
    /// 玩家进入触发区后按 E 阅读（NoteSystem 处理），可重复阅读。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class NoteEntity : MonoBehaviour
    {
        [SerializeField] private int _noteId = 1;
        [SerializeField] private SpriteRenderer _visualRenderer;

        /// <summary>
        /// 纸条底色（纸白色）。
        /// </summary>
        private static readonly Color PaperColor = new Color(0.92f, 0.88f, 0.75f, 1f);

        /// <summary>
        /// 交互光圈颜色（乳白色）。
        /// </summary>
        private static readonly Color GlowRingColor = new Color(1f, 0.97f, 0.87f, 0.85f);

        // 光圈呼吸参数：缓慢轻微的缩放+透明度脉动，提示"可交互"又不抢戏
        private const float GLOW_RING_DIAMETER = 1.2f;
        private const float GLOW_PULSE_PERIOD = 2.2f;
        private const float GLOW_SCALE_AMPLITUDE = 0.07f;
        private const float GLOW_ALPHA_BASE = 0.75f;
        private const float GLOW_ALPHA_AMPLITUDE = 0.2f;

        private SpriteRenderer _glowRingRenderer;
        private float _glowPulsePhase;

        public int NoteId => _noteId;
        public bool PlayerInside { get; private set; }

        private void Awake()
        {
            var collider = GetComponent<BoxCollider>();
            collider.isTrigger = true;
            if (collider.size == Vector3.one)
            {
                // 默认 1m 立方体触发区太小，放宽到 2m 便于交互
                collider.size = new Vector3(2f, 2f, 2f);
            }

            EnsureVisualRenderer();
            EnsureGlowRing();
        }

        /// <summary>
        /// 光环呼吸脉动：每个纸条随机相位，避免多个纸条同步闪烁的机械感。
        /// </summary>
        private void Update()
        {
            if (_glowRingRenderer == null) return;

            _glowPulsePhase += Time.deltaTime * (Mathf.PI * 2f / GLOW_PULSE_PERIOD);
            float wave = Mathf.Sin(_glowPulsePhase) * 0.5f + 0.5f; // 0~1

            float scale = GLOW_RING_DIAMETER * (1f + (wave - 0.5f) * 2f * GLOW_SCALE_AMPLITUDE);
            _glowRingRenderer.transform.localScale = new Vector3(scale, scale, 1f);

            var color = GlowRingColor;
            color.a = GLOW_ALPHA_BASE + (wave - 0.5f) * 2f * GLOW_ALPHA_AMPLITUDE;
            _glowRingRenderer.color = color;
        }

        private void EnsureVisualRenderer()
        {
            if (_visualRenderer != null) return;
            _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_visualRenderer != null) return;

            // 占位视觉：0.4m 平铺纸块（X+90° 平躺渲染）
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            visualGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visualGo.transform.localScale = new Vector3(0.5f, 0.35f, 1f);
            // 抬高 5cm，避免与地面（y=0）共面 z-fighting 闪烁
            visualGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _visualRenderer = visualGo.AddComponent<SpriteRenderer>();
            _visualRenderer.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            _visualRenderer.color = PaperColor;
            _visualRenderer.sortingOrder = 1;
        }

        /// <summary>
        /// 地面乳白色光圈：提示此物可交互。平躺在纸块下方（y 更低避免共面闪烁），
        /// 呼吸脉动在 <see cref="Update"/> 中驱动。
        /// </summary>
        private void EnsureGlowRing()
        {
            if (_glowRingRenderer != null) return;
            var existing = transform.Find("GlowRing");
            if (existing != null)
            {
                _glowRingRenderer = existing.GetComponent<SpriteRenderer>();
                if (_glowRingRenderer != null) return;
            }

            var ringGo = new GameObject("GlowRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ringGo.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ringGo.transform.localScale = new Vector3(GLOW_RING_DIAMETER, GLOW_RING_DIAMETER, 1f);
            _glowRingRenderer = ringGo.AddComponent<SpriteRenderer>();
            _glowRingRenderer.sprite = PlaceholderSpriteProvider.GetRingSprite64();
            _glowRingRenderer.color = GlowRingColor;
            _glowRingRenderer.sortingOrder = 0;
            // 随机初始相位
            _glowPulsePhase = (GetInstanceID() % 100) * 0.0628f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = true;
            NoteSystem.Instance?.OnPlayerEnteredNote(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = false;
            NoteSystem.Instance?.OnPlayerExitedNote(this);
        }
    }
}
