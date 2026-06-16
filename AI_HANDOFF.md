# AI Handoff

这是 Tenlux 的稳妥交接包，目标是：

- 源码完整
- 构建必需文件完整
- 说明文件完整
- 宣传网页完整
- 不带编译缓存垃圾

## 来源

- 主体源码：`D:\Codex\src-winui`
- 当前版本：`2.0.0Preview`
- 网页目录：当前包内的 `promo-site/`

## 已保留

- `Assets/`
- `Helpers/`
- `Pages/`
- `Properties/`
- `Tools/`
- `Marketing/`
- `promo-site/`
- `app.manifest`
- `Package.appxmanifest`
- `App.xaml`
- `App.xaml.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `README.md`
- `LICENSE.md`
- `RELEASE.md`
- `SUPPORT.md`
- `PROJECT.md`
- `ROADMAP.md`
- `CHANGELOG.md`
- `ToggleDarkMode.WinUI.csproj`

## 已排除

- `bin/`
- `obj/`
- `dist/`
- `publish_test/`
- `.claude/`

## 编译建议

```powershell
dotnet build .\ToggleDarkMode.WinUI.csproj -c Debug -p:Platform=x64
```

## 启动建议

```powershell
.\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\Tenlux.exe --open=Dashboard
```
