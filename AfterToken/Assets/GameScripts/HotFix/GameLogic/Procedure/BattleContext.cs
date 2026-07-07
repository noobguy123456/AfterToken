namespace GameLogic
{
    /// <summary>
    /// 战斗上下文（临时，用于 Procedure 与 UI 之间传递关卡信息）。
    /// </summary>
    public static class BattleContext
    {
        /// <summary>
        /// 当前选中的关卡 ID。
        /// </summary>
        public static int CurrentLevelId { get; set; } = 1;

        /// <summary>
        /// 自定义目标场景名。
        /// 当需要从传送门等入口直接指定场景名时使用，优先级高于 CurrentLevelId。
        /// </summary>
        public static string CustomSceneName { get; set; }

        /// <summary>
        /// 清除所有上下文。
        /// </summary>
        public static void Clear()
        {
            CurrentLevelId = 1;
            CustomSceneName = null;
        }
    }
}
