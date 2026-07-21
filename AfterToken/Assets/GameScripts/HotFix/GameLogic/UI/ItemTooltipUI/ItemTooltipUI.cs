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
        /// </summary>
        /// <param name="itemId">道具 ID（读取配置表）。</param>
        /// <param name="screenPos">提示窗锚定的屏幕位置（通常为鼠标位置）。</param>
        public static void ShowTooltip(int itemId, Vector2 screenPos)
        {
            ShowAsync(itemId, screenPos).Forget();
        }

        /// <summary>
        /// 隐藏提示窗。
        /// </summary>
        public static void HideTooltip()
        {
            GameModule.UI.CloseUI<ItemTooltipUI>();
        }

        private static async UniTaskVoid ShowAsync(int itemId, Vector2 screenPos)
        {
            var ui = await GameModule.UI.ShowUIAsyncAwait<ItemTooltipUI>();
            ui?.SetItem(itemId, screenPos);
        }

        private void SetItem(int itemId, Vector2 screenPos)
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

            UpdatePosition(screenPos);
        }

        private void UpdatePosition(Vector2 screenPos)
        {
            if (_panel == null)
            {
                return;
            }

            var parentRect = _panel.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, null, out var localPos))
            {
                // 向右上方偏移，避免遮挡鼠标
                _panel.anchoredPosition = localPos + new Vector2(20f, 20f);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UpdatePosition(Input.mousePosition);
        }
    }
}
