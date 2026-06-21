using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 运行时 TMP 默认字体提供器。
    /// 优先使用 TMP_Settings 的默认字体；未配置时动态创建一份 Arial 字体资产。
    /// </summary>
    public static class TMPFontProvider
    {
        private static TMP_FontAsset _defaultFont;

        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                {
                    TryLoadDefaultFont();
                }
                return _defaultFont;
            }
        }

        private static void TryLoadDefaultFont()
        {
            try
            {
                _defaultFont = TMP_Settings.defaultFontAsset;
            }
            catch
            {
                _defaultFont = null;
            }

            if (_defaultFont == null)
            {
                var sourceFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
                _defaultFont = TMP_FontAsset.CreateFontAsset(sourceFont);
            }
        }
    }
}
