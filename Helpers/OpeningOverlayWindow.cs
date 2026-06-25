using System.Runtime.InteropServices;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Helpers;

internal sealed class OpeningOverlayWindow : IDisposable
{
    private const string ClassName = "TenluxOpeningOverlay";
    private const int BrushOffset = 0;
    private static readonly int IconOffset = IntPtr.Size;
    private static readonly int IconSizeOffset = IntPtr.Size * 2;
    private static readonly WindowProc WndProc = OverlayWndProc;
    private static bool _registered;
    private nint _hwnd;
    private nint _brush;
    private nint _icon;

    public bool IsVisible => _hwnd != 0;

    public void Show(bool isLight, int x, int y, int width, int height)
    {
        Dispose();
        EnsureRegistered();

        _brush = CreateSolidBrush(isLight ? 0x00FFFFFFu : 0x00202020u);
        var icon = LoadOverlayIcon(width);
        _icon = icon.Handle;
        _hwnd = CreateWindowEx(
            WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED,
            ClassName,
            null,
            WS_POPUP,
            x,
            y,
            width,
            height,
            IntPtr.Zero,
            IntPtr.Zero,
            GetModuleHandle(null!),
            IntPtr.Zero);

        if (_hwnd == 0)
        {
            AppLogger.Log("Native opening overlay CreateWindowEx returned null");
            Dispose();
            return;
        }

        SetWindowLongPtrW(_hwnd, BrushOffset, _brush);
        SetWindowLongPtrW(_hwnd, IconOffset, _icon);
        SetWindowLongPtrW(_hwnd, IconSizeOffset, icon.Size);
        ApplyRoundedRegion(_hwnd, width, height);
        SetOpacity(255);
        ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
        SetWindowPos(_hwnd, new IntPtr(-1), x, y, width, height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        UpdateWindow(_hwnd);
    }

    public void SetOpacity(byte opacity)
    {
        if (_hwnd == 0)
            return;

        SetLayeredWindowAttributes(_hwnd, 0, opacity, LWA_ALPHA);
    }

    public void Dispose()
    {
        if (_hwnd != 0)
        {
            DestroyWindow(_hwnd);
            _hwnd = 0;
        }

        if (_brush != 0)
        {
            DeleteObject(_brush);
            _brush = 0;
        }

        if (_icon != 0)
        {
            DestroyIcon(_icon);
            _icon = 0;
        }
    }

    private static void EnsureRegistered()
    {
        if (_registered)
            return;

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            cbWndExtra = IntPtr.Size * 3,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProc),
            hInstance = GetModuleHandle(null!),
            hbrBackground = 0,
            lpszClassName = ClassName,
        };

        var atom = RegisterClassEx(ref wc);
        _registered = atom != 0;
        if (!_registered)
            AppLogger.Log("Native opening overlay RegisterClassEx failed");
    }

    private static nint OverlayWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_ERASEBKGND)
        {
            var brush = GetWindowLongPtrW(hWnd, BrushOffset);
            if (brush != 0 && GetClientRect(hWnd, out var rect))
                FillRect(wParam, ref rect, brush);
            return 1;
        }

        if (msg == WM_PAINT)
        {
            var hdc = BeginPaint(hWnd, out var ps);
            var brush = GetWindowLongPtrW(hWnd, BrushOffset);
            if (brush != 0 && GetClientRect(hWnd, out var rect))
            {
                FillRect(hdc, ref rect, brush);
                var icon = GetWindowLongPtrW(hWnd, IconOffset);
                if (icon != 0)
                {
                    var size = (int)GetWindowLongPtrW(hWnd, IconSizeOffset);
                    if (size <= 0)
                        size = 64;

                    DrawIconEx(
                        hdc,
                        (rect.Right - rect.Left - size) / 2,
                        (rect.Bottom - rect.Top - size) / 2,
                        icon,
                        size,
                        size,
                        0,
                        0,
                        DI_NORMAL);
                }
            }
            EndPaint(hWnd, ref ps);
            return 0;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static (nint Handle, nint Size) LoadOverlayIcon(int overlayWidth)
    {
        var scale = Math.Clamp(overlayWidth / 640.0, 1.0, 3.0);
        var size = Math.Clamp((int)Math.Round(64 * scale), 64, 192);
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        var icon = LoadImageW(IntPtr.Zero, path, IMAGE_ICON, size, size, LR_LOADFROMFILE);

        if (icon != 0)
            return (icon, size);

        AppLogger.Log("Native opening overlay LoadImageW failed; falling back to ExtractIconW");
        return (ExtractIconW(IntPtr.Zero, path, 0), 64);
    }

    private static void ApplyRoundedRegion(nint hwnd, int width, int height)
    {
        var scale = Math.Clamp(width / 640.0, 1.0, 3.0);
        var radius = Math.Max(8, (int)Math.Round(8 * scale));
        var region = CreateRoundRectRgn(0, 0, width + 1, height + 1, radius * 2, radius * 2);
        if (region == 0)
        {
            AppLogger.Log("Native opening overlay CreateRoundRectRgn failed");
            return;
        }

        if (SetWindowRgn(hwnd, region, true) == 0)
        {
            DeleteObject(region);
            AppLogger.Log("Native opening overlay SetWindowRgn failed");
        }
    }
}
