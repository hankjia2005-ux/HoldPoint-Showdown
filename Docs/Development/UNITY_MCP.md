# Unity MCP 开发工具

本项目使用 [MCP for Unity](https://github.com/CoplayDev/unity-mcp) v10.3.0，让 Codex 通过本机 stdio MCP 服务直接控制 Unity Editor。

## 组成

- Unity Package：`com.coplaydev.unity-mcp`，固定到 `v10.3.0`。
- Codex Skill：`unity-mcp-orchestrator`，安装于用户级 Skill 目录。
- MCP Server：`mcpforunityserver==10.3.0`，安装于独立 Python 虚拟环境。
- Editor 启动器：`Assets/_Game/Editor/UnityMcpBootstrap.cs`，打开工程后自动启动 stdio Bridge。

## 使用方式

1. 打开 `UnityProject/HoldPointShowdown`。
2. 等待 Unity 完成脚本编译，确认 Console 没有编译错误。
3. 重新启动 Codex，使新增的 `unityMCP` 服务和 Skill 生效。
4. 如连接中断，在 Unity 菜单执行 `HoldPoint Showdown > Tools > Restart Unity MCP Bridge`。

## 验证标准

- Codex 能读取 `mcpforunity://editor/state` 和 `mcpforunity://project/info`。
- MCP 能列出当前场景、Console 和 Unity Test Framework 测试。
- 场景与 Prefab 修改必须通过 Unity API 完成，禁止直接编辑 `.unity`、`.prefab` 或 `.asset` YAML。

机器级虚拟环境和 Codex 配置不提交到 Git；仓库只提交可复现的 Package 版本、启动器和说明。
