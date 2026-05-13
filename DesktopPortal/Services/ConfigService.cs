using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopPortal.Models;
using DesktopPortal.Utilities;

namespace DesktopPortal.Services;

public sealed class ConfigService
{
    public const int DefaultConfigBackupRetention = 5;

    private const string BackupDirectoryName = "backups";

    private readonly string _appDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigService() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopPortal"))
    {
    }

    public ConfigService(string appDirectory)
    {
        _appDirectory = appDirectory;
        ConfigPath = Path.Combine(_appDirectory, "config.json");
        Logger.Initialize(_appDirectory);
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    public string AppDirectory => _appDirectory;

    public string ConfigPath { get; }

    public string? LastBrokenBackupPath { get; private set; }

    public AppConfig Load()
    {
        Directory.CreateDirectory(_appDirectory);
        LastBrokenBackupPath = null;

        if (!File.Exists(ConfigPath))
        {
            var created = CreateDefaultConfig();
            Save(created);
            Logger.Info($"Created default config: {ConfigPath}");
            return created;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? CreateDefaultConfig();
            EnsureConfigShape(config);
            Logger.Info($"Loaded config: {ConfigPath}");
            return config;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load config; backing up broken file.", ex);
            BackupBrokenConfig();
            var config = CreateDefaultConfig();
            Save(config);
            return config;
        }
    }

    public void Save(AppConfig config)
    {
        var result = TrySave(config);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message ?? "配置保存失败。");
        }
    }

    public ServiceResult TrySave(AppConfig config)
    {
        var tempPath = Path.Combine(_appDirectory, $"config.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(_appDirectory);
            EnsureConfigShape(config);
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(tempPath, json);
            if (File.Exists(ConfigPath))
            {
                BackupCurrentConfig();
                File.Replace(tempPath, ConfigPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, ConfigPath);
            }

            Logger.Info($"Saved config: {ConfigPath}");
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save config.", ex);
            TryDeleteTempFile(tempPath);
            return ServiceResult.Fail($"配置保存失败：{ex.Message}");
        }
    }

    public static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            StartWithWindows = false,
            PauseAllHotkeys = false,
            Rules = new List<PortalRule>()
        };
    }

    private void BackupBrokenConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            return;
        }

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
        var backupPath = Path.Combine(_appDirectory, $"config.broken.{timestamp}.json");
        File.Move(ConfigPath, backupPath, overwrite: true);
        LastBrokenBackupPath = backupPath;
        Logger.Warn($"Broken config backed up to: {backupPath}");
    }

    private void BackupCurrentConfig()
    {
        try
        {
            var backupDirectory = Path.Combine(_appDirectory, BackupDirectoryName);
            Directory.CreateDirectory(backupDirectory);

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmssfff");
            var backupPath = Path.Combine(backupDirectory, $"config.backup.{timestamp}.json");
            if (File.Exists(backupPath))
            {
                backupPath = Path.Combine(backupDirectory, $"config.backup.{timestamp}.{Guid.NewGuid():N}.json");
            }

            File.Copy(ConfigPath, backupPath);
            PruneConfigBackups(backupDirectory);
            Logger.Info($"Config backup created: {backupPath}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Config backup failed: {ex.Message}");
        }
    }

    private static void PruneConfigBackups(string backupDirectory)
    {
        var backups = Directory
            .GetFiles(backupDirectory, "config.backup.*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(DefaultConfigBackupRetention);

        foreach (var backup in backups)
        {
            try
            {
                backup.Delete();
            }
            catch
            {
                // Best effort retention cleanup only.
            }
        }
    }

    private static void EnsureConfigShape(AppConfig config)
    {
        config.Rules ??= new List<PortalRule>();
        foreach (var rule in config.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                rule.Id = Guid.NewGuid().ToString("D");
            }

            if (rule.CreatedAt == default)
            {
                rule.CreatedAt = DateTimeOffset.UtcNow;
            }

            if (rule.UpdatedAt == default)
            {
                rule.UpdatedAt = rule.CreatedAt;
            }
        }
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }
}
