# HoldPoint Showdown / 红蓝据点枪战

一款使用 Unity 6 开发的 3D 卡通低模多人据点对抗射击游戏。首个作品集版本聚焦 2V2 垂直切片：物资发育、基地争夺与两命终局决战。

## 当前状态

- 阶段：项目基础设施与玩法原型
- Unity：6000.6.6f1
- 渲染：Universal Render Pipeline 17.6
- 目标平台：Windows（主开发）/ Android（后续适配）
- 首版模式：单机 1 人 + 3 AI、自定义 2V2 房间

## 仓库结构

```text
ArtSource/          美术源文件，不直接作为 Unity 导入目录
AudioSource/        音频源文件
Docs/               策划、技术、测试和项目管理文档
Portfolio/          作品集截图、视频脚本与架构图
ThirdPartyNotices/  第三方资产许可证和来源
UnityProject/       Unity 工程
```

Unity 工程位于 `UnityProject/HoldPointShowdown`。

## 打开项目

1. 安装 Unity `6000.6.6f1`。
2. 在 Unity Hub 中添加 `UnityProject/HoldPointShowdown`。
3. 首次打开等待 Package Manager 完成依赖解析。
4. 打开 `Assets/_Game/Scenes/TestGym.unity` 运行基础训练场。

键鼠原型操作：

- `WASD`：移动
- 鼠标：旋转视角
- `Space`：跳跃
- `Left Ctrl`：下蹲
- 鼠标左键：射击
- `R`：换弹
- `Esc`：释放/锁定鼠标

## 分支策略

- `main`：稳定、可演示版本。
- `develop`：日常集成分支。
- `codex/feature-*`：功能开发。
- `codex/fix-*`：缺陷修复。
- `codex/art-*`：需要独立评审的大型美术导入批次。

代码、美术、音频的长期分类使用目录，不使用长期资源分支。详细规则见 `Docs/ProjectManagement/BRANCHING.md`。

## 文档

- [首版 Demo 开发计划](Docs/ProjectManagement/红蓝据点枪战_首版Demo开发计划.md)
- [技术基线](Docs/Design/TECHNICAL_BASELINE.md)
- [贡献与提交规范](CONTRIBUTING.md)

## 许可证

当前仓库尚未声明开源许可证。未经仓库所有者明确授权，不应将项目代码或资产视为可自由再分发内容。
