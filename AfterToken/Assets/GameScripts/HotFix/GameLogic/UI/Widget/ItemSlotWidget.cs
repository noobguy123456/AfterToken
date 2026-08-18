using TMPro;
using TEngine;
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

        /// <summary>
        /// 选中高亮色（黄色）。
        /// 稀有度调色板中的 Yellow 档已改为橙色，不会与选中框撞色。
        /// </summary>
        private static readonly Color SelectedColor = Color.yellow;

        /// <summary>
        /// 当前稀有度框颜色（取消选中时恢复用）。
        /// </summary>
        private Color _frameColor = Color.white;

        private bool _selected;
        private bool _hasItem;

        /// <summary>
        /// 当前是否处于选中高亮状态。
        /// </summary>
        public bool IsSelected => _selected;

        /// <summary>
        /// 格子内是否有道具（空槽位不参与选中）。
        /// </summary>
        public bool HasItem => _hasItem;

        /// <summary>
        /// 格子被点击时回调（由所属 UIWindow 注册）。
        /// </summary>
        public System.Action<ItemSlotWidget> Clicked;

        protected override void ScriptGenerator()
        {
            _rarityFrame = FindChildComponent<Image>("m_img_RarityFrame");
            _icon = FindChildComponent<Image>("m_img_RarityFrame/m_img_Icon");
            _countText = FindChildComponent<TextMeshProUGUI>("m_img_RarityFrame/m_text_Count");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var click = gameObject.GetComponent<ItemSlotClickHandler>();
            if (click == null)
            {
                click = gameObject.AddComponent<ItemSlotClickHandler>();
            }
            click.OnClicked = () => Clicked?.Invoke(this);
        }

        /// <summary>
        /// 设置选中高亮：选中时稀有度框变黄，取消时恢复稀有度颜色。
        /// </summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyFrameColor();
        }

        private void ApplyFrameColor()
        {
            if (_rarityFrame != null)
            {
                _rarityFrame.color = _selected ? SelectedColor : _frameColor;
            }
        }

        /// <summary>
        /// 绑定道具堆叠数据并刷新显示。
        /// </summary>
        public void SetItem(ItemStack stack)
        {
            _hasItem = true;
            var quality = ItemConfigMgr.Instance.GetQuality(stack.ItemId);
            _frameColor = RarityColors.Get(quality);
            ApplyFrameColor();

            if (_icon != null)
            {
                var iconLocation = ItemConfigMgr.Instance.Get(stack.ItemId)?.Icon;
                if (!string.IsNullOrEmpty(iconLocation))
                {
                    // 配置表填了图标但资源尚未制作时静默跳过（保持占位白图），避免加载报错。
                    // SetSprite 内置缓存池，无需手动释放
                    var hasResult = GameModule.Resource.HasAsset(iconLocation);
                    if (hasResult == HasAssetResult.AssetOnDisk || hasResult == HasAssetResult.AssetOnFileSystem)
                    {
                        _icon.SetSprite(iconLocation);
                    }
                }
                // 无图标配置时保持占位白图
                _icon.color = Color.white;
            }

            if (_countText != null)
            {
                _countText.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }

            // 悬浮提示：挂上/更新转发器，鼠标进入时由 ItemTooltipUI 显示配置表信息
            var hover = gameObject.GetComponent<ItemSlotHoverHandler>();
            if (hover == null)
            {
                hover = gameObject.AddComponent<ItemSlotHoverHandler>();
            }
            hover.ItemId = stack.ItemId;
        }

        /// <summary>
        /// 清空格子显示为空槽位（保留稀有度框默认颜色，无图标/数量/悬浮提示）。
        /// </summary>
        public void SetEmpty()
        {
            _hasItem = false;
            _frameColor = RarityColors.GetDefault();
            ApplyFrameColor();

            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.color = new Color(1, 1, 1, 0);
            }

            if (_countText != null)
            {
                _countText.text = string.Empty;
            }

            var hover = gameObject.GetComponent<ItemSlotHoverHandler>();
            if (hover != null)
            {
                hover.ItemId = 0;
            }
        }
    }
}
