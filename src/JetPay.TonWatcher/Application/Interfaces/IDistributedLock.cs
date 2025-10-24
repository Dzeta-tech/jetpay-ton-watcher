namespace JetPay.TonWatcher.Application.Interfaces;

public interface IDistributedLock
{
    Task<IDisposable?> AcquireLockAsync(string resource, TimeSpan timeout, CancellationToken cancellationToken = default);
}

