using System;
using System.Collections.Generic;
using System.Reflection;
using GameLogic;
#if ENABLE_OBFUZ
using Obfuz;
#endif
using TEngine;
using UnityEngine;
#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameApp
{
    private static List<Assembly> _hotfixAssembly;
    private static IFsm<IProcedureModule> _procedureFsm;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        GameEventHelper.Init();
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Log.Warning("======= Entrance GameApp =======");
        Utility.Unity.AddDestroyListener(Release);
        Log.Warning("======= StartGameLogic =======");
        StartGameLogic();

#if UNITY_EDITOR
        // 临时挂载 Alt+Tab 诊断组件。
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot != null && uiRoot.GetComponent<GameLogic.FocusPauseDiagnostics>() == null)
        {
            uiRoot.AddComponent<GameLogic.FocusPauseDiagnostics>();
        }

        // 如果 Play Mode 期间发生了程序集重载，尝试恢复到之前的流程。
        TryResumeLastProcedure();
#endif
    }

#if UNITY_EDITOR
    private static void TryResumeLastProcedure()
    {
        var lastProcedure = ProcedureStateRecorder.GetLastProcedure();
        if (string.IsNullOrEmpty(lastProcedure) || lastProcedure == nameof(ProcedureMainMenu))
        {
            return;
        }

        Log.Warning($"[GameApp] 检测到程序集重载，准备从主菜单恢复到 {lastProcedure}");

        // Domain Reload 后 UIModule 的窗口堆栈已丢失，但旧 Panel GameObject 可能还挂在 UIRoot 下，需要手动清理。
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot != null)
        {
            var canvas = uiRoot.GetComponentInChildren<Canvas>(true)?.transform;
            if (canvas != null)
            {
                for (int i = canvas.childCount - 1; i >= 0; i--)
                {
                    var child = canvas.GetChild(i);
                    if (child != null)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                }
            }
        }

        GameModule.UI.CloseAll();

        var oldBattleRoot = GameObject.Find("BattleRoot");
        if (oldBattleRoot != null)
        {
            UnityEngine.Object.Destroy(oldBattleRoot);
        }

        switch (lastProcedure)
        {
            case nameof(ProcedureLobby):
                ChangeProcedure<ProcedureLobby>();
                break;
            case nameof(ProcedureBattle):
                ChangeProcedure<ProcedureBattle>();
                break;
            default:
                Log.Warning($"[GameApp] 未知流程 {lastProcedure}，保持主菜单。");
                break;
        }
    }
#endif
    
    private static void StartGameLogic()
    {
        Log.Warning("======= Start Battle Game Logic =======");

        // 热更后重新初始化流程管理器，注册主菜单、大厅、战斗流程。
        var procedureModule = GameModule.Procedure;
        var procedureModuleType = procedureModule.GetType();
        var shutdownMethod = procedureModuleType.GetMethod("Shutdown", BindingFlags.Public | BindingFlags.Instance);
        shutdownMethod?.Invoke(procedureModule, null);

        var fsmModule = ModuleSystem.GetModule<IFsmModule>();
        var procedures = new ProcedureBase[]
        {
            new GameLogic.ProcedureMainMenu(),
            new GameLogic.ProcedureLobby(),
            new GameLogic.ProcedureBattle(),
        };
        procedureModule.Initialize(fsmModule, procedures);
        _procedureFsm = GetProcedureFsm(procedureModule);
        procedureModule.StartProcedure<GameLogic.ProcedureMainMenu>();
    }

    /// <summary>
    /// 切换流程（FSM 已运行时通过反射调用内部 ChangeState）。
    /// 切换前先强制关闭所有 UI，避免旧流程 UI 残留叠加。
    /// </summary>
    public static void ChangeProcedure<T>() where T : ProcedureBase
    {
        if (_procedureFsm == null)
        {
            Log.Error("[GameApp] Procedure FSM not initialized.");
            return;
        }

        Log.Debug($"[GameApp] 准备切换流程到 {typeof(T).Name}，先关闭所有 UI");
        GameModule.UI.CloseAll();

        try
        {
            var method = _procedureFsm.GetType().GetMethod("ChangeState", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (method == null)
            {
                Log.Error("[GameApp] 找不到 FSM ChangeState 方法，流程切换失败。");
                return;
            }
            method.MakeGenericMethod(typeof(T)).Invoke(_procedureFsm, null);
            Log.Debug($"[GameApp] 已切换流程到 {typeof(T).Name}");
        }
        catch (Exception e)
        {
            Log.Error($"[GameApp] 切换流程到 {typeof(T).Name} 失败: {e}");
        }
    }

    private static IFsm<IProcedureModule> GetProcedureFsm(IProcedureModule procedureModule)
    {
        var field = procedureModule.GetType().GetField("_procedureFsm", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IFsm<IProcedureModule>)field?.GetValue(procedureModule);
    }
    
    private static void Release()
    {
        CursorManager.Release();
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}
