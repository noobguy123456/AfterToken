using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 经营场景输入系统：处理 Esc 键（打开/关闭设置菜单）。
    /// </summary>
    public class SimulationInputSystem : MonoBehaviour
    {
        [Header("输入设置")]
        [SerializeField] private KeyCode _settingsKey = KeyCode.Escape;

        private void Update()
        {
            HandleEscapeInput();
        }

        private void HandleEscapeInput()
        {
            if (!Input.GetKeyDown(_settingsKey))
            {
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
            if (TryCloseUI<BuildingInfoUI>()) return;
            if (TryCloseUI<SettingsUI>()) return;
            if (TryCloseUI<BuildingSelectionUI>()) return;
            if (TryCloseUI<WarehouseUI>()) return;
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
