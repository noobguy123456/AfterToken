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
            Log.Debug("[ProcedureLobby] 进入大厅流程");
            return LoadSceneWithLoadingAsync("LobbyScene", async ct =>
            {
                Log.Debug("[ProcedureLobby] 场景加载完成，打开 LobbyUI");
                await GameModule.UI.ShowUIAsyncAwait<LobbyUI>();
                Log.Debug("[ProcedureLobby] LobbyUI 已打开");
            });
        }
    }
}
