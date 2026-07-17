using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 主菜单 UI。
    /// </summary>
    [Window(UILayer.UI, "MainMenuUI", true)]
    public class MainMenuUI : UIWindow
    {
        /// <summary>
        /// 主菜单打开时暂停游戏进程（不影响声音）。
        /// 若 UI Prefab 上挂了 UIWindowTimeScale，Inspector 值可覆盖此处默认值。
        /// </summary>
        public override float TimeScaleWhenVisible => InspectorTimeScale ?? 0f;

        private TextMeshProUGUI _titleText;
        private Button _startButton;
        private Button _exitButton;
        private Button _settingsButton;

        #region 脚本工具生成的代码
        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_text_Title");
            _startButton = FindChildComponent<Button>("m_rect_ButtonRoot/m_btn_Start");
            _exitButton = FindChildComponent<Button>("m_rect_ButtonRoot/m_btn_Exit");
            _settingsButton = FindChildComponent<Button>("m_rect_ButtonRoot/m_btn_Settings");
        }
        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            SetupDefaultCursor();
            CursorManager.Instance?.ShowCursor();
            BindEvents();
        }

        protected override void OnDestroy()
        {
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }

        /// <summary>
        /// 设置默认光标纹理。
        /// 若后续希望替换为美术资源，可将纹理放到 YooAsset 并修改此处加载逻辑。
        /// </summary>
        private void SetupDefaultCursor()
        {
            var texture = CreateDefaultCursorTexture();
            Vector2 hotSpot = new Vector2(texture.width * 0.1f, texture.height * 0.1f);
            CursorManager.Instance?.SetDefaultCursor(texture, hotSpot);
        }

        /// <summary>
        /// 创建一个默认箭头形状光标纹理（占位，可替换为美术资源）。
        /// </summary>
        private Texture2D CreateDefaultCursorTexture()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color clear = Color.clear;
            Color green = new Color(0.2f, 1f, 0.2f, 0.95f);
            Color white = new Color(1f, 1f, 1f, 0.95f);

            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, clear);

            // 绘制简单箭头
            for (int i = 0; i < size - 4; i++)
            {
                int x = i;
                int y = size - 4 - i;
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    tex.SetPixel(x, y, green);
                    tex.SetPixel(x + 1, y, green);
                    tex.SetPixel(x, y - 1, green);
                }
            }

            // 箭头外框
            for (int i = 0; i < size - 6; i++)
            {
                int x = i;
                int y = size - 6 - i;
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    tex.SetPixel(x + 1, y + 1, white);
                }
            }

            tex.Apply();
            return tex;
        }

        private void BindEvents()
        {
            if (_titleText != null)
            {
                _titleText.text = "AfterToken";
            }

            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureLobby>());
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveAllListeners();
                _exitButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() => GameModule.UI.ShowUIAsync<SettingsUI>());
            }
        }
    }
}
