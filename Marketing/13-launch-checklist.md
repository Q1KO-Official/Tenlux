# Tenlux / 执光 发布前检查清单

## 产品本体

- `dotnet build ToggleDarkMode.WinUI.csproj -p:Platform=x64` 成功
- `powershell -ExecutionPolicy Bypass -File .\Tools\Run-ValidationSuite.ps1` 至少跑过一遍
  运行验收脚本时不要并行再起第二个构建，避免 `obj` 文件锁冲突
- 如需准备正式分发包，`powershell -ExecutionPolicy Bypass -File .\Tools\Run-ValidationSuite.ps1 -IncludeReleaseBundle` 至少跑过一遍
- 如系统里已有旧 Tenlux 占位，至少跑过一次：
  `powershell -ExecutionPolicy Bypass -File .\Tools\Measure-BackgroundState.ps1 -InstanceSuffix codexqa`
- 托盘图标可见，单击 / 双击逻辑符合当前设置
- 深浅模式切换正常
- 壁纸联动正常
- 全局热键正常
- 定时切换时间配置正常
- 设置窗口关闭后能回到托盘后台态

## 视觉检查

- 至少有一张当前工作区版本的真实窗口截图产物
- 首页文案没有截断
- 设置页标题区显示产品名、副标题和版本号
- Hotkey / Wallpaper / About / Onboarding 页面没有明显布局错位
- 深色 / 浅色模式下文本对比度正常

## 发布素材

- `03-bilibili.md` 已按本次版本调整
- `04-douyin.md` 已按本次版本调整
- `05-xiaohongshu.md` 已按本次版本调整
- `06-xiaoheihe.md` 已按本次版本调整
- `08-release-notes.md` 已填入本次更新重点
- `12-faq.md` 中回答与当前版本一致

## 宣传网页

- `promo-site/index.html` 本地可访问
- 标题、Hero、Go To Market 区块存在
- 样式文件加载正常
- 所有本地链接都能打开对应文件

## 社区运营

- 评论回复模板准备好
- 两周内容日历已排好
- 首发封面图和录屏已完成
