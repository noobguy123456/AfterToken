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
                // 回到大厅 = 一局结束：清空关卡临时背包（胜利转入仓库已在 PortalSystem 完成，此处统一兜底）
                RunInventory.Clear();

                CursorManager.Instance?.SetLockMode(GameCursorLockMode.Free);
                CursorManager.Instance?.ForceShowCursor();
                await GameModule.UI.ShowUIAsyncAwait<LobbyUI>();
            });
        }
    }
}
