using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using static Tenlux.Helpers.NativeMethods;

namespace Tenlux.Helpers;

internal sealed class GlobalHotkeyService : IDisposable
{
    private readonly SettingsManager _settings;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _onHotkey;

    private nint _keyboardHook;
    private LowLevelKeyboardProc? _hookProc;
    private (uint mod, uint vk) _parsedHotkey;

    public GlobalHotkeyService(SettingsManager settings, DispatcherQueue dispatcherQueue, Action onHotkey)
    {
        _settings = settings;
        _dispatcherQueue = dispatcherQueue;
        _onHotkey = onHotkey;
    }

    public void Register()
    {
        Unregister();
        if (!_settings.GlobalHotkey) return;

        _parsedHotkey = ParseHotkey(_settings.HotkeyText);
        if (_parsedHotkey.vk == 0) return;

        _hookProc = HookCallback;
        var moduleHandle = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, moduleHandle, 0);
        if (_keyboardHook == 0)
            AppLogger.Log($"Global hotkey hook registration failed: {Marshal.GetLastWin32Error()}");
    }

    public void Unregister()
    {
        if (_keyboardHook == 0) return;

        if (!UnhookWindowsHookEx(_keyboardHook))
            AppLogger.Log($"Global hotkey hook unregister failed: {Marshal.GetLastWin32Error()}");
        _keyboardHook = 0;
        _hookProc = null;
    }

    public void Dispose()
    {
        Unregister();
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var (mod, targetVk) = _parsedHotkey;

            if (vkCode == (int)targetVk && ReadCurrentModifiers() == mod)
            {
                if (_settings.DisableHotkeyInFullscreen && IsFullscreenAppActive())
                    return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);

                _dispatcherQueue.TryEnqueue(() => _onHotkey());
                return 1;
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private static uint ReadCurrentModifiers()
    {
        uint mod = 0;
        if ((GetAsyncKeyState(0x11) & 0x8000) != 0) mod |= MOD_CONTROL;
        if ((GetAsyncKeyState(0x12) & 0x8000) != 0) mod |= MOD_ALT;
        if ((GetAsyncKeyState(0x10) & 0x8000) != 0) mod |= MOD_SHIFT;
        if ((GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
            (GetAsyncKeyState(0x5C) & 0x8000) != 0) mod |= MOD_WIN;
        return mod;
    }

    private static (uint mod, uint vk) ParseHotkey(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (0, 0);

        uint mod = 0;
        uint vk = 0;
        foreach (var part in SplitHotkeyParts(text))
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL" or "CONTROL": mod |= MOD_CONTROL; break;
                case "ALT": mod |= MOD_ALT; break;
                case "SHIFT": mod |= MOD_SHIFT; break;
                case "WIN" or "SUPER": mod |= MOD_WIN; break;
                default:
                    vk = ParseKeyPart(part);
                    break;
            }
        }

        return (mod, vk);
    }

    private static string[] SplitHotkeyParts(string text)
    {
        if (text.Contains(" + ", StringComparison.Ordinal))
            return text.Split(" + ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (text.Trim() == "+")
            return [text.Trim()];

        return text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static uint ParseKeyPart(string part)
    {
        var token = part.Trim().ToUpperInvariant().Replace(" ", "");

        if (token.Length == 1)
        {
            var ch = token[0];
            if (char.IsLetterOrDigit(ch))
                return char.ToUpperInvariant(ch);

            return ch switch
            {
                '*' => 0x6A,
                '+' => 0xBB,
                ',' => 0xBC,
                '-' => 0xBD,
                '.' => 0xBE,
                '/' => 0xBF,
                '`' => 0xC0,
                ';' => 0xBA,
                '[' => 0xDB,
                '\\' => 0xDC,
                ']' => 0xDD,
                '\'' => 0xDE,
                '=' => 0xBB,
                _ => 0,
            };
        }

        if (token.StartsWith('F') && int.TryParse(token[1..], out var fn) && fn is >= 1 and <= 24)
            return 0x70 + (uint)(fn - 1);

        if (token.StartsWith("NUMPAD") && token.Length == 7 && char.IsDigit(token[6]))
            return 0x60 + (uint)(token[6] - '0');

        if (token.StartsWith("NUM") && token.Length == 4 && char.IsDigit(token[3]))
            return 0x60 + (uint)(token[3] - '0');

        return token switch
        {
            "SPACE" => 0x20,
            "ESC" or "ESCAPE" => 0x1B,
            "DEL" or "DELETE" => 0x2E,
            "INS" or "INSERT" => 0x2D,
            "TAB" => 0x09,
            "ENTER" or "RETURN" => 0x0D,
            "BACKSPACE" => 0x08,
            "PLUS" or "OEMPLUS" => 0xBB,
            "MINUS" or "OEMMINUS" => 0xBD,
            "COMMA" or "OEMCOMMA" => 0xBC,
            "PERIOD" or "OEMPERIOD" => 0xBE,
            "SLASH" or "OEM2" => 0xBF,
            "BACKSLASH" or "OEM5" => 0xDC,
            "SEMICOLON" or "OEM1" => 0xBA,
            "QUOTE" or "APOSTROPHE" or "OEM7" => 0xDE,
            "BACKQUOTE" or "GRAVE" or "OEM3" => 0xC0,
            "LEFTBRACKET" or "OEM4" => 0xDB,
            "RIGHTBRACKET" or "OEM6" => 0xDD,
            "NUM*" or "NUMMULTIPLY" or "NUMPADMULTIPLY" => 0x6A,
            "NUM+" or "NUMPLUS" or "NUMPADPLUS" => 0x6B,
            "NUM-" or "NUMMINUS" or "NUMPADMINUS" => 0x6D,
            "NUM." or "NUMDECIMAL" or "NUMPADDECIMAL" => 0x6E,
            "NUM/" or "NUMDIVIDE" or "NUMPADDIVIDE" => 0x6F,
            _ => 0,
        };
    }
}
