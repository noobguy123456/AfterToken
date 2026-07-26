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
        private Button _warehouseButton;
        private Button _simulationButton;

        private readonly List<GameObject> _levelButtonInstances = new List<GameObject>();

        #region 脚本工具生成的代码
        protected override void ScriptGenerator()
        {
            _titleText = FindChildComponent<TextMeshProUGUI>("m_text_Title");
            _levelListRoot = FindChildComponent<RectTransform>("m_rect_LevelList");
            _levelButtonTemplate = FindChildComponent<Button>("m_rect_LevelList/m_btn_LevelTemplate");
            _backButton = FindChildComponent<Button>("m_btn_Back");
            _warehouseButton = FindChildComponent<Button>("m_btn_Warehouse");

            // 调试日志：检查组件是否正确获取
            Log.Info($"[LobbyUI] ScriptGenerator: _titleText={_titleText != null}, _levelListRoot={_levelListRoot != null}, _levelButtonTemplate={_levelButtonTemplate != null}, _backButton={_backButton != null}, _warehouseButton={_warehouseButton != null}");
        }
        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            FixFullScreenCanvas();
            CursorManager.Instance?.ShowCursor();
            BuildUI();
        }

        private void BuildUI()
        {
            if (_titleText != null)
            {
                _titleText.text = "Select Level";
                _titleText.fontSize = 48;
                _titleText.alignment = TextAlignmentOptions.Center;
                
                // 调整标题位置到顶部
                var titleRect = _titleText.transform as RectTransform;
                if (titleRect != null)
                {
                    titleRect.anchoredPosition = new Vector2(0, 400);
                }
            }

            ClearLevelButtons();

            if (_levelListRoot != null && _levelButtonTemplate != null)
            {
                // 调整关卡列表布局
                SetupLevelListLayout();
                
                foreach (var level in LevelConfigMgr.Instance.GetAll())
                {
                    CreateLevelButton(level);
                }
            }

            // 调整功能按钮位置到底部
            SetupFunctionButtons();

            // 动态创建模拟经营入口按钮
            CreateSimulationButton();
        }

        /// <summary>
        /// 设置关卡列表布局（使用 Grid Layout Group）。
        /// </summary>
        private void SetupLevelListLayout()
        {
            if (_levelListRoot == null)
            {
                Log.Warning("[LobbyUI] _levelListRoot 为 null，无法设置关卡列表布局");
                return;
            }

            if (_levelListRoot.gameObject == null)
            {
                Log.Warning("[LobbyUI] _levelListRoot.gameObject 为 null，无法设置关卡列表布局");
                return;
            }

            // 添加或获取 Grid Layout Group
            var gridLayout = _levelListRoot.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                try
                {
                    gridLayout = _levelListRoot.gameObject.AddComponent<GridLayoutGroup>();
                }
                catch (System.Exception e)
                {
                    Log.Error($"[LobbyUI] 添加 GridLayoutGroup 组件失败: {e.Message}");
                    return;
                }

                if (gridLayout == null)
                {
                    Log.Error("[LobbyUI] 无法添加 GridLayoutGroup 组件（AddComponent 返回 null）");
                    return;
                }
            }

            // 设置 Grid Layout Group 参数
            gridLayout.cellSize = new Vector2(300, 80);
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            // 调整关卡列表位置到中心
            var listRect = _levelListRoot;
            if (listRect != null)
            {
                listRect.anchoredPosition = new Vector2(0, 50);
            }
        }

        /// <summary>
        /// 设置功能按钮位置到底部。
        /// </summary>
        private void SetupFunctionButtons()
        {
            if (_backButton != null)
            {
                var backRect = _backButton.transform as RectTransform;
                if (backRect != null)
                {
                    backRect.anchoredPosition = new Vector2(-300, -400);
                }
                
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureMainMenu>());
            }

            if (_warehouseButton != null)
            {
                var warehouseRect = _warehouseButton.transform as RectTransform;
                if (warehouseRect != null)
                {
                    warehouseRect.anchoredPosition = new Vector2(0, -400);
                }
                
                _warehouseButton.onClick.RemoveAllListeners();
                _warehouseButton.onClick.AddListener(() => GameModule.UI.ShowUIAsync<WarehouseUI>());
            }
        }

        /// <summary>
        /// 动态创建模拟经营入口按钮（Prefab 未包含该节点时的临时方案）。
        /// </summary>
        private void CreateSimulationButton()
        {
            if (_warehouseButton == null) return;

            var parent = _warehouseButton.transform.parent;
            if (parent == null) return;

            var go = Object.Instantiate(_warehouseButton.gameObject, parent, false);
            go.name = "m_btn_Simulation";

            var rect = go.transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(300, -400);
            }

            _simulationButton = go.GetComponent<Button>();
            if (_simulationButton != null)
            {
                _simulationButton.onClick.RemoveAllListeners();
                _simulationButton.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureSimulation>());
            }

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "Simulation";
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
                text.text = $"Stage {level.id}";
                text.fontSize = 24;
                text.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                Log.Warning($"[LobbyUI] 关卡按钮 {level.id} 找不到 TextMeshProUGUI 组件");
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
            CursorManager.Instance?.HideCursor();
            base.OnDestroy();
        }
    }
}
