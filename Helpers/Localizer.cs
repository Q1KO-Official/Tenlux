namespace Tenlux.Helpers;

internal static class Localizer
{
    private static int _lang = 0; // 0=简体 1=English 2=繁體

    public static int Lang
    {
        get => _lang;
        set { if (value is >= 0 and <= 2) _lang = value; }
    }

    // Named string keys
    public const int S_Language = 0;
    public const int S_English = 1;
    public const int S_ChineseSimplified = 2;
    public const int S_ChineseTraditional = 3;
    public const int S_SwitchMode = 4;
    public const int S_SingleClick = 5;
    public const int S_DoubleClick = 6;
    public const int S_Exit = 7;
    public const int S_DarkMode = 8;
    public const int S_LightMode = 9;
    public const int S_Startup = 10;
    public const int S_Settings = 11;
    public const int S_Automation = 12;
    public const int S_DarkWallpaper = 13;
    public const int S_LightWallpaper = 14;
    public const int S_Browse = 15;
    public const int S_Fill = 16;
    public const int S_Fit = 17;
    public const int S_Stretch = 18;
    public const int S_Tile = 19;
    public const int S_WallpaperDisplay = 20;
    public const int S_AutoSwitchWallpaper = 21;
    public const int S_ScheduledSwitch = 22;
    public const int S_LightTime = 23;
    public const int S_DarkTime = 24;
    public const int S_GlobalHotkey = 25;
    public const int S_TrayToggle = 26;
    public const int S_NavGeneral = 27;
    public const int S_NavHotkey = 28;
    public const int S_NavWallpaper = 29;
    public const int S_NavAbout = 30;
    public const int S_ScheduleOn = 32;
    public const int S_ScheduleOff = 33;
    public const int S_Edit = 37;
    public const int S_Presets = 38;
    public const int S_Back = 39;
    public const int S_Save = 40;
    public const int S_Delete = 41;
    public const int S_Apply = 42;
    public const int S_ToastTitle = 43;
    public const int S_ToastNotification = 44;
    public const int S_ToastSound = 45;
    public const int S_ToastDarkSwitched = 46;
    public const int S_ToastLightSwitched = 47;
    public const int S_OnWelcome = 48;
    public const int S_OnWelcomeDesc = 49;
    public const int S_OnThemeTitle = 50;
    public const int S_OnThemeDesc = 51;
    public const int S_OnWallpaperTitle = 54;
    public const int S_OnWallpaperDesc = 55;
    public const int S_OnHotkeyTitle = 56;
    public const int S_OnHotkeyDesc = 57;
    public const int S_OnStartTitle = 58;
    public const int S_OnStartDesc = 59;
    public const int S_OnReadyTitle = 60;
    public const int S_OnReadyDesc = 61;
    public const int S_Next = 62;
    public const int S_Previous = 63;
    public const int S_Skip = 64;
    public const int S_StartUsing = 65;
    public const int S_AboutBrief = 66;
    public const int S_AboutVersion = 67;
    public const int S_AboutDeveloper = 68;
    public const int S_AboutProjectSource = 69;
    public const int S_AboutLicense = 72;
    public const int S_AboutCopyright = 73;
    public const int S_On = 75;
    public const int S_Off = 76;
    public const int S_PressHotkey = 77;
    public const int S_ViewTutorial = 78;
    public const int S_Current = 79;
    public const int S_PresetName = 80;
    public const int S_ExportConfig = 81;
    public const int S_ImportConfig = 82;
    public const int S_ExportDone = 83;
    public const int S_ImportDone = 84;
    public const int S_ImportFail = 85;
    public const int S_ExportFile = 86;
    public const int S_ExportFileDone = 87;
    public const int S_ImportFile = 88;
    public const int S_AppName = 89;
    public const int S_Error = 90;
    public const int S_OK = 91;
    public const int S_TrayHint = 92;
    public const int S_OnTrayTitle = 93;
    public const int S_OnTrayDesc = 94;
    public const int S_TrayStep1 = 95;
    public const int S_TrayStep2 = 96;
    public const int S_TrayStep3 = 97;
    public const int S_ConfigMigration = 98;
    public const int S_Export = 99;
    public const int S_Import = 100;
    public const int S_ConfigToken = 101;
    public const int S_ConfigFileWP = 102;
    public const int S_DisableHotkeyFullscreen = 103;
    public const int S_NavDashboard = 104;
    public const int S_DashboardCurrentWallpaper = 105;
    public const int S_QuickSettings = 106;
    public const int S_SwitchTo = 107;
    public const int S_ResetSettings = 110;
    public const int S_ResetSettingsConfirm = 111;
    public const int S_ResetSettingsDone = 112;
    public const int S_AboutStatus = 116;
    public const int S_OnboardingHint = 117;
    public const int S_HealthTray = 121;
    public const int S_HealthStartup = 122;
    public const int S_HealthWallpaper = 123;
    public const int S_HealthSchedule = 124;
    public const int S_CurrentPreset = 129;
    public const int S_NoPreset = 130;
    public const int S_AddPreset = 131;
    public const int S_ConfirmSingleKeyHotkeyTitle = 132;
    public const int S_ConfirmSingleKeyHotkeyMessage = 133;
    public const int S_SetAnyway = 134;
    public const int S_Cancel = 135;
    public const int S_AddPresetFirst = 136;
    public const int S_OpenTutorial = 137;
    public const int S_Logs = 138;
    public const int S_ExportLogs = 139;
    public const int S_LogExportDone = 140;
    public const int S_OriginalWallpaper = 141;
    public const int S_RestoreWallpaper = 142;
    public const int S_RestoreWallpaperDone = 143;
    public const int S_RestoreWallpaperUnavailable = 144;

    private static readonly Dictionary<int, string[]> Strings = new()
    {
        [0]  = new[] { "语言",                   "Language",                "語言" },
        [1]  = new[] { "English",                "English",                 "English" },
        [2]  = new[] { "中文（简体）",            "Chinese (Simplified)",    "中文（簡體）" },
        [3]  = new[] { "中文（繁体）",            "Chinese (Traditional)",   "中文（繁體）" },
        [4]  = new[] { "切换模式",               "Switch Mode",             "切換模式" },
        [5]  = new[] { "单击切换",               "Single Click",            "單擊切換" },
        [6]  = new[] { "双击切换",               "Double Click",            "雙擊切換" },
        [7]  = new[] { "退出",                   "Exit",                    "退出" },
        [8]  = new[] { "深色模式",               "Dark Mode",               "深色模式" },
        [9]  = new[] { "浅色模式",               "Light Mode",              "淺色模式" },
        [10] = new[] { "开机自动启动",            "Start with Windows",      "隨開機自動執行" },
        [11] = new[] { "设置",                   "Settings",                "設定" },
        [12] = new[] { "自动化",                 "Automation",              "自動化" },
        [13] = new[] { "深色壁纸",               "Dark Wallpaper",          "深色壁紙" },
        [14] = new[] { "浅色壁纸",               "Light Wallpaper",         "淺色壁紙" },
        [15] = new[] { "浏览",                   "Browse",                  "瀏覽" },
        [16] = new[] { "填充",                   "Fill",                    "填充" },
        [17] = new[] { "适应",                   "Fit",                     "適應" },
        [18] = new[] { "拉伸",                   "Stretch",                 "拉伸" },
        [19] = new[] { "平铺",                   "Tile",                    "平鋪" },
        [20] = new[] { "壁纸显示模式",           "Wallpaper Display",       "壁紙顯示模式" },
        [21] = new[] { "自动切换深浅壁纸",       "Auto Switch Wallpaper",   "自動切換深淺壁紙" },
        [22] = new[] { "在指定时间内开启深色模式", "Scheduled switching",     "在指定時間內開啟深色模式" },
        [23] = new[] { "浅色时间",               "Light Mode Time",         "淺色時間" },
        [24] = new[] { "深色时间",               "Dark Mode Time",          "深色時間" },
        [25] = new[] { "全局热键",               "Global Hotkey",           "全域快速鍵" },
        [26] = new[] { "点击托盘图标切换",       "Tray Icon Click Toggle",  "點選工作列圖示切換" },
        [27] = new[] { "常规设置",               "General",                 "一般設定" },
        [28] = new[] { "切换选项",               "Switch Options",          "切換選項" },
        [29] = new[] { "深浅壁纸",               "Wallpaper",               "深淺桌布" },
        [30] = new[] { "关于",                   "About",                   "關於" },
        [32] = new[] { "启用",                   "Turn on at",              "開始時間：" },
        [33] = new[] { "关闭",                   "Turn off at",             "結束時間：" },
        [37] = new[] { "编辑",                   "Edit",                    "編輯" },
        [38] = new[] { "预设",                   "Presets",                 "預設" },
        [39] = new[] { "返回",                   "Back",                    "返回" },
        [40] = new[] { "保存",                   "Save",                    "儲存" },
        [41] = new[] { "删除",                   "Delete",                  "刪除" },
        [42] = new[] { "应用",                   "Apply",                   "應用" },
        [43] = new[] { "气泡提示（Windows通知）", "Toast Notifications",     "快顯通知（Windows 通知）" },
        [44] = new[] { "切换完成显示Toast通知",   "Show toast on toggle",    "切換完成顯示彈出式通知" },
        [45] = new[] { "开启通知音效",           "Enable notification sound","開啟通知音效" },
        [46] = new[] { "已切换至深色模式",       "Switched to Dark Mode",   "已切換至深色模式" },
        [47] = new[] { "已切换至浅色模式",       "Switched to Light Mode",  "已切換至淺色模式" },
        [48] = new[] { "欢迎使用",               "Welcome",                 "歡迎使用" },
        [49] = new[] { "一款轻量级 Windows 深色/浅色模式切换工具", "A lightweight Windows dark/light mode toggle", "輕量級 Windows 深淺模式切換工具" },
        [50] = new[] { "一键切换主题",           "One-Click Theme Switch",  "一鍵切換主題" },
        [51] = new[] { "点击托盘图标即可在深色与浅色模式之间快速切换", "Click the tray icon to quickly switch between dark and light modes", "點擊工作列圖示即可在深淺模式之間快速切換" },
        [54] = new[] { "自动切换壁纸",           "Auto Switch Wallpaper",   "自動切換桌布" },
        [55] = new[] { "为深色和浅色模式设置不同壁纸，切换主题时自动更换", "Set different wallpapers for dark and light modes, auto-switch with theme", "為深淺模式設定不同桌布，切換主題時自動更換" },
        [56] = new[] { "自定义热键",             "Custom Hotkeys",          "自訂快速鍵" },
        [57] = new[] { "设置全局热键，随时随地一键切换主题", "Set a global hotkey to toggle theme anytime", "設定全域熱鍵，隨時隨地一鍵切換主題" },
        [58] = new[] { "开机自启动",             "Start with Windows",      "開機自動啟動" },
        [59] = new[] { "随系统启动自动运行，无需手动打开", "Automatically runs on system startup", "隨系統啟動自動執行，無需手動開啟" },
        [60] = new[] { "准备就绪",               "All Set",                 "準備就緒" },
        [61] = new[] { "开始使用执光", "Start using Tenlux", "開始使用執光" },
        [62] = new[] { "下一步",                 "Next",                    "下一步" },
        [63] = new[] { "上一步",                 "Back",                    "上一步" },
        [64] = new[] { "跳过",                   "Skip",                    "跳過" },
        [65] = new[] { "开始使用",               "Get Started",             "開始使用" },
        [66] = new[] { "一款轻量级 Windows 深色/浅色模式切换工具", "A lightweight Windows dark/light mode toggle", "一款輕量級 Windows 深色/淺色模式切換工具" },
        [67] = new[] { "版本",                   "Version",                 "版本" },
        [68] = new[] { "开发者",                 "Developer",               "開發者" },
        [69] = new[] { "项目源码",               "Project Source",          "專案原始碼" },
        [72] = new[] { "许可证",                 "License",                 "授權條款" },
        [73] = new[] { "© 2026 Q1KO. 保留所有权利.", "© 2026 Q1KO. All rights reserved.", "© 2026 Q1KO. 保留所有權利." },
        [75] = new[] { "开启",                   "On",                      "開啟" },
        [76] = new[] { "关闭",                   "Off",                     "關閉" },
        [77] = new[] { "请按下快捷键",           "Press a hotkey",          "請按下快捷鍵" },
        [78] = new[] { "使用指南",             "User Guide",              "使用指南" },
        [79] = new[] { "当前",                 "Current",                 "當前" },
        [80] = new[] { "预设名称",             "Preset Name",             "預設名稱" },
        [81] = new[] { "导出配置口令",           "Export Config Token",      "匯出設定代碼" },
        [82] = new[] { "导入配置口令",           "Import Config Token",      "匯入設定代碼" },
        [83] = new[] { "配置已复制到剪贴板",   "Config copied to clipboard", "設定已複製到剪貼簿" },
        [84] = new[] { "配置导入成功",         "Config imported",         "設定匯入成功" },
        [85] = new[] { "配置导入失败",         "Import failed",           "設定匯入失敗" },
        [86] = new[] { "导出配置文件（含壁纸）", "Export Config File (with Wallpapers)", "匯出設定檔（含桌布）" },
        [87] = new[] { "配置文件已保存",       "Config file saved",       "設定檔已儲存" },
        [88] = new[] { "导入配置文件（含壁纸）", "Import Config File (with Wallpapers)", "匯入設定檔（含桌布）" },
        [89] = new[] { "执光",                 "Tenlux",                  "執光" },
        [90] = new[] { "错误",                 "Error",                   "錯誤" },
        [91] = new[] { "确定",                 "OK",                      "確定" },
        [92] = new[] { "我在这里！点击任务栏隐藏区域的 ^ 图标可以找到我", "I'm here! Click the ^ arrow in the taskbar to find me", "我在這裡！點擊工作列隱藏區域的 ^ 圖示可以找到我" },
        [93] = new[] { "找到我",                   "Find Me",                 "找到我" },
        [94] = new[] { "Tenlux 运行在系统托盘里。把图标从隐藏区域拖到任务栏上，方便下次找到我。", "Tenlux lives in the system tray. Drag the icon from the hidden area to the taskbar so you can find me easily.", "Tenlux 運行在系統匣裡。把圖示從隱藏區域拖到工作列上，方便下次找到我。" },
        [95] = new[] { "点击任务栏右侧的 ^ 箭头，打开隐藏图标区域", "Click the ^ arrow on the right side of the taskbar to open the hidden icons area", "點擊工作列右側的 ^ 箭頭，打開隱藏圖示區域" },
        [96] = new[] { "找到 Tenlux 图标，按住拖到任务栏上", "Find the Tenlux icon, hold and drag it to the taskbar", "找到 Tenlux 圖示，按住拖到工作列上" },
        [97] = new[] { "松手，图标就固定在任务栏了！", "Release, and the icon is pinned to the taskbar!", "鬆手，圖示就固定在工作列了！" },
        [98] = new[] { "配置迁移",             "Config Migration",        "移轉" },
        [99] = new[] { "导出",                 "Export",                  "匯出" },
        [100] = new[] { "导入",                "Import",                  "匯入" },
        [101] = new[] { "配置口令",             "Config Code",             "設定碼" },
        [102] = new[] { "配置文件（含壁纸）",    "Config File (Wallpapers)", "設定檔（含桌布）" },
        [103] = new[] { "全屏时禁用",            "Disable in fullscreen",   "全螢幕時停用" },
        [104] = new[] { "首页",                 "Home",                    "首頁" },
        [105] = new[] { "当前壁纸",             "Current Wallpaper",       "當前桌布" },
        [106] = new[] { "快速设置",             "Quick Settings",          "快速設定" },
        [107] = new[] { "切换到",               "Switch to ",              "切換到" },
        [110] = new[] { "重置设置",             "Reset Settings",          "重設設定" },
        [111] = new[] { "这会恢复默认设置，但不会删除已导入的壁纸文件。", "This restores default settings without deleting imported wallpaper files.", "這會恢復預設設定，但不會刪除已匯入的桌布檔案。" },
        [112] = new[] { "设置已恢复默认值",     "Settings reset to defaults", "設定已恢復預設值" },
        [116] = new[] { "轻量托盘工具 / WinUI 3 / 深浅模式联动", "Tray-first utility / WinUI 3 / Theme + wallpaper switching", "輕量系統匣工具 / WinUI 3 / 深淺模式連動" },
        [117] = new[] { "接下来只要几步，就能把 Tenlux 调成最适合你的状态。", "A few quick steps and Tenlux will feel like part of your desktop.", "接下來只要幾步，就能把 Tenlux 調成最適合你的狀態。" },
        [121] = new[] { "托盘点击",             "Tray Click",              "系統匣點擊" },
        [122] = new[] { "开机自启",             "Startup",                 "開機自啟" },
        [123] = new[] { "壁纸联动",             "Wallpaper Link",          "桌布連動" },
        [124] = new[] { "定时切换",             "Scheduled Switch",        "定時切換" },
        [129] = new[] { "当前预设",             "Current Preset",          "目前預設" },
        [130] = new[] { "未设置",               "Not Set",                 "未設定" },
        [131] = new[] { "添加预设",             "Add Preset",              "新增預設" },
        [132] = new[] { "确认单键热键",         "Confirm single-key hotkey", "確認單鍵快速鍵" },
        [133] = new[] { "“{0}” 不含 Ctrl、Alt、Shift 或 Win，可能会在打字时误触。确定要设置吗？", "\"{0}\" has no Ctrl, Alt, Shift, or Win modifier, so it may trigger while typing. Set it anyway?", "「{0}」不含 Ctrl、Alt、Shift 或 Win，可能會在打字時誤觸。確定要設定嗎？" },
        [134] = new[] { "仍然设置",             "Set anyway",              "仍然設定" },
        [135] = new[] { "取消",                 "Cancel",                  "取消" },
        [136] = new[] { "还没有壁纸预设。先添加一个深浅壁纸预设，再开启自动切换。", "No wallpaper preset yet. Add a dark/light wallpaper preset before turning on auto switch.", "還沒有桌布預設。先新增一個深淺桌布預設，再開啟自動切換。" },
        [137] = new[] { "打开指南",             "Open Guide",              "開啟指南" },
        [138] = new[] { "日志",                 "Logs",                    "日誌" },
        [139] = new[] { "导出日志",             "Export Logs",             "匯出日誌" },
        [140] = new[] { "日志已导出",           "Logs exported",           "日誌已匯出" },
        [141] = new[] { "原本壁纸",             "Original Wallpaper",      "原本桌布" },
        [142] = new[] { "恢复原本壁纸",         "Restore Wallpaper",       "恢復原本桌布" },
        [143] = new[] { "已恢复原本壁纸",       "Original wallpaper restored", "已恢復原本桌布" },
        [144] = new[] { "没有可恢复的原本壁纸记录", "No original wallpaper backup found", "沒有可恢復的原本桌布記錄" },
    };

    public static string T(int index) => Strings.TryGetValue(index, out var arr) ? arr[_lang] : "";

    public static void PopulateLangCombo(Microsoft.UI.Xaml.Controls.ComboBox cmb)
    {
        cmb.Items.Add(T(S_ChineseSimplified));
        cmb.Items.Add(T(S_English));
        cmb.Items.Add(T(S_ChineseTraditional));
        cmb.SelectedIndex = _lang;
    }

    public static void RefreshLangCombo(Microsoft.UI.Xaml.Controls.ComboBox cmb)
    {
        if (cmb.Items.Count < 3) { PopulateLangCombo(cmb); return; }
        var idx = cmb.SelectedIndex;
        cmb.Items[0] = T(S_ChineseSimplified);
        cmb.Items[1] = T(S_English);
        cmb.Items[2] = T(S_ChineseTraditional);
        if (idx >= 0) cmb.SelectedIndex = idx;
    }
}
