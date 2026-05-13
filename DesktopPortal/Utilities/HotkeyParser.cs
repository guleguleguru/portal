using WpfKey = System.Windows.Input.Key;
using WpfKeyboard = System.Windows.Input.Keyboard;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfModifierKeys = System.Windows.Input.ModifierKeys;

namespace DesktopPortal.Utilities;

public static class HotkeyParser
{
    private static readonly HashSet<string> FunctionKeys = Enumerable.Range(1, 12).Select(i => $"F{i}").ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MouseKeys = new(StringComparer.OrdinalIgnoreCase) { "MouseMiddle", "MouseBack", "MouseForward" };
    private static readonly HashSet<string> DangerousKeys = new(StringComparer.OrdinalIgnoreCase) { "ESC", "ESCAPE", "ENTER", "RETURN", "TAB", "DELETE", "DEL" };
    private static readonly HashSet<HotkeyModifiers> AllowedModifierSets = new()
    {
        HotkeyModifiers.Control,
        HotkeyModifiers.Alt,
        HotkeyModifiers.Shift,
        HotkeyModifiers.Control | HotkeyModifiers.Alt,
        HotkeyModifiers.Control | HotkeyModifiers.Shift,
        HotkeyModifiers.Alt | HotkeyModifiers.Shift
    };

    public static bool TryParse(string? input, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var tokens = input.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        string? key = null;
        foreach (var token in tokens)
        {
            if (IsModifierToken(token, out var modifier))
            {
                modifiers |= modifier;
                continue;
            }

            if (key is not null)
            {
                return false;
            }

            key = NormalizeKeyToken(token);
        }

        if (key is null || !IsAllowed(modifiers, key))
        {
            return false;
        }

        var kind = MouseKeys.Contains(key) ? HotkeyTriggerKind.Mouse : HotkeyTriggerKind.Keyboard;
        hotkey = new Hotkey(kind, modifiers, key, kind == HotkeyTriggerKind.Keyboard ? GetVirtualKey(key) : 0);
        return true;
    }

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (!TryParse(input, out var hotkey))
        {
            return false;
        }

        normalized = hotkey.ToString();
        return true;
    }

    public static bool TryCreateFromKeyEvent(WpfKeyEventArgs e, out string normalized, out string? message)
    {
        var key = e.Key == WpfKey.System ? e.SystemKey : e.Key;
        var modifiers = e.KeyboardDevice?.Modifiers ?? WpfKeyboard.Modifiers;
        return TryCreateFromKey(key, modifiers, out normalized, out message);
    }

    public static bool TryCreateFromKey(WpfKey key, WpfModifierKeys modifiers, out string normalized, out string? message)
    {
        normalized = string.Empty;
        message = null;

        if (key == WpfKey.None || IsModifierKey(key))
        {
            message = "请按下数字键、F1-F12、鼠标中键/侧键，或 Ctrl/Alt/Shift + 字母/数字。";
            return false;
        }

        var keyToken = KeyToToken(key);
        if (keyToken is null)
        {
            message = "该按键不支持作为快捷键。";
            return false;
        }

        if (FunctionKeys.Contains(keyToken))
        {
            modifiers = WpfModifierKeys.None;
        }

        var parts = new List<string>();
        if (modifiers.HasFlag(WpfModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(WpfModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(WpfModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(WpfModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(keyToken);
        var candidate = string.Join("+", parts);
        if (!TryNormalize(candidate, out normalized))
        {
            message = "该快捷键不受支持或可能与系统快捷键冲突。";
            return false;
        }

        return true;
    }

    private static bool IsAllowed(HotkeyModifiers modifiers, string key)
    {
        if (modifiers.HasFlag(HotkeyModifiers.Win))
        {
            return false;
        }

        if (DangerousKeys.Contains(key))
        {
            return false;
        }

        if (MouseKeys.Contains(key))
        {
            return modifiers == HotkeyModifiers.None;
        }

        if (FunctionKeys.Contains(key))
        {
            return modifiers == HotkeyModifiers.None;
        }

        if (IsDigit(key))
        {
            return modifiers == HotkeyModifiers.None || AllowedModifierSets.Contains(modifiers);
        }

        if (!IsLetter(key))
        {
            return false;
        }

        return AllowedModifierSets.Contains(modifiers);
    }

    public static bool TryCreateFromMouseButton(MouseHotkeyButton button, out string normalized)
    {
        normalized = button switch
        {
            MouseHotkeyButton.Middle => "MouseMiddle",
            MouseHotkeyButton.Back => "MouseBack",
            MouseHotkeyButton.Forward => "MouseForward",
            _ => string.Empty
        };

        return TryNormalize(normalized, out normalized);
    }

    private static bool IsLetter(string key)
    {
        return key.Length == 1 && char.IsAsciiLetter(key[0]);
    }

    private static bool IsDigit(string key)
    {
        return key.Length == 1 && char.IsAsciiDigit(key[0]);
    }

    private static bool IsModifierToken(string token, out HotkeyModifiers modifier)
    {
        modifier = HotkeyModifiers.None;
        switch (token.Trim().ToUpperInvariant())
        {
            case "CTRL":
            case "CONTROL":
                modifier = HotkeyModifiers.Control;
                return true;
            case "ALT":
                modifier = HotkeyModifiers.Alt;
                return true;
            case "SHIFT":
                modifier = HotkeyModifiers.Shift;
                return true;
            case "WIN":
            case "WINDOWS":
                modifier = HotkeyModifiers.Win;
                return true;
            default:
                return false;
        }
    }

    private static string NormalizeKeyToken(string token)
    {
        var upper = token.Trim().ToUpperInvariant();
        return upper switch
        {
            "MOUSEMIDDLE" => "MouseMiddle",
            "MIDDLEMOUSE" => "MouseMiddle",
            "MBUTTON" => "MouseMiddle",
            "MOUSEBACK" => "MouseBack",
            "MOUSEX1" => "MouseBack",
            "XBUTTON1" => "MouseBack",
            "MOUSEFORWARD" => "MouseForward",
            "MOUSEX2" => "MouseForward",
            "XBUTTON2" => "MouseForward",
            "ESCAPE" => "Esc",
            "ESC" => "Esc",
            "RETURN" => "Enter",
            "DEL" => "Delete",
            _ when upper.Length == 1 && char.IsAsciiLetterOrDigit(upper[0]) => upper,
            _ when FunctionKeys.Contains(upper) => upper,
            _ => upper
        };
    }

    private static string? KeyToToken(WpfKey key)
    {
        if (key >= WpfKey.A && key <= WpfKey.Z)
        {
            return key.ToString().ToUpperInvariant();
        }

        if (key >= WpfKey.D0 && key <= WpfKey.D9)
        {
            return ((int)(key - WpfKey.D0)).ToString();
        }

        if (key >= WpfKey.NumPad0 && key <= WpfKey.NumPad9)
        {
            return ((int)(key - WpfKey.NumPad0)).ToString();
        }

        if (key >= WpfKey.F1 && key <= WpfKey.F12)
        {
            return key.ToString().ToUpperInvariant();
        }

        return key switch
        {
            WpfKey.Escape => "Esc",
            WpfKey.Enter => "Enter",
            WpfKey.Tab => "Tab",
            WpfKey.Delete => "Delete",
            _ => null
        };
    }

    private static bool IsModifierKey(WpfKey key)
    {
        return key is WpfKey.LeftCtrl or WpfKey.RightCtrl or WpfKey.LeftAlt or WpfKey.RightAlt or WpfKey.LeftShift or WpfKey.RightShift or WpfKey.LWin or WpfKey.RWin;
    }

    private static uint GetVirtualKey(string key)
    {
        if (key.Length == 1)
        {
            return key[0];
        }

        if (key.StartsWith('F') && int.TryParse(key[1..], out var fNumber) && fNumber is >= 1 and <= 12)
        {
            return (uint)(0x70 + fNumber - 1);
        }

        return 0;
    }
}
