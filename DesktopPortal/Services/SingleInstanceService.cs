namespace DesktopPortal.Services;

public readonly record struct SingleInstanceNames(string MutexName, string ActivationEventName);

public sealed class SingleInstanceService : IDisposable
{
    private const string DefaultApplicationId = "DesktopPortal";

    private readonly SingleInstanceNames _names;
    private Mutex? _mutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _registeredWaitHandle;
    private bool _ownsMutex;

    public SingleInstanceService()
        : this(DefaultApplicationId)
    {
    }

    public SingleInstanceService(string applicationId)
    {
        _names = CreateNames(applicationId);
    }

    public event EventHandler? ActivationRequested;

    public static SingleInstanceNames CreateNames(string applicationId)
    {
        var safeId = string.Join(
            ".",
            applicationId
                .Split(['\\', '/', ':', '*', '?', '"', '<', '>', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        if (string.IsNullOrWhiteSpace(safeId))
        {
            safeId = DefaultApplicationId;
        }

        return new SingleInstanceNames(
            $@"Local\{safeId}.Mutex",
            $@"Local\{safeId}.Activate");
    }

    public bool TryAcquireOrSignalExisting()
    {
        _mutex = new Mutex(initiallyOwned: true, _names.MutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            SignalExistingInstance();
            return false;
        }

        _ownsMutex = true;
        _activationEvent = new EventWaitHandle(
            initialState: false,
            mode: EventResetMode.AutoReset,
            name: _names.ActivationEventName);
        _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, timedOut) =>
            {
                if (!timedOut)
                {
                    ActivationRequested?.Invoke(this, EventArgs.Empty);
                }
            },
            state: null,
            timeout: Timeout.InfiniteTimeSpan,
            executeOnlyOnce: false);
        return true;
    }

    public void SignalExistingInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(_names.ActivationEventName);
            activationEvent.Set();
        }
        catch
        {
            // If the first instance is still starting or is elevated in another session,
            // the mutex still prevents this instance from continuing.
        }
    }

    public void Dispose()
    {
        _registeredWaitHandle?.Unregister(null);
        _registeredWaitHandle = null;
        _activationEvent?.Dispose();
        _activationEvent = null;

        if (_ownsMutex && _mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // Process exit should not fail because a mutex was already released.
            }
        }

        _mutex?.Dispose();
        _mutex = null;
        _ownsMutex = false;
    }
}
