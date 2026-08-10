using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 货币系统：金币、钻石、体力管理。
    /// 持久化由 SaveSystem 接管（变动即存），首次访问时从存档懒加载。
    /// </summary>
    public static class CurrencySystem
    {
        private const long DEFAULT_GOLD = 500;
        private const long DEFAULT_DIAMOND = 0;
        private const int DEFAULT_ENERGY = 100;
        private const int DEFAULT_MAX_ENERGY = 100;

        private static long _gold = DEFAULT_GOLD;
        private static long _diamond = DEFAULT_DIAMOND;
        private static int _energy = DEFAULT_ENERGY;
        private static int _maxEnergy = DEFAULT_MAX_ENERGY;
        private static bool _loaded;

        public static long Gold { get { EnsureLoaded(); return _gold; } }
        public static long Diamond { get { EnsureLoaded(); return _diamond; } }
        public static int Energy { get { EnsureLoaded(); return _energy; } }
        public static int MaxEnergy { get { EnsureLoaded(); return _maxEnergy; } }

        /// <summary>
        /// 首次访问时从存档恢复；无存档时保留默认值。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.currency;
            if (!d.initialized) return;

            _gold = d.gold;
            _diamond = d.diamond;
            _energy = d.energy;
            _maxEnergy = d.maxEnergy;
        }

        /// <summary>
        /// 变动即存：写回数据段并立即落盘。
        /// </summary>
        private static void Persist()
        {
            var d = SaveSystem.Data.currency;
            d.initialized = true;
            d.gold = _gold;
            d.diamond = _diamond;
            d.energy = _energy;
            d.maxEnergy = _maxEnergy;
            SaveSystem.Flush();
        }

        public static bool HasGold(long amount) => amount >= 0 && Gold >= amount;
        public static bool HasDiamond(long amount) => amount >= 0 && Diamond >= amount;

        public static void AddGold(long amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            _gold += amount;
            GameEvent.Get<ICurrencyEvent>()?.OnGoldChanged(_gold);
            Persist();
        }

        public static bool TryConsumeGold(long amount)
        {
            // HasGold 内部已 EnsureLoaded
            if (amount <= 0 || !HasGold(amount)) return false;
            _gold -= amount;
            GameEvent.Get<ICurrencyEvent>()?.OnGoldChanged(_gold);
            Persist();
            return true;
        }

        public static void AddDiamond(long amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            _diamond += amount;
            GameEvent.Get<ICurrencyEvent>()?.OnDiamondChanged(_diamond);
            Persist();
        }

        public static bool TryConsumeDiamond(long amount)
        {
            // HasDiamond 内部已 EnsureLoaded
            if (amount <= 0 || !HasDiamond(amount)) return false;
            _diamond -= amount;
            GameEvent.Get<ICurrencyEvent>()?.OnDiamondChanged(_diamond);
            Persist();
            return true;
        }

        public static void SetEnergy(int value, int maxValue)
        {
            EnsureLoaded();
            _energy = value;
            _maxEnergy = maxValue;
            GameEvent.Get<ICurrencyEvent>()?.OnEnergyChanged(_energy, _maxEnergy);
            Persist();
        }

        public static void Reset()
        {
            _gold = DEFAULT_GOLD;
            _diamond = DEFAULT_DIAMOND;
            _energy = DEFAULT_ENERGY;
            _maxEnergy = DEFAULT_MAX_ENERGY;
            GameEvent.Get<ICurrencyEvent>()?.OnGoldChanged(_gold);
            GameEvent.Get<ICurrencyEvent>()?.OnDiamondChanged(_diamond);
            GameEvent.Get<ICurrencyEvent>()?.OnEnergyChanged(_energy, _maxEnergy);
            Persist();
        }
    }
}
