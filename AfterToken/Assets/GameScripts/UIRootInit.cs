using UnityEngine;

/// <summary>
/// 确保 UIRoot 在场景切换时不被销毁。
/// </summary>
public class UIRootInit : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
