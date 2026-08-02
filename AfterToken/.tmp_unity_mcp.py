#!/usr/bin/env python
"""Unity MCP (mcp-for-unity-server) streamable HTTP 调用助手。
用法: python .tmp_unity_mcp.py <tool_name> [json_arguments]
      python .tmp_unity_mcp.py --list          列出所有工具
      python .tmp_unity_mcp.py --resource <uri> 读取资源
"""
import json
import sys
import urllib.request

URL = "http://localhost:8080/mcp"


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
    mcp = Mcp()
    if sys.argv[1] == "--list":
        r = mcp.call("tools/list", {})
        for t in r.get("result", {}).get("tools", []):
            print(f"- {t['name']}: {t.get('description', '')[:120]}")
        return
    if sys.argv[1] == "--resource":
        r = mcp.call("resources/read", {"uri": sys.argv[2]})
        print(json.dumps(r, ensure_ascii=False, indent=2)[:8000])
        return
    tool = sys.argv[1]
    args = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    r = mcp.call("tools/call", {"name": tool, "arguments": args})
    res = r.get("result", r)
    # MCP 工具返回 content 数组，提取文本
    content = res.get("content") if isinstance(res, dict) else None
    if content:
        for c in content:
            if c.get("type") == "text":
                print(c["text"][:8000])
    else:
        print(json.dumps(res, ensure_ascii=False, indent=2)[:8000])


if __name__ == "__main__":
    main()
