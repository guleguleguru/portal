using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DesktopPortal.Models;

public sealed class PortalRule : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("D");
    private string _name = string.Empty;
    private string _hotkey = string.Empty;
    private TargetType _targetType = TargetType.Url;
    private string _target = string.Empty;
    private OpenMode _openMode = OpenMode.Normal;
    private bool _enabled = true;
    private string? _windowTitleHint;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    private bool _isRegistered;
    private string? _registrationError;
    private string? _healthError;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("D") : value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value.Trim());
    }

    public string Hotkey
    {
        get => _hotkey;
        set => SetField(ref _hotkey, value.Trim());
    }

    public TargetType TargetType
    {
        get => _targetType;
        set
        {
            if (SetField(ref _targetType, value))
            {
                OnPropertyChanged(nameof(TargetTypeText));
            }
        }
    }

    public string Target
    {
        get => _target;
        set => SetField(ref _target, value.Trim());
    }

    public OpenMode OpenMode
    {
        get => _openMode;
        set
        {
            if (SetField(ref _openMode, value))
            {
                OnPropertyChanged(nameof(OpenModeText));
            }
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetField(ref _enabled, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string? WindowTitleHint
    {
        get => _windowTitleHint;
        set => SetField(ref _windowTitleHint, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }

    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        set => SetField(ref _createdAt, value);
    }

    public DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        set => SetField(ref _updatedAt, value);
    }

    [JsonIgnore]
    public bool IsRegistered
    {
        get => _isRegistered;
        set
        {
            if (SetField(ref _isRegistered, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    [JsonIgnore]
    public string? RegistrationError
    {
        get => _registrationError;
        set
        {
            if (SetField(ref _registrationError, value))
            {
                OnPropertyChanged(nameof(HasConflict));
                OnPropertyChanged(nameof(HasIssue));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    [JsonIgnore]
    public string? HealthError
    {
        get => _healthError;
        set
        {
            if (SetField(ref _healthError, value))
            {
                OnPropertyChanged(nameof(HasIssue));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    [JsonIgnore]
    public bool HasConflict => !string.IsNullOrWhiteSpace(RegistrationError);

    [JsonIgnore]
    public bool HasIssue => HasConflict || !string.IsNullOrWhiteSpace(HealthError);

    [JsonIgnore]
    public string StatusText
    {
        get
        {
            if (!Enabled)
            {
                return "已禁用";
            }

            if (!string.IsNullOrWhiteSpace(RegistrationError))
            {
                return RegistrationError;
            }

            if (!string.IsNullOrWhiteSpace(HealthError))
            {
                return HealthError;
            }

            return IsRegistered ? "已启用" : "未启用";
        }
    }

    [JsonIgnore]
    public string TargetTypeText => TargetType switch
    {
        TargetType.Url => "网页",
        TargetType.File => "文件",
        TargetType.Folder => "文件夹",
        TargetType.Exe => "程序",
        _ => TargetType.ToString()
    };

    [JsonIgnore]
    public string OpenModeText => TargetType == TargetType.Url
        ? OpenMode switch
        {
            OpenMode.Normal => "普通浏览器",
            OpenMode.App => "独立窗口",
            _ => OpenMode.ToString()
        }
        : "-";

    public PortalRule Clone()
    {
        return new PortalRule
        {
            Id = Id,
            Name = Name,
            Hotkey = Hotkey,
            TargetType = TargetType,
            Target = Target,
            OpenMode = OpenMode,
            Enabled = Enabled,
            WindowTitleHint = WindowTitleHint,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    public void CopyFrom(PortalRule other)
    {
        Name = other.Name;
        Hotkey = other.Hotkey;
        TargetType = other.TargetType;
        Target = other.Target;
        OpenMode = other.OpenMode;
        Enabled = other.Enabled;
        WindowTitleHint = other.WindowTitleHint;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
