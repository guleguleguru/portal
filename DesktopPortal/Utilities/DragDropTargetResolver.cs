using DesktopPortal.Models;

namespace DesktopPortal.Utilities;

public readonly record struct DroppedTarget(TargetType TargetType, string Target);

public static class DragDropTargetResolver
{
    public static bool TryResolve(IReadOnlyList<string>? paths, out DroppedTarget target)
    {
        target = default;
        var path = paths?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        path = path.Trim();
        if (Directory.Exists(path))
        {
            target = new DroppedTarget(TargetType.Folder, path);
            return true;
        }

        if (!File.Exists(path))
        {
            return false;
        }

        var targetType = string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase)
            ? TargetType.Exe
            : TargetType.File;

        target = new DroppedTarget(targetType, path);
        return true;
    }
}
