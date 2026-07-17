using TMPro;

namespace GameLogic
{
    /// <summary>
    /// 交互提示 UI。
    /// 显示在屏幕中央偏下，用于提示玩家按交互键。
    /// </summary>
    [Window(UILayer.Top, location: "InteractionPromptUI")]
    public class InteractionPromptUI : UIWindow
    {
        private TextMeshProUGUI _promptText;

        #region 脚本工具生成的代码

        protected override void ScriptGenerator()
        {
            _promptText = FindChildComponent<TextMeshProUGUI>("m_text_Prompt");
        }

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            SetPrompt(string.Empty);
        }

        /// <summary>
        /// 设置提示文本。
        /// </summary>
        public void SetPrompt(string text)
        {
            if (_promptText != null)
            {
                _promptText.text = text;
            }
        }
    }
}
