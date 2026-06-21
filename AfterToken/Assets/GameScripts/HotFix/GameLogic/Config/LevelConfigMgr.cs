using System.Collections.Generic;
using System.Linq;

namespace GameLogic
{
    /// <summary>
    /// 关卡配置管理器（临时实现，后续接入 Luban 配置表）。
    /// </summary>
    public class LevelConfigMgr
    {
        private static LevelConfigMgr _instance;
        public static LevelConfigMgr Instance => _instance ??= new LevelConfigMgr();

        private readonly Dictionary<int, LevelConfig> _configs = new Dictionary<int, LevelConfig>();

        private LevelConfigMgr()
        {
            _configs[1] = new LevelConfig
            {
                id = 1,
                displayName = "Training Ground",
                sceneName = "BattleScene",
                description = "Basic training level.",
                defaultWeaponIds = new[] { 1001, 1002, 1003 },
                playerMaxHp = 100,
                enemyCount = 3,
                enemySpawnRadius = 6f,
                enemyConfigId = 9001,
                enemyMaxHp = 50,
            };

            _configs[2] = new LevelConfig
            {
                id = 2,
                displayName = "Abandoned Factory",
                sceneName = "BattleScene_L01",
                description = "Advanced level with more enemies.",
                defaultWeaponIds = new[] { 1002, 1003, 1004 },
                playerMaxHp = 100,
                enemyCount = 5,
                enemySpawnRadius = 8f,
                enemyConfigId = 9001,
                enemyMaxHp = 80,
            };
        }

        public LevelConfig Get(int id)
        {
            _configs.TryGetValue(id, out var config);
            return config;
        }

        public IEnumerable<LevelConfig> GetAll() => _configs.Values.OrderBy(c => c.id);
    }
}
