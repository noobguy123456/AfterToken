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

            // 按 UI 层级从高到低尝试关闭最上层弹窗；一次 ESC 只关闭一个。
            if (TryCloseUI<SettingsUI>()) return;
            if (TryCloseUI<BuildingSelectionUI>()) return;
            if (TryCloseUI<WarehouseUI>()) return;

            // 没有可关闭 UI 时打开设置面板
            GameModule.UI.ShowUIAsync<SettingsUI>();
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
