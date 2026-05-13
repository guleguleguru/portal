namespace DesktopPortal.Services;

public readonly record struct ServiceResult(bool Success, string? Message)
{
    public static ServiceResult Ok(string? message = null) => new(true, message);

    public static ServiceResult Fail(string message) => new(false, message);
}
