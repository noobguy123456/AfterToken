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

        private static int GetExpToNextLevel(int level)
        {
            return level * 100;
        }

        public static void Reset()
        {
            _level = DEFAULT_LEVEL;
            _exp = DEFAULT_EXP;
            _expToNextLevel = DEFAULT_EXP_TO_NEXT;
            GameEvent.Get<IPlayerProfileEvent>()?.OnExpChanged(_exp, _expToNextLevel);
            Persist();
        }
    }
}
