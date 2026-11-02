# 技术基线

## 固定版本

| 模块 | 版本 |
|---|---|
| Unity Editor | 6000.6.6f1 |
| Universal Render Pipeline | 17.6.0 |
| Input System | 1.20.0 |
| Netcode for GameObjects | 2.13.3 |
| Multiplayer Services | 2.4.1 |
| Multiplayer Play Mode | 3.0.0 |
| AI Navigation | 2.0.16 |
| Cinemachine | 6.6.0 |
| Test Framework | 1.8.0 |

未经技术验证，不在开发中途升级 Unity 主版本或上述核心包。

## 架构原则

- 主机权威：阶段、伤害、生命、物资、背包、共享箱、AI 和结算由主机裁定。
- 数据驱动：武器、物资、比赛规则和 AI 参数使用 ScriptableObject。
- 先纵向打通：每项功能同时包含运行逻辑、反馈、异常处理和测试。
- AI 与真人共享交互接口，不为 AI 复制一套物资或战斗规则。
- Runtime、Editor、Tests 使用程序集定义隔离。

## 初始场景

- `Bootstrap`：全局服务与启动入口。
- `MainMenu`：模式和房间入口。
- `TestGym`：移动、相机、战斗和网络技术验证。
- `ArenaTown`：正式城镇地图的灰盒迭代场景。

## 性能基线

- Windows：1080p，目标稳定 60 FPS。
- Android：中档设备 720p，目标稳定 30 FPS。
- Gameplay Update 中避免持续分配托管内存。
- 高频生成对象使用对象池；材质、粒子和实时阴影均设预算。
