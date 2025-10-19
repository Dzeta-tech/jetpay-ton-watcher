using System.ComponentModel.DataAnnotations;
using Dzeta.Configuration;

namespace JetPay.TonWatcher.Configuration;

public class AppConfiguration : BaseConfiguration
{
    [Required] public DatabaseConnectionConfiguration Database { get; set; } = null!;
    [Required] public string BotToken { get; set; } = null!;
}