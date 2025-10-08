# RabbitMQ Messaging Architecture - Refactored

## 📋 Overview

This document describes the refactored RabbitMQ messaging architecture using MassTransit, following Clean Architecture principles with improved scalability, maintainability, and testability.

## 🎯 Key Improvements

### 1. **Strongly-Typed Configuration**
- ✅ Replaced hardcoded configuration with typed `RabbitMqSettings` and `OutboxSettings`
- ✅ Added validation for all configuration properties
- ✅ Support for advanced features (SSL, heartbeat, connection pooling)

### 2. **Modular Architecture**
- ✅ Separated concerns into distinct components:
  - **Configuration**: Settings models with validation
  - **Setup**: Bus configurators for Publisher and Consumer
  - **Abstractions**: Interfaces and registries for extensibility
  - **Outbox**: Enhanced publisher with circuit breaker and idempotency

### 3. **Convention-Based Consumer Registration**
- ✅ Automatic consumer discovery from assemblies
- ✅ Eliminated manual switch-case mapping
- ✅ Support for explicit consumer-to-exchange mapping when needed

### 4. **Enhanced Error Handling**
- ✅ Circuit breaker pattern for outbox processing
- ✅ Exponential backoff retry strategy
- ✅ Dead letter queue configuration
- ✅ Structured logging with correlation IDs

### 5. **Better Testability**
- ✅ Dependency injection throughout
- ✅ Interfaces for all major components
- ✅ No static methods (eliminated `MassTransitConfig`)

## 🏗️ Architecture

```
Infrastructure/
├── Messaging/
│   ├── Configuration/
│   │   ├── RabbitMqSettings.cs         # Connection & exchange settings
│   │   └── OutboxSettings.cs           # Outbox processing settings
│   ├── Setup/
│   │   ├── PublisherBusConfigurator.cs # MassTransit setup for publisher
│   │   ├── ConsumerBusConfigurator.cs  # MassTransit setup for consumer
│   │   └── ConsumerRegistrar.cs        # Auto-discovery & registration
│   ├── Abstractions/
│   │   └── IMessageTypeRegistry.cs     # Type registry for deserialization
│   ├── Consumers/
│   │   └── BaseConsumer.cs             # Enhanced base consumer
│   ├── Outbox/
│   │   └── OutboxPublisherService.cs   # Enhanced outbox processor
│   └── Extensions/
│       └── MessagingServiceExtensions.cs # Clean registration APIs
```

## 🔧 Usage

### WebAPI (Publisher)

```csharp
// Program.cs
services.AddInfrastructure(config, addOutboxPublisher: true);
```

This automatically:
1. Registers RabbitMQ publisher
2. Configures MassTransit with settings from `appsettings.json`
3. Registers integration event types
4. Starts outbox background service

### Worker (Consumer)

```csharp
// Program.cs
services.AddInfrastructure(config, addOutboxPublisher: false);

services.AddRabbitMqConsumer(config, registrar =>
{
    // Option 1: Auto-discover consumers from assembly
    registrar.AddConsumersFromAssembly(typeof(ProductCreatedConsumer).Assembly);
    
    // Option 2: Explicit mapping (optional)
    registrar.MapConsumer<ProductCreatedConsumer>("ProductCreated");
    registrar.MapConsumer<OrderCreatedConsumer>("OrderCreated");
});
```

### Creating a Consumer

```csharp
public class ProductCreatedConsumer : BaseConsumer<ProductCreatedIntegrationEvent>
{
    private readonly ElasticService _elastic;

    public ProductCreatedConsumer(
        ILogger<ProductCreatedConsumer> logger,
        ElasticService elastic) 
        : base(logger)
    {
        _elastic = elastic;
    }

    protected override async Task ProcessMessageAsync(
        ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        
        // Your business logic here
        await _elastic.IndexAsync(new 
        { 
            id = message.ProductId, 
            name = message.Name, 
            price = message.Price
        });
    }

    // Optional: Add custom validation
    protected override async Task<ValidationResult> ValidateMessageAsync(
        ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        var errors = new List<string>();

        if (message.ProductId == Guid.Empty)
            errors.Add("ProductId is required");

        return errors.Any()
            ? ValidationResult.Invalid(errors.ToArray())
            : ValidationResult.Valid();
    }
}
```

## ⚙️ Configuration

### appsettings.json

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "PrefetchCount": 16,
    "UseSSL": false,
    "Retry": {
      "RetryCount": 5,
      "IntervalSeconds": 5,
      "UseExponentialBackoff": true,
      "MaxIntervalSeconds": 300
    },
    "Connection": {
      "RequestedHeartbeat": 60,
      "RequestedConnectionTimeout": 30,
      "MaxConnectionRetries": 3
    },
    "Exchanges": {
      "ProductCreated": {
        "Name": "product.created.exchange",
        "Type": "fanout",
        "Queue": "products.fanout.queue",
        "Durable": true,
        "QueueSettings": {
          "DeadLetter": {
            "ExchangeName": "product.dlx",
            "RoutingKey": "dead-letter"
          }
        }
      }
    }
  },
  "Outbox": {
    "PollIntervalSeconds": 5,
    "BatchSize": 50,
    "MaxDegreeOfParallelism": 4,
    "MaxRetryCount": 10,
    "EnableCircuitBreaker": true,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerResetTimeoutSeconds": 60
  }
}
```

## 🚀 Performance Features

### Circuit Breaker
- Automatically trips when failure threshold is reached
- Prevents cascading failures
- Auto-resets after timeout period

### Batch Processing
- Configurable batch size (default: 50 messages)
- Parallel execution (default: 4 threads)
- Efficient database queries

### Retry Strategy
- Exponential backoff support
- Configurable retry count and intervals
- Per-message retry tracking

### Connection Pooling
- Heartbeat monitoring
- Automatic reconnection
- Connection timeout configuration

## 📊 Monitoring

All consumers and the outbox publisher include structured logging:

```
[ProductCreatedConsumer] Processing message started | MessageId=xxx, MessageType=ProductCreatedIntegrationEvent
[ProductCreatedConsumer] Processing product | ProductId=xxx, Name=xxx, Price=xxx
[ProductCreatedConsumer] Product indexed successfully | ProductId=xxx
[ProductCreatedConsumer] Processing message completed | Duration=45ms
```

## 🔍 Troubleshooting

### Circuit Breaker Tripped
Check logs for:
```
Circuit breaker TRIPPED | FailureCount=5, ResetIn=60s
```
Solution: Investigate root cause (database, network, etc.) and wait for auto-reset.

### Type Resolution Failures
Check logs for:
```
Message type not found in registry | Type=xxx
```
Solution: Ensure integration events are registered in `RegisterIntegrationEvents()`.

### Consumer Not Receiving Messages
1. Verify exchange configuration matches between publisher and consumer
2. Check queue bindings in RabbitMQ Management UI
3. Verify consumer is registered in `Program.cs`

## 🎓 Best Practices

1. **Always extend `BaseConsumer<T>`** for consistent error handling and logging
2. **Use convention-based naming**: `{EventName}Consumer` (e.g., `ProductCreatedConsumer`)
3. **Implement validation** in `ValidateMessageAsync()` for data integrity
4. **Configure dead letter queues** for failed messages
5. **Monitor circuit breaker** status in production
6. **Use structured logging** with correlation IDs
7. **Keep consumers idempotent** - handle duplicate messages gracefully

## 📈 Scalability

The refactored architecture supports:

- ✅ **Horizontal scaling**: Multiple worker instances consume from same queue
- ✅ **Vertical scaling**: Adjust `MaxDegreeOfParallelism` for more threads
- ✅ **Queue partitioning**: Use routing keys for message distribution
- ✅ **Load balancing**: RabbitMQ automatically distributes messages
- ✅ **Backpressure handling**: Configurable prefetch count

## 🔒 Security

- ✅ SSL/TLS support for encrypted connections
- ✅ Credentials from configuration (use environment variables in production)
- ✅ Virtual host isolation
- ✅ Connection timeout and retry limits

## 🧪 Testing

The new architecture is highly testable:

```csharp
// Mock dependencies
var mockLogger = new Mock<ILogger<ProductCreatedConsumer>>();
var mockElastic = new Mock<ElasticService>();

var consumer = new ProductCreatedConsumer(mockLogger.Object, mockElastic.Object);

// Create test context
var context = Mock.Of<ConsumeContext<ProductCreatedIntegrationEvent>>(
    c => c.Message == new ProductCreatedIntegrationEvent(Guid.NewGuid(), "Test", 99.99m)
);

// Act
await consumer.Consume(context);

// Assert
mockElastic.Verify(e => e.IndexAsync(It.IsAny<object>()), Times.Once);
```

## 📝 Migration Guide

### From Old to New

**Old:**
```csharp
MassTransitConfig.AddMassTransitConsumers(services, config, (context, endpoint, key) =>
{
    switch (key)
    {
        case "ProductCreated":
            endpoint.ConfigureConsumer<ProductCreatedConsumer>(context);
            break;
    }
});
```

**New:**
```csharp
services.AddRabbitMqConsumer(config, registrar =>
{
    registrar.AddConsumersFromAssembly(typeof(ProductCreatedConsumer).Assembly);
});
```

## 🤝 Contributing

When adding new consumers:

1. Extend `BaseConsumer<TMessage>`
2. Follow naming convention: `{EventName}Consumer`
3. Implement `ProcessMessageAsync()`
4. Add validation in `ValidateMessageAsync()` if needed
5. Update exchange configuration in `appsettings.json`

---

**Last Updated:** 2025-01-08
**Version:** 2.0
