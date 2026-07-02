using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家事件接口。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPlayerEvent
    {
        void OnPlayerCreated(Vector3 position);
        void OnPlayerPositionChanged(Vector3 position);
        void OnPlayerStateChanged(string stateName, string prevStateName);
        void OnHpChanged(int currentHp, int maxHp);
        void OnStaminaChanged(int currentStamina, int maxStamina);
        void OnAmmoChanged(int currentAmmo, int maxAmmo);
        void OnPlayerDamaged(int damage, Vector2 hitDirection);
        void OnPlayerDied();
    }
}
