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
            with open("unity_logs_full.txt", "w", encoding="utf-8") as f:
                for e in data["data"]:
                    f.write(e + "\n")
            print("saved", len(data["data"]))

asyncio.run(main())
