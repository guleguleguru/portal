namespace DesktopPortal.Utilities;

public readonly record struct Hotkey(HotkeyTriggerKind Kind, HotkeyModifiers Modifiers, string Key, uint VirtualKey)
{
    public bool IsMouse => Kind == HotkeyTriggerKind.Mouse;

    public override string ToString()
    {
        if (IsMouse)
        {
            return Key;
        }

        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }

        parts.Add(Key);
        return string.Join("+", parts);
    }
}
