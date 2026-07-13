#if UNITY_EDITOR
using UnityEditor;
using GameLogic;

/// <summary>
/// 在 Play Mode 期间锁定程序集重载，防止切回编辑器时因脚本编译触发 Domain Reload，
/// 导致战斗系统静态状态/事件监听丢失而卡死。
/// </summary>
[InitializeOnLoad]
public static class LockAssemblyReloadInPlayMode
{
    static LockAssemblyReloadInPlayMode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
                EditorApplication.LockReloadAssemblies();
                UnityEngine.Debug.Log("[LockAssemblyReload] Play Mode 已锁定程序集重载。");
                break;

            case PlayModeStateChange.ExitingPlayMode:
                EditorApplication.UnlockReloadAssemblies();
                ProcedureStateRecorder.Clear();
                UnityEngine.Debug.Log("[LockAssemblyReload] Play Mode 结束，已解锁程序集重载并清除流程记录。");
                break;
        }
    }
}
#endif
