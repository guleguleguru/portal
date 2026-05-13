using DesktopPortal.Models;

namespace DesktopPortal.Utilities;

public static class PathValidator
{
    public static ValidationResult ValidateTarget(TargetType targetType, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ValidationResult.Failure("目标不能为空。");
        }

        return targetType switch
        {
            TargetType.Url => ValidateUrl(target),
            TargetType.File => File.Exists(target) ? ValidationResult.Success() : ValidationResult.Failure("文件不存在。"),
            TargetType.Folder => Directory.Exists(target) ? ValidationResult.Success() : ValidationResult.Failure("文件夹不存在。"),
            TargetType.Exe => ValidateExe(target),
            _ => ValidationResult.Failure("未知目标类型。")
        };
    }

    private static ValidationResult ValidateUrl(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return ValidationResult.Failure("URL 格式不合法。");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateExe(string target)
    {
        if (!File.Exists(target))
        {
            return ValidationResult.Failure("程序不存在。");
        }

        return string.Equals(Path.GetExtension(target), ".exe", StringComparison.OrdinalIgnoreCase)
            ? ValidationResult.Success()
            : ValidationResult.Failure("程序目标必须是 .exe 文件。");
    }
}
