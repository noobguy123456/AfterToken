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
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            EnterAsync().Forget();
        }

        protected abstract UniTaskVoid EnterAsync();

        protected override void OnLeave(IFsm<IProcedureModule> procedureOwner, bool isShutdown)
        {
            Log.Debug($"[GameplayProcedureBase] OnLeave 开始，isShutdown={isShutdown}，当前 UI 栈数量={GameModule.UI.WindowCount}");
            base.OnLeave(procedureOwner, isShutdown);
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            Cleanup();
            Log.Debug($"[GameplayProcedureBase] OnLeave 完成，当前 UI 栈数量={GameModule.UI.WindowCount}");
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
                Log.Debug($"[GameplayProcedureBase] 准备加载场景 {sceneName}，先显示 LoadingUI");
                loadingUI = await GameModule.UI.ShowUIAsyncAwait<LoadingUI>();
                loadingUI?.SetProgress(0f);
                Log.Debug($"[GameplayProcedureBase] LoadingUI 已显示，开始加载场景 {sceneName}");

                await GameModule.Scene.LoadSceneAsync(sceneName, progressCallBack: p =>
                {
                    loadingUI?.SetProgress(p);
                });

                Log.Debug($"[GameplayProcedureBase] 场景 {sceneName} 加载完成");

                if (_cts == null || _cts.IsCancellationRequested)
                {
                    Log.Debug($"[GameplayProcedureBase] 场景 {sceneName} 加载后流程已被取消，跳过后续逻辑");
                    if (loadingUI != null) GameModule.UI.CloseUI<LoadingUI>();
                    return;
                }

                await afterLoadAction.Invoke(_cts.Token);
                Log.Debug($"[GameplayProcedureBase] 场景 {sceneName} 后续逻辑执行完成");
            }
            catch (OperationCanceledException)
            {
                Log.Debug($"[GameplayProcedureBase] 场景 {sceneName} 加载已取消。");
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
            Log.Debug("[GameplayProcedureBase] Cleanup：关闭所有 UI、Timer、卸载资源");
            GameModule.UI.CloseAll();
            Log.Debug($"[GameplayProcedureBase] CloseAll 后 UI 栈数量={GameModule.UI.WindowCount}");
            GameModule.Timer.RemoveAllTimer();
            GameModule.Resource.UnloadUnusedAssets();
        }
    }
}
