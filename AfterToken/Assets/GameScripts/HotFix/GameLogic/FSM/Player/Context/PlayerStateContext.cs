using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家状态机黑板。
    /// 承载输入、意图与运行时状态，供输入系统、状态机、武器系统等共享。
    /// 本类是玩家运行期实时状态（死亡/闪避/换弹/瞄准/输入）的唯一数据源：
    /// PlayerEntity、WeaponSystem 等处的同名属性均为转发或本地兜底，不持有副本。
    /// 血量/体力归 PlayerSystem，弹药归 WeaponInstance；跨场景连续性由 PlayerAttrStore
    /// 变动即存承担（PortalPlayerState 只记录场景上下文），均不在此重复。
    /// </summary>
    public class PlayerStateContext
    {
        #region 输入（由 InputSystem 每帧写入）

        /// <summary>
        /// 移动输入方向。
        /// </summary>
        public Vector2 MoveInput;

        /// <summary>
        /// 瞄准位置（世界坐标）。
        /// </summary>
        public Vector2 AimInput;

        /// <summary>
        /// 本帧是否按下开火。
        /// </summary>
        public bool FirePressed;

        /// <summary>
        /// 本帧是否按下换弹。
        /// </summary>
        public bool ReloadPressed;

        /// <summary>
        /// 本帧是否按下闪避。
        /// </summary>
        public bool DodgePressed;

        #endregion

        #region 意图（每帧清理，由输入或系统写入）

        /// <summary>
        /// 是否想闪避。
        /// </summary>
        public bool WantsToDodge;

        /// <summary>
        /// 是否想换弹。
        /// </summary>
        public bool WantsToReload;

        /// <summary>
        /// 是否想开火。
        /// </summary>
        public bool WantsToFire;

        #endregion

        #region 运行时状态（由各系统维护）

        /// <summary>
        /// 是否死亡。
        /// </summary>
        public bool IsDead;

        /// <summary>
        /// 是否正在换弹（玩家 FSM 语义：处于 PlayerReloadState）。
        /// 与 WeaponInstance.IsReloading（武器换弹计时器）是两个概念：
        /// 武器打空自动换弹时后者为 true 而本字段未必为 true，二者互不转发。
        /// </summary>
        public bool IsReloading;

        /// <summary>
        /// 是否正在闪避。
        /// </summary>
        public bool IsDodging;

        /// <summary>
        /// 是否正在瞄准。由 WeaponSystem.SetAimState 写入（WeaponSystem.IsAiming 转发到此处），
        /// AimAssistSystem 等读取方经 WeaponSystem.IsAiming 访问。
        /// </summary>
        public bool IsAiming;

        /// <summary>
        /// 当前武器实例。
        /// </summary>
        public WeaponInstance CurrentWeapon;

        /// <summary>
        /// 是否可以移动。
        /// </summary>
        public bool CanMove => !IsDead && !IsDodging;

        /// <summary>
        /// 是否可以闪避。
        /// </summary>
        public bool CanDodge;

        /// <summary>
        /// 是否可以开火。
        /// </summary>
        public bool CanFire => !IsDead && !IsReloading && !IsDodging;

        #endregion

        #region 状态切换请求

        /// <summary>
        /// 待处理的状态切换请求。由状态写入，PlayerStateMachineDriver 统一消费。
        /// </summary>
        public StateTransitionRequest PendingRequest { get; set; }

        #endregion

        /// <summary>
        /// 清理本帧意图，避免遗留到下一帧。
        /// </summary>
        public void ResetIntent()
        {
            WantsToDodge = false;
            WantsToReload = false;
            WantsToFire = false;
        }
    }
}
