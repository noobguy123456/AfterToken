using UnityEditor;
using UnityEditor.Compilation;

public static class ForceRecompile
{
    [MenuItem("Tools/Force Recompile")]
    public static void Recompile()
    {
        CompilationPipeline.RequestScriptCompilation();
        UnityEngine.Debug.Log("[ForceRecompile] Script compilation requested.");
    }
}
