using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 准星样式与颜色设置。
    /// 持久化由 SaveSystem 接管（settings.crosshair*，变动即存）。
    /// 变动通过 OnChanged 事件广播，BattleMainUI 订阅后即时刷新准星。
    /// </summary>
    public static class CrosshairSetting
    {
        /// <summary>
        /// 样式或颜色变动事件（无参，订阅方自行重新读取）。
        /// </summary>
        public static event System.Action OnChanged;

        /// <summary>
        /// 默认颜色（与原 BattleMainUI 内置默认值一致）。
        /// </summary>
        private static readonly Color DefaultColor = new Color(0.2f, 1f, 0.2f, 0.9f);

        /// <summary>
        /// 可选预设色（设置面板色板顺序）。
        /// </summary>
        public static readonly Color[] PresetColors =
        {
            new Color(1f, 1f, 1f, 0.9f),    // White
            new Color(0.2f, 1f, 0.2f, 0.9f),// Green
            new Color(1f, 0.25f, 0.25f, 0.9f), // Red
            new Color(0.3f, 1f, 1f, 0.9f),  // Cyan
            new Color(1f, 0.9f, 0.2f, 0.9f),// Yellow
            new Color(1f, 0.4f, 1f, 0.9f),  // Magenta
        };

        /// <summary>
        /// 可循环切换的样式（Reloading 是换弹瞬态样式，不参与循环）。
        /// </summary>
        private static readonly CrosshairStyle[] CyclableStyles =
        {
            CrosshairStyle.Dot,
            CrosshairStyle.Cross,
            CrosshairStyle.Circle,
            CrosshairStyle.TShape,
        };

        /// <summary>
        /// 当前样式（无存档时默认 Cross）。
        /// </summary>
        public static CrosshairStyle Style
        {
            get
            {
                var d = SaveSystem.Data.settings;
                return d.crosshairStyleInitialized ? (CrosshairStyle)d.crosshairStyle : CrosshairStyle.Cross;
            }
            set
            {
                var d = SaveSystem.Data.settings;
                d.crosshairStyleInitialized = true;
                d.crosshairStyle = (int)value;
                SaveSystem.Flush();
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// 当前颜色（无存档时默认绿色）。
        /// </summary>
        public static Color Color
        {
            get
            {
                var d = SaveSystem.Data.settings;
                return d.crosshairColorInitialized
                    ? new Color(d.crosshairColorR, d.crosshairColorG, d.crosshairColorB, d.crosshairColorA)
                    : DefaultColor;
            }
            set
            {
                var d = SaveSystem.Data.settings;
                d.crosshairColorInitialized = true;
                d.crosshairColorR = value.r;
                d.crosshairColorG = value.g;
                d.crosshairColorB = value.b;
                d.crosshairColorA = value.a;
                SaveSystem.Flush();
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// 循环切换到下一个样式（设置面板按钮与 C 键共用）。
        /// </summary>
        public static void CycleStyle()
        {
            int idx = System.Array.IndexOf(CyclableStyles, Style);
            if (idx < 0) idx = 0;
            Style = CyclableStyles[(idx + 1) % CyclableStyles.Length];
        }

        /// <summary>
        /// 样式显示名（UI 文本统一英文）。
        /// </summary>
        public static string GetStyleDisplayName(CrosshairStyle style)
        {
            return style.ToString();
        }

        /// <summary>
        /// 保留给设置面板的"关闭时落盘"语义；变动即存模式下写入时已经落盘，这里无需再做什么。
        /// </summary>
        public static void Save()
        {
        }
    }
}
