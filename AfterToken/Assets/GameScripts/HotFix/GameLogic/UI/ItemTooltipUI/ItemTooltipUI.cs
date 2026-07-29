using Cysharp.Threading.Tasks;
using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 道具悬浮提示窗。
    /// 鼠标悬停道具格时显示配置表中的道具信息（名称/稀有度/类型/价格/描述）。
    /// </summary>
    [Window(UILayer.Tips, location: "ItemTooltipUI", fullScreen: false)]
    public class ItemTooltipUI : UIWindow
    {
        private RectTransform _panel;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _qualityText;
        private TextMeshProUGUI _typeText;
        private TextMeshProUGUI _priceText;
        private TextMeshProUGUI _descText;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            // FindChildComponent 基于 transform.Find（不递归），内容节点均在 m_rect_Panel 下，必须写完整路径。
            _panel = FindChildComponent<RectTransform>("m_rect_Panel");
            _nameText = FindChildComponent<TextMeshProUGUI>("m_rect_Panel/m_text_Name");
            _qualityText = FindChildComponent<TextMeshProUGUI>("m_rect_Panel/m_text_Quality");
            _typeText = FindChildComponent<TextMeshProUGUI>("m_rect_Panel/m_text_Type");
            _priceText = FindChildComponent<TextMeshProUGUI>("m_rect_Panel/m_text_Price");
            _descText = FindChildComponent<TextMeshProUGUI>("m_rect_Panel/m_text_Desc");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
        }

        /// <summary>
        /// 显示指定道具的提示信息。
        /// 提示窗锚定在格子边缘：格子位于屏幕左半区时固定到格子右侧，右半区时固定到左侧，避免飞出屏幕。
        /// </summary>
        /// <param name="itemId">道具 ID（读取配置表）。</param>
        /// <param name="slotRect">悬停格子的 RectTransform。</param>
        public static void ShowTooltip(int itemId, RectTransform slotRect)
        {
            ShowAsync(itemId, slotRect).Forget();
        }

        /// <summary>
        /// 隐藏提示窗。
        /// </summary>
        public static void HideTooltip()
        {
            GameModule.UI.CloseUI<ItemTooltipUI>();
        }

        private static async UniTaskVoid ShowAsync(int itemId, RectTransform slotRect)
        {
            var ui = await GameModule.UI.ShowUIAsyncAwait<ItemTooltipUI>();
            ui?.SetItem(itemId, slotRect);
        }

        private void SetItem(int itemId, RectTransform slotRect)
        {
            var item = ItemConfigMgr.Instance.Get(itemId);
            if (item == null)
            {
                return;
            }

            var qualityColor = RarityColors.Get(item.Quality);

            if (_nameText != null)
            {
                _nameText.text = item.Name;
                _nameText.color = qualityColor;
            }
            if (_qualityText != null)
            {
                _qualityText.text = $"Quality: {item.Quality}";
                _qualityText.color = qualityColor;
            }
            if (_typeText != null)
            {
                _typeText.text = $"Type: {item.ItemType}";
            }
            if (_priceText != null)
            {
                _priceText.text = $"Price: {item.Price}";
            }
            if (_descText != null)
            {
                _descText.text = item.Desc;
            }

            UpdatePosition(slotRect);
        }

        private void UpdatePosition(RectTransform slotRect)
        {
            if (_panel == null || slotRect == null)
            {
                return;
            }

            var parentRect = _panel.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            // 格子世界角点（Overlay 画布下世界坐标即屏幕坐标）：0=左下，2=右上
            var corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);
            float slotLeft = corners[0].x;
            float slotRight = corners[2].x;
            float slotCenterY = (corners[0].y + corners[2].y) * 0.5f;

            const float margin = 8f;
            var size = _panel.rect.size;
            var pivot = _panel.pivot;

            // 格子在屏幕左半区 → 面板固定到格子右侧；右半区 → 固定到左侧，避免面板飞出屏幕
            bool placeRight = (slotLeft + slotRight) * 0.5f < Screen.width * 0.5f;
            float panelScreenX = placeRight
                ? slotRight + margin + pivot.x * size.x
                : slotLeft - margin - (1f - pivot.x) * size.x;
            float panelScreenY = slotCenterY - (0.5f - pivot.y) * size.y;

            // 钳制面板完整落在屏幕内（考虑 pivot 与外间距），防止格子靠近屏幕边缘时面板越界
            float minX = margin + pivot.x * size.x;
            float maxX = Screen.width - margin - (1f - pivot.x) * size.x;
            float minY = margin + pivot.y * size.y;
            float maxY = Screen.height - margin - (1f - pivot.y) * size.y;
            panelScreenX = Mathf.Clamp(panelScreenX, minX, maxX);
            panelScreenY = Mathf.Clamp(panelScreenY, minY, maxY);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, new Vector2(panelScreenX, panelScreenY), null, out var localPos))
            {
                _panel.anchoredPosition = localPos;
            }
        }
    }
}
