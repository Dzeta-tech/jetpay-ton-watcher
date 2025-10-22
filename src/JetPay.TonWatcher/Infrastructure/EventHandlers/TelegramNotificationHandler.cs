using JetPay.TonWatcher.Configuration;
using JetPay.TonWatcher.Domain.Events;
using MediatR;
using Telegram.Bot;

namespace JetPay.TonWatcher.Infrastructure.EventHandlers;

public class TelegramNotificationHandler(
    TelegramBotClient botClient,
    AppConfiguration config,
    ILogger<TelegramNotificationHandler> logger)
    : INotificationHandler<TransactionFoundEvent>
{
    public async Task Handle(TransactionFoundEvent notification, CancellationToken cancellationToken)
    {
        if (!config.Telegram.Enabled)
            return;

        try
        {
            string message = $"Transaction found!\n\n" +
                           $"Address: {notification.Address}\n" +
                           $"TxHash: {notification.TxHash}\n" +
                           $"Lt: {notification.Lt}";

            await botClient.SendMessage(config.Telegram.ChatId, message, cancellationToken: cancellationToken);

            logger.LogInformation("Sent Telegram notification for transaction {TxHash}", notification.TxHash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending Telegram notification");
        }
    }
}

