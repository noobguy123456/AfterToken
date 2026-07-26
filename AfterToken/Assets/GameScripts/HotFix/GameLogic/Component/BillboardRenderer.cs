using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 2D人物 Billboard 渲染。
    /// 使2D人物始终面向相机。
    /// </summary>
    public class BillboardRenderer : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                // 使2D人物始终面向相机
                transform.LookAt(_mainCamera.transform);
                transform.Rotate(0f, 180f, 0f); // 翻转，使文本正面朝向相机
            }
        }
    }
}
