using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器所有者接口。
    /// 将 WeaponSystem 与 PlayerSystem 解耦，允许任何实体作为武器操控者。
    /// </summary>
    public interface IWeaponOwner
    {
        /// <summary>
        /// 所有者实例 ID。
        /// </summary>
        int OwnerId { get; }

        /// <summary>
        /// 当前世界位置。
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// 当前瞄准位置（世界坐标）。
        /// </summary>
        Vector2 AimPosition { get; }

        /// <summary>
        /// 当前移动方向。
        /// </summary>
        Vector2 MoveDirection { get; }

        /// <summary>
        /// 是否正在移动。
        /// </summary>
        bool IsMoving { get; }
    }
}
