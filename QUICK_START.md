# Quick Start Guide: Thêm Event Mới

## 🚀 5 Bước Thêm Event Mới (5 phút)

### Ví dụ: Thêm `FaceRecognitionEvent` cho Hikvision

---

### **Bước 1: Tạo Domain Event** (30 giây)

📁 `Domain/Events/Hikvision/FaceRecognitionDomainEvent.cs`

```csharp
using Domain.Common;
using MediatR;

namespace Domain.Events.Hikvision
{
    public class FaceRecognitionDomainEvent : BaseEvent, INotification
    {
        public string DeviceId { get; }
        public string PersonId { get; }
        public string FaceImageUrl { get; }
        public double Confidence { get; }
        public DateTime RecognizedAt { get; }

        public FaceRecognitionDomainEvent(string deviceId, string personId, 
            string faceImageUrl, double confidence, DateTime recognizedAt)
        {
            DeviceId = deviceId;
            PersonId = personId;
            FaceImageUrl = faceImageUrl;
            Confidence = confidence;
            RecognizedAt = recognizedAt;
        }
    }
}
```

---

### **Bước 2: Tạo Integration Event** (30 giây)

📁 `Shared/Contracts/IntegrationEvents/Hikvision/FaceRecognitionIntegrationEvent.cs`

```csharp
using Shared.Common;

namespace Shared.IntegrationEvents.Contracts.Hikvision
{
    public class FaceRecognitionIntegrationEvent : IntegrationEvent
    {
        public string DeviceId { get; set; } = string.Empty;
        public string PersonId { get; set; } = string.Empty;
        public string FaceImageUrl { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime RecognizedAt { get; set; }
    }
}
```

---

### **Bước 3: Tạo Domain Event Handler** (1 phút)

📁 `Application/EventHandlers/Hikvision/FaceRecognitionDomainEventHandler.cs`

```csharp
using Application.Abstractions.Common;
using Domain.Events.Hikvision;
using MediatR;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Application.EventHandlers.Hikvision
{
    public class FaceRecognitionDomainEventHandler 
        : INotificationHandler<FaceRecognitionDomainEvent>
    {
        private readonly IUnitOfWork _uow;

        public FaceRecognitionDomainEventHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(FaceRecognitionDomainEvent notification, 
            CancellationToken cancellationToken)
        {
            var integrationEvent = new FaceRecognitionIntegrationEvent
            {
                DeviceId = notification.DeviceId,
                PersonId = notification.PersonId,
                FaceImageUrl = notification.FaceImageUrl,
                Confidence = notification.Confidence,
                RecognizedAt = notification.RecognizedAt
            };

            await _uow.AddIntegrationEventToOutboxAsync(integrationEvent);
        }
    }
}
```

---

### **Bước 4: Tạo Consumer** (1 phút)

📁 `Worker/Consumers/Hikvision/FaceRecognitionConsumer.cs`

```csharp
using Infrastructure.Consumers.Common;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Infrastructure.Consumers.Hikvision
{
    public class FaceRecognitionConsumer : TConsumer<FaceRecognitionIntegrationEvent>
    {
        public FaceRecognitionConsumer(ILogger<FaceRecognitionConsumer> logger) 
            : base(logger) { }

        protected override async Task ProcessMessageAsync(
            ConsumeContext<FaceRecognitionIntegrationEvent> context)
        {
            var msg = context.Message;
            
            Logger.LogInformation(
                "Face Recognition | Device: {DeviceId}, Person: {PersonId}, Confidence: {Confidence}%",
                msg.DeviceId, msg.PersonId, msg.Confidence * 100);

            // TODO: Implement business logic
            // - Verify confidence threshold
            // - Update attendance system
            // - Send notification if VIP
            // - Store face image to blob storage
            
            await Task.CompletedTask;
        }
    }
}
```

---

### **Bước 5: Configuration** (2 phút)

#### 5.1. Thêm Exchange & Queue

📁 `Shared/appsettings.Shared.json`

```json
{
  "RabbitMq": {
    "Exchanges": {
      // ... existing exchanges ...
      "HikvisionFaceRecognition": {
        "Name": "hikvision.face.recognition.exchange",
        "Type": "topic",
        "RoutingKey": "hikvision.face.#",
        "Queue": "hikvision.face.recognition.queue"
      }
    }
  }
}
```

#### 5.2. Đăng ký Consumer

📁 `Worker/Program.cs`

```csharp
// Thêm vào services.AddMassTransit(x => { ... })
x.AddConsumer<FaceRecognitionConsumer>();

// Thêm vào switch case trong MassTransitConfig.AddMassTransitConsumers()
case "HikvisionFaceRecognition":
    endpoint.ConfigureConsumer<FaceRecognitionConsumer>(context);
    break;
```

---

## ✅ Done! Test Your Event

### **Raise Domain Event từ Business Logic**

```csharp
public class AccessControlService : IAccessControlService
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public async Task<Result> RecordFaceRecognitionAsync(FaceRecognitionRequest request)
    {
        await _uow.BeginTransactionAsync();
        
        // Business logic: Save to database
        // ...

        // Raise domain event
        var domainEvent = new FaceRecognitionDomainEvent(
            request.DeviceId,
            request.PersonId,
            request.FaceImageUrl,
            request.Confidence,
            DateTime.UtcNow
        );
        
        // MediatR sẽ trigger handler → Add to Outbox
        await _mediator.Publish(domainEvent);
        
        await _uow.CommitTransactionAsync();
        
        return Result.Success();
    }
}
```

### **Kiểm Tra Logs**

```
[FaceRecognitionIntegrationEvent] Processing started | MessageId: abc123
Face Recognition | Device: CAM-01, Person: EMP-456, Confidence: 98.5%
[FaceRecognitionIntegrationEvent] Processing completed | Duration: 12ms
```

---

## 🎯 Checklist

- [ ] Domain Event created (kế thừa `BaseEvent, INotification`)
- [ ] Integration Event created (kế thừa `IntegrationEvent`)
- [ ] Domain Event Handler implemented (`INotificationHandler<T>`)
- [ ] Consumer created (kế thừa `TConsumer<T>`)
- [ ] Exchange & Queue added to `appsettings.Shared.json`
- [ ] Consumer registered in `Worker/Program.cs`
- [ ] Business logic implemented in `ProcessMessageAsync()`
- [ ] Tested end-to-end

---

## 🔍 Debugging Tips

### **Check Outbox Messages**

```sql
SELECT * FROM OutboxMessages 
WHERE Type LIKE '%FaceRecognition%' 
ORDER BY OccurredOn DESC;
```

### **Check RabbitMQ Management UI**

```
URL: http://localhost:15672
Username: guest
Password: guest

Navigate to: Queues → hikvision.face.recognition.queue
Check: Message rates, Consumer count
```

### **Enable Verbose Logging**

📁 `appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Infrastructure.Consumers": "Debug",
      "MassTransit": "Debug"
    }
  }
}
```

---

## 💡 Pro Tips

### **Tip 1: Batch Processing**

Nếu cần xử lý nhiều events cùng lúc:

```csharp
public class FaceRecognitionConsumer : TConsumer<FaceRecognitionIntegrationEvent>
{
    private static readonly List<FaceRecognitionIntegrationEvent> _batch = new();
    private static readonly SemaphoreSlim _lock = new(1);

    protected override async Task ProcessMessageAsync(...)
    {
        await _lock.WaitAsync();
        try
        {
            _batch.Add(msg);
            
            if (_batch.Count >= 10) // Batch size
            {
                await ProcessBatchAsync(_batch);
                _batch.Clear();
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

### **Tip 2: Priority Queue**

Cho critical events (VIP face recognition):

```json
{
  "HikvisionFaceRecognitionVIP": {
    "Name": "hikvision.face.vip.exchange",
    "Type": "direct",
    "RoutingKey": "vip",
    "Queue": "hikvision.face.vip.queue",
    "Priority": 10
  }
}
```

### **Tip 3: Conditional Routing**

Trong Domain Event Handler:

```csharp
public async Task Handle(FaceRecognitionDomainEvent notification, ...)
{
    // Route to VIP queue if confidence > 95%
    if (notification.Confidence > 0.95)
    {
        var vipEvent = new FaceRecognitionVIPIntegrationEvent { ... };
        await _uow.AddIntegrationEventToOutboxAsync(vipEvent);
    }
    else
    {
        var normalEvent = new FaceRecognitionIntegrationEvent { ... };
        await _uow.AddIntegrationEventToOutboxAsync(normalEvent);
    }
}
```

---

## 📚 References

- [RABBITMQ_ARCHITECTURE.md](./RABBITMQ_ARCHITECTURE.md) - Full architecture guide
- [KAFKA_VS_RABBITMQ.md](./KAFKA_VS_RABBITMQ.md) - Why RabbitMQ?
- [MassTransit Docs](https://masstransit-project.com/)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)

---

**Happy Coding! 🚀**
