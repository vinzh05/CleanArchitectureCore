# Clean Architecture với RabbitMQ Event-Driven Architecture

## 📋 Tổng Quan

Project này sử dụng **Clean Architecture** kết hợp với **Event-Driven Architecture** thông qua **RabbitMQ** và **MassTransit** để xử lý các sự kiện trong hệ thống một cách bất đồng bộ, có khả năng mở rộng cao.

## 🏗️ Kiến Trúc Tổng Thể

```
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Domain Events (MediatR INotification)                │  │
│  │ • ProductCreatedDomainEvent                          │  │
│  │ • OrderCreatedDomainEvent                            │  │
│  │ • AccessControlDomainEvent (Hikvision)               │  │
│  │ • DeviceStatusChangedDomainEvent (Hikvision)         │  │
│  │ • AlarmTriggeredDomainEvent (Hikvision)              │  │
│  │ • PersonSyncedDomainEvent (Hikvision)                │  │
│  │ • DoorOpenedDomainEvent (Hikvision)                  │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                Application Layer                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Domain Event Handlers (MediatR)                      │  │
│  │ • Map Domain Events → Integration Events            │  │
│  │ • Add Integration Events to Outbox (DB Transaction) │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Infrastructure Layer (Outbox Pattern)          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ OutboxPublisherService (Background Service)          │  │
│  │ • Poll OutboxMessages table (every 5s)               │  │
│  │ • Batch read (50 messages)                           │  │
│  │ • Parallel publish (4 threads)                       │  │
│  │ • Publish to RabbitMQ via MassTransit                │  │
│  │ • Mark as Processed on success                       │  │
│  │ • Retry on failure (increment RetryCount)            │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    RabbitMQ Broker                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Exchanges & Queues:                                  │  │
│  │ • product.created.exchange → products.queue          │  │
│  │ • hikvision.access.control.exchange → queue          │  │
│  │ • hikvision.device.status.exchange → queue           │  │
│  │ • hikvision.alarm.exchange → queue                   │  │
│  │ • hikvision.person.sync.exchange → queue             │  │
│  │ • hikvision.door.opened.exchange → queue             │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Worker Service (Consumers)                     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ MassTransit Consumers (kế thừa TConsumer)            │  │
│  │ • ProductCreatedConsumer                             │  │
│  │ • OrderCreatedConsumer                               │  │
│  │ • AccessControlConsumer                              │  │
│  │ • DeviceStatusChangedConsumer                        │  │
│  │ • AlarmTriggeredConsumer                             │  │
│  │ • PersonSyncedConsumer                               │  │
│  │ • DoorOpenedConsumer                                 │  │
│  │                                                       │  │
│  │ Features:                                             │  │
│  │ • Auto logging (start/end, duration, errors)         │  │
│  │ • Auto metrics tracking                              │  │
│  │ • Error handling & retry (MassTransit)               │  │
│  │ • Prefetch control (16 messages)                     │  │
│  │ • Retry policy (5 times, 5s interval)                │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Luồng Xử Lý Chi Tiết

### 1️⃣ **Publisher Flow (WebAPI)**

```
WebAPI Request
    │
    ▼
Service Layer
    │
    ▼
Raise Domain Event (e.g., ProductCreatedDomainEvent)
    │
    ▼
Domain Event Handler (MediatR)
    │
    ├─► Map to Integration Event
    │
    └─► UnitOfWork.AddIntegrationEventToOutboxAsync()
         │
         └─► Insert to OutboxMessages table (same transaction)
              │
              └─► SaveChanges & Commit Transaction
```

### 2️⃣ **Outbox Publisher Flow (Background Service)**

```
OutboxPublisherService.ExecuteAsync() [Every 5s]
    │
    ├─► Query OutboxMessages WHERE Processed = false (LIMIT 50)
    │
    ├─► Parallel Processing (Max 4 threads)
    │    │
    │    ├─► Deserialize JSON to typed event
    │    │
    │    ├─► Publish to RabbitMQ (IPublishEndpoint)
    │    │
    │    ├─► On Success:
    │    │    └─► Update Processed = true, ProcessedOn = UtcNow
    │    │
    │    └─► On Failure:
    │         └─► Increment RetryCount, Log error
    │
    └─► Repeat after 5 seconds
```

### 3️⃣ **Consumer Flow (Worker Service)**

```
RabbitMQ Queue
    │
    ▼
MassTransit Consumer (kế thừa TConsumer<T>)
    │
    ├─► TConsumer.Consume()
    │    │
    │    ├─► Start Stopwatch
    │    │
    │    ├─► Log: "[EventType] Processing started"
    │    │
    │    ├─► Call ProcessMessageAsync() [Abstract method]
    │    │    │
    │    │    └─► Derived class business logic
    │    │
    │    ├─► On Success:
    │    │    └─► Log: "Processing completed | Duration: Xms"
    │    │
    │    └─► On Failure:
    │         ├─► Log error with full details
    │         └─► Re-throw (MassTransit auto retry 5 times)
    │
    └─► ACK/NACK to RabbitMQ
```

## 🚀 Tối Ưu Hiệu Năng

### ✅ **Outbox Pattern**
- **Transactional Outbox**: Đảm bảo consistency giữa business logic và event publishing
- **Batch Processing**: Xử lý 50 messages mỗi lần poll
- **Parallel Execution**: 4 threads xử lý đồng thời
- **Optimistic Locking**: Tránh race condition

### ✅ **RabbitMQ Configuration**
- **Prefetch Count**: 16 messages (balance throughput & memory)
- **Exchange Type**: Topic (flexible routing)
- **Durable Queues**: Persist messages to disk
- **Auto Retry**: 5 lần, interval 5 giây

### ✅ **Consumer Pattern**
- **Base Class Template**: TConsumer<T> giảm code duplication
- **Auto Logging**: Không cần log thủ công trong mỗi consumer
- **Performance Metrics**: Tự động track thời gian xử lý
- **Error Handling**: Centralized, consistent

### ✅ **Database**
- **Connection Pooling**: Tái sử dụng connections
- **Indexed Queries**: Index trên `Processed`, `OccurredOn`
- **Batch Updates**: Update nhiều records cùng lúc

## 📊 Hikvision Events

### **1. AccessControlIntegrationEvent**
```csharp
// Khi có người vào/ra (card swipe, fingerprint, face recognition)
- DeviceId: ID thiết bị kiểm soát
- PersonId: ID người dùng
- DoorId: ID cửa
- AccessGranted: true/false
- AccessType: Card, Fingerprint, Face, etc.
```

**Business Logic trong Consumer:**
- Log access event to database
- Check access permissions
- Trigger access rules (alert if denied)
- Send real-time notifications
- Update dashboard

### **2. DeviceStatusChangedIntegrationEvent**
```csharp
// Khi thiết bị online/offline hoặc thay đổi trạng thái
- DeviceId, DeviceName
- PreviousStatus → CurrentStatus
- Reason (network error, power loss, etc.)
```

**Business Logic:**
- Update device health status
- Trigger alerts if offline
- Send maintenance notifications
- Update monitoring dashboard

### **3. AlarmTriggeredIntegrationEvent**
```csharp
// Khi có cảnh báo (door forced open, tamper, fire, etc.)
- AlarmId, DeviceId
- AlarmType, AlarmLevel (Critical, Warning, Info)
- Location, MetaData
```

**Business Logic:**
- Route by alarm level (critical → SMS, email, push)
- Trigger automated response (lock doors, call security)
- Log to incident management system

### **4. PersonSyncedIntegrationEvent**
```csharp
// Khi sync dữ liệu người dùng (Created, Updated, Deleted)
- PersonId, PersonName, CardNo
- SyncAction (Created/Updated/Deleted)
- AccessDoorIds (danh sách cửa được phép)
```

**Business Logic:**
- Update access rights in system
- Sync với HR system
- Update dashboards & reports

### **5. DoorOpenedIntegrationEvent**
```csharp
// Khi cửa được mở
- DoorId, DoorName, DeviceId
- OpenMethod (Card, Remote, Button, etc.)
- DurationSeconds (thời gian mở)
```

**Business Logic:**
- Track door usage statistics
- Detect anomalies (door open too long)
- Update real-time dashboards

## 🔧 Configuration

### **appsettings.Shared.json**
```json
{
  "RabbitMq": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest",
    "PrefetchCount": 16,
    "Retry": {
      "RetryCount": 5,
      "IntervalSeconds": 5
    },
    "Exchanges": {
      "HikvisionAccessControl": {
        "Name": "hikvision.access.control.exchange",
        "Type": "topic",
        "RoutingKey": "hikvision.access.#",
        "Queue": "hikvision.access.control.queue"
      }
      // ... other exchanges
    }
  },
  "Outbox": {
    "PollIntervalSeconds": 5,
    "BatchSize": 50,
    "MaxDegreeOfParallelism": 4
  }
}
```

## 📝 Cách Sử Dụng

### **Tạo Domain Event Mới**
1. Tạo file trong `Domain/Events/Hikvision/YourDomainEvent.cs`
2. Kế thừa `BaseEvent` và implement `INotification`

### **Tạo Integration Event Mới**
1. Tạo file trong `Shared/Contracts/IntegrationEvents/Hikvision/YourIntegrationEvent.cs`
2. Kế thừa `IntegrationEvent`

### **Tạo Domain Event Handler**
1. Tạo file trong `Application/EventHandlers/Hikvision/YourDomainEventHandler.cs`
2. Implement `INotificationHandler<YourDomainEvent>`
3. Map domain event → integration event
4. Call `_uow.AddIntegrationEventToOutboxAsync(integrationEvent)`

### **Tạo Consumer**
1. Tạo file trong `Worker/Consumers/Hikvision/YourConsumer.cs`
2. Kế thừa `TConsumer<YourIntegrationEvent>`
3. Override `ProcessMessageAsync()`
4. Implement business logic

### **Đăng Ký Consumer**
1. Thêm consumer vào `Worker/Program.cs`:
   ```csharp
   x.AddConsumer<YourConsumer>();
   ```
2. Map consumer trong `MassTransitConfig.AddMassTransitConsumers()`:
   ```csharp
   case "YourExchangeKey":
       endpoint.ConfigureConsumer<YourConsumer>(context);
       break;
   ```

### **Thêm Exchange & Queue**
1. Thêm vào `Shared/appsettings.Shared.json`:
   ```json
   "YourExchangeKey": {
     "Name": "your.exchange.name",
     "Type": "topic",
     "RoutingKey": "your.routing.#",
     "Queue": "your.queue.name"
   }
   ```

## 🎯 Best Practices

### ✅ **Do's**
- Luôn sử dụng Outbox Pattern cho transactional consistency
- Kế thừa `TConsumer<T>` để có logging & metrics tự động
- Sử dụng Topic exchange cho flexible routing
- Config prefetch count dựa trên processing time
- Monitor OutboxMessages table (cleanup old messages)
- Log structured với semantic logging

### ❌ **Don'ts**
- Không publish trực tiếp từ Domain Event Handler (dùng Outbox)
- Không xử lý business logic trong Domain Event Handler
- Không bắt exception mà không re-throw (sẽ mất retry)
- Không tạo nhiều exchange/queue không cần thiết
- Không hardcode connection strings

## 🔍 Monitoring & Debugging

### **OutboxMessages Table**
```sql
-- Check pending messages
SELECT COUNT(*) FROM OutboxMessages WHERE Processed = false;

-- Check retry failed messages
SELECT * FROM OutboxMessages WHERE RetryCount > 5;

-- Cleanup old processed messages (retention policy)
DELETE FROM OutboxMessages WHERE Processed = true AND ProcessedOn < NOW() - INTERVAL '7 days';
```

### **RabbitMQ Management UI**
- URL: http://localhost:15672
- Monitor queue depth, consumer count, message rates
- Check dead letter queues

### **Logs**
```
[AccessControlIntegrationEvent] Processing started | MessageId: xxx
Access Control | Device: D001, Person: P123, Granted: true
[AccessControlIntegrationEvent] Processing completed | Duration: 45ms
```

## 📦 Dependencies

- **.NET 9.0**
- **MassTransit** (RabbitMQ Transport)
- **MediatR** (Domain Events)
- **Entity Framework Core** (Outbox Pattern)
- **RabbitMQ** (Message Broker)
- **Serilog** (Structured Logging)

## 🏁 Kết Luận

Kiến trúc này cung cấp:
- ✅ **High Performance**: Batch processing, parallel execution, prefetch control
- ✅ **Reliability**: Transactional Outbox, auto retry, error handling
- ✅ **Scalability**: Multiple workers, horizontal scaling
- ✅ **Maintainability**: Clean Architecture, DRY principles, centralized logging
- ✅ **Observability**: Structured logging, metrics, monitoring
