using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IWeaponEvent
    {
        void OnWeaponEquipped(int ownerId, int slot, int weaponConfigId);
        void OnStartFire(int ownerId);
        void OnStopFire(int ownerId);
        void OnReload(int ownerId);
        void OnReloadStateChanged(int ownerId, bool isReloading);
        void OnWeaponSwitched(int ownerId, int slot);
        void OnAimStateChanged(int ownerId, bool isAiming);
        void OnFire(Vector2 origin, Vector2 direction, int weaponConfigId, int ownerId);
    }
}
