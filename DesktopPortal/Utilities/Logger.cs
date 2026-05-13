namespace DesktopPortal.Utilities;

public static class Logger
{
    private const long DefaultMaxLogBytes = 5 * 1024 * 1024;
    private const int DefaultMaxArchiveFiles = 3;
    private static readonly object Sync = new();
    private static string _logFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DesktopPortal",
        "logs",
        "app.log");
    private static long _maxLogBytes = DefaultMaxLogBytes;
    private static int _maxArchiveFiles = DefaultMaxArchiveFiles;

    public static void Initialize(string appDirectory, long maxLogBytes = DefaultMaxLogBytes, int maxArchiveFiles = DefaultMaxArchiveFiles)
    {
        var logDirectory = Path.Combine(appDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, "app.log");
        _maxLogBytes = Math.Max(1, maxLogBytes);
        _maxArchiveFiles = Math.Max(1, maxArchiveFiles);
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
    {
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
                RotateIfNeeded();
                File.AppendAllText(_logFilePath, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath))
        {
            return;
        }

        var fileInfo = new FileInfo(_logFilePath);
        if (fileInfo.Length < _maxLogBytes)
        {
            return;
        }

        var logDirectory = fileInfo.DirectoryName;
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            return;
        }

        var archivePath = Path.Combine(logDirectory, $"app.{DateTimeOffset.Now:yyyyMMddHHmmssfff}.log");
        File.Move(_logFilePath, archivePath, overwrite: true);
        CleanupOldArchives(logDirectory);
    }

    private static void CleanupOldArchives(string logDirectory)
    {
        var archives = Directory
            .GetFiles(logDirectory, "app.*.log")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(_maxArchiveFiles);

        foreach (var archive in archives)
        {
            try
            {
                archive.Delete();
            }
            catch
            {
                // Archive cleanup is best effort.
            }
        }
    }
}
