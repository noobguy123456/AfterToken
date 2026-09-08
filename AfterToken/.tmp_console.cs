var asm = typeof(UnityEditor.Editor).Assembly;
var logEntries = asm.GetType("UnityEditor.LogEntries") ?? asm.GetType("UnityEditorInternal.LogEntries");
if (logEntries == null) return "no type";
var sb = new System.Text.StringBuilder();
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
// 先清再统计不行，直接读行
var startGetting = logEntries.GetMethod("StartGettingEntries", flags);
var getEntry = logEntries.GetMethod("GetEntryInternal", flags);
var endGetting = logEntries.GetMethod("EndGettingEntries", flags);
var entryType = asm.GetType("UnityEditor.LogEntry") ?? asm.GetType("UnityEditorInternal.LogEntry");
if (startGetting == null || getEntry == null || entryType == null) return "methods missing";
int total = (int)startGetting.Invoke(null, null);
var entry = System.Activator.CreateInstance(entryType);
int errors = 0;
var msgField = entryType.GetField("message") ?? entryType.GetField("condition");
var modeField = entryType.GetField("mode");
int shown = 0;
for (int i = total - 1; i >= 0 && shown < 8; i--)
{
    var args = new object[]{ i, entry };
    if (!(bool)getEntry.Invoke(null, args)) continue;
    entry = args[1];
    int mode = modeField != null ? (int)modeField.GetValue(entry) : 0;
    // mode bit: Error=1<<0? 实际 Error=1, Assert=2... 用 ScriptCompileError 位 (1<<8?) — 简化：包含 "error CS" 即显示
    string msg = (string)msgField.GetValue(entry) ?? "";
    if (msg.Contains("error CS") || msg.Contains("Error"))
    {
        var firstLine = msg.Split('\n')[0];
        sb.AppendLine(firstLine.Length > 200 ? firstLine.Substring(0,200) : firstLine);
        shown++;
    }
}
endGetting.Invoke(null, null);
return shown == 0 ? "no compile errors in console" : sb.ToString();
