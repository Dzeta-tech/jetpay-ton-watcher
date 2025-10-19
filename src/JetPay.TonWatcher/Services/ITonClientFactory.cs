using TonSdk.Client;

namespace JetPay.TonWatcher.Services;

public interface ITonClientFactory
{
    public Task Initialize();
    public TonClient GetClient();
}