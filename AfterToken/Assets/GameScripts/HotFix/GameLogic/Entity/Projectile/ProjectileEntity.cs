using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 子弹实体。
    /// 负责显示和碰撞回调转发。
    /// </summary>
    public class ProjectileEntity : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public ProjectileData Data { get; private set; }
        public bool IsActive => Data != null && Data.IsActive;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(ProjectileData data)
        {
            Data = data;
            UpdateVisual();
        }

        public void OnRecycle()
        {
            Data = null;
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        }

        public void UpdateVisual()
        {
            if (Data == null) return;

            transform.position = Data.Position;

            if (Data.Direction.sqrMagnitude > 0.001f)
            {
                transform.up = (Vector3)Data.Direction;
            }

            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive) return;

            GameEvent.Get<IProjectileEvent>().OnProjectileHit(Data.Id, other.gameObject);
        }
    }
}
