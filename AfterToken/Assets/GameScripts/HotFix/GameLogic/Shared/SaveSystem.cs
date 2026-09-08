using System;
using System.Collections.Generic;
using System.IO;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 存档系统：玩家跨会话持久化数据的统一入口。
    /// 后端为每槽位一个 JSON 文件（Application.persistentDataPath/save_N.json，N=1..SlotCount），
    /// 变动即存——各模块修改自己的数据段后调用 <see cref="Flush"/> 立即写盘。
    /// 首次访问自动初始化（懒加载），版本号不一致时走迁移钩子。
    /// 旧版单文件 save.json 在首次运行时自动迁移为槽位 1。
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>
        /// 当前存档版本号。结构变更时递增并在 <see cref="Migrate"/> 中补升级逻辑。
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// 存档位数量。
        /// </summary>
        public const int SlotCount = 3;

        private const string LEGACY_FILE_NAME = "save.json";
        private const string SLOT_FILE_PATTERN = "save_{0}.json";
        private const string PREF_KEY_SLOT = "Setting.SaveSlot";

        private static SaveData _data;
        private static int _currentSlot = -1;

        /// <summary>
        /// 当前存档位（1..SlotCount），持久化在 PlayerPrefs，默认 1。
        /// </summary>
        public static int CurrentSlot
        {
            get
            {
                if (_currentSlot < 1)
                {
                    _currentSlot = Mathf.Clamp(PlayerPrefs.GetInt(PREF_KEY_SLOT, 1), 1, SlotCount);
                }
                return _currentSlot;
            }
            private set
            {
                _currentSlot = Mathf.Clamp(value, 1, SlotCount);
                PlayerPrefs.SetInt(PREF_KEY_SLOT, _currentSlot);
                PlayerPrefs.Save();
            }
        }

        public static string SaveFilePath => GetSlotPath(CurrentSlot);

        /// <summary>
        /// 指定槽位的存档文件路径。
        /// </summary>
        public static string GetSlotPath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, string.Format(SLOT_FILE_PATTERN, slot));
        }

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
            MigrateLegacyFile();
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
        /// 切换存档位：先把当前数据落盘，再加载目标槽位，最后失效各模块缓存。
        /// 只在主菜单等无进行中对局的时机调用。
        /// </summary>
        public static void SwitchSlot(int slot)
        {
            slot = Mathf.Clamp(slot, 1, SlotCount);
            if (slot == CurrentSlot && _data != null)
            {
                return;
            }

            // 当前槽位落盘后再切换，避免最后改动丢失
            if (_data != null)
            {
                Flush();
            }

            CurrentSlot = slot;
            _data = null;
            Initialize();
            Log.Info($"[SaveSystem] 切换到存档位 {slot}");

            // 失效所有缓存了存档数据的模块，下次访问时从新槽位重读
            CurrencySystem.InvalidateCache();
            PlayerProfileSystem.InvalidateCache();
            UnlockSystem.InvalidateCache();
            Warehouse.InvalidateCache();
            QuestSystem.InvalidateCache();
            SensitivitySetting.InvalidateCache();
            VolumeSetting.InvalidateCache();
            QualitySetting.InvalidateCache();

            // 新槽位立即落盘：空槽位被选中后即在存档界面显示为有效新档（Lv.1/默认货币）
            Flush();
            OnSlotChanged?.Invoke();
        }

        /// <summary>
        /// 存档位切换事件（主菜单槽位指示等 UI 刷新用）。
        /// </summary>
        public static event Action OnSlotChanged;

        /// <summary>
        /// 槽位摘要（存档选择界面用）。只读解析文件，不影响当前内存数据。
        /// </summary>
        public struct SlotSummary
        {
            public bool exists;
            public int level;
            public long gold;
            public long diamond;
        }

        /// <summary>
        /// 读取指定槽位的摘要信息；文件不存在/损坏时 exists=false。
        /// </summary>
        public static SlotSummary GetSlotSummary(int slot)
        {
            var summary = new SlotSummary();
            string path = GetSlotPath(slot);
            if (slot == 1)
            {
                // 旧版单文件尚未迁移时，槽位 1 的摘要直接读旧文件
                MigrateLegacyFile();
            }

            try
            {
                if (File.Exists(path))
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (data != null)
                    {
                        summary.exists = true;
                        summary.level = data.profile != null && data.profile.initialized ? data.profile.level : 1;
                        summary.gold = data.currency != null ? data.currency.gold : 0;
                        summary.diamond = data.currency != null ? data.currency.diamond : 0;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[SaveSystem] 槽位 {slot} 摘要读取失败：{e.Message}");
            }
            return summary;
        }

        /// <summary>
        /// 旧版单文件 save.json 迁移为槽位 1（一次性）。
        /// </summary>
        private static void MigrateLegacyFile()
        {
            try
            {
                string legacy = Path.Combine(Application.persistentDataPath, LEGACY_FILE_NAME);
                string slot1 = GetSlotPath(1);
                if (File.Exists(legacy) && !File.Exists(slot1))
                {
                    File.Move(legacy, slot1);
                    Log.Info("[SaveSystem] 旧版 save.json 已迁移为 save_1.json");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SaveSystem] 旧版存档迁移失败：{e.Message}");
            }
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
        public DialogueSaveData dialogue = new DialogueSaveData();
        public QuestSaveData quest = new QuestSaveData();
    }

    /// <summary>
    /// 任务存档段：状态字符串存枚举（与 KeyBindingEntry 同惯例），
    /// 进度按 objectiveId 存（策划改表增删目标时不串位）。
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        public bool initialized;
        public List<QuestEntry> quests = new List<QuestEntry>();
    }

    [Serializable]
    public class QuestEntry
    {
        public int questId;
        /// <summary>QuestState 枚举名。</summary>
        public string state;
        public List<QuestObjectiveProgress> objectiveProgress = new List<QuestObjectiveProgress>();
    }

    [Serializable]
    public class QuestObjectiveProgress
    {
        public int objectiveId;
        public int count;
    }

    [Serializable]
    public class DialogueSaveData
    {
        public bool initialized;
        /// <summary>
        /// 已设置的对话/剧情标志位集合（含 onceOnly 对话的 dlg_seen_{id}）。
        /// 读写走 <see cref="DialogueFlagSystem"/>。
        /// </summary>
        public List<string> flags = new List<string>();
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
    public class KeyBindingEntry
    {
        /// <summary>
        /// KeyBindAction 的枚举名（字符串存档，枚举顺序调整不影响旧档）。
        /// </summary>
        public string action;
        /// <summary>
        /// KeyCode 的整数值。
        /// </summary>
        public int keyCode;
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
        /// <summary>
        /// 准星样式（CrosshairStyle 枚举 int 值）。
        /// </summary>
        public bool crosshairStyleInitialized;
        public int crosshairStyle;
        /// <summary>
        /// 准星颜色 RGBA。
        /// </summary>
        public bool crosshairColorInitialized;
        public float crosshairColorR;
        public float crosshairColorG;
        public float crosshairColorB;
        public float crosshairColorA;
        /// <summary>
        /// 玩家自定义按键绑定；空列表 = 全部使用默认键位。
        /// </summary>
        public List<KeyBindingEntry> keyBindings = new List<KeyBindingEntry>();
        /// <summary>
        /// 主音量（0..1，控 AudioListener.volume）。
        /// </summary>
        public bool masterVolumeInitialized;
        public float masterVolume;
        /// <summary>
        /// 音乐音量（0..1，走 AudioMixer 的 MusicVolume 参数）。
        /// </summary>
        public bool musicVolumeInitialized;
        public float musicVolume;
        /// <summary>
        /// 音效音量（0..1，走 AudioMixer 的 SoundVolume 参数）。
        /// </summary>
        public bool soundVolumeInitialized;
        public float soundVolume;
        /// <summary>
        /// 画质档位（QualitySettings 档位索引）。
        /// </summary>
        public bool qualityLevelInitialized;
        public int qualityLevel;
    }
}
