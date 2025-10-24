using System.ComponentModel.DataAnnotations;
using Dzeta.Configuration;

namespace JetPay.TonWatcher.Configuration;

public class AppConfiguration : BaseConfiguration
{
    [Required] public DatabaseConnectionConfiguration Database { get; set; } = null!;
    [Required] public LiteClientConfiguration LiteClient { get; set; } = null!;
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
    public TelegramConfiguration Telegram { get; set; } = new();
}

public class LiteClientConfiguration : BaseConfiguration
{
    [Required] public string Host { get; set; } = null!;
    [Required] public int Port { get; set; }
    [Required] public string PublicKey { get; set; } = null!;
    public int Ratelimit { get; set; } = 10;
}

public class RabbitMqConfiguration : BaseConfiguration
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "ton-transactions";
}

public class TelegramConfiguration : BaseConfiguration
{
    public bool Enabled { get; set; } = false;
    public string BotToken { get; set; } = null!;
    public long ChatId { get; set; }
}