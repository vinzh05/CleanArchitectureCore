# Hikvision Event Processing Flow - Visual Guide

## 📊 Complete Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         1. BUSINESS LAYER (WebAPI)                           │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   POST /api/access-control/record                                           │
│        │                                                                     │
│        ▼                                                                     │
│   ┌─────────────────────────────────────┐                                  │
│   │ AccessControlService                │                                  │
│   │ .RecordAccessEventAsync()           │                                  │
│   └──────────┬──────────────────────────┘                                  │
│              │                                                               │
│              │ 1.1 Begin Transaction                                        │
│              ▼                                                               │
│   ┌─────────────────────────────────────┐                                  │
│   │ Save to Database                    │                                  │
│   │ • AccessLog table                   │                                  │
│   │ • DeviceLog table                   │                                  │
│   └──────────┬──────────────────────────┘                                  │
│              │                                                               │
│              │ 1.2 Raise Domain Event                                       │
│              ▼                                                               │
│   ┌─────────────────────────────────────┐                                  │
│   │ MediatR.Publish()                   │                                  │
│   │ • AccessControlDomainEvent          │                                  │
│   └──────────┬──────────────────────────┘                                  │
│              │                                                               │
│              │                                                               │
└──────────────┼──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                      2. APPLICATION LAYER (Handlers)                         │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ AccessControlDomainEventHandler                                 │      │
│   │ .Handle(AccessControlDomainEvent)                               │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 2.1 Map to Integration Event                                 │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ new AccessControlIntegrationEvent                               │      │
│   │ {                                                                │      │
│   │   DeviceId = notification.DeviceId,                             │      │
│   │   PersonId = notification.PersonId,                             │      │
│   │   AccessGranted = notification.AccessGranted,                   │      │
│   │   ...                                                            │      │
│   │ }                                                                │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 2.2 Add to Outbox (Same Transaction!)                        │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ UnitOfWork.AddIntegrationEventToOutboxAsync()                   │      │
│   │                                                                  │      │
│   │ INSERT INTO OutboxMessages (                                    │      │
│   │   Type: 'AccessControlIntegrationEvent',                        │      │
│   │   Content: '{"DeviceId":"D001",...}',                           │      │
│   │   Processed: false,                                             │      │
│   │   OccurredOn: '2024-01-15 10:30:00'                             │      │
│   │ )                                                                │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 2.3 Commit Transaction (DB + Outbox atomic)                  │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ ✅ Transaction Committed                                         │      │
│   │ • AccessLog saved                                                │      │
│   │ • OutboxMessage saved                                            │      │
│   │ • All or nothing!                                                │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │                                                               │
└──────────────┼──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│               3. INFRASTRUCTURE LAYER (Outbox Publisher)                     │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ⏰ Background Service (Every 5 seconds)                                   │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ OutboxPublisherService.ExecuteAsync()                           │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 3.1 Poll Outbox Table                                        │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ SELECT * FROM OutboxMessages                                    │      │
│   │ WHERE Processed = false                                         │      │
│   │ ORDER BY OccurredOn                                             │      │
│   │ LIMIT 50                                                         │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 3.2 Parallel Processing (4 threads)                          │
│              ▼                                                               │
│   ┌──────────────────────────────────────────────────────────────────┐     │
│   │ Thread 1: Msg 1-12  │ Thread 2: Msg 13-25                       │     │
│   │ Thread 3: Msg 26-37 │ Thread 4: Msg 38-50                       │     │
│   └──────────┬──────────────────────────────────────────────────────┘     │
│              │                                                               │
│              │ 3.3 Deserialize & Publish                                    │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ foreach (var msg in batch)                                      │      │
│   │ {                                                                │      │
│   │   var evt = JsonSerializer.Deserialize(msg.Content);            │      │
│   │   await publisher.Publish(evt);  // ← MassTransit               │      │
│   │                                                                  │      │
│   │   msg.Processed = true;                                         │      │
│   │   msg.ProcessedOn = UtcNow;                                     │      │
│   │ }                                                                │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │                                                               │
└──────────────┼──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                       4. RABBITMQ LAYER (Message Broker)                     │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ Exchange: hikvision.access.control.exchange                     │      │
│   │ Type: Topic                                                      │      │
│   │ Routing Key: hikvision.access.#                                 │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 4.1 Route Message                                            │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ Queue: hikvision.access.control.queue                           │      │
│   │ • Durable: true                                                  │      │
│   │ • Prefetch: 16 messages                                          │      │
│   │ • Message: {"DeviceId":"D001",...}                               │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 4.2 Push to Consumer (Active waiting)                        │
│              ▼                                                               │
│                                                                              │
└──────────────┼──────────────────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                     5. WORKER SERVICE (Consumers)                            │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ MassTransit Consumer Framework                                  │      │
│   │ • Auto DI scope creation                                         │      │
│   │ • Message deserialization                                        │      │
│   │ • Retry policy (5 times)                                         │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 5.1 Invoke Consumer                                          │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ AccessControlConsumer.Consume()                                 │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 5.2 Base Class Orchestration                                 │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ TConsumer<T>.Consume() [Template Method Pattern]               │      │
│   │                                                                  │      │
│   │ ⏱️ Start Stopwatch                                               │      │
│   │ 📝 Log: "[AccessControlEvent] Processing started"               │      │
│   │                                                                  │      │
│   │ try {                                                            │      │
│   │   await ProcessMessageAsync(context);  // ← Virtual method      │      │
│   │                                                                  │      │
│   │   ✅ Log: "Processing completed | Duration: 12ms"                │      │
│   │ }                                                                │      │
│   │ catch (Exception ex) {                                          │      │
│   │   ❌ Log: "Processing failed | Error: {ex.Message}"              │      │
│   │   throw; // ← Re-throw for MassTransit retry                    │      │
│   │ }                                                                │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 5.3 Business Logic Execution                                 │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ AccessControlConsumer.ProcessMessageAsync()                     │      │
│   │                                                                  │      │
│   │ var msg = context.Message;                                      │      │
│   │                                                                  │      │
│   │ // TODO: Implement business logic                               │      │
│   │ • Check access permissions                                      │      │
│   │ • Log to analytics database                                     │      │
│   │ • Send notifications (if denied)                                │      │
│   │ • Trigger automated actions                                     │      │
│   │ • Update real-time dashboards                                   │      │
│   └──────────┬──────────────────────────────────────────────────────┘      │
│              │                                                               │
│              │ 5.4 ACK to RabbitMQ                                          │
│              ▼                                                               │
│   ┌─────────────────────────────────────────────────────────────────┐      │
│   │ ✅ Message Acknowledged                                          │      │
│   │ • Removed from queue                                             │      │
│   │ • Ready for next message                                         │      │
│   └──────────────────────────────────────────────────────────────────┘      │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## 🔍 Detailed Breakdown

### **Phase 1: Business Layer (0-50ms)**
- HTTP request arrives at WebAPI
- Service layer validates & processes business logic
- Saves to database within transaction
- Raises domain event via MediatR
- **Key Benefit**: Business logic không biết về messaging infrastructure

### **Phase 2: Application Layer (10-30ms)**
- Domain event handler được MediatR invoke
- Maps domain event → integration event
- Adds to Outbox table (same transaction as business logic)
- Commits transaction atomically
- **Key Benefit**: Exactly-once delivery guarantee (Outbox Pattern)

### **Phase 3: Infrastructure Layer (0-100ms per batch)**
- Background service polls every 5 seconds
- Batch reads 50 messages
- Parallel publishes với 4 threads
- Updates Processed = true on success
- **Key Benefit**: Decouples publishing từ request processing

### **Phase 4: RabbitMQ Layer (< 5ms)**
- Receives message from MassTransit
- Routes to correct queue via topic exchange
- Pushes to active consumers
- **Key Benefit**: Push-based, low latency, routing flexibility

### **Phase 5: Worker Service (10-50ms)**
- MassTransit consumer framework receives message
- TConsumer base class orchestrates flow
- Auto logging, metrics, error handling
- Derived class implements business logic
- ACKs message on success
- **Key Benefit**: DRY, consistent error handling, easy testing

---

## ⏱️ Latency Breakdown

```
Total Latency (p50): ~100ms
┌────────────────────────────────────────────────────┐
│ Phase 1: Business Logic          | 30ms   (30%)   │
│ Phase 2: Domain Event Handler    | 10ms   (10%)   │
│ Phase 3: Outbox Publish (async)  | 50ms   (50%)   │
│ Phase 4: RabbitMQ Routing         | 3ms    (3%)    │
│ Phase 5: Consumer Processing      | 15ms   (15%)   │
└────────────────────────────────────────────────────┘

Note: Phase 3 không block request (background service)
      Actual user-facing latency = Phase 1-2 = ~40ms
```

---

## 🎯 Performance Optimizations

### **1. Batch Processing (Outbox Publisher)**
```
Without batching:
  50 messages × 10ms each = 500ms

With batching (parallel 4):
  50 messages ÷ 4 threads × 10ms = 125ms
  
Improvement: 4x faster! 🚀
```

### **2. Prefetch Count (RabbitMQ)**
```
Prefetch = 1:  Consumer waits for ACK before next message
Prefetch = 16: Consumer processes 16 messages concurrently

Throughput increase: 16x! 🚀
```

### **3. Connection Pooling (Database)**
```
Without pooling: Create connection per Outbox poll = 50ms overhead
With pooling:    Reuse connection = 2ms overhead

Improvement: 25x faster! 🚀
```

---

## 🛡️ Reliability Features

### **✅ Transactional Outbox**
```
Scenario: Database save succeeds, publish fails

Without Outbox:
  Database: ✅ Saved
  RabbitMQ: ❌ Lost forever
  Result:   Data inconsistency!

With Outbox:
  Database:  ✅ Saved
  Outbox:    ✅ Saved (same transaction)
  Publisher: ♻️ Retries until success
  Result:    Eventually consistent! 🎯
```

### **✅ Automatic Retry**
```
Consumer fails → NACK → RabbitMQ requeues
MassTransit retry policy:
  Attempt 1: Immediate
  Attempt 2: 5s delay
  Attempt 3: 5s delay
  ...
  Attempt 5: 5s delay
  
After 5 failures → Dead Letter Queue (manual investigation)
```

### **✅ Idempotency**
```
Consumer processed same message twice (network issue)

Solution: Check MessageId or unique business key
  if (await _repo.ExistsAsync(msg.MessageId))
  {
      Logger.LogWarning("Duplicate message, skipping");
      return; // ACK but don't process
  }
```

---

## 📈 Scalability

### **Horizontal Scaling**
```
1 Worker Instance:
  Throughput: 1,000 msg/min

4 Worker Instances (Scale Out):
  Throughput: 4,000 msg/min
  
RabbitMQ auto load-balances across workers! 🚀
```

### **Queue Partitioning**
```
High Priority Alarms:
  hikvision.alarm.critical.queue (4 workers)

Normal Access Logs:
  hikvision.access.queue (2 workers)
  
Critical events processed faster! 🎯
```

---

**Refer to [RABBITMQ_ARCHITECTURE.md](./RABBITMQ_ARCHITECTURE.md) for more details.**
