using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 货币系统：金币、钻石、体力管理。
    /// 本期为内存态，重启清空；持久化由 save-system 后续统一实现。
    /// </summary>
    public static class CurrencySystem
    {
        private static long _gold = 500;
        private static long _diamond = 0;
        private static int _energy = 100;
        private static int _maxEnergy = 100;

        public static long Gold => _gold;
        public static long Diamond => _diamond;
        public static int Energy => _energy;
        public static int MaxEnergy => _maxEnergy;

        public static bool HasGold(long amount) => amount >= 0 && _gold >= amount;
        public static bool HasDiamond(long amount) => amount >= 0 && _diamond >= amount;

        public static void AddGold(long amount)
        {
            if (amount <= 0) return;
            _gold += amount;
            GameEvent.Get<ICurrencyEvent>().OnGoldChanged(_gold);
        }

        public static bool TryConsumeGold(long amount)
        {
            if (amount <= 0 || !HasGold(amount)) return false;
            _gold -= amount;
            GameEvent.Get<ICurrencyEvent>().OnGoldChanged(_gold);
            return true;
        }

        public static void AddDiamond(long amount)
        {
            if (amount <= 0) return;
            _diamond += amount;
            GameEvent.Get<ICurrencyEvent>().OnDiamondChanged(_diamond);
        }

        public static bool TryConsumeDiamond(long amount)
        {
            if (amount <= 0 || !HasDiamond(amount)) return false;
            _diamond -= amount;
            GameEvent.Get<ICurrencyEvent>().OnDiamondChanged(_diamond);
            return true;
        }

        public static void SetEnergy(int value, int maxValue)
        {
            _energy = value;
            _maxEnergy = maxValue;
            GameEvent.Get<ICurrencyEvent>().OnEnergyChanged(_energy, _maxEnergy);
        }

        public static void Reset()
        {
            _gold = 500;
            _diamond = 0;
            _energy = 100;
            _maxEnergy = 100;
            GameEvent.Get<ICurrencyEvent>().OnGoldChanged(_gold);
            GameEvent.Get<ICurrencyEvent>().OnDiamondChanged(_diamond);
            GameEvent.Get<ICurrencyEvent>().OnEnergyChanged(_energy, _maxEnergy);
        }
    }
}
