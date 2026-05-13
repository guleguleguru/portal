using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DesktopPortal.Models;
using DesktopPortal.Services;
using DesktopPortal.Utilities;
using DesktopPortal.Views;
using WpfClipboard = System.Windows.Clipboard;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;

namespace DesktopPortal;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly StartupService _startupService = new();
    private readonly HotkeyService _hotkeyService = new();
    private readonly RuleHealthChecker _healthChecker = new();
    private readonly TrayService _trayService = new();
    private readonly TargetExecutor _targetExecutor;
    private AppConfig _config = ConfigService.CreateDefaultConfig();
    private bool _isExiting;
    private bool _isSynchronizingRules;

    public MainWindow()
    {
        InitializeComponent();

        Rules = new ObservableCollection<PortalRule>();
        DataContext = this;

        var activator = new WindowActivator();
        _targetExecutor = new TargetExecutor(activator, new BrowserLocator());

        SourceInitialized += (_, _) =>
        {
            _hotkeyService.Attach(this);
            RegisterHotkeys();
        };
        Loaded += (_, _) => LoadConfig(showBrokenNotice: true);
        Closing += MainWindow_Closing;

        _hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
        _trayService.OpenRequested += (_, _) => ShowMainWindow();
        _trayService.TogglePauseRequested += (_, _) => TogglePauseAll();
        _trayService.ReloadRequested += (_, _) => ReloadConfig();
        _trayService.ExitRequested += (_, _) => ExitApplication();

        Logger.Info("Desktop Portal started.");
    }

    public ObservableCollection<PortalRule> Rules { get; }

    private PortalRule? SelectedRule => RulesGrid.SelectedItem as PortalRule;

    private void LoadConfig(bool showBrokenNotice)
    {
        _config = _configService.Load();
        _isSynchronizingRules = true;
        try
        {
            foreach (var rule in Rules)
            {
                rule.PropertyChanged -= Rule_PropertyChanged;
            }

            Rules.Clear();
            foreach (var rule in _config.Rules)
            {
                rule.PropertyChanged += Rule_PropertyChanged;
                Rules.Add(rule);
            }
        }
        finally
        {
            _isSynchronizingRules = false;
        }

        RegisterHotkeys();
        UpdateStatus();

        if (showBrokenNotice && !string.IsNullOrWhiteSpace(_configService.LastBrokenBackupPath))
        {
            System.Windows.MessageBox.Show(
                this,
                $"配置文件损坏，已备份到：{_configService.LastBrokenBackupPath}\n已创建新的默认配置。",
                "配置已恢复",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private bool SaveConfig()
    {
        _config.Rules = Rules.ToList();
        var result = _configService.TrySave(_config);
        if (!result.Success)
        {
            System.Windows.MessageBox.Show(
                this,
                result.Message ?? "配置保存失败。",
                "保存失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void RegisterHotkeys()
    {
        if (!IsInitialized)
        {
            return;
        }

        _hotkeyService.RegisterRules(Rules, _config.PauseAllHotkeys);
        _healthChecker.Apply(Rules, _config.PauseAllHotkeys);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        RegisteredCountText.Text = _hotkeyService.RegisteredCount.ToString();
        ConflictCountText.Text = _hotkeyService.ConflictCount.ToString();
        RulesCountText.Text = Rules.Count.ToString();
        PausedStateText.Text = _config.PauseAllHotkeys ? "状态：已暂停" : "状态：正常";
        ConfigPathText.Text = _configService.ConfigPath;
        PauseAllButton.Content = _config.PauseAllHotkeys ? "恢复快捷键" : "暂停全部";
        _trayService.UpdatePaused(_config.PauseAllHotkeys);
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        AddRule();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        EditSelectedRule();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedRule();
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        TestSelectedRule();
    }

    private void HealthCheckButton_Click(object sender, RoutedEventArgs e)
    {
        var issueCount = _healthChecker.Apply(Rules, _config.PauseAllHotkeys);
        ShowInfo(issueCount == 0 ? "健康检查完成，未发现问题。" : $"健康检查完成，发现 {issueCount} 个问题，请查看状态列。");
        UpdateStatus();
    }

    private void PauseAllButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePauseAll();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(_config, _configService.ConfigPath, _startupService) { Owner = this };
        if (settings.ShowDialog() == true)
        {
            SaveConfig();
            UpdateStatus();
        }
    }

    private void RulesGrid_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (_isSynchronizingRules)
        {
            return;
        }

        SaveConfig();
        RegisterHotkeys();
    }

    private void RulesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        e.Handled = true;

        var columnHeader = FindVisualParent<DataGridColumnHeader>(source);
        if (columnHeader?.Column is not null)
        {
            OpenColumnContextMenu(columnHeader.Column);
            return;
        }

        var row = FindVisualParent<DataGridRow>(source);
        if (row?.Item is PortalRule rule)
        {
            RulesGrid.SelectedItem = rule;
            RulesGrid.CurrentItem = rule;
            row.Focus();
            OpenRuleContextMenu(rule);
            return;
        }

        OpenGridContextMenu();
    }

    private void RulesGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void OpenRuleContextMenu(PortalRule rule)
    {
        var menu = CreateContextMenu();
        foreach (var action in RuleContextMenuPolicy.GetRowActions(rule))
        {
            if (action is RuleContextAction.EnableRule or RuleContextAction.DisableRule or RuleContextAction.DeleteRule)
            {
                menu.Items.Add(new Separator());
            }

            AddRowActionMenuItem(menu, rule, action);
        }

        OpenContextMenu(menu);
    }

    private void OpenGridContextMenu()
    {
        var menu = CreateContextMenu();
        foreach (var action in RuleContextMenuPolicy.GetGridActions(_config.PauseAllHotkeys))
        {
            AddGridActionMenuItem(menu, action);
        }

        OpenContextMenu(menu);
    }

    private void OpenColumnContextMenu(DataGridColumn column)
    {
        var visibleColumns = RulesGrid.Columns.Count(c => c.Visibility == Visibility.Visible);
        var canHideColumn = visibleColumns > 1;
        var menu = CreateContextMenu();
        foreach (var action in RuleContextMenuPolicy.GetColumnActions(canHideColumn))
        {
            AddColumnActionMenuItem(menu, column, action, canHideColumn);
        }

        OpenContextMenu(menu);
    }

    private void AddRowActionMenuItem(WpfContextMenu menu, PortalRule rule, RuleContextAction action)
    {
        switch (action)
        {
            case RuleContextAction.TestRule:
                AddMenuItem(menu, "测试执行", (_, _) => ExecuteRule(rule, showSuccess: true));
                break;
            case RuleContextAction.EditRule:
                AddMenuItem(menu, "编辑规则", (_, _) => EditRule(rule));
                break;
            case RuleContextAction.DuplicateRule:
                AddMenuItem(menu, "复制一份", (_, _) => DuplicateRule(rule));
                break;
            case RuleContextAction.EnableRule:
                AddMenuItem(menu, "启用规则", (_, _) => ToggleRuleEnabled(rule));
                break;
            case RuleContextAction.DisableRule:
                AddMenuItem(menu, "禁用规则", (_, _) => ToggleRuleEnabled(rule));
                break;
            case RuleContextAction.CopyTarget:
                AddMenuItem(
                    menu,
                    "复制目标",
                    (_, _) => CopyText(rule.Target, "目标已复制。"),
                    !string.IsNullOrWhiteSpace(rule.Target));
                break;
            case RuleContextAction.CopyHotkey:
                AddMenuItem(
                    menu,
                    "复制快捷键",
                    (_, _) => CopyText(rule.Hotkey, "快捷键已复制。"),
                    !string.IsNullOrWhiteSpace(rule.Hotkey));
                break;
            case RuleContextAction.OpenTargetLocation:
                AddMenuItem(
                    menu,
                    GetOpenLocationText(rule),
                    (_, _) => OpenTargetLocation(rule),
                    !string.IsNullOrWhiteSpace(rule.Target));
                break;
            case RuleContextAction.DeleteRule:
                AddMenuItem(menu, "删除规则", (_, _) => DeleteRule(rule));
                break;
        }
    }

    private void AddGridActionMenuItem(WpfContextMenu menu, RuleContextAction action)
    {
        switch (action)
        {
            case RuleContextAction.AddRule:
                AddMenuItem(menu, "新增规则", (_, _) => AddRule());
                break;
            case RuleContextAction.PauseAllHotkeys:
                AddMenuItem(menu, "暂停全部快捷键", (_, _) => TogglePauseAll());
                break;
            case RuleContextAction.ResumeHotkeys:
                AddMenuItem(menu, "恢复全部快捷键", (_, _) => TogglePauseAll());
                break;
            case RuleContextAction.ReloadConfig:
                AddMenuItem(menu, "重载配置", (_, _) => ReloadConfig());
                break;
        }
    }

    private void AddColumnActionMenuItem(
        WpfContextMenu menu,
        DataGridColumn column,
        RuleContextAction action,
        bool canHideColumn)
    {
        switch (action)
        {
            case RuleContextAction.HideColumn:
                AddMenuItem(
                    menu,
                    $"隐藏“{column.Header}”列",
                    (_, _) => column.Visibility = Visibility.Collapsed,
                    canHideColumn);
                break;
            case RuleContextAction.ShowAllColumns:
                AddMenuItem(menu, "显示全部列", (_, _) =>
                {
                    foreach (var gridColumn in RulesGrid.Columns)
                    {
                        gridColumn.Visibility = Visibility.Visible;
                    }
                });
                break;
        }
    }

    private static WpfContextMenu CreateContextMenu()
    {
        return new WpfContextMenu
        {
            Placement = PlacementMode.MousePoint
        };
    }

    private void OpenContextMenu(WpfContextMenu menu)
    {
        menu.PlacementTarget = RulesGrid;
        menu.IsOpen = true;
    }

    private static void AddMenuItem(WpfContextMenu menu, string header, RoutedEventHandler click, bool isEnabled = true)
    {
        var item = new WpfMenuItem
        {
            Header = header,
            IsEnabled = isEnabled
        };
        item.Click += click;
        menu.Items.Add(item);
    }

    private void Rule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSynchronizingRules)
        {
            return;
        }

        if (e.PropertyName == nameof(PortalRule.Enabled))
        {
            if (sender is PortalRule rule)
            {
                rule.UpdatedAt = DateTimeOffset.UtcNow;
            }

            SaveConfig();
            RegisterHotkeys();
        }
    }

    private void HotkeyService_HotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_config.PauseAllHotkeys)
            {
                return;
            }

            var rule = Rules.FirstOrDefault(r => r.Id == e.RuleId);
            if (rule is null || !rule.Enabled)
            {
                return;
            }

            ExecuteRule(rule, showSuccess: false);
        });
    }

    private void AddRule()
    {
        var editor = new RuleEditorWindow(ownerRule: null) { Owner = this };
        if (editor.ShowDialog() == true && editor.Rule is not null)
        {
            editor.Rule.PropertyChanged += Rule_PropertyChanged;
            Rules.Add(editor.Rule);
            RulesGrid.SelectedItem = editor.Rule;
            SaveConfig();
            RegisterHotkeys();
        }
    }

    private void EditSelectedRule()
    {
        if (SelectedRule is not { } selected)
        {
            ShowInfo("请先选择一条规则。");
            return;
        }

        EditRule(selected);
    }

    private void EditRule(PortalRule rule)
    {
        var editor = new RuleEditorWindow(rule) { Owner = this };
        if (editor.ShowDialog() == true && editor.Rule is not null)
        {
            rule.CopyFrom(editor.Rule);
            SaveConfig();
            RegisterHotkeys();
        }
    }

    private void DeleteSelectedRule()
    {
        if (SelectedRule is not { } selected)
        {
            ShowInfo("请先选择一条规则。");
            return;
        }

        DeleteRule(selected);
    }

    private void DeleteRule(PortalRule rule)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"确定删除规则“{rule.Name}”吗？",
            "删除规则",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        rule.PropertyChanged -= Rule_PropertyChanged;
        Rules.Remove(rule);
        SaveConfig();
        RegisterHotkeys();
    }

    private void TestSelectedRule()
    {
        if (SelectedRule is not { } selected)
        {
            ShowInfo("请先选择一条规则。");
            return;
        }

        ExecuteRule(selected, showSuccess: true);
    }

    private void DuplicateRule(PortalRule rule)
    {
        var duplicated = rule.Clone();
        duplicated.Id = Guid.NewGuid().ToString("D");
        duplicated.Name = CreateDuplicateName(rule.Name);
        duplicated.Enabled = false;
        duplicated.CreatedAt = DateTimeOffset.UtcNow;
        duplicated.UpdatedAt = DateTimeOffset.UtcNow;
        duplicated.PropertyChanged += Rule_PropertyChanged;
        Rules.Add(duplicated);
        RulesGrid.SelectedItem = duplicated;
        SaveConfig();
        RegisterHotkeys();
    }

    private string CreateDuplicateName(string originalName)
    {
        var baseName = string.IsNullOrWhiteSpace(originalName) ? "未命名规则" : originalName;
        var candidate = $"{baseName} 副本";
        var index = 2;
        while (Rules.Any(rule => string.Equals(rule.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseName} 副本 {index}";
            index++;
        }

        return candidate;
    }

    private void ToggleRuleEnabled(PortalRule rule)
    {
        rule.Enabled = !rule.Enabled;
    }

    private void CopyText(string text, string successMessage)
    {
        try
        {
            WpfClipboard.SetText(text);
            _trayService.ShowBalloon("桌面传送门", successMessage);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to copy text to clipboard.", ex);
            ShowInfo("复制失败，剪贴板可能暂时不可用。");
        }
    }

    private void OpenTargetLocation(PortalRule rule)
    {
        try
        {
            if (rule.TargetType == TargetType.Url)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = rule.Target,
                    UseShellExecute = true
                });
                return;
            }

            if (rule.TargetType == TargetType.Folder)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true,
                    Arguments = $"\"{rule.Target}\""
                });
                return;
            }

            var path = rule.Target;
            if (!File.Exists(path))
            {
                ShowInfo("目标文件不存在，无法打开所在位置。");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
                Arguments = $"/select,\"{path}\""
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to open target location: {rule.Name}", ex);
            ShowInfo("打开目标位置失败。");
        }
    }

    private static string GetOpenLocationText(PortalRule rule)
    {
        return rule.TargetType switch
        {
            TargetType.Url => "打开网页",
            TargetType.Folder => "打开文件夹",
            _ => "打开所在位置"
        };
    }

    private void ExecuteRule(PortalRule rule, bool showSuccess)
    {
        var result = _targetExecutor.Execute(rule);
        if (!result.Success)
        {
            System.Windows.MessageBox.Show(
                this,
                result.Message ?? "执行目标失败。",
                "执行失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (showSuccess)
        {
            ShowInfo(result.Message ?? "执行成功。");
        }
    }

    private void TogglePauseAll()
    {
        _config.PauseAllHotkeys = !_config.PauseAllHotkeys;
        SaveConfig();
        RegisterHotkeys();
    }

    private void ReloadConfig()
    {
        LoadConfig(showBrokenNotice: true);
        _trayService.ShowBalloon("桌面传送门", "配置已重载。");
    }

    public void ShowMainWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            _trayService.ShowBalloon("桌面传送门仍在运行", "可通过托盘菜单打开或退出。");
            return;
        }

        _hotkeyService.Dispose();
        _trayService.Dispose();
        Logger.Info("Desktop Portal exited.");
    }

    private void ShowInfo(string message)
    {
        System.Windows.MessageBox.Show(this, message, "桌面传送门", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static T? FindVisualParent<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T typed)
            {
                return typed;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}
