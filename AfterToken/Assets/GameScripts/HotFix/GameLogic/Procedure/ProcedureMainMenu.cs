using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 主菜单流程。
    /// </summary>
    public class ProcedureMainMenu : GameplayProcedureBase
    {
        protected override UniTaskVoid EnterAsync()
        {
            Log.Debug("[ProcedureMainMenu] 进入主菜单流程");
            return LoadSceneWithLoadingAsync("MainMenuScene", async ct =>
            {
                Log.Debug("[ProcedureMainMenu] 场景加载完成，隐藏启动器 UI 并打开 MainMenuUI");
                HideLauncherUI();
                await GameModule.UI.ShowUIAsyncAwait<MainMenuUI>();
                Log.Debug("[ProcedureMainMenu] MainMenuUI 已打开");
            });
        }

        private void HideLauncherUI()
        {
            var loadUI = GameObject.Find("LoadUpdateUI");
            if (loadUI != null) Object.DestroyImmediate(loadUI);

            var tipsUI = GameObject.Find("LoadTipsUI");
            if (tipsUI != null) Object.DestroyImmediate(tipsUI);
        }
    }
}
