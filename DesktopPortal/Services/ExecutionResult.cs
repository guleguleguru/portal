namespace DesktopPortal.Services;

public readonly record struct ExecutionResult(bool Success, string? Message)
{
    public static ExecutionResult Ok(string? message = null) => new(true, message);

    public static ExecutionResult Fail(string message) => new(false, message);
}
