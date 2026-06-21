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
}