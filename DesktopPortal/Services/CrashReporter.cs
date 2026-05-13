using System.Threading.Tasks;
using DesktopPortal.Utilities;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace DesktopPortal.Services;

public static class CrashReporter
{
    public static void Install(WpfApplication application)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogUnhandledException("AppDomain.UnhandledException", e.ExceptionObject);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogUnhandledException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        application.DispatcherUnhandledException += (_, e) =>
        {
            LogUnhandledException("DispatcherUnhandledException", e.Exception);
            WpfMessageBox.Show(
                e.Exception.Message,
                "桌面传送门遇到错误",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Error);
            e.Handled = true;
        };
    }

    public static void LogUnhandledException(string source, object? exceptionObject)
    {
        Logger.Error(FormatUnhandledException(source, exceptionObject));
    }

    public static string FormatUnhandledException(string source, object? exceptionObject)
    {
        return exceptionObject switch
        {
            Exception exception => $"Unhandled exception from {source}: {exception}",
            null => $"Unhandled exception from {source}: <null>",
            _ => $"Unhandled exception from {source}: {exceptionObject}"
        };
    }
}
