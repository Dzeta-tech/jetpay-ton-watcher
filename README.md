# TON Watcher

A high-performance blockchain transaction monitoring service for the TON network. This service continuously watches tracked addresses and publishes transaction events via NATS JetStream.

> **Note:** This is an open-source component of [Jetpay](https://jetpay.dev/en) — a fast on-chain payment solution for Telegram & TON. While Jetpay itself is a closed-source commercial product, we're open-sourcing this core infrastructure component to benefit the TON ecosystem.

## About Jetpay

[Jetpay](https://jetpay.dev/en) is a reliable backend for accepting TON/USDT payments with automatic fund aggregation. It provides instant payment processing, gas-free transactions for users, and supports 1000+ tokens. This watcher service is a critical component that powers Jetpay's real-time transaction detection capabilities.

## What This Service Does

TON Watcher monitors the TON blockchain in real-time and detects transactions for any tracked addresses:

1. **Syncs Masterchain**: Continuously fetches new shard blocks from the TON masterchain
2. **Processes Blocks**: Analyzes blocks to find transactions involving tracked addresses
3. **Efficient Matching**: Uses Bloom filter for fast address lookups
4. **Event Publishing**: Publishes transaction events to NATS JetStream when matches are found
5. **gRPC API**: Provides a gRPC interface for managing tracked addresses

## Features

- ✅ Real-time transaction monitoring
- ✅ Efficient Bloom filter-based address matching
- ✅ NATS JetStream integration for reliable event streaming
- ✅ gRPC API for address management
- ✅ PostgreSQL persistence
- ✅ Health checks for monitoring
- ✅ Production-ready with proper error handling

## Architecture

The service is built with modern .NET patterns:

- **CQRS**: Clean separation using MediatR
- **Domain Events**: Decoupled event handling
- **Background Services**: Continuous block syncing and processing
- **gRPC**: Type-safe service interface
- **NATS JetStream**: Reliable message streaming

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL database
- NATS server with JetStream enabled
- TON LiteClient access

### Configuration

Set the following environment variables:

```bash
# Database
DATABASE_HOST=localhost
DATABASE_PORT=5432
DATABASE_DATABASE=tonwatcher
DATABASE_USERNAME=postgres
DATABASE_PASSWORD=password

# TON LiteClient
LITE_CLIENT_HOST=your-liteclient-host
LITE_CLIENT_PORT=your-liteclient-port
LITE_CLIENT_PUBLIC_KEY=your-public-key-hex
LITE_CLIENT_RATELIMIT=10

# NATS
NATS_URL=nats://localhost:4222
NATS_USER=optional-user
NATS_PASSWORD=optional-password
NATS_TOKEN=optional-token
```

### Running

```bash
dotnet build
dotnet run --project src/JetPay.TonWatcher/JetPay.TonWatcher.csproj
```

### Docker

```bash
docker build -t ton-watcher .
docker run -p 50051:50051 --env-file .env ton-watcher
```

## gRPC API

The service exposes a gRPC interface defined in `Protos/tonwatcher.proto`.

### Service Methods

#### AddTrackedAddress
Adds a new address to track. Accepts addresses in any format (raw 0:hex, base64, etc.).

```protobuf
rpc AddTrackedAddress (AddTrackedAddressRequest) returns (AddTrackedAddressResponse);
```

#### DisableTrackedAddress
Disables tracking for an address.

```protobuf
rpc DisableTrackedAddress (DisableTrackedAddressRequest) returns (DisableTrackedAddressResponse);
```

#### IsAddressTracked
Checks if an address is currently being tracked.

```protobuf
rpc IsAddressTracked (IsAddressTrackedRequest) returns (IsAddressTrackedResponse);
```

#### GetStatus
Returns service health status.

```protobuf
rpc GetStatus (StatusRequest) returns (StatusResponse);
```

### Generating Client Code

For client applications, generate gRPC client code from the proto file:

```bash
# C#
dotnet add package Grpc.Tools
# Then the proto will be compiled automatically

# Other languages
# Use the standard gRPC tools for your language
```

## NATS JetStream Integration

### Stream Configuration

The service automatically creates a JetStream stream named `TON_TRANSACTIONS` with:
- **Subjects**: `ton.transactions.*`
- **Storage**: File-based
- **Retention**: WorkQueue policy

### Message Format

When a transaction is detected, a message is published to `ton.transactions.transactionfoundevent`:

```json
{
  "address": "EQD...",
  "txHash": "base64-encoded-hash",
  "lt": 12345678901234567
}
```

**Fields:**
- `address` (string): The TON address that had a transaction
- `txHash` (string): Base64-encoded transaction hash
- `lt` (number): Logical time of the transaction

### Consuming Events

Example consumer in C#:

```csharp
using NATS.Client.Core;
using NATS.Net;

var nats = new NatsConnection(NatsOpts.Default with { Url = "nats://localhost:4222" });
var js = nats.CreateJetStreamContext();

await js.SubscribeAsync("ton.transactions.transactionfoundevent", async msg =>
{
    var json = System.Text.Encoding.UTF8.GetString(msg.Data.ToArray());
    var transaction = JsonSerializer.Deserialize<TransactionFoundEvent>(json);
    Console.WriteLine($"Transaction found: {transaction.Address}");
});
```

## Health Checks

The service exposes health check endpoints:

- `GET /health` - Overall health status
- `GET /health/ready` - Readiness check (includes database connectivity)

## Project Structure

```
src/JetPay.TonWatcher/
├── Application/          # Commands, queries, and business logic
├── Configuration/        # DI, app configuration
├── Domain/              # Entities, events, value objects
├── Infrastructure/      # Database, NATS, background services
├── Presentation/        # gRPC services
└── Protos/             # gRPC service definitions
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Database Migrations

Migrations are automatically applied on startup. To create a new migration:

```bash
dotnet ef migrations add MigrationName --project src/JetPay.TonWatcher
```

## License

This project is open source. Please check the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Related Projects

- [Jetpay](https://jetpay.dev/en) - The main commercial product that uses this service
- [Ton.NET](https://github.com/Dzeta-tech/Ton.Net) - Modern .NET SDK for TON blockchain
- [TON](https://ton.org/) - The Open Network blockchain

## Support

For issues related to this open-source component, please open an issue on GitHub.

For questions about Jetpay (the commercial product), visit [jetpay.dev](https://jetpay.dev/en).

---

Made with ❤️ for the TON community
