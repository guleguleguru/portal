using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopPortal.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopPortal";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to read startup registry key.", ex);
            return false;
        }
    }

    public ServiceResult SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true) ??
                            Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return ServiceResult.Fail("无法打开开机自启注册表项。");
            }

            if (enabled)
            {
                var executablePath = GetExecutablePath();
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    return ServiceResult.Fail("无法定位当前程序路径。");
                }

                key.SetValue(ValueName, $"\"{executablePath}\"");
                Logger.Info($"Startup enabled: {executablePath}");
                return ServiceResult.Ok();
            }

            key.DeleteValue(ValueName, throwOnMissingValue: false);
            Logger.Info("Startup disabled.");
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to update startup registry key.", ex);
            return ServiceResult.Fail($"开机自启设置失败：{ex.Message}");
        }
    }

    private static string? GetExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
        {
            return Environment.ProcessPath;
        }

        try
        {
            using var process = Process.GetCurrentProcess();
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
