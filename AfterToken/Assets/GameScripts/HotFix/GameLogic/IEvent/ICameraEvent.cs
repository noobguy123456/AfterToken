using TEngine;
namespace GameLogic
{
    /// <summary>
    /// 相机事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ICameraEvent
    {
        void OnCameraShake(float magnitude, float duration);
        void OnAimFovChanged(float targetFov);
    }
}
