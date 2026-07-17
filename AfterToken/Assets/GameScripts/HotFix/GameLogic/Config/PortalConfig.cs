namespace GameLogic
{
    /// <summary>
    /// 传送门配置（对 Luban 生成配置的运行时适配）。
    /// </summary>
    public class PortalConfig
    {
        /// <summary>
        /// 传送门配置 ID。
        /// </summary>
        public int id;

        /// <summary>
        /// 传送门类型。
        /// </summary>
        public string portalType;

        /// <summary>
        /// 目标关卡 ID。
        /// </summary>
        public int targetLevelId;

        /// <summary>
        /// 目标场景名。
        /// </summary>
        public string targetSceneName;

        /// <summary>
        /// 出现条件。
        /// </summary>
        public string spawnCondition;

        /// <summary>
        /// 条件参数。
        /// </summary>
        public string conditionParam;

        /// <summary>
        /// 是否保留玩家状态。
        /// </summary>
        public bool keepPlayerState;

        /// <summary>
        /// 交互提示文本。
        /// </summary>
        public string promptText;

        /// <summary>
        /// 转场类型。
        /// </summary>
        public string transitionType;

        /// <summary>
        /// 转场时长。
        /// </summary>
        public float transitionDuration;

        public PortalConfig() { }

        public PortalConfig(GameConfig.cfg.Portal portal)
        {
            id = portal.Id;
            portalType = portal.PortalType;
            targetLevelId = portal.TargetLevelId;
            targetSceneName = portal.TargetSceneName;
            spawnCondition = portal.SpawnCondition;
            conditionParam = portal.ConditionParam;
            keepPlayerState = portal.KeepPlayerState;
            promptText = portal.PromptText;
            transitionType = portal.TransitionType;
            transitionDuration = portal.TransitionDuration;
        }
    }
}
