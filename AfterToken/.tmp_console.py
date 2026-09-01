import subprocess, json, sys

# 用法: python .tmp_console.py [filter_regex]
pattern = sys.argv[1] if len(sys.argv) > 1 else ""

code = r'''
var logEntriesType = System.AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
    .FirstOrDefault(t => t.FullName == "UnityEditor.LogEntries");
var logEntryType = System.AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
    .FirstOrDefault(t => t.FullName == "UnityEditor.LogEntry");
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var flagsI = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
int count = (int)logEntriesType.GetMethod("GetCount", flags).Invoke(null, null);
var entry = System.Activator.CreateInstance(logEntryType);
var getEntry = logEntriesType.GetMethod("GetEntryInternal", flags);
var msgField = logEntryType.GetField("message", flagsI);
var modeField = logEntryType.GetField("mode", flagsI);
var sb = new System.Text.StringBuilder();
int shown = 0;
for (int i = 0; i < count && shown < 40; i++)
{
    getEntry.Invoke(null, new object[] { i, entry });
    string m = (string)msgField.GetValue(entry);
    int mode = (int)modeField.GetValue(entry);
    var line = "[" + mode + "] " + m.Split('\n')[0];
    sb.AppendLine(line);
    shown++;
}
return "total=" + count + "\n" + sb.ToString();
'''
payload = json.dumps({"action": "execute", "code": code})
r = subprocess.run(["python", ".tmp_unity_mcp.py", "execute_code", payload],
                   capture_output=True, text=True, encoding="utf-8", errors="replace")
out = r.stdout
try:
    data = json.loads(out[out.index("{"):])
    print(data.get("data", {}).get("result", out))
except Exception:
    print(out[-2000:])
if r.stderr.strip():
    print("STDERR:", r.stderr[-300:], file=sys.stderr)
