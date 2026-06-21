using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DebugUILogger
{
    [MenuItem("Tools/Log UI Objects")]
    public static void LogUIObjects()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var sb = new StringBuilder();
        sb.AppendLine($"[DebugUILogger] Active canvases: {canvases.Length}");
        foreach (var canvas in canvases)
        {
            sb.AppendLine($" - {canvas.name}: layer={LayerMask.LayerToName(canvas.gameObject.layer)}, " +
                          $"renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, " +
                          $"enabled={canvas.enabled}, activeInHierarchy={canvas.gameObject.activeInHierarchy}, " +
                          $"childCount={canvas.transform.childCount}");
        }

        var uiRoots = GameObject.FindGameObjectsWithTag("UIRoot");
        sb.AppendLine($"[DebugUILogger] UIRoot count: {uiRoots.Length}");
        foreach (var root in uiRoots)
        {
            sb.AppendLine($" - {root.name} active={root.activeInHierarchy}");
        }

        Debug.Log(sb.ToString());
    }
}
