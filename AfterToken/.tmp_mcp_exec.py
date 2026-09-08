#!/usr/bin/env python
"""从文件读取 C# 代码并调用 Unity MCP execute_code，避免 shell 引号转义问题。
用法: python .tmp_mcp_exec.py <code_file.cs>
"""
import json
import sys
import urllib.request

URL = "http://127.0.0.1:8080/mcp"


def _post(payload, session_id=None):
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
    }
    if session_id:
        headers["mcp-session-id"] = session_id
    req = urllib.request.Request(URL, data=json.dumps(payload).encode("utf-8"), headers=headers)
    resp = urllib.request.urlopen(req, timeout=300)
    sid = resp.headers.get("mcp-session-id", session_id)
    body = resp.read().decode("utf-8", errors="replace")
    result = None
    for line in body.splitlines():
        if line.startswith("data:"):
            result = json.loads(line[5:].strip())
    if result is None and body.strip():
        try:
            result = json.loads(body)
        except json.JSONDecodeError:
            result = {"raw": body}
    return result, sid


class Mcp:
    def __init__(self):
        init, self.sid = _post({
            "jsonrpc": "2.0", "id": 1, "method": "initialize",
            "params": {"protocolVersion": "2025-03-26", "capabilities": {},
                       "clientInfo": {"name": "kimi-cli", "version": "1.0"}}
        })
        if "error" in init:
            raise RuntimeError(f"initialize failed: {init['error']}")
        _post({"jsonrpc": "2.0", "method": "notifications/initialized"}, self.sid)
        self._id = 1

    def call(self, method, params):
        self._id += 1
        result, self.sid = _post({"jsonrpc": "2.0", "id": self._id, "method": method, "params": params}, self.sid)
        return result


def main():
    with open(sys.argv[1], "r", encoding="utf-8") as f:
        code = f.read()
    mcp = Mcp()
    r = mcp.call("tools/call", {"name": "execute_code", "arguments": {"action": "execute", "code": code}})
    res = r.get("result", r)
    content = res.get("content") if isinstance(res, dict) else None
    if content:
        for c in content:
            if c.get("type") == "text":
                print(c["text"][:16000])
    else:
        print(json.dumps(res, ensure_ascii=False, indent=2)[:16000])


if __name__ == "__main__":
    main()
