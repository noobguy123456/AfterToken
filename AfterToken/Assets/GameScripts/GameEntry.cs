using TEngine;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    void Awake()
    {
        // 确保 UIRoot 在首个 Procedure 切场景前被标记为 DontDestroyOnLoad
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot != null)
        {
            DontDestroyOnLoad(uiRoot);
        }
        DontDestroyOnLoad(this);

        ModuleSystem.GetModule<IUpdateDriver>();
        ModuleSystem.GetModule<IResourceModule>();
        ModuleSystem.GetModule<IDebuggerModule>();
        ModuleSystem.GetModule<IFsmModule>();
        Settings.ProcedureSetting.StartProcedure().Forget();
    }

    void Start()
    {
        // 帧率上限跟随显示器刷新率（Start 中设置，保证在 RootModule.Awake 的默认值之后生效）。
        // 固定上限（如 120）与非整数倍刷新率（如 144Hz）错配会产生帧生产差拍
        // （实测帧时间 3ms/15ms 交替），移动时画面呈现为一抖一抖。
        int refreshRate = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
        Application.targetFrameRate = refreshRate > 0 ? refreshRate : 60;
    }
}