# Tenlux 项目完整说明文档

## 一、项目概述

Tenlux（执光）是一款轻量级 Windows 深色/浅色模式切换工具。用户通过系统托盘图标一键切换系统主题，支持壁纸自动切换、全局快捷键、定时切换、通知提示。

- **作者**：Q1KO (GitHub: Q1KO-Official)
- **许可证**：CC BY-NC-SA 4.0（与应用内 About 页保持一致）
- **版本**：2.0.0（从程序集元数据读取并展示）
- **名字来源**：Ten(拉丁语 tenēre=执) + Lux(光) = 执光，意为"主动掌控光与暗"

## 二、技术栈

| 项目 | 详情 |
|------|------|
| 框架 | .NET 10 + WinUI 3 (Windows App SDK 2.1.3) |
| 目标框架 | `net10.0-windows10.0.26100.0` |
| 最低系统 | Windows 10 v1809 (build 17763) |
| 平台 | x64/x86/ARM64 |
| 打包模式 | `WindowsPackageType=None`（非打包，直接跑 exe） |
| UI 控件库 | CommunityToolkit.WinUI.Controls.SettingsControls 8.2 |
| 托盘图标 | H.NotifyIcon.WinUI 2.4.1 |
| 架构模式 | code-behind，无 MVVM |
| 背景材质 | MicaBackdrop（毛玻璃，懒加载） |

## 三、项目结构

```
src-winui/
├── README.md                           # 仓库入口说明
├── CHANGELOG.md                        # 版本变更摘要
├── RELEASE.md                          # 当前版本发布说明
├── ROADMAP.md                          # 后续路线图
├── SUPPORT.md                          # 支持说明
├── LICENSE.md                          # 当前许可证文件
├── App.xaml / App.xaml.cs              # 应用入口，Mutex 单实例检查
├── MainWindow.xaml / .cs               # 主窗口，所有核心逻辑
├── MainWindow.xaml                     # 仅含 <Frame x:Name="RootFrame"/>
├── ToggleDarkMode.WinUI.csproj         # 项目文件
├── app.manifest                        # 应用清单
├── Assets/
│   ├── AppIcon.ico                     # 应用图标（exe icon）
│   ├── dark.ico                        # 托盘深色图标（4KB）
│   ├── light.ico                       # 托盘浅色图标（4KB）
│   └── tray-guide.png                  # 托盘固定引导截图
├── Helpers/
│   ├── NativeMethods.cs                # Win32 P/Invoke 声明
│   ├── ThemeHelper.cs                  # 注册表主题读写 + WM_SETTINGCHANGE 广播
│   ├── WallpaperHelper.cs              # IDesktopWallpaper COM 接口
│   ├── WallpaperTheme.cs               # 壁纸主题数据模型
│   ├── SettingsManager.cs              # 配置文件读写
│   ├── Localizer.cs                    # 三语言本地化
│   ├── StartupHelper.cs                # 注册表开机自启动
│   ├── ToastHelper.cs                  # Windows Toast 通知
│   ├── ImageHelper.cs                  # 图片加载辅助
│   ├── UiCleanupHelper.cs              # 页面/图片资源释放辅助
│   ├── ProductInfo.cs                  # 产品元数据统一入口
│   └── SimpleCommand.cs                # ICommand 简单实现
├── Marketing/                          # 对外传播文案、平台脚本、发布节奏
├── Tools/                              # 验收、预览、打包、支持脚本
├── promo-site/                         # 静态宣传网页
└── Pages/
    ├── SettingsPage.xaml / .cs         # 主框架页（NavigationView + Frame）
    ├── DashboardPage.xaml / .cs        # 首页仪表盘
    ├── GeneralPage.xaml / .cs          # 常规设置
    ├── HotkeyPage.xaml / .cs           # 快捷键设置
    ├── WallpaperPage.xaml / .cs        # 壁纸容器页
    ├── WallpaperOverviewPage.xaml / .cs # 壁纸预设列表
    ├── WallpaperEditPage.xaml / .cs     # 壁纸编辑
    ├── AboutPage.xaml / .cs            # 关于页
    └── OnboardingPage.xaml / .cs       # 首次运行引导
```

## 四、核心架构

### 4.1 启动流程

1. `App.OnLaunched()` → 检查 Mutex 单实例，加载 SettingsManager，创建 MainWindow
   - 如果已有实例在运行，则通过 `Tenlux_ShowSettings` 事件通知已有实例显示设置窗口
2. MainWindow 构造函数：
   - `ExtendsContentIntoTitleBar = true`（自定义标题栏）
   - 窗口最小 480x560，初始 640x560（DPI 感知）
   - 设置 `WS_EX_NOREDIRECTIONBITMAP` 防白闪
   - 设置 `DWMWA_CLOAK` 隐藏窗口
   - 加载深浅托盘图标（BitmapImage，16x16）
   - 创建托盘图标（H.NotifyIcon.TaskbarIcon）
   - 注册全局热键（如果已启用）
   - 启动定时切换
   - 拦截 `AppWindow.Closing` → 隐藏窗口而非关闭
   - 首次运行 → 显示 OnboardingPage + RevealWindow(showImmediately: true)
   - 非首次 → 窗口隐藏，等托盘操作触发 ShowSettings()

### 4.2 主题切换流程（MainWindow.ToggleTheme()）

```
1. _isLight = !_isLight  // 翻转标志
2. _toggling = true      // 防止 SyncThemeFromSystem 重入
3. 更新托盘图标和工具提示
4. RootFrame.RequestedTheme = isLight ? Light : Dark
5. 标题栏按钮颜色同步
6. DWMWA_USE_IMMERSIVE_DARK_MODE 设置
7. 显示 Toast 通知（如果启用）
8. 刷新首页 / 壁纸页预览
9. ThreadPool → ThemeHelper.ApplyThemeToggle() // 写注册表 + 广播
10. ThreadPool → WallpaperHelper.SetWallpaper() // 切壁纸（如果启用）
```

### 4.3 窗口显示/隐藏机制

**隐藏（Closing handler）：**
1. `args.Cancel = true` — 窗口永不关闭
2. `Settings.FlushPendingSave()` 强制写入待保存配置
3. 取消延迟 trim / 渲染回调等待执行 UI 工作
4. `UiCleanupHelper.ReleaseFrame()` 释放页面、图片和缓存
5. 释放 Mica：`SystemBackdrop = null`
6. 清除静态引用：`SettingsPage.ClearInstance()`, `DashboardPage.ClearInstance()`, `WallpaperOverviewPage.ClearInstance()`
7. 释放 COM 对象：`WallpaperHelper.Release()`（如果壁纸切换关闭）
8. 释放 Toast：`ToastHelper.Release()`
9. `DWMWA_CLOAK` 隐藏窗口
10. `CompactMemory()`：LOH compact + GC + TrimWorkingSet

**显示（ShowSettings → RevealWindow）：**
1. 设置 DWM 暗色模式 + RequestedTheme + MicaBackdrop（懒加载）
2. 窗口居中
3. `Content.Opacity = 0`
4. `AppWindow.Show()`（窗口显示但 cloaked）
5. 等待 `CompositionTarget.Rendering` 事件
6. `DwmFlush()` 等 DWM 合成 Mica
7. `DWMWA_CLOAK = 0` uncloak
8. `Content.Opacity = 1`
9. 延迟 `ScheduleTrim()`，在页面加载后收一次工作集

### 4.4 全局热键

使用 `WH_KEYBOARD_LL` 低级键盘钩子实现（不用 WinUI 的 KeyboardAccelerator）。

- 解析 `HotkeyText` 字符串（如 "Ctrl+Alt+D"）为修饰键 + 虚拟键码
- 钩子回调里先比 vkCode（整数比较），不匹配直接 return
- 匹配后检查修饰键状态（GetAsyncKeyState 5 次）
- 支持全屏时禁用（`IsFullscreenAppActive()` — 比较前台窗口尺寸和屏幕尺寸）
- 注意：钩子注入所有进程的消息循环，每次系统按键都触发回调

### 4.5 定时切换

- `System.Threading.Timer` 单次触发，每次触发后重新计算下一个时间点
- 支持跨天（如 23:00 切深色，07:00 切浅色）
- 监听 `PowerModeChanged` 事件，从休眠恢复时重新计算

## 五、各页面详解

### 5.1 SettingsPage — 主框架

- `NavigationView`（左紧凑模式，默认折叠）+ `ContentFrame` 导航
- 顶部自定义标题栏：产品名 / 副标题 / 版本 chip
- 5 个导航项：Dashboard、General、Hotkey、Wallpaper（MenuItems）、About（FooterMenuItems）
- `NavigateTo(string tag)` 支持程序化导航 + 展开 HotkeyPage 子菜单
- `ClearPageCache()` 窗口隐藏时清除导航项和缓存
- 导航动画：`EntranceNavigationTransitionInfo`

### 5.2 DashboardPage — 首页仪表盘

- **左 2/3**：壁纸预览（`PreviewBorder`，屏幕比例，`SizeChanged` 事件动态计算高度）
- **右 1/3**：状态面板（FontIcon + 模式文字 + 切换按钮 + 热键/定时信息）
  - 信息面板只在有热键或定时设置时显示
  - 定时开启时显示下一次切换时间
  - 切换按钮调用 `MainWindow.Instance.ToggleTheme()`
- **下方 2x2 卡片**：4 个 Border，每个含可点击 Button（标题 + "›"）+ ToggleSwitch
  - 标题点击跳转到对应页面（Tag: Wallpaper/HotkeyExpand/Schedule/Toast）
  - 跳转到 HotkeyPage 时自动展开对应 SettingsExpander
- 壁纸预览随主题切换更新（`RefreshPreviewIfVisible()` 被 MainWindow 调用）

### 5.3 GeneralPage — 常规设置

- 开机自启（ToggleSwitch → StartupHelper）
- 语言切换（ComboBox → Localizer.Lang）
- 配置迁移（两个 DropDownButton）：
  - 导出 ▾ → 配置口令（GZip+Base64 复制到剪贴板）/ 配置文件（.tx zip 保存）
  - 导入 ▾ → 配置口令（ContentDialog 粘贴）/ 配置文件（打开 .tx zip）
- 重置设置（恢复默认值，但不删除已导入壁纸文件）

### 5.4 HotkeyPage — 快捷键设置

4 个 SettingsExpander，每个可展开显示子项：

- **托盘点击**：ToggleSwitch + ComboBox（单击/双击模式）
- **全局热键**：ToggleSwitch + TextBox（捕获按键组合）+ "全屏时禁用"子项
- **定时切换**：ToggleSwitch + 两个 TimePicker（24 小时制）
- **通知提示**（默认折叠）："切换时显示通知" + "开启通知音效"
- 页首提示：推荐使用 `Ctrl + Alt + 字母` 避免热键冲突

`ExpandSection(string section)` 方法可程序化展开指定 Expander，其他自动折叠。

### 5.5 WallpaperPage / WallpaperOverviewPage / WallpaperEditPage

- WallpaperPage 是容器，内含 SubFrame（CacheSize=1）
- WallpaperOverviewPage：壁纸预览（屏幕比例动画）+ 预设卡片网格（2 列，最多 4 个）
  - 预设卡片：双图（左浅右深）、名称标签、hover 遮罩 + Apply/Delete 按钮
  - Delete 逻辑：移位填补空缺，自动命名
  - 页面提示：建议固定 1 组当前启用预设
- WallpaperEditPage：接收导航参数（索引 0-3），编辑预设名/深浅壁纸/显示模式

### 5.6 AboutPage

- 应用图标 + 名称 + 版本描述
- 版本号（程序集元数据读取）、GitHub 链接、开发者、"查看教程"按钮
- 状态说明：`轻量托盘工具 / WinUI 3 / 深浅模式联动`
- 使用指南入口（重新打开首次引导）
- 许可证 Expander（CC BY-NC-SA 4.0）

### 5.7 OnboardingPage — 首次引导

6 步流程：欢迎 → 主题切换 → 壁纸自动化 → 自定义热键 → 开机自启 → 准备就绪

- 语言选择（ComboBox）在欢迎页
- 欢迎页增加引导提示文案
- 底部：Skip/Previous/Next 按钮 + 6 个圆点指示器
- 完成后：设置 `FirstRunDone=true`，导航到 SettingsPage，显示托盘教程 ContentDialog

## 六、本地化系统

`Localizer.cs` 使用硬编码三语言字符串数组。

**语言索引**：0=简体中文，1=English，2=繁體中文

**当前共 136 个字符串常量，已实现 127 个字符串项**（当前索引 0-135，仍保留若干历史空洞索引），跳过了 31, 34-36, 52-53, 70-71, 74。

**添加新字符串步骤**：
1. 在 `const int` 区域添加命名常量（如 `S_NewString = 108`）
2. 在 `Strings` 数组末尾追加 `new[] { "简体", "English", "繁體" }`
3. 在对应页面的 `ApplyLabels()` 方法中使用 `T(S_NewString)`

**繁体中文注意**：
- 壁纸 → 桌布（不是"壁紙"）
- 快捷键 → 快速鍵（不是"快捷鍵"）
- 通知提示（不用"气泡提示"，不用"Toast"）
- 开机自启 → 隨開機自動執行
- 迁移 → 移轉
- 配置口令 → 設定碼（不是"設定代碼"）
- 启用/关闭（时间选择器）→ 開始時間：/ 結束時間：

**ToggleSwitch 特殊处理**：WinUI 3 的 ToggleSwitch 默认 On/Off 文字跟随系统语言，需显式设置 `OnContent`/`OffContent`（使用 `S_On`/`S_Off` 索引 75/76）。

## 七、配置文件

**路径**：`%AppData%\Tenlux\Tenlux.cfg`（纯文本 key=value）

**旧版路径**：`%USERPROFILE%\Documents\ToggleDarkMode\ToggleDarkMode.cfg`（首次加载时自动迁移）

**所有配置项**：
```
Language=0/1/2
FirstRunDone=0/1
SingleClickToggle=0/1
TrayClickEnabled=0/1
AutoSwitchWallpaper=0/1
ScheduledSwitch=0/1
LightTime=HH:MM
DarkTime=HH:MM
GlobalHotkey=0/1
HotkeyText=Ctrl+Alt+D
DisableHotkeyInFullscreen=0/1
ToastNotification=0/1
ToastSound=0/1
Theme0_Name=xxx
Theme0_Dark=path
Theme0_Light=path
Theme0_Style=0/1/2/3
Theme0_Enabled=0/1
（Theme1-3 同上，最多 4 组壁纸预设）
```

**导出/导入**：
- 口令：GZip + Base64URL 压缩（前缀 `TX1.`），解压上限 1MB 防 zip bomb
- 文件：`.tx` zip 包（含壁纸图片），通过 FileSavePicker/FileOpenPicker 操作
- 重置设置：恢复默认值，同时关闭开机自启，保留现有壁纸文件

**导出 key 映射**：SCT/TCE/LNG/ASW/SCH/LT/DT/GHK/HKT/DHK/TN/TS/T{i}N/T{i}S/T{i}E

## 八、构建与运行

```bash
# 编译
dotnet build 'D:/Codex/src-winui/ToggleDarkMode.WinUI.csproj' -c Debug -p:Platform=x64 --nologo -v q

# 运行
"D:/Codex/src-winui/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/Tenlux.exe"

# 杀进程
taskkill //F //IM Tenlux.exe
```

```powershell
# 测量隐藏后台态基线
powershell -ExecutionPolicy Bypass -File .\Tools\Measure-BackgroundState.ps1

# 本地预览宣传网页
powershell -ExecutionPolicy Bypass -File .\Tools\Preview-PromoSite.ps1

# 一键跑构建 + 后台态测量 + 宣传页探测
powershell -ExecutionPolicy Bypass -File .\Tools\Run-ValidationSuite.ps1

# 导出营销素材与说明文件打包
powershell -ExecutionPolicy Bypass -File .\Tools\Export-LaunchPack.ps1

# 导出可分发的软件发布包
powershell -ExecutionPolicy Bypass -File .\Tools\Export-ReleaseBundle.ps1
```

说明：

- 如果系统里已经有来自其他路径的 `Tenlux.exe` 实例在运行，脚本会返回 `BlockedByOtherInstance`
- 这是单实例保护在生效，不是脚本错误

**重要**：
- `WindowsPackageType=None` 不能用 `winapp run`，必须直接跑 exe
- 编译成功后必须杀旧进程再启动新版本
- 发布时 csproj 有 MSBuild target 删除 16 个不用的 DLL（onnxruntime、DirectML、WebView2 等）

## 九、P/Invoke 注意事项（NativeMethods.cs）

- 句柄类型混用 `IntPtr` 和 `nint`（运行时等价，风格不一致）
- `SetWindowLongPtrW` 用于 `GWLP_WNDPROC`（正确），`GetWindowLong`/`SetWindowLong` 仅用于 `GWL_EXSTYLE`（32 位安全）
- `NOTIFYICONDATA.cbSize` 用 `Marshal.SizeOf<NOTIFYICONDATA>()` 动态计算（跨架构安全）
- `SetPreferredAppMode` 使用未文档化的 `uxtheme.dll #135` 序号导出
- `IActiveDesktop` COM 接口只声明了 7 个方法（vtable 有 14 个），只调前 2 个所以安全
- `GetWindowHandle` 缓存 HWND，单窗口应用不会失效

## 十、已知问题

1. `AccentColorMenu` 注册表值被旧版设为 0 会导致任务栏颜色异常（已不主动设置此值，用 Windows 设置切一次即可恢复）
2. 打开设置窗口时右上角 X 按钮会短暂高亮（WinUI 3 + ExtendsContentIntoTitleBar + DWMWA_CLOAK 的已知行为，WinAppSDK 1.8 修复了类似 bug #10529，但我们用 2.1.3 仍有此现象）
3. `WindowsPackageType=None` 不能用 `winapp run`
4. ToggleSwitch 的 On/Off 文字需手动设置

## 十一、修改指南

### 添加新设置项
1. `SettingsManager.cs`：添加属性 + Load case + Save 条目 + Export/Import key
2. 对应 `Page.xaml`：添加 UI 控件
3. `Page.xaml.cs`：处理事件 + `Cfg.Save()`

### 添加新页面
1. `Pages/` 下创建 Page
2. `SettingsPage.xaml.cs`：`OnNavSelectionChanged` 加路由，`ApplyNavLabels` 加标签
3. 导航项在 `OnLoaded` 中创建并添加到 `Nav.MenuItems`

### 添加新本地化字符串
1. `Localizer.cs`：`const int` 常量 + `Strings` 数组追加
2. 对应页面 `ApplyLabels()` 中使用 `T(S_XXX)`

### 首页 DashboardPage 卡片跳转
- 卡片按钮的 `Tag` 属性决定跳转目标
- `Tag="Wallpaper"` → 壁纸页面
- `Tag="HotkeyExpand"` → 快捷键页面，展开全局热键
- `Tag="Schedule"` → 快捷键页面，展开定时切换
- `Tag="Toast"` → 快捷键页面，展开通知提示
- `SettingsPage.NavigateTo(tag)` 处理映射和展开逻辑

## 十四、营销资产

- `Marketing/README.md`：营销资料索引
- `01-positioning.md`：产品定位
- `02-launch-plan.md`：发布节奏
- `03-bilibili.md`：B 站视频脚本
- `04-douyin.md`：抖音短视频脚本
- `05-xiaohongshu.md`：小红书图文
- `06-xiaoheihe.md`：小黑盒帖子
- `07-press-kit.md`：媒体素材包说明
- `08-release-notes.md`：版本更新文案
- `09-platform-fit.md`：平台适配说明
- `10-comment-replies.md`：评论回复模板
- `11-content-calendar.md`：两周内容日历
- `12-faq.md`：常见问题说明
- `13-launch-checklist.md`：发布前检查清单
- `14-shotlist.md`：录屏与截图拍摄清单
- `15-release-package.md`：对外营销发布包说明
- `16-release-bundle.md`：软件发布产物导出说明
- `LICENSE.md`：当前对外许可证文件
- `Tools/README.md`：工具脚本索引

## 十五、宣传网页

- `promo-site/index.html`：静态宣传网页入口
- `promo-site/styles.css`：页面样式
- 直接打开 `index.html` 即可本地预览

## 十六、运行态基线

- 测试时间：`2026-06-07 11:01:59`
- 测试方式：启动 `bin/x64/Debug/.../Tenlux.exe`，等待约 4 秒，读取进程占用后退出
- 当前隐藏后台态基线：
  - Working Set：`31.93 MB`
  - Private Memory：`9.72 MB`
  - Threads：`8`
  - Handles：`275`

说明：

- 这条数据用于后续继续优化“未打开设置 / 设置关闭后”的后台状态
- 它不是发布指标，只是当前版本在本机上的一次实际测量基线

### 当前工作区构建隔离实例测量

- 测试时间：`2026-06-07 12:32:25`
- 实例后缀：`codexqa`
- 当前 `D:\Codex\src-winui` 构建后台态：
  - Working Set：`31.87 MB`
  - Private Memory：`9.68 MB`
  - Threads：`8`
  - Handles：`273`

说明：

- 这次测量使用 `TENLUX_INSTANCE_SUFFIX` 隔离实例机制，绕开了系统里外部旧 Tenlux 实例的单实例占位

### 当前工作区构建窗口截图验证

- `Dashboard` 页截图已成功生成：
  - `dist/Tenlux-DashboardCapture.png`
- `General / Hotkey / Wallpaper / About` 页的逐页截图路径已打通一部分，但当前脚本在等待主窗口句柄阶段还不够稳定

说明：

- 这意味着当前版本已经有窗口级可视证据，但逐页截图流程仍需继续打磨成稳定工具

## 十七、文件依赖关系

```
App.xaml.cs
  ├── SettingsManager (Load/Save)
  ├── MainWindow (创建实例)
  ├── NativeMethods (Mutex / Window APIs)
  └── ProductInfo (产品元数据)

MainWindow.xaml.cs
  ├── ThemeHelper (主题切换)
  ├── WallpaperHelper (壁纸切换)
  ├── ToastHelper (通知)
  ├── NativeMethods (Win32 API)
  ├── SettingsManager (配置)
  ├── Localizer (本地化)
  ├── UiCleanupHelper (资源释放)
  ├── SimpleCommand (ICommand)
  └── Pages/* (导航)

Pages/*
  ├── SettingsManager (读写配置)
  ├── Localizer (UI 文字)
  ├── ThemeHelper (读取当前主题)
  ├── WallpaperHelper (壁纸操作)
  ├── StartupHelper (开机自启)
  ├── ImageHelper (图片加载)
  └── MainWindow.Instance (窗口操作)
```

## 十八、外部目录说明

- 根目录下 `src/`、`build/`、`installer/`、`scripts/` 是**旧版遗弃项目**，不要修改
- 唯一活跃项目在 `src-winui/`
- `backups/` 是备份文件（当前最新：`backups/v2.0.0/`）
- `installer/Tenlux.iss` 是 Inno Setup 安装脚本
- `Tools/Measure-BackgroundState.ps1` 用于重复测量 Tenlux 隐藏后台态基线
- `Tools/Preview-PromoSite.ps1` 用于本地启动宣传网页预览服务
- `Tools/Run-ValidationSuite.ps1` 用于一键执行当前基础验收流程
- `Tools/Export-LaunchPack.ps1` 用于导出营销素材、宣传页与说明文件打包
- `Tools/Export-ReleaseBundle.ps1` 用于导出可分发的软件发布包
- `Tools/README.md` 用于集中说明这些脚本的用途和推荐顺序
