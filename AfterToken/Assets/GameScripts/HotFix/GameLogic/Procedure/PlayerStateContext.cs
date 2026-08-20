using UnityEngine;

namespace GameLogic.Portal
{
    /// <summary>
    /// 传送门场景上下文。
    /// 职责边界：只记录玩家经传送门前往的场景，以及到达场景是否保留战斗属性；
    /// 玩家属性（血量/体力/武器弹药）不在此快照——属性由 PlayerAttrStore 变动即存，
    /// 传送门触发时无需也不应捕获属性。
    /// </summary>
    public static class PortalPlayerState
    {
        /// <summary>
        /// 是否有转场记录。
        /// </summary>
        public static bool HasRecord { get; private set; }

        /// <summary>
        /// 目标关卡 ID（无目标关卡时为 0）。
        /// </summary>
        public static int TargetLevelId { get; private set; }

        /// <summary>
        /// 目标场景名（无目标场景时为空）。
        /// </summary>
        public static string TargetSceneName { get; private set; }

        /// <summary>
        /// 到达目标场景后是否恢复 PlayerAttrStore 中的战斗属性（由 portal 配置的 keepPlayerState 决定）。
        /// </summary>
        public static bool CarryPlayerState { get; private set; }

        /// <summary>
        /// 记录一次传送门转场（仅场景信息）。
        /// </summary>
        public static void RecordTransition(int targetLevelId, string targetSceneName, bool carryPlayerState)
        {
            TargetLevelId = targetLevelId;
            TargetSceneName = targetSceneName;
            CarryPlayerState = carryPlayerState;
            HasRecord = true;
        }

        /// <summary>
        /// 清除转场记录（一局结束/转场中止时调用）。
        /// </summary>
        public static void Clear()
        {
            HasRecord = false;
            TargetLevelId = 0;
            TargetSceneName = null;
            CarryPlayerState = false;
        }
    }

    /// <summary>
    /// 武器状态数据（PlayerAttrStore 的存储单元）。
    /// </summary>
    public struct WeaponStateData
    {
        public static WeaponStateData Empty => new WeaponStateData { ConfigId = 0, CurrentAmmo = 0 };

        public int ConfigId;
        public int CurrentAmmo;

        public bool IsValid => ConfigId > 0;
    }
}
