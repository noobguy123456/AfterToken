using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 经营场景输入系统：处理 Esc 键（关闭菜单 UI/打开设置）、E 键（传送门/NPC 交互）与 J 键（任务日志）。
    /// </summary>
    public class SimulationInputSystem : MonoBehaviour
    {
        [Header("输入设置")]
        [SerializeField] private KeyCode _settingsKey = KeyCode.Escape;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _questLogKey = KeyCode.J;

        private void Update()
        {
            HandleEscapeInput();

            // 队友聊天输入框打开时键盘归输入框，E/J 等字母键不触发玩法功能
            if (CompanionChatUI.IsOpen)
            {
                return;
            }

            HandleInteractInput();
            HandleQuestLogInput();
        }

        /// <summary>
        /// J 键开关任务日志。对话进行中不响应（避免与对话操作冲突）。
        /// </summary>
        private void HandleQuestLogInput()
        {
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsPlaying)
            {
                return;
            }

            if (Input.GetKeyDown(_questLogKey))
            {
                if (GameModule.UI.HasWindow<QuestLogUI>())
                {
                    GameModule.UI.CloseUI<QuestLogUI>();
                }
                else
                {
                    GameModule.UI.ShowUIAsync<QuestLogUI>();
                }
            }
        }

        /// <summary>
        /// E 键交互：复用战斗输入事件，PortalSystem/NpcSystem 订阅它处理基地内交互。
        /// 对话进行中 E 是对话推进键（DialogueSystem 自己读输入），这里不广播交互。
        /// </summary>
        private void HandleInteractInput()
        {
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsPlaying)
            {
                return;
            }

            if (Input.GetKeyDown(_interactKey))
            {
                GameEvent.Get<IBattleInputEvent>()?.OnInteractPressed();
            }
        }

        private void HandleEscapeInput()
        {
            if (!Input.GetKeyDown(_settingsKey))
            {
                return;
            }

            // 对话进行中 ESC 先关闭对话
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsPlaying)
            {
                DialogueSystem.Instance.EndDialogue();
                return;
            }

            // 摆放模式下 ESC 由 BuildingPlacementSystem 处理（退回建筑选择 UI），这里不拦截，避免误开设置面板
            var simRoot = SingletonSystem.GetGameObject("SimulationRoot");
            var placement = simRoot != null ? simRoot.GetComponent<BuildingPlacementSystem>() : null;
            if (placement != null && placement.IsPlacing)
            {
                return;
            }

            // ESC 统一语义：优先关闭最上层菜单 UI，一次 ESC 只关闭一个；
            // 仅当画面中没有任何菜单 UI（HUD/血条/物品栏等常驻 UI 不算）时，ESC 才弹出设置面板。
            if (TryCloseUI<CompanionChatUI>()) return;
            if (TryCloseUI<BuildingInfoUI>()) return;
            if (TryCloseUI<SettingsUI>()) return;
            if (TryCloseUI<LobbyUI>()) return;
            if (TryCloseUI<BuildingSelectionUI>()) return;
            if (TryCloseUI<WarehouseUI>()) return;
            if (TryCloseUI<QuestLogUI>()) return;
            if (TryCloseUI<QuestBoardUI>()) return;
            if (TryCloseManagementPanel()) return;

            // 没有可关闭 UI 时打开设置面板
            GameModule.UI.ShowUIAsync<SettingsUI>();
        }

        /// <summary>管理面板（SimulationMainUI 内嵌面板而非独立窗口）开着时收起。</summary>
        private bool TryCloseManagementPanel()
        {
            var ui = GameModule.UI.GetUI<SimulationMainUI>();
            if (ui != null && ui.IsPanelVisible)
            {
                ui.CloseManagementPanel();
                return true;
            }
            return false;
        }

        private bool TryCloseUI<T>() where T : UIWindow, new()
        {
            if (GameModule.UI.HasWindow<T>())
            {
                GameModule.UI.CloseUI<T>();
                return true;
            }
            return false;
        }
    }
}
