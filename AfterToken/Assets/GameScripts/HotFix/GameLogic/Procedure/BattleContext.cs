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
    }
}
