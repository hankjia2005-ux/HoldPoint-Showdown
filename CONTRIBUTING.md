# 贡献规范

## 开发流程

1. 从最新 `develop` 创建短期分支。
2. 一个分支只解决一个可验收目标。
3. 提交前确保 Unity 控制台无编译错误，并运行相关测试。
4. 通过 Pull Request 合入 `develop`。
5. 达到演示门禁后，由 `develop` 合入 `main` 并打版本标签。

## 分支命名

- `codex/feature-player-controller`
- `codex/feature-match-flow`
- `codex/fix-duplicate-loot`
- `codex/art-town-kit`
- `codex/docs-game-rules`

## 提交消息

采用简洁的 Conventional Commits 风格：

- `feat(player): add prototype third-person movement`
- `fix(combat): prevent duplicate damage on host`
- `art(environment): import town modular kit`
- `docs(plan): update milestone acceptance criteria`
- `test(gameflow): cover phase boundary transitions`

## Unity 资源约束

- 必须提交 `.meta` 文件。
- 禁止提交 `Library`、`Temp`、`Logs`、`obj` 和 `UserSettings`。
- 场景、Prefab、材质和 ScriptableObject 使用 Force Text。
- 大型二进制美术、音频和视频文件必须经过 Git LFS。
- 第三方资源在 `ThirdPartyNotices` 中记录来源、版本和许可证。
- 不要在仓库中提交 UGS 密钥、Android 签名文件或个人令牌。
