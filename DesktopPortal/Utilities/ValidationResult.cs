namespace DesktopPortal.Utilities;

public readonly record struct ValidationResult(bool IsValid, string? Message)
{
    public static ValidationResult Success() => new(true, null);

    public static ValidationResult Failure(string message) => new(false, message);
}
