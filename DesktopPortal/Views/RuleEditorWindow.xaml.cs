using System.Windows;
using System.Windows.Controls;
using DesktopPortal.Models;
using DesktopPortal.Utilities;
using Forms = System.Windows.Forms;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DesktopPortal.Views;

public partial class RuleEditorWindow : Window
{
    private readonly PortalRule? _ownerRule;
    private bool _capturingHotkey;

    public RuleEditorWindow(PortalRule? ownerRule)
    {
        InitializeComponent();
        _ownerRule = ownerRule;
        InitializeOptions();
        LoadRule(ownerRule);
    }

    public PortalRule? Rule { get; private set; }

    private void InitializeOptions()
    {
        TargetTypeComboBox.ItemsSource = new[]
        {
            new Option<TargetType>("网页", TargetType.Url),
            new Option<TargetType>("文件", TargetType.File),
            new Option<TargetType>("文件夹", TargetType.Folder),
            new Option<TargetType>("程序", TargetType.Exe)
        };
        TargetTypeComboBox.DisplayMemberPath = nameof(Option<TargetType>.Text);
        TargetTypeComboBox.SelectedValuePath = nameof(Option<TargetType>.Value);

        OpenModeComboBox.ItemsSource = new[]
        {
            new Option<OpenMode>("普通浏览器", OpenMode.Normal),
            new Option<OpenMode>("独立窗口", OpenMode.App)
        };
        OpenModeComboBox.DisplayMemberPath = nameof(Option<OpenMode>.Text);
        OpenModeComboBox.SelectedValuePath = nameof(Option<OpenMode>.Value);
    }

    private void LoadRule(PortalRule? rule)
    {
        var source = rule?.Clone() ?? new PortalRule
        {
            TargetType = TargetType.Url,
            OpenMode = OpenMode.Normal,
            Enabled = true
        };

        NameTextBox.Text = source.Name;
        TargetTypeComboBox.SelectedValue = source.TargetType;
        TargetTextBox.Text = source.Target;
        OpenModeComboBox.SelectedValue = source.OpenMode;
        HotkeyTextBox.Text = source.Hotkey;
        WindowTitleHintTextBox.Text = source.WindowTitleHint ?? string.Empty;
        EnabledCheckBox.IsChecked = source.Enabled;
        UpdateTargetModeUi();
    }

    private void TargetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTargetModeUi();
    }

    private void UpdateTargetModeUi()
    {
        var targetType = GetSelectedTargetType();
        BrowseButton.IsEnabled = targetType != TargetType.Url;
        OpenModeComboBox.IsEnabled = targetType == TargetType.Url;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        switch (GetSelectedTargetType())
        {
            case TargetType.File:
                BrowseFile("选择文件", "所有文件|*.*");
                break;
            case TargetType.Exe:
                BrowseFile("选择程序", "程序|*.exe|所有文件|*.*");
                break;
            case TargetType.Folder:
                using (var dialog = new Forms.FolderBrowserDialog())
                {
                    dialog.Description = "选择文件夹";
                    dialog.UseDescriptionForTitle = true;
                    if (dialog.ShowDialog() == Forms.DialogResult.OK)
                    {
                        TargetTextBox.Text = dialog.SelectedPath;
                    }
                }

                break;
        }
    }

    private void BrowseFile(string title, string filter)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            TargetTextBox.Text = dialog.FileName;
        }
    }

    private void TargetTextBox_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void TargetTextBox_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return;
        }

        var paths = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
        if (!DragDropTargetResolver.TryResolve(paths, out var droppedTarget))
        {
            ShowValidation("拖放的目标不存在或不受支持。");
            return;
        }

        TargetTypeComboBox.SelectedValue = droppedTarget.TargetType;
        TargetTextBox.Text = droppedTarget.Target;
        UpdateTargetModeUi();
        e.Handled = true;
    }

    private void CaptureHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _capturingHotkey = true;
        CaptureHotkeyButton.Content = "请按键...";
        HotkeyHintText.Text = "请按下要录入的键盘快捷键，或点击鼠标中键/侧键。";
        HotkeyTextBox.Focus();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_capturingHotkey)
        {
            return;
        }

        e.Handled = true;
        if (HotkeyParser.TryCreateFromKeyEvent(e, out var normalized, out var message))
        {
            HotkeyTextBox.Text = normalized;
            HotkeyHintText.Text = "快捷键已录入。";
            _capturingHotkey = false;
            CaptureHotkeyButton.Content = "录入快捷键";
            return;
        }

        HotkeyHintText.Text = message ?? "该快捷键不受支持。";
    }

    private void Window_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_capturingHotkey)
        {
            return;
        }

        if (!TryGetMouseHotkeyButton(e.ChangedButton, out var button))
        {
            return;
        }

        e.Handled = true;
        if (HotkeyParser.TryCreateFromMouseButton(button, out var normalized))
        {
            HotkeyTextBox.Text = normalized;
            HotkeyHintText.Text = "鼠标快捷键已录入。";
            _capturingHotkey = false;
            CaptureHotkeyButton.Content = "录入快捷键";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ShowValidation("名称不能为空。");
            return;
        }

        if (!HotkeyParser.TryNormalize(HotkeyTextBox.Text, out var normalizedHotkey))
        {
            ShowValidation("快捷键格式无效或不受支持。");
            return;
        }

        var targetType = GetSelectedTargetType();
        var validation = PathValidator.ValidateTarget(targetType, TargetTextBox.Text);
        if (!validation.IsValid)
        {
            ShowValidation(validation.Message ?? "目标无效。");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        Rule = new PortalRule
        {
            Id = _ownerRule?.Id ?? Guid.NewGuid().ToString("D"),
            Name = NameTextBox.Text,
            Hotkey = normalizedHotkey,
            TargetType = targetType,
            Target = TargetTextBox.Text,
            OpenMode = targetType == TargetType.Url ? GetSelectedOpenMode() : OpenMode.Normal,
            Enabled = EnabledCheckBox.IsChecked == true,
            WindowTitleHint = WindowTitleHintTextBox.Text,
            CreatedAt = _ownerRule?.CreatedAt ?? now,
            UpdatedAt = now
        };

        DialogResult = true;
    }

    private TargetType GetSelectedTargetType()
    {
        return TargetTypeComboBox.SelectedValue is TargetType targetType ? targetType : TargetType.Url;
    }

    private OpenMode GetSelectedOpenMode()
    {
        return OpenModeComboBox.SelectedValue is OpenMode openMode ? openMode : OpenMode.Normal;
    }

    private void ShowValidation(string message)
    {
        System.Windows.MessageBox.Show(this, message, "规则无效", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static bool TryGetMouseHotkeyButton(System.Windows.Input.MouseButton mouseButton, out MouseHotkeyButton button)
    {
        button = mouseButton switch
        {
            System.Windows.Input.MouseButton.Middle => MouseHotkeyButton.Middle,
            System.Windows.Input.MouseButton.XButton1 => MouseHotkeyButton.Back,
            System.Windows.Input.MouseButton.XButton2 => MouseHotkeyButton.Forward,
            _ => default
        };

        return mouseButton is System.Windows.Input.MouseButton.Middle
            or System.Windows.Input.MouseButton.XButton1
            or System.Windows.Input.MouseButton.XButton2;
    }

    private sealed record Option<T>(string Text, T Value);
}
