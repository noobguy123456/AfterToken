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
