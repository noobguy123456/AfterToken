namespace GameLogic
{
    /// <summary>
    /// 关卡配置（对 Luban 生成配置的运行时适配）。
    /// </summary>
    public class LevelConfig
    {
        /// <summary>
        /// 关卡 ID。
        /// </summary>
        public int id;

        /// <summary>
        /// 显示名称。
        /// </summary>
        public string displayName;

        /// <summary>
        /// 场景地址（YooAsset 地址）。
        /// </summary>
        public string sceneName;

        /// <summary>
        /// 关卡描述。
        /// </summary>
        public string description;

        /// <summary>
        /// 本关默认携带的武器配置 ID 列表。
        /// </summary>
        public int[] defaultWeaponIds;

        /// <summary>
        /// 玩家血量上限。
        /// </summary>
        public int playerMaxHp;

        /// <summary>
        /// 敌人生成数量。
        /// </summary>
        public int enemyCount;

        /// <summary>
        /// 敌人生成半径。
        /// </summary>
        public float enemySpawnRadius;

        /// <summary>
        /// 敌人配置 ID。
        /// </summary>
        public int enemyConfigId;

        /// <summary>
        /// 敌人最大血量。
        /// </summary>
        public int enemyMaxHp;

        public LevelConfig() { }

        public LevelConfig(GameConfig.cfg.Level level)
        {
            id = level.Id;
            displayName = level.DisplayName;
            sceneName = level.SceneName;
            description = level.Description;
            defaultWeaponIds = level.DefaultWeaponIds;
            playerMaxHp = level.PlayerMaxHp;
            enemyCount = level.EnemyCount;
            enemySpawnRadius = level.EnemySpawnRadius;
            enemyConfigId = level.EnemyConfigId;
            enemyMaxHp = level.EnemyMaxHp;
        }
    }
}
