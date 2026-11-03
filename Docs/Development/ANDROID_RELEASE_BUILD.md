# Android Release APK 构建

当前工程的发布基线为：包名 `com.hankjia.holdpoint`、IL2CPP、ARM64、Target API 34、Release Minify。Unity `6000.6.6f1` 会把旧的 Min API 24 请求钳制到本版本实际支持的 API 26，因此工程以 API 26 为真实下限，避免界面显示值与成品 Manifest 不一致。

## 一次性本机准备

1. 在 Unity Hub 为 `6000.6.6f1` 添加 Android Build Support、Android SDK & NDK Tools、OpenJDK。
2. 使用 Unity 随附 JDK 的 `keytool` 在仓库外生成发布 keystore，并至少制作两份离线备份。`.gitignore` 已屏蔽 `*.keystore` 与 `*.jks`。
3. 在本机用户环境变量中设置：
   - `HPS_KEYSTORE_PATH`
   - `HPS_KEYSTORE_PASSWORD`
   - `HPS_KEY_ALIAS`
   - `HPS_KEY_PASSWORD`

密码只保存在本机环境变量或密码管理器中，不写入 Unity 资源、文档、批处理或 Git 历史。

## 构建

打开 Unity 后执行 `HoldPoint Showdown > Build > Android Release APK`。输出位于 `Builds/Android/HoldPointShowdown-release.apk`。脚本会在 Android 模块或签名信息缺失时直接失败，不会生成无法发布的调试包。

## 真机验收

- 冷启动进入中文主菜单，按钮可点击。
- 双摇杆、射击、疾跑、跳跃、下蹲、换弹、交互、切枪和暂停均可触屏操作。
- 完整打完 9 分钟一局，能够显示结算并“再来一局”。
- 耳机可分辨左右枪声；受击方向提示与攻击者方位一致。
- 连续完成 3 局无崩溃、无软锁、无错误结算，再作为简历演示包发布。
