using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopPortal.Services;

public sealed class WindowActivator
{
    private const int SwRestore = 9;
    private const int SwShow = 5;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTop = IntPtr.Zero;

    private readonly Dictionary<string, IntPtr> _cachedWindows = new(StringComparer.Ordinal);

    public ServiceResult TryActivateByProcessName(string processName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName);
        return TryActivateMatchingWindow(
            cacheKey: null,
            predicate: window => string.Equals(window.ProcessName, normalized, StringComparison.OrdinalIgnoreCase),
            missMessage: "未找到已打开的目标窗口。");
    }

    public ServiceResult TryActivateByTitle(string? titlePart)
    {
        if (string.IsNullOrWhiteSpace(titlePart))
        {
            return ServiceResult.Fail("未提供窗口标题提示。");
        }

        return TryActivateMatchingWindow(
            cacheKey: null,
            predicate: window => window.Title.Contains(titlePart.Trim(), StringComparison.OrdinalIgnoreCase),
            missMessage: "未找到匹配标题的窗口。");
    }

    public ServiceResult TryActivateUrl(string ruleId, string url, string? titleHint, IEnumerable<string>? processNames = null)
    {
        var candidates = WindowMatchPlan.BuildUrlTitleCandidates(url, titleHint);
        var browserProcesses = new HashSet<string>(
            (processNames ?? KnownBrowserProcessNames())
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))!,
            StringComparer.OrdinalIgnoreCase);

        return TryActivateMatchingWindow(
            cacheKey: CacheKey(ruleId, "url"),
            predicate: window =>
                WindowMatchPlan.TitleMatches(window.Title, candidates) &&
                (browserProcesses.Count == 0 || browserProcesses.Contains(window.ProcessName)),
            missMessage: "未找到已打开的网页窗口。");
    }

    public ServiceResult TryActivateFile(string ruleId, string filePath, string? titleHint)
    {
        var candidates = WindowMatchPlan.BuildFileTitleCandidates(filePath, titleHint);
        return TryActivateMatchingWindow(
            cacheKey: CacheKey(ruleId, "file"),
            predicate: window => WindowMatchPlan.FileWindowMatches(window.Title, window.ProcessName, candidates),
            missMessage: "未找到已打开的文件窗口。");
    }

    public ServiceResult TryActivateExecutable(string executablePath)
    {
        return TryActivateExecutable(ruleId: string.Empty, executablePath);
    }

    public ServiceResult TryActivateExecutable(string ruleId, string executablePath)
    {
        var processName = Path.GetFileNameWithoutExtension(executablePath);
        return TryActivateMatchingWindow(
            cacheKey: string.IsNullOrWhiteSpace(ruleId) ? null : CacheKey(ruleId, "exe"),
            predicate: window =>
                WindowMatchPlan.PathsEqual(window.ProcessPath, executablePath) ||
                string.Equals(window.ProcessName, processName, StringComparison.OrdinalIgnoreCase),
            missMessage: "未找到已打开的程序窗口。");
    }

    public ServiceResult TryActivateFolder(string folderPath)
    {
        return TryActivateFolder(ruleId: string.Empty, folderPath);
    }

    public ServiceResult TryActivateFolder(string ruleId, string folderPath)
    {
        var candidates = WindowMatchPlan.BuildFolderTitleCandidates(folderPath);
        return TryActivateMatchingWindow(
            cacheKey: string.IsNullOrWhiteSpace(ruleId) ? null : CacheKey(ruleId, "folder"),
            predicate: window =>
                string.Equals(window.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase) &&
                (WindowMatchPlan.PathsEqual(window.ExplorerFolderPath, folderPath) ||
                 WindowMatchPlan.TitleMatches(window.Title, candidates)),
            missMessage: "未找到已打开的文件夹窗口。");
    }

    public ServiceResult BringToFront(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
        {
            return ServiceResult.Fail("窗口句柄无效。");
        }

        try
        {
            if (IsIconic(windowHandle))
            {
                ShowWindow(windowHandle, SwRestore);
            }
            else
            {
                ShowWindow(windowHandle, SwShow);
            }

            var foregroundWindow = GetForegroundWindow();
            var currentThread = GetCurrentThreadId();
            var foregroundThread = foregroundWindow == IntPtr.Zero
                ? 0
                : GetWindowThreadProcessId(foregroundWindow, out _);
            var targetThread = GetWindowThreadProcessId(windowHandle, out _);

            var attachedForeground = false;
            var attachedTarget = false;
            try
            {
                if (foregroundThread != 0 && foregroundThread != currentThread)
                {
                    attachedForeground = AttachThreadInput(currentThread, foregroundThread, true);
                }

                if (targetThread != 0 && targetThread != currentThread)
                {
                    attachedTarget = AttachThreadInput(currentThread, targetThread, true);
                }

                BringWindowToTop(windowHandle);
                SetWindowPos(windowHandle, HwndTop, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);

                if (!SetForegroundWindow(windowHandle) && GetForegroundWindow() != windowHandle)
                {
                    return ServiceResult.Fail("无法激活窗口，可能是权限或系统前台窗口限制导致。");
                }

                SetActiveWindow(windowHandle);
                SetFocus(windowHandle);
            }
            finally
            {
                if (attachedTarget)
                {
                    AttachThreadInput(currentThread, targetThread, false);
                }

                if (attachedForeground)
                {
                    AttachThreadInput(currentThread, foregroundThread, false);
                }
            }

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to activate window.", ex);
            return ServiceResult.Fail("窗口激活失败。");
        }
    }

    private ServiceResult TryActivateMatchingWindow(string? cacheKey, Func<WindowInfo, bool> predicate, string missMessage)
    {
        if (!string.IsNullOrWhiteSpace(cacheKey) &&
            _cachedWindows.TryGetValue(cacheKey, out var cachedHandle))
        {
            if (TryGetWindowInfo(cachedHandle, out var cachedWindow) && predicate(cachedWindow))
            {
                var cachedResult = BringToFront(cachedHandle);
                if (cachedResult.Success)
                {
                    Logger.Info($"Activated cached window: {cacheKey} {cachedWindow.Title}");
                    return cachedResult;
                }

                Logger.Warn($"Cached window activation failed: {cacheKey} {cachedResult.Message}");
            }

            _cachedWindows.Remove(cacheKey);
        }

        var window = EnumerateWindows().FirstOrDefault(predicate);
        if (window.Handle == IntPtr.Zero)
        {
            return ServiceResult.Fail(missMessage);
        }

        var result = BringToFront(window.Handle);
        if (result.Success && !string.IsNullOrWhiteSpace(cacheKey))
        {
            _cachedWindows[cacheKey] = window.Handle;
            Logger.Info($"Activated and cached window: {cacheKey} {window.Title}");
        }

        return result;
    }

    private static IReadOnlyList<WindowInfo> EnumerateWindows()
    {
        var windows = new List<WindowInfo>();
        var explorerPaths = GetExplorerFolderPathsByHandle();

        EnumWindows((hWnd, _) =>
        {
            if (TryGetWindowInfo(hWnd, explorerPaths, out var window))
            {
                windows.Add(window);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static bool TryGetWindowInfo(IntPtr hWnd, out WindowInfo window)
    {
        return TryGetWindowInfo(hWnd, GetExplorerFolderPathsByHandle(), out window);
    }

    private static bool TryGetWindowInfo(
        IntPtr hWnd,
        IReadOnlyDictionary<IntPtr, string> explorerPaths,
        out WindowInfo window)
    {
        window = default;
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd) || !IsWindowVisible(hWnd) || GetWindowTextLength(hWnd) == 0)
        {
            return false;
        }

        var title = GetWindowTitle(hWnd);
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        GetWindowThreadProcessId(hWnd, out var processId);
        string processName;
        string? processPath = null;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
            try
            {
                processPath = process.MainModule?.FileName;
            }
            catch
            {
                processPath = null;
            }
        }
        catch
        {
            return false;
        }

        explorerPaths.TryGetValue(hWnd, out var explorerFolderPath);
        window = new WindowInfo(hWnd, title, processName, processPath, explorerFolderPath);
        return true;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        var builder = new StringBuilder(length + 1);
        GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static IReadOnlyDictionary<IntPtr, string> GetExplorerFolderPathsByHandle()
    {
        var paths = new Dictionary<IntPtr, string>();
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                return paths;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            foreach (var shellWindow in shell.Windows())
            {
                try
                {
                    var handle = new IntPtr(Convert.ToInt64(shellWindow.HWND));
                    string? locationUrl = Convert.ToString((object?)shellWindow.LocationURL);
                    if (!string.IsNullOrWhiteSpace(locationUrl) &&
                        Uri.TryCreate(locationUrl, UriKind.Absolute, out Uri? uri) &&
                        uri.IsFile)
                    {
                        paths[handle] = uri.LocalPath;
                        continue;
                    }

                    var path = Convert.ToString(shellWindow.Document?.Folder?.Self?.Path);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        paths[handle] = path;
                    }
                }
                catch
                {
                    // Ignore individual shell windows that cannot expose their path.
                }
            }
        }
        catch
        {
            // Shell COM may be unavailable or blocked; title fallback still works.
        }

        return paths;
    }

    private static IEnumerable<string> KnownBrowserProcessNames()
    {
        yield return "chrome";
        yield return "msedge";
        yield return "firefox";
        yield return "brave";
        yield return "opera";
        yield return "opera_gx";
        yield return "vivaldi";
    }

    private static string CacheKey(string ruleId, string targetKind) => $"{ruleId}:{targetKind}";

    private readonly record struct WindowInfo(
        IntPtr Handle,
        string Title,
        string ProcessName,
        string? ProcessPath,
        string? ExplorerFolderPath);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
