# JetPay TON Watcher

A blockchain transaction monitoring service for the TON network. Watches tracked addresses and publishes transaction events to RabbitMQ.

## How It Works

The service continuously monitors the TON blockchain for transactions involving tracked addresses:

1. Syncs new shard blocks from the TON masterchain
2. Processes blocks to find transactions for tracked addresses
3. Uses Bloom filter for efficient address matching
4. Publishes transaction events to RabbitMQ when matches are found
5. Optionally sends Telegram notifications

## API Endpoints

### Health Check
```
GET /api/v1/tracked-addresses/status
```

**Response:**
```json
{
  "success": true
}
```

### Add Tracked Address
```
POST /api/v1/tracked-addresses/add/{address}
```

**Parameters:**
- `address` (path): TON address to track (e.g., `EQD...`)

**Success Response (200):**
```json
{
  "success": true,
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Response (400):**
```json
{
  "success": false,
  "errorMessage": "Invalid address format: ..."
}
```

## RabbitMQ Integration

### Exchange Configuration
- **Type**: Fanout
- **Name**: Configurable via `RABBIT_MQ_EXCHANGE_NAME` (default: `ton-transactions`)
- **Durable**: Yes

### Message Format

When a transaction is detected for a tracked address, the following message is published:

```json
{
  "address": "EQD...",
  "txHash": "base64-encoded-hash",
  "lt": 12345678901234567,
  "detectedAt": "2025-10-22T10:30:00.000Z"
}
```

**Fields:**
- `address` (string): The TON address that had a transaction
- `txHash` (string): Base64-encoded transaction hash
- `lt` (number): Logical time of the transaction
- `detectedAt` (string): ISO 8601 timestamp when the transaction was detected

### Consumer Example

To consume these messages, create a queue and bind it to the exchange:

```csharp
// Example in C# with RabbitMQ.Client
var factory = new ConnectionFactory() { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync("my-queue", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("my-queue", "ton-transactions", "");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var transaction = JsonSerializer.Deserialize<TransactionEvent>(message);
    
    // Process transaction
    Console.WriteLine($"Transaction found: {transaction.Address}");
};

await channel.BasicConsumeAsync("my-queue", autoAck: true, consumer: consumer);
```

## Configuration

Environment variables:

```bash

# TON LiteClient (Required)
LITE_CLIENT_HOST=your-liteclient-host
LITE_CLIENT_PORT=your-liteclient-port
LITE_CLIENT_PUBLIC_KEY=your-public-key
LITE_CLIENT_RATELIMIT=10

# Redis (Required for streaming transactions)
REDIS__HOST=localhost
REDIS__PORT=6379
REDIS__USER=
REDIS__PASSWORD=

# Telegram (Optional - disable if not needed)
TELEGRAM_ENABLED=false
TELEGRAM_BOT_TOKEN=your-bot-token
TELEGRAM_CHAT_ID=your-chat-id
```

## Architecture

Built with:
- **CQRS Pattern**: Clean separation of commands and queries using MediatR
- **Domain Events**: Decoupled event handling for transaction notifications
- **Repository Pattern**: Clean data access abstractions
- **Background Services**: Continuous block syncing and processing

### Project Structure
- `Domain/`: Business entities, events, and value objects
- `Application/`: Commands, queries, and business logic handlers
- `Infrastructure/`: Database, LiteClient, Redis Streams, event handlers
- `Presentation/`: API controllers
