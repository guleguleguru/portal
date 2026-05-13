using System.Windows;
using DesktopPortal.Models;
using DesktopPortal.Services;

namespace DesktopPortal.Views;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly StartupService _startupService;

    public SettingsWindow(AppConfig config, string configPath, StartupService startupService)
    {
        InitializeComponent();
        _config = config;
        _startupService = startupService;
        StartWithWindowsCheckBox.IsChecked = config.StartWithWindows || startupService.IsEnabled();
        ConfigPathTextBox.Text = configPath;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var enabled = StartWithWindowsCheckBox.IsChecked == true;
        var result = _startupService.SetEnabled(enabled);
        if (!result.Success)
        {
            System.Windows.MessageBox.Show(
                this,
                result.Message ?? "开机自启设置失败。",
                "设置失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _config.StartWithWindows = enabled;
        DialogResult = true;
    }
}
