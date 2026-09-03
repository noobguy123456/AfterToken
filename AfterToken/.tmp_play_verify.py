import json, subprocess, sys, time

def mcp(code, timeout=90):
    payload = json.dumps({"action": "execute", "code": code})
    r = subprocess.run(["python", ".tmp_unity_mcp.py", "execute_code", payload],
                       capture_output=True, encoding="utf-8", errors="replace", timeout=timeout)
    out = r.stdout
    try:
        d = json.loads(out)
        if not d.get("success"):
            return "FAIL: " + out[-800:]
        return str(d.get("data", {}).get("result", ""))
    except Exception:
        return "RAW: " + out[-800:]

CONSOLE_READER = r'''
System.Type logEntriesType = null, logEntryType = null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) {
    System.Type[] types;
    try { types = a.GetTypes(); } catch { continue; }
    foreach (var t in types) {
        if (t.FullName == "UnityEditor.LogEntries") logEntriesType = t;
        if (t.FullName == "UnityEditor.LogEntry") logEntryType = t;
    }
}
int count = (int)logEntriesType.GetMethod("GetCount").Invoke(null, null);
var msgs = new System.Collections.Generic.List<string>();
var entry = System.Activator.CreateInstance(logEntryType);
var getEntry = logEntriesType.GetMethod("GetEntryInternal");
var msgField = logEntryType.GetField("message");
var modeField = logEntryType.GetField("mode");
int err = 0, warn = 0;
for (int i = count - 1; i >= 0; i--) {
    getEntry.Invoke(null, new object[]{ i, entry });
    string m = (string)msgField.GetValue(entry);
    if (m.Contains("[ERROR]") || m.Contains("Exception") || m.Contains("error CS")) { err++; if (msgs.Count < 15) msgs.Add(m.Split('\n')[0]); }
    else if (m.Contains("[WARNING]")) { warn++; if (msgs.Count < 15) msgs.Add("W: " + m.Split('\n')[0]); }
}
return "total=" + count + " errors=" + err + " warnings=" + warn + "\n" + string.Join("\n---\n", msgs);
'''

def console_errors():
    return mcp(CONSOLE_READER)

if __name__ == "__main__":
    pass
