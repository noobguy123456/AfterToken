using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家档案系统：等级、经验、解锁。
    /// 本期为简化内存态，供经营系统使用；持久化由 save-system 后续统一实现。
    /// </summary>
    public static class PlayerProfileSystem
    {
        private static int _level = 1;
        private static int _exp = 0;
        private static int _expToNextLevel = 100;

        public static int Level => _level;
        public static int Exp => _exp;
        public static int ExpToNextLevel => _expToNextLevel;

        public static void AddExp(int amount)
        {
            if (amount <= 0) return;
            _exp += amount;
            while (_exp >= _expToNextLevel)
            {
                _exp -= _expToNextLevel;
                _level++;
                _expToNextLevel = GetExpToNextLevel(_level);
                GameEvent.Get<IPlayerProfileEvent>().OnPlayerLevelUp(_level);
            }
            GameEvent.Get<IPlayerProfileEvent>().OnExpChanged(_exp, _expToNextLevel);
        }

        private static int GetExpToNextLevel(int level)
        {
            return level * 100;
        }

        public static void Reset()
        {
            _level = 1;
            _exp = 0;
            _expToNextLevel = 100;
            GameEvent.Get<IPlayerProfileEvent>().OnExpChanged(_exp, _expToNextLevel);
        }
    }
}
