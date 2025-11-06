using System.ComponentModel.DataAnnotations;
using Dzeta.Configuration;

namespace JetPay.TonWatcher.Configuration;

public class AppConfiguration : BaseConfiguration
{
    [Required] public DatabaseConnectionConfiguration Database { get; set; } = null!;
    [Required] public LiteClientConfiguration LiteClient { get; set; } = null!;
    [Required] public NatsConfiguration Nats { get; set; } = null!;
}

public class LiteClientConfiguration : BaseConfiguration
{
    [Required] public string Host { get; set; } = null!;
    [Required] public int Port { get; set; }
    [Required] public string PublicKey { get; set; } = null!;
    public int Ratelimit { get; set; } = 10;
}

public class NatsConfiguration : BaseConfiguration
{
    [Required] public string Url { get; set; } = "nats://localhost:4222";
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
}