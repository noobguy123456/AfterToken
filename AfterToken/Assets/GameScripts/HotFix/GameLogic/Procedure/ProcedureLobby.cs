using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 大厅/关卡选择流程。
    /// </summary>
    public class ProcedureLobby : GameplayProcedureBase
    {
        protected override UniTaskVoid EnterAsync()
        {
            return LoadSceneWithLoadingAsync("LobbyScene", async ct =>
            {
                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
                CursorManager.Instance?.ForceShowCursor();
                await GameModule.UI.ShowUIAsyncAwait<LobbyUI>();
            });
        }
    }
}
