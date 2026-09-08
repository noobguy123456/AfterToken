using TMPro;
using UnityEngine;

namespace GameLogic.Narrative
{
    /// <summary>
    /// 任务板实体（基地场景）。
    /// 挂载在场景中的任务板对象上，负责触发区检测与占位视觉（立式木牌 + 淡金色呼吸光圈）。
    /// 玩家进入触发区后按 E 查看可接任务列表（QuestBoardSystem 处理）。
    /// 任务板发布的任务 giverNpc=0（quest.csv 表头约定）。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class QuestBoardEntity : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;

        /// <summary>
        /// 占位牌子颜色（木棕色）。
        /// </summary>
        private static readonly Color BoardColor = new Color(0.55f, 0.38f, 0.2f, 1f);

        /// <summary>
        /// 占位支柱颜色（深木色）。
        /// </summary>
        private static readonly Color PostColor = new Color(0.35f, 0.24f, 0.13f, 1f);

        /// <summary>
        /// 交互光圈颜色（淡金色）。
        /// </summary>
        private static readonly Color GlowRingColor = new Color(1f, 0.88f, 0.55f, 0.85f);

        // 光圈呼吸参数（同 NoteEntity：缓慢轻微脉动，提示"可交互"又不抢戏）
        private const float GLOW_RING_DIAMETER = 1.8f;
        private const float GLOW_PULSE_PERIOD = 2.2f;
        private const float GLOW_SCALE_AMPLITUDE = 0.07f;
        private const float GLOW_ALPHA_BASE = 0.75f;
        private const float GLOW_ALPHA_AMPLITUDE = 0.2f;

        private SpriteRenderer _glowRingRenderer;
        private float _glowPulsePhase;

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

            EnsureVisual();
            EnsureGlowRing();
        }

        private void Start()
        {
            // 头顶名字牌文本走词条
            var labelGo = transform.Find("Visual/NameLabel");
            if (labelGo != null)
            {
                var label = labelGo.GetComponent<TextMeshPro>();
                if (label != null)
                {
                    label.text = Loc.Get("ui.questboard.title");
                }
            }
        }

        /// <summary>
        /// 光环呼吸脉动（同 NoteEntity）。
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

        /// <summary>
        /// 占位视觉：两根支柱 + 一块横板 + 头顶名字牌（Billboard）。
        /// 后续换正式模型时整体替换 Visual 子节点即可。
        /// </summary>
        private void EnsureVisual()
        {
            var visualRoot = _visualRoot != null ? _visualRoot : transform.Find("Visual");
            if (visualRoot == null)
            {
                var rootGo = new GameObject("Visual");
                rootGo.transform.SetParent(transform, false);
                visualRoot = rootGo.transform;
            }

            if (visualRoot.Find("Board") == null)
            {
                foreach (float x in new[] { -0.7f, 0.7f })
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = x < 0f ? "PostLeft" : "PostRight";
                    post.transform.SetParent(visualRoot, false);
                    post.transform.localPosition = new Vector3(x, 0.75f, 0f);
                    post.transform.localScale = new Vector3(0.12f, 1.5f, 0.12f);
                    StripPrimitiveCollider(post);
                    Tint(post, PostColor);
                }

                var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
                board.name = "Board";
                board.transform.SetParent(visualRoot, false);
                board.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                board.transform.localScale = new Vector3(1.7f, 1.0f, 0.1f);
                StripPrimitiveCollider(board);
                Tint(board, BoardColor);
            }

            if (visualRoot.Find("NameLabel") == null)
            {
                var labelGo = new GameObject("NameLabel");
                labelGo.transform.SetParent(visualRoot, false);
                labelGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
                var label = labelGo.AddComponent<TextMeshPro>();
                // 同 NpcEntity：小字号 + 缩放控制显示尺寸
                label.fontSize = 2f;
                labelGo.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(1f, 0.9f, 0.6f, 1f);
                labelGo.AddComponent<BillboardFaceCamera>();
            }
        }

        /// <summary>
        /// 占位图元自带碰撞体会挡住玩家移动与触发区检测，删掉。
        /// </summary>
        private static void StripPrimitiveCollider(GameObject primitive)
        {
            var col = primitive.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }

        private static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        /// <summary>
        /// 地面淡金色光圈：提示此物可交互（同 NoteEntity 范式，颜色换金）。
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
            QuestBoardSystem.Instance?.OnPlayerEnteredBoard(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInside = false;
            QuestBoardSystem.Instance?.OnPlayerExitedBoard(this);
        }
    }
}
