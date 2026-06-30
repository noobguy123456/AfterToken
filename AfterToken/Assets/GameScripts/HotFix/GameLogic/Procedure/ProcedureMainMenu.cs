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
                Log.Debug("[ProcedureMainMenu] 场景加载完成，显示系统光标并打开 MainMenuUI");
                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
                CursorManager.Instance?.ForceShowCursor();
                HideLauncherUI();
                await GameModule.UI.ShowUIAsyncAwait<MainMenuUI>();
                Log.Debug("[ProcedureMainMenu] MainMenuUI 已打开");

#if UNITY_EDITOR
                // TODO: 临时调试入口，自动进入战斗
                // 注意：开启后会 500ms 自动跳转战斗并锁定光标，影响主菜单/大厅的光标测试。
                // await UniTask.Delay(500, cancellationToken: ct);
                // GameApp.ChangeProcedure<ProcedureBattle>();
#endif
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
