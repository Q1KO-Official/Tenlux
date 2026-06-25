using System.Runtime.InteropServices;

namespace Tenlux.Helpers;

internal static class NativeMethods
{
    private static int _startupScreenWidth = 1920;
    private static int _startupScreenHeight = 1080;

    // ── P/Invoke: Messaging ──

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, string? lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    public static extern bool SystemParametersInfo(int uAction, int uParam, string? lpvParam, int fuWinIni);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    public static extern int DwmFlush();

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    // ── P/Invoke: Window management ──

    [DllImport("user32.dll")]
    public static extern nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, nint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    public static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", EntryPoint = "TrackPopupMenu")]
    public static extern int TrackPopupMenuRet(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern nint LoadImageW(nint hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll")]
    public static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("gdi32.dll")]
    public static extern nint CreateSolidBrush(uint colorRef);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(nint hObject);

    [DllImport("gdi32.dll")]
    public static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

    [DllImport("user32.dll")]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int SetWindowRgn(nint hWnd, nint hRgn, bool redraw);

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    public static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern nint CallWindowProcW(nint lpPrevWndFunc, nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(nint hwnd);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    public delegate nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint GetModuleHandle(string? lpModuleName);

    // ── P/Invoke: Window enumeration ──

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    // ── P/Invoke: Icon ──

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr ExtractIconW(IntPtr hInst, string lpszExeFileName, uint nIconIndex);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern int FillRect(nint hDC, ref RECT lprc, nint hbr);

    [DllImport("user32.dll")]
    public static extern nint BeginPaint(nint hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    public static extern bool EndPaint(nint hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    public static extern bool DrawIconEx(nint hdc, int xLeft, int yTop, nint hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, nint hbrFlickerFreeDraw, uint diFlags);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public nint hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore;
        public int fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    private static nint _windowIcon;

    // ── Structs ──

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    // ── Constants ──

    public static readonly IntPtr HWND_BROADCAST = new(0xffff);
    public const uint WM_SETTINGCHANGE     = 0x001A;
    public const uint WM_THEMECHANGED      = 0x031A;
    public const uint SMTO_ABORTIFHUNG     = 0x0002;
    public const int SPI_SETDESKWALLPAPER  = 0x0014;
    public const int SPIF_UPDATEINIFILE    = 0x01;
    public const int SPIF_SENDWININICHANGE = 0x02;

    public const uint WM_SETICON = 0x0080;
    public const uint ICON_SMALL = 0, ICON_BIG = 1;
    public const uint WM_COMMAND = 0x0111;
    public const uint WM_PAINT = 0x000F;
    public const uint WM_ERASEBKGND = 0x0014;
    public const uint WM_CANCELMODE = 0x001F;
    public const uint WM_GETMINMAXINFO = 0x0024;
    public const uint WM_NCMOUSELEAVE = 0x02A2;
    public const uint WM_MOUSELEAVE = 0x02A3;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_CLOAK = 13;
    public const int SM_CXSCREEN = 0, SM_CYSCREEN = 1;
    public const uint MB_OK = 0x00000000;
    public const uint MB_ICONINFORMATION = 0x00000040;

    public const uint MF_STRING = 0x0000;
    public const uint MF_SEPARATOR = 0x0800;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_BOTTOMALIGN = 0x0020;
    public const uint TPM_RETURNCMD = 0x0100;
    public const int GWLP_WNDPROC = -4;
    public const int GWLP_USERDATA = -21;
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
    public const uint WS_EX_TOPMOST = 0x00000008;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_LAYERED = 0x00080000;
    public const uint WS_EX_NOACTIVATE = 0x08000000;
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_VISIBLE = 0x10000000;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const int SW_SHOWNOACTIVATE = 4;
    public const uint DI_NORMAL = 0x0003;
    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x00000010;
    public const uint LWA_ALPHA = 0x00000002;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_SYSKEYDOWN = 0x0104;

    public const string ThemeRegPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    public const string StartupRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string StartupValueName = "Tenlux";

    // ── IActiveDesktop COM ──

    public const int AD_APPLY_ALL = 0x00000007;
    public const uint WM_SPAWN_WORKERW = 0x052C;

    [ComImport, Guid("F490EB00-1240-11D1-9888-006097DEACF9"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActiveDesktop
    {
        [PreserveSig] int ApplyChanges(int dwFlags);
        [PreserveSig] int SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string pwszWallpaper, int dwReserved);
        [PreserveSig] int GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pwszWallpaper, int cchWallpaper, int dwReserved);
        [PreserveSig] int SetWallpaperOptions(/* WALLPAPEROPT* */ IntPtr pwpo, int dwReserved);
        [PreserveSig] int GetWallpaperOptions(/* WALLPAPEROPT* */ IntPtr pwpo, int dwReserved);
        [PreserveSig] int ApplyNow();
        [PreserveSig] int SetWallpaperComponent();
    }

    [ComImport, Guid("75048700-EF1F-11D0-9888-006097DEACF9")]
    public class ActiveDesktopClass { }

    // ── IDesktopWallpaper COM ──

    public enum DWPosition { Center = 0, Tile = 1, Stretch = 2, Fit = 3, Fill = 4, Span = 5 }

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                          [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetMonitorDevicePathCount();
        void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID, out RECT displayRect);
        void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetBackgroundColor();
        void SetPosition([MarshalAs(UnmanagedType.I4)] DWPosition position);
        [return: MarshalAs(UnmanagedType.I4)]
        DWPosition GetPosition();
    }

    [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    public class DesktopWallpaperClass { }

    // ── WorkerW helpers (Active Desktop transition) ──

    private static bool _workerWEnabled;

    public static void EnableWorkerW()
    {
        if (_workerWEnabled) return;
        var progman = FindWindow("ProgMan", null);
        SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 1000, out _);
        _workerWEnabled = true;
    }

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, nint dwMinimumWorkingSetSize, nint dwMaximumWorkingSetSize);

    // ── Dark mode popup menu (undocumented uxtheme) ──

    public enum PreferredAppMode { Default = 0, AllowDark = 1, ForceDark = 2, ForceLight = 3, Max = 4 }

    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    public static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode appMode);

    // ── Shell_NotifyIcon balloon tip ──

    public const int NIM_ADD = 0x00000000;
    public const int NIM_MODIFY = 0x00000001;
    public const int NIM_DELETE = 0x00000002;
    public const int NIF_INFO = 0x00000010;
    public const int NIIF_NONE = 0x00000000;
    public const int NIIF_INFO = 0x00000001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    // ── Window handle cache ──

    private static nint _cachedHwnd;

    public static nint GetWindowHandle(Microsoft.UI.Xaml.Window window)
    {
        if (_cachedHwnd == 0)
            _cachedHwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        return _cachedHwnd;
    }

    // ── Icon helper ──

    public static void SetWindowIcon(nint hwnd)
    {
        if (_windowIcon != 0) return; // Already set, avoid leak
        var exePath = Environment.ProcessPath ?? "";
        var hIcon = ExtractIconW(IntPtr.Zero, exePath, 0);
        if (hIcon == IntPtr.Zero || hIcon == new IntPtr(1)) return;
        _windowIcon = hIcon;
        SendMessageW(hwnd, WM_SETICON, (nint)ICON_SMALL, hIcon);
        SendMessageW(hwnd, WM_SETICON, (nint)ICON_BIG, hIcon);
    }

    public static void ReleaseWindowIcon()
    {
        if (_windowIcon != 0)
        {
            DestroyIcon(_windowIcon);
            _windowIcon = 0;
        }
    }

    // ── Fullscreen detection ──

    public static bool IsFullscreenAppActive()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == 0) return false;
        if (!GetWindowRect(hwnd, out var rect)) return false;
        var w = GetSystemMetrics(SM_CXSCREEN);
        var h = GetSystemMetrics(SM_CYSCREEN);
        return rect.Left == 0 && rect.Top == 0 && rect.Right == w && rect.Bottom == h;
    }

    public static void RefreshStartupScreenMetrics()
    {
        var width = GetSystemMetrics(SM_CXSCREEN);
        var height = GetSystemMetrics(SM_CYSCREEN);
        if (width > 0)
            _startupScreenWidth = width;
        if (height > 0)
            _startupScreenHeight = height;
    }

    public static (int Width, int Height) GetStartupScreenSize()
    {
        return (_startupScreenWidth, _startupScreenHeight);
    }

    public static double GetStartupScreenAspectRatio()
    {
        return _startupScreenHeight > 0
            ? (double)_startupScreenWidth / _startupScreenHeight
            : 16.0 / 9.0;
    }

    // ── Window size helper ──

    public static (int w, int h) GetScaledWindowSize(nint hwnd)
    {
        var dpi = (float)GetDpiForWindow(hwnd) / 96f;
        return (
            (int)(640 * dpi),
            (int)(560 * dpi)
        );
    }
}
