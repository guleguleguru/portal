namespace DesktopPortal.Services;

public sealed class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyPressedEventArgs(string ruleId)
    {
        RuleId = ruleId;
    }

    public string RuleId { get; }
}
