using TonSdk.Adnl.LiteClient;

namespace JetPay.TonWatcher.Application.Interfaces;

public interface ILiteClientService
{
    Task InitializeAsync();
    bool IsConnected();
    Task<MasterChainInfoExtended> GetMasterChainInfoAsync();
    Task<BlockIdExtended[]> GetShardsAsync(BlockIdExtended blockId);
    Task<BlockIdExtended?> LookupBlockAsync(int workchain, long shard, long seqno);
    Task<ListBlockTransactionsResult> GetBlockTransactionsAsync(BlockIdExtended blockId, uint count = 10000);
}