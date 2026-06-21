using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 大厅/关卡选择 UI。
    /// </summary>
    [Window(UILayer.UI, location: "LobbyUI", fullScreen: true)]
    public class LobbyUI : UIWindow
    {
        private TextMeshProUGUI _titleText;
        private RectTransform _levelListRoot;
        private Button _levelButtonTemplate;
        private Button _backButton;

        private readonly List<GameObject> _levelButtonInstances = new List<GameObject>();

        #region 脚本工具生成的代码
        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_text_Title");
            _levelListRoot = FindChildComponent<RectTransform>("m_rect_LevelList");
            _levelButtonTemplate = FindChildComponent<Button>("m_rect_LevelList/m_btn_LevelTemplate");
            _backButton = FindChildComponent<Button>("m_btn_Back");
        }
        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            Log.Debug($"[LobbyUI] 节点绑定: Title={_titleText != null}, LevelList={_levelListRoot != null}, Template={_levelButtonTemplate != null}, Back={_backButton != null}");
            BuildUI();
        }

        private void BuildUI()
        {
            if (_titleText != null)
            {
                _titleText.text = "Select Level";
            }

            ClearLevelButtons();

            if (_levelListRoot != null && _levelButtonTemplate != null)
            {
                foreach (var level in LevelConfigMgr.Instance.GetAll())
                {
                    CreateLevelButton(level);
                }
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureMainMenu>());
            }
        }

        private void CreateLevelButton(LevelConfig level)
        {
            var go = Object.Instantiate(_levelButtonTemplate.gameObject, _levelListRoot, false);
            go.SetActive(true);
            go.name = $"Btn_Level_{level.id}";

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                int levelId = level.id;
                btn.onClick.AddListener(() =>
                {
                    BattleContext.CurrentLevelId = levelId;
                    GameApp.ChangeProcedure<ProcedureBattle>();
                });
            }

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"Level {level.id}: {level.displayName}";
            }

            _levelButtonInstances.Add(go);
        }

        private void ClearLevelButtons()
        {
            foreach (var go in _levelButtonInstances)
            {
                if (go != null) Object.Destroy(go);
            }
            _levelButtonInstances.Clear();
        }

        protected override void OnDestroy()
        {
            ClearLevelButtons();
            base.OnDestroy();
        }
    }
}
