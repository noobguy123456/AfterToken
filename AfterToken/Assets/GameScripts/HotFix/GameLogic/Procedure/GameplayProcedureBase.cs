using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏业务流程基类。
    /// 统一处理：LoadingUI 场景切换、取消令牌、离开时的资源清理。
    /// </summary>
    public abstract class GameplayProcedureBase : ProcedureBase
    {
        private CancellationTokenSource _cts;

        protected CancellationToken CancellationToken => _cts?.Token ?? default;

        protected override void OnEnter(IFsm<IProcedureModule> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            ProcedureStateRecorder.Record(GetType().Name);
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            EnterAsync().Forget();
        }

        protected abstract UniTaskVoid EnterAsync();

        protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            Cleanup();
        }

        /// <summary>
        /// 通用场景加载：显示 LoadingUI，异步加载场景，更新进度，加载完成后执行后续逻辑。
        /// </summary>
        /// <param name="sceneName">要加载的场景地址。</param>
        /// <param name="afterLoadAction">场景加载完成后执行的操作（打开 UI、初始化系统等）。</param>
        protected async UniTaskVoid LoadSceneWithLoadingAsync(string sceneName, Func<CancellationToken, UniTask> afterLoadAction)
        {
            LoadingUI loadingUI = null;
            try
            {
                loadingUI = await GameModule.UI.ShowUIAsyncAwait<LoadingUI>();
                loadingUI?.SetProgress(0f);

                await GameModule.Scene.LoadSceneAsync(sceneName, progressCallBack: p =>
                {
                    loadingUI?.SetProgress(p);
                });

                if (_cts == null || _cts.IsCancellationRequested)
                {
                    if (loadingUI != null) GameModule.UI.CloseUI<LoadingUI>();
                    return;
                }

                await afterLoadAction.Invoke(_cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Log.Error($"[GameplayProcedureBase] 场景 {sceneName} 加载或初始化失败: {e}");
            }
            finally
            {
                if (loadingUI != null)
                {
                    GameModule.UI.CloseUI<LoadingUI>();
                }
            }
        }

        private void Cleanup()
        {
            GameModule.UI.CloseAll();
            GameModule.Timer.RemoveAllTimer();
            GameModule.Resource.UnloadUnusedAssets();
        }
    }
}
