using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DesktopPortal.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _pauseMenuItem;

    public event EventHandler? OpenRequested;
    public event EventHandler? TogglePauseRequested;
    public event EventHandler? ReloadRequested;
    public event EventHandler? ExitRequested;

    public TrayService()
    {
        _pauseMenuItem = new Forms.ToolStripMenuItem("暂停全部快捷键", null, (_, _) => TogglePauseRequested?.Invoke(this, EventArgs.Empty));

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(new Forms.ToolStripMenuItem("打开设置", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(_pauseMenuItem);
        menu.Items.Add(new Forms.ToolStripMenuItem("重载配置", null, (_, _) => ReloadRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty)));

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "桌面传送门 Desktop Portal",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdatePaused(bool paused)
    {
        _pauseMenuItem.Text = paused ? "恢复快捷键" : "暂停全部快捷键";
    }

    public void ShowBalloon(string title, string message)
    {
        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(3000);
        }
        catch
        {
            // Tray notifications are best effort.
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private static Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/DesktopPortal.ico", UriKind.Absolute);
            var resource = System.Windows.Application.GetResourceStream(iconUri);
            if (resource?.Stream is not null)
            {
                return new Drawing.Icon(resource.Stream);
            }
        }
        catch
        {
            // Fall back to the system icon if resource loading fails.
        }

        return Drawing.SystemIcons.Application;
    }
}
