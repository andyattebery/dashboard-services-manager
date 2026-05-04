namespace Dsm.Managers;

// Singleton lock that serializes the read-modify-write path on the dashboard YAML files.
// Without this, two concurrent POST /dashboard-services calls from different providers
// can both read the same on-disk state and clobber each other on write — the second writer
// drops the first writer's contribution. Self-heals on the next tick but produces a visible
// flicker on the dashboard.
public sealed class DashboardWriteCoordinator : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task WaitAsync(CancellationToken cancellationToken = default) =>
        _semaphore.WaitAsync(cancellationToken);

    public int Release() => _semaphore.Release();

    public void Dispose() => _semaphore.Dispose();
}
