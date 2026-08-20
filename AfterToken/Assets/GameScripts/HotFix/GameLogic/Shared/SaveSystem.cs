using System;
using System.Collections.Generic;
using System.IO;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 存档系统：玩家跨会话持久化数据的统一入口。
    /// 后端为单个 JSON 文件（Application.persistentDataPath/save.json），
    /// 变动即存——各模块修改自己的数据段后调用 <see cref="Flush"/> 立即写盘。
    /// 首次访问自动初始化（懒加载），版本号不一致时走迁移钩子。
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>
        /// 当前存档版本号。结构变更时递增并在 <see cref="Migrate"/> 中补升级逻辑。
        /// </summary>
        public const int CurrentVersion = 1;

        private const string FILE_NAME = "save.json";

        private static SaveData _data;

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, FILE_NAME);

        /// <summary>
        /// 存档根数据（模块读写自己的数据段）。访问前确保已初始化。
        /// </summary>
        public static SaveData Data
        {
            get
            {
                Initialize();
                return _data;
            }
        }

        /// <summary>
        /// 读取存档文件到内存。幂等；文件不存在或损坏时使用默认数据。
        /// </summary>
        public static void Initialize()
        {
            if (_data != null) return;

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _data = JsonUtility.FromJson<SaveData>(json);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SaveSystem] 存档读取失败，使用默认数据：{e.Message}");
                _data = null;
            }

            if (_data == null)
            {
                _data = new SaveData();
            }

            Migrate(_data);
        }

        /// <summary>
        /// 立即把内存中的存档写盘（变动即存）。
        /// </summary>
        public static void Flush()
        {
            Initialize();

            try
            {
                _data.version = CurrentVersion;
                string json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception e)
            {
                Log.Error($"[SaveSystem] 存档写盘失败：{e.Message}");
            }
        }

        /// <summary>
        /// 删除存档文件并重置内存数据（GM 调试用；各运行中模块需自行 Reset 后重新持久化）。
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SaveSystem] 存档删除失败：{e.Message}");
            }

            _data = new SaveData();
        }

        /// <summary>
        /// 版本迁移钩子：旧版本存档逐级升级到 CurrentVersion。
        /// </summary>
        private static void Migrate(SaveData data)
        {
            if (data.version >= CurrentVersion) return;

            // 未来示例：if (data.version < 2) { ...v1→v2 迁移... data.version = 2; }

            data.version = CurrentVersion;
        }
    }

    /// <summary>
    /// 存档根对象。每个模块一个数据段，initialized 标记区分"无存档"（用默认值/旧 PlayerPrefs 导入）。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version;
        public CurrencySaveData currency = new CurrencySaveData();
        public ProfileSaveData profile = new ProfileSaveData();
        public WarehouseSaveData warehouse = new WarehouseSaveData();
        public SettingsSaveData settings = new SettingsSaveData();
        public UnlockSaveData unlock = new UnlockSaveData();
    }

    [Serializable]
    public class CurrencySaveData
    {
        public bool initialized;
        public long gold;
        public long diamond;
        public int energy;
        public int maxEnergy;
    }

    [Serializable]
    public class ProfileSaveData
    {
        public bool initialized;
        public int level;
        public int exp;
        public int expToNextLevel;
        /// <summary>
        /// 已通关（成功撤离）的关卡 ID 列表，供解锁系统判定关卡链解锁。
        /// </summary>
        public List<int> completedLevels = new List<int>();
    }

    [Serializable]
    public class UnlockSaveData
    {
        public bool initialized;
        /// <summary>
        /// 已付费解锁的 TbUnlock 记录 ID。
        /// 免费项满足条件即视为解锁，不入此列表。
        /// </summary>
        public List<int> unlockedIds = new List<int>();
    }

    [Serializable]
    public class WarehouseSaveData
    {
        public bool initialized;
        public List<ItemStack> items = new List<ItemStack>();
        /// <summary>
        /// ItemStack 获取序号的分配水位，重启后从这里继续，保证获取时间可比较。
        /// </summary>
        public long nextSeq;
    }

    [Serializable]
    public class SettingsSaveData
    {
        public bool sensitivityInitialized;
        public float sensitivity;
        public bool scopeSensitivityInitialized;
        public float scopeSensitivity;
        public bool sniperAimModeInitialized;
        public bool sniperAimModeToggle;
    }
}
