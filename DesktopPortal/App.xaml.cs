using System.Windows;
using DesktopPortal.Services;

namespace DesktopPortal;

public partial class App : System.Windows.Application
{
    private SingleInstanceService? _singleInstanceService;

    public App()
    {
        CrashReporter.Install(this);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceService = new SingleInstanceService();
        if (!_singleInstanceService.TryAcquireOrSignalExisting())
        {
            Shutdown();
            return;
        }

        _singleInstanceService.ActivationRequested += (_, _) =>
            Dispatcher.BeginInvoke(ShowMainWindow);

        base.OnStartup(e);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceService?.Dispose();
        _singleInstanceService = null;
        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        if (MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.ShowMainWindow();
    }
}
