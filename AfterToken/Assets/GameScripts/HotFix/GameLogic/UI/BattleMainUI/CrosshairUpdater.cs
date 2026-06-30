using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 准星更新器。
    /// 作为 MonoBehaviour 挂载在准星节点上，确保即使 BattleMainUI 被 UI 栈隐藏时，
    /// 准星位置仍能被刷新并跟随鼠标指针。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CrosshairUpdater : MonoBehaviour
    {
        [SerializeField] private RectTransform _crosshair;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private BattleMainUI _owner;

        private void Awake()
        {
            if (_crosshair == null) _crosshair = transform as RectTransform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        }

        public void Initialize(BattleMainUI owner, RectTransform crosshair, Canvas canvas)
        {
            _owner = owner;
            _crosshair = crosshair;
            _canvas = canvas;
        }

        private void Update()
        {
            UpdatePosition();
            UpdateRotation();
            HandleStyleSwitch();
        }

        /// <summary>
        /// 更新准星位置到鼠标指针位置。
        /// </summary>
        private void UpdatePosition()
        {
            if (_crosshair == null) return;

            var parent = _crosshair.parent as RectTransform;
            if (parent == null) return;

            Camera cam = _canvas != null ? _canvas.worldCamera : null;
            Vector2 screenPos = Input.mousePosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPos, cam, out Vector2 localPos))
            {
                _crosshair.anchoredPosition = localPos;
            }
        }

        /// <summary>
        /// 换弹期间让转圈准星持续旋转。
        /// </summary>
        private void UpdateRotation()
        {
            if (_crosshair == null || _owner == null) return;

            if (_owner.IsReloading)
            {
                _crosshair.Rotate(0f, 0f, -_owner.ReloadingSpinSpeed * Time.deltaTime);
            }
            else
            {
                _crosshair.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 按 C 键循环切换准星样式。
        /// </summary>
        private void HandleStyleSwitch()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                _owner?.CycleCrosshairStyle();
            }
        }
    }
}
