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

            // 玩法平面坐标 (x, z) 写回世界坐标，弹丸视觉高度 0.5
            transform.position = Data.Position.ToWorld(0.5f);

            if (Data.Direction.sqrMagnitude > 0.001f)
            {
                transform.forward = Data.Direction.ToWorld();
            }

            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;

            GameEvent.Get<IProjectileEvent>().OnProjectileHit(Data.Id, other.gameObject);
        }
    }
}
