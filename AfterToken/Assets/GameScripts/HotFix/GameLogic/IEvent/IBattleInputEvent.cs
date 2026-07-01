using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗输入事件接口。
    /// 输入系统发送，玩家/武器系统接收。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IBattleInputEvent
    {
        void OnMoveInput(Vector2 direction);
        void OnAimInput(Vector2 worldPosition);
        void OnFirePressed();
        void OnFireReleased();
        void OnAimPressed();
        void OnAimReleased();
        void OnReloadPressed();
        void OnWeaponSwitch(int delta);
        void OnWeaponWheelToggled(bool visible);
        void OnWeaponSelected(int slot);
        void OnDodgePressed();
        void OnCycleCrosshairStyle();
    }
}
