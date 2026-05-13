using System.Diagnostics;
using DesktopPortal.Models;
using DesktopPortal.Utilities;

namespace DesktopPortal.Services;

public sealed class TargetExecutor
{
    private static readonly string[] AppBrowserProcessNames = ["chrome", "msedge"];
    private static readonly string[] BrowserProcessNames = ["chrome", "msedge", "firefox", "brave", "opera", "opera_gx", "vivaldi"];

    private readonly WindowActivator _windowActivator;
    private readonly BrowserLocator _browserLocator;

    public TargetExecutor(WindowActivator windowActivator, BrowserLocator browserLocator)
    {
        _windowActivator = windowActivator;
        _browserLocator = browserLocator;
    }

    public ExecutionResult Execute(PortalRule rule)
    {
        if (!rule.Enabled)
        {
            return ExecutionResult.Fail("规则已禁用。");
        }

        try
        {
            var result = rule.TargetType switch
            {
                TargetType.Url => ExecuteUrl(rule),
                TargetType.File => ExecuteFile(rule),
                TargetType.Folder => ExecuteFolder(rule),
                TargetType.Exe => ExecuteExe(rule),
                _ => ExecutionResult.Fail("未知目标类型。")
            };

            if (result.Success)
            {
                Logger.Info($"Executed target: {rule.Name} {rule.TargetType} {rule.Target}");
            }
            else
            {
                Logger.Warn($"Execute target failed: {rule.Name} {result.Message}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Execute target exception: {rule.Name}", ex);
            return ExecutionResult.Fail($"目标程序启动失败：{ex.Message}");
        }
    }

    private ExecutionResult ExecuteUrl(PortalRule rule)
    {
        var validation = PathValidator.ValidateTarget(TargetType.Url, rule.Target);
        if (!validation.IsValid)
        {
            return ExecutionResult.Fail(validation.Message ?? "URL 格式不合法。");
        }

        if (rule.OpenMode == OpenMode.App)
        {
            var activation = _windowActivator.TryActivateUrl(rule.Id, rule.Target, rule.WindowTitleHint, AppBrowserProcessNames);
            if (activation.Success)
            {
                return ExecutionResult.Ok("已激活现有网页窗口。");
            }

            Logger.Info($"No existing app URL window activated: {rule.Name} {activation.Message}");
            var browser = _browserLocator.FindAppBrowser();
            if (browser is not null)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = browser.Path,
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add($"--app={rule.Target}");
                Process.Start(startInfo);
                return ExecutionResult.Ok($"已使用 {browser.Name} 独立窗口打开。");
            }

            Logger.Warn("Chrome or Edge not found for app mode; falling back to default browser.");
        }
        else
        {
            var activation = _windowActivator.TryActivateUrl(rule.Id, rule.Target, rule.WindowTitleHint, BrowserProcessNames);
            if (activation.Success)
            {
                return ExecutionResult.Ok("已激活现有网页窗口。");
            }

            Logger.Info($"No existing URL window activated: {rule.Name} {activation.Message}");
        }

        OpenWithShell(rule.Target);
        return ExecutionResult.Ok("已用默认浏览器打开。");
    }

    private ExecutionResult ExecuteFile(PortalRule rule)
    {
        var validation = PathValidator.ValidateTarget(TargetType.File, rule.Target);
        if (!validation.IsValid)
        {
            return ExecutionResult.Fail(validation.Message ?? "文件不存在。");
        }

        var activation = _windowActivator.TryActivateFile(rule.Id, rule.Target, rule.WindowTitleHint);
        if (activation.Success)
        {
            return ExecutionResult.Ok("已激活现有文件窗口。");
        }

        Logger.Info($"No existing file window activated: {rule.Name} {activation.Message}");
        OpenWithShell(rule.Target);
        return ExecutionResult.Ok("已打开文件。");
    }

    private ExecutionResult ExecuteFolder(PortalRule rule)
    {
        var validation = PathValidator.ValidateTarget(TargetType.Folder, rule.Target);
        if (!validation.IsValid)
        {
            return ExecutionResult.Fail(validation.Message ?? "文件夹不存在。");
        }

        var activation = _windowActivator.TryActivateFolder(rule.Id, rule.Target);
        if (activation.Success)
        {
            return ExecutionResult.Ok("已激活现有文件夹窗口。");
        }

        Logger.Info($"No existing folder window activated: {rule.Name} {activation.Message}");
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add(rule.Target);
        Process.Start(startInfo);
        return ExecutionResult.Ok("已打开文件夹。");
    }

    private ExecutionResult ExecuteExe(PortalRule rule)
    {
        var validation = PathValidator.ValidateTarget(TargetType.Exe, rule.Target);
        if (!validation.IsValid)
        {
            return ExecutionResult.Fail(validation.Message ?? "程序不存在。");
        }

        var activation = _windowActivator.TryActivateExecutable(rule.Id, rule.Target);
        if (activation.Success)
        {
            return ExecutionResult.Ok("已激活现有程序窗口。");
        }

        Logger.Info($"No existing exe window activated: {rule.Name} {activation.Message}");
        var startInfo = new ProcessStartInfo
        {
            FileName = rule.Target,
            WorkingDirectory = Path.GetDirectoryName(rule.Target) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };
        Process.Start(startInfo);
        return ExecutionResult.Ok("已启动程序。");
    }

    private static void OpenWithShell(string target)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}
