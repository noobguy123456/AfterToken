using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 经营场景玩家控制器：WASD 移动角色（XZ 平面）、面向移动方向、限制在地面范围内。
    /// 移速读取 TbPlayer（与战斗同源），相机由 SimulationCameraController 跟随。
    /// </summary>
    public class SimulationPlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private int _playerConfigId = 1;

        private float _moveSpeed = 5f;
        private Rigidbody _rb;

        // 地面为 50x50，边缘留 1 米
        private const float GROUND_HALF_SIZE = 24f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            LoadMoveSpeed();
        }

        /// <summary>
        /// 从 TbPlayer 读取移速（读取失败保留默认值）。
        /// </summary>
        private void LoadMoveSpeed()
        {
            try
            {
                var cfg = ConfigSystem.Instance.Tables.TbPlayer.GetOrDefault(_playerConfigId);
                if (cfg != null && cfg.MoveSpeed > 0f)
                {
                    _moveSpeed = cfg.MoveSpeed;
                }
            }
            catch (System.Exception e)
            {
                Log.Warning($"[SimulationPlayerController] 读取玩家配置 {_playerConfigId} 失败，使用默认移速: {e.Message}");
            }
        }

        private void FixedUpdate()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            var dir = new Vector3(h, 0f, v);
            if (dir.sqrMagnitude > 1f)
            {
                dir.Normalize();
            }

            if (_rb != null)
            {
                _rb.linearVelocity = dir * _moveSpeed;

                // 边界钳制：只有确实越界时才回写，避免打断插值
                Vector3 clamped = _rb.position;
                clamped.x = Mathf.Clamp(clamped.x, -GROUND_HALF_SIZE, GROUND_HALF_SIZE);
                clamped.z = Mathf.Clamp(clamped.z, -GROUND_HALF_SIZE, GROUND_HALF_SIZE);
                if (clamped != _rb.position)
                {
                    _rb.position = clamped;
                }
            }
            else
            {
                Vector3 pos = transform.position + dir * (_moveSpeed * Time.fixedDeltaTime);
                pos.x = Mathf.Clamp(pos.x, -GROUND_HALF_SIZE, GROUND_HALF_SIZE);
                pos.z = Mathf.Clamp(pos.z, -GROUND_HALF_SIZE, GROUND_HALF_SIZE);
                transform.position = pos;
            }

            // 面向移动方向
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
