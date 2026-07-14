import asyncio
import json
from mcp.client.streamable_http import streamable_http_client
from mcp import ClientSession

async def main():
    async with streamable_http_client("http://127.0.0.1:8080/mcp") as streams:
        read, write = streams[0], streams[1]
        async with ClientSession(read, write) as session:
            await session.initialize()
            result = await session.call_tool("read_console", arguments={
                "action": "get",
                "count": 500,
                "types": '["log"]',
                "format": "plain",
                "include_stacktrace": False
            })
            data = json.loads(result.content[0].text)
            filtered = [e for e in data["data"] if "SHAKE" in e or "TS_CHECK2" in e or "TEXT m_text_Hp" in e]
            print(json.dumps(filtered, ensure_ascii=False, indent=2))

asyncio.run(main())
