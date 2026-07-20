using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 道具格子 Widget：稀有度框 + 图标 + 数量。
    /// 与 Assets/AssetRaw/UI/ItemSlot/ItemSlot.prefab 配套使用。
    /// </summary>
    public class ItemSlotWidget : UIWidget
    {
        private Image _rarityFrame;
        private Image _icon;
        private TextMeshProUGUI _countText;

        protected override void ScriptGenerator()
        {
            _rarityFrame = FindChildComponent<Image>("m_img_RarityFrame");
            _icon = FindChildComponent<Image>("m_img_RarityFrame/m_img_Icon");
            _countText = FindChildComponent<TextMeshProUGUI>("m_img_RarityFrame/m_text_Count");
        }

        /// <summary>
        /// 绑定道具堆叠数据并刷新显示。
        /// </summary>
        public void SetItem(ItemStack stack)
        {
            var quality = ItemConfigMgr.Instance.GetQuality(stack.ItemId);
            if (_rarityFrame != null)
            {
                _rarityFrame.color = RarityColors.Get(quality);
            }

            if (_icon != null)
            {
                var iconLocation = ItemConfigMgr.Instance.Get(stack.ItemId)?.Icon;
                if (!string.IsNullOrEmpty(iconLocation))
                {
                    // SetSprite 内置缓存池，无需手动释放
                    _icon.SetSprite(iconLocation);
                }
                // 无图标配置时保持占位白图
            }

            if (_countText != null)
            {
                _countText.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }
        }
    }
}
