using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家档案系统：等级、经验、解锁。
    /// 持久化由 SaveSystem 接管（变动即存），首次访问时从存档懒加载。
    /// </summary>
    public static class PlayerProfileSystem
    {
        private const int DEFAULT_LEVEL = 1;
        private const int DEFAULT_EXP = 0;
        private const int DEFAULT_EXP_TO_NEXT = 100;

        private static int _level = DEFAULT_LEVEL;
        private static int _exp = DEFAULT_EXP;
        private static int _expToNextLevel = DEFAULT_EXP_TO_NEXT;
        private static readonly HashSet<int> _completedLevels = new HashSet<int>();
        private static bool _loaded;

        public static int Level { get { EnsureLoaded(); return _level; } }
        public static int Exp { get { EnsureLoaded(); return _exp; } }
        public static int ExpToNextLevel { get { EnsureLoaded(); return _expToNextLevel; } }

        /// <summary>
        /// 首次访问时从存档恢复；无存档时保留默认值。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.profile;
            if (!d.initialized) return;

            _level = d.level;
            _exp = d.exp;
            _expToNextLevel = d.expToNextLevel;

            _completedLevels.Clear();
            foreach (var id in d.completedLevels)
            {
                _completedLevels.Add(id);
            }
        }

        /// <summary>
        /// 变动即存：写回数据段并立即落盘。
        /// </summary>
        private static void Persist()
        {
            var d = SaveSystem.Data.profile;
            d.initialized = true;
            d.level = _level;
            d.exp = _exp;
            d.expToNextLevel = _expToNextLevel;
            d.completedLevels.Clear();
            d.completedLevels.AddRange(_completedLevels);
            SaveSystem.Flush();
        }

        public static void AddExp(int amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            _exp += amount;
            while (_exp >= _expToNextLevel)
            {
                _exp -= _expToNextLevel;
                _level++;
                _expToNextLevel = GetExpToNextLevel(_level);
                GameEvent.Get<IPlayerProfileEvent>()?.OnPlayerLevelUp(_level);
            }
            GameEvent.Get<IPlayerProfileEvent>()?.OnExpChanged(_exp, _expToNextLevel);
            Persist();
        }

        /// <summary>
        /// 升级所需经验：优先读 TbPlayerLevel 配置表，配置缺失时回退 level*100。
        /// </summary>
        private static int GetExpToNextLevel(int level)
        {
            var cfg = ConfigSystem.Instance.Tables.TbPlayerLevel.GetOrDefault(level);
            if (cfg != null && cfg.ExpToNext > 0)
            {
                return cfg.ExpToNext;
            }
            return level * 100;
        }

        /// <summary>
        /// 标记关卡已通关（成功撤离）。供解锁系统的关卡链条件判定。
        /// </summary>
        public static void MarkLevelCompleted(int levelId)
        {
            EnsureLoaded();
            if (_completedLevels.Add(levelId))
            {
                Persist();
            }
        }

        /// <summary>
        /// 关卡是否已通关。
        /// </summary>
        public static bool IsLevelCompleted(int levelId)
        {
            EnsureLoaded();
            return _completedLevels.Contains(levelId);
        }

        public static void Reset()
        {
            _level = DEFAULT_LEVEL;
            _exp = DEFAULT_EXP;
            _expToNextLevel = DEFAULT_EXP_TO_NEXT;
            _completedLevels.Clear();
            GameEvent.Get<IPlayerProfileEvent>()?.OnExpChanged(_exp, _expToNextLevel);
            Persist();
        }
    }
}
