using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 挂在 UI Prefab 上，用于在 Inspector 中配置该 UI 打开时的时间缩放。
    /// 0 表示完全暂停，0.2 表示像武器轮盘一样的慢动作，1 表示不影响时间。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIWindowTimeScale : MonoBehaviour
    {
        [Tooltip("该 UI 可见时希望的时间缩放。0=暂停，1=正常，0.2=慢动作。")]
        [SerializeField] [Range(0f, 1f)]
        private float _timeScaleWhenVisible = 1f;

        public float TimeScaleWhenVisible => _timeScaleWhenVisible;
    }
}
