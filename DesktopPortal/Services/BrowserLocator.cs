namespace DesktopPortal.Services;

public sealed record BrowserInfo(string Name, string Path, string ProcessName);

public sealed class BrowserLocator
{
    public BrowserInfo? FindAppBrowser()
    {
        return FindChrome() ?? FindEdge();
    }

    private static BrowserInfo? FindChrome()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
        };

        var path = candidates.FirstOrDefault(File.Exists);
        return path is null ? null : new BrowserInfo("Chrome", path, "chrome");
    }

    private static BrowserInfo? FindEdge()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe")
        };

        var path = candidates.FirstOrDefault(File.Exists);
        return path is null ? null : new BrowserInfo("Edge", path, "msedge");
    }
}
