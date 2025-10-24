using System.ComponentModel.DataAnnotations;
using Dzeta.Configuration;

namespace JetPay.TonWatcher.Configuration;

public class AppConfiguration : BaseConfiguration
{
    [Required] public DatabaseConnectionConfiguration Database { get; set; } = null!;
    [Required] public LiteClientConfiguration LiteClient { get; set; } = null!;
    [Required] public RedisConfiguration Redis { get; set; } = null!;
    public TelegramConfiguration Telegram { get; set; } = new();
}

public class LiteClientConfiguration : BaseConfiguration
{
    [Required] public string Host { get; set; } = null!;
    [Required] public int Port { get; set; }
    [Required] public string PublicKey { get; set; } = null!;
    public int Ratelimit { get; set; } = 10;
}

public class RedisConfiguration : BaseConfiguration
{
    [Required] public string Host { get; set; } = string.Empty;
    [Required] public string Port { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? Password { get; set; }
}

public class TelegramConfiguration : BaseConfiguration
{
    public bool Enabled { get; set; } = false;
    public string BotToken { get; set; } = null!;
    public long ChatId { get; set; }
}