using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 可改绑的战斗输入动作。
    /// 移动（WASD）与滚轮切枪走 Input Manager 的 Axis，不在改绑范围内；
    /// ESC 是全局 UI 关闭键，固定不可改绑。
    /// </summary>
    public enum KeyBindAction
    {
        Fire = 0,
        Aim = 1,
        Reload = 2,
        Dodge = 3,
        Interact = 4,
        WeaponWheel = 5,
        Bag = 6,
        CrosshairStyle = 7,
        Map = 8,
        Ping = 9,
        CompanionFollow = 10,
    }

    /// <summary>
    /// 按键绑定设置。
    /// 持久化由 SaveSystem 接管（settings.keyBindings，变动即存）；空列表即全部默认键位。
    /// </summary>
    public static class KeyBindingSetting
    {
        private static readonly KeyBindAction[] AllActions = (KeyBindAction[])System.Enum.GetValues(typeof(KeyBindAction));

        /// <summary>
        /// 全部可改绑动作（枚举定义序，即设置面板的显示顺序）。
        /// </summary>
        public static IReadOnlyList<KeyBindAction> Actions => AllActions;

        /// <summary>
        /// 动作的默认键位。
        /// </summary>
        public static KeyCode GetDefault(KeyBindAction action)
        {
            switch (action)
            {
                case KeyBindAction.Fire: return KeyCode.Mouse0;
                case KeyBindAction.Aim: return KeyCode.Mouse1;
                case KeyBindAction.Reload: return KeyCode.R;
                case KeyBindAction.Dodge: return KeyCode.Space;
                case KeyBindAction.Interact: return KeyCode.E;
                case KeyBindAction.WeaponWheel: return KeyCode.Tab;
                case KeyBindAction.Bag: return KeyCode.B;
                case KeyBindAction.CrosshairStyle: return KeyCode.C;
                case KeyBindAction.Map: return KeyCode.M;
                case KeyBindAction.Ping: return KeyCode.Mouse2;
                case KeyBindAction.CompanionFollow: return KeyCode.G;
                default: return KeyCode.None;
            }
        }

        /// <summary>
        /// 动作在设置面板的显示名（UI 文本统一英文）。
        /// </summary>
        public static string GetDisplayName(KeyBindAction action)
        {
            switch (action)
            {
                case KeyBindAction.Fire: return "Fire";
                case KeyBindAction.Aim: return "Aim / Scope";
                case KeyBindAction.Reload: return "Reload";
                case KeyBindAction.Dodge: return "Dodge";
                case KeyBindAction.Interact: return "Interact";
                case KeyBindAction.WeaponWheel: return "Weapon Wheel";
                case KeyBindAction.Bag: return "Bag";
                case KeyBindAction.CrosshairStyle: return "Crosshair Style";
                case KeyBindAction.Map: return "Map";
                case KeyBindAction.Ping: return "Ping";
                case KeyBindAction.CompanionFollow: return "Companion Follow";
                default: return action.ToString();
            }
        }

        /// <summary>
        /// 当前生效的键位（无自定义记录时返回默认）。
        /// </summary>
        public static KeyCode GetKey(KeyBindAction action)
        {
            var list = SaveSystem.Data.settings.keyBindings;
            string name = action.ToString();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].action == name)
                {
                    return (KeyCode)list[i].keyCode;
                }
            }
            return GetDefault(action);
        }

        /// <summary>
        /// 写入自定义键位并立即落盘。
        /// </summary>
        public static void SetKey(KeyBindAction action, KeyCode key)
        {
            var list = SaveSystem.Data.settings.keyBindings;
            string name = action.ToString();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].action == name)
                {
                    list[i].keyCode = (int)key;
                    SaveSystem.Flush();
                    return;
                }
            }
            list.Add(new KeyBindingEntry { action = name, keyCode = (int)key });
            SaveSystem.Flush();
        }

        /// <summary>
        /// 查找某按键当前绑定的动作（改绑时的冲突检测用）。
        /// </summary>
        public static bool TryGetActionByKey(KeyCode key, out KeyBindAction usedBy)
        {
            for (int i = 0; i < AllActions.Length; i++)
            {
                if (GetKey(AllActions[i]) == key)
                {
                    usedBy = AllActions[i];
                    return true;
                }
            }
            usedBy = default;
            return false;
        }

        /// <summary>
        /// 清空全部自定义绑定，恢复默认键位。
        /// </summary>
        public static void ResetToDefaults()
        {
            SaveSystem.Data.settings.keyBindings.Clear();
            SaveSystem.Flush();
        }

        /// <summary>
        /// 键位显示名：鼠标键转成 LMB/RMB/MMB 风格，其余直接用 KeyCode 名。
        /// </summary>
        public static string GetKeyDisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0: return "LMB";
                case KeyCode.Mouse1: return "RMB";
                case KeyCode.Mouse2: return "MMB";
                case KeyCode.Mouse3: return "Mouse4";
                case KeyCode.Mouse4: return "Mouse5";
                case KeyCode.Mouse5: return "Mouse6";
                case KeyCode.Mouse6: return "Mouse7";
                case KeyCode.Space: return "Space";
                default: return key.ToString();
            }
        }

        /// <summary>
        /// 保留给设置面板的"关闭时落盘"语义；变动即存模式下写入时已经落盘，这里无需再做什么。
        /// </summary>
        public static void Save()
        {
        }
    }
}
