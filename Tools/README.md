# Tenlux Tools

这个目录放的是当前项目的本地工具脚本。

## 脚本列表

- `Measure-BackgroundState.ps1`
  测量 Tenlux 隐藏后台态的内存 / 句柄 / 线程基线。
  如遇外部旧实例占位，可用 `-InstanceSuffix` 启动隔离实例测量当前构建。
  测量完成后应确认当前工作区拉起的隔离实例已经退出。

- `Preview-PromoSite.ps1`
  本地启动宣传网页预览服务。

- `Run-ValidationSuite.ps1`
  一键跑基础验收流程：构建、后台态探测、宣传页探测。
  建议单独串行执行，不要再并行手动跑第二个 `dotnet build`。

- `Smoke-SettingsUi.ps1`
  针对已运行实例做快速设置页 UIA 冒烟检查。

- `Export-LaunchPack.ps1`
  导出营销资产、宣传页和说明文件打包。

- `Export-ReleaseBundle.ps1`
  导出真正给用户分发的软件发布包。

## 推荐顺序

1. 功能改动后先跑 `..\Debug\Debug-AllFeatures.ps1`
2. 只想快速看设置页是否能打开时跑 `Smoke-SettingsUi.ps1`
3. 发版前跑 `Run-ValidationSuite.ps1`
4. 要看宣传页时跑 `Preview-PromoSite.ps1`
5. 发宣传内容时用 `Export-LaunchPack.ps1`
6. 发软件时用 `Export-ReleaseBundle.ps1`
7. 若做隔离实例截图或测量，结束后确认只剩外部旧实例，不要留下当前工作区孤儿进程
