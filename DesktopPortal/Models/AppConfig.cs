namespace DesktopPortal.Models;

public sealed class AppConfig
{
    public bool StartWithWindows { get; set; }

    public bool PauseAllHotkeys { get; set; }

    public List<PortalRule> Rules { get; set; } = new();
}
