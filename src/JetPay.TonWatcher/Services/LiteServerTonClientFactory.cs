using TonSdk.Adnl.LiteClient;
using TonSdk.Client;

namespace JetPay.TonWatcher.Services;

public class LiteServerTonClientFactory : ITonClientFactory
{
    TonClient client;
    public async Task Initialize(){
        client = new TonClient(TonClientType.LITECLIENT, new LiteClientParameters("49.13.44.74", 30116, "LKO4eLtBqxWmaRwMJuPbfxB/6BlqP94gCTvMBh5oPYQ="));
    }
    public TonClient GetClient()
    {
        return client;
    }
}