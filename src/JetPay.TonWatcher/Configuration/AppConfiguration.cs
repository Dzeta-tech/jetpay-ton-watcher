using System.ComponentModel.DataAnnotations;
using Dzeta.Configuration;

namespace JetPay.TonWatcher.Configuration;

public class AppConfiguration : BaseConfiguration
{
    [Required] public DatabaseConnectionConfiguration Database { get; set; } = null!;
    [Required] public string BotToken { get; set; } = null!;
    [Required] public LiteClientConfiguration LiteClient { get; set; } = null!;
}

public class LiteClientConfiguration : BaseConfiguration
{
    [Required] public string Host { get; set; } = null!;
    [Required] public int Port { get; set; }
    [Required] public string PublicKey { get; set; } = null!;
    public int Ratelimit { get; set; } = 10;
}