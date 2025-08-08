using Domain.Entities.Identity;
using Domain.Events.Product;
using Infrastructure.Contracts.Product;
using Infrastructure.IntegrationEvents.Product;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public class OutboxMessagePublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OutboxMessagePublisher> _logger;

        // Mapping giữa domain event type và hàm tạo integration event
        private readonly Dictionary<Type, Func<object, object>> _eventMapping;

        public OutboxMessagePublisher(IPublishEndpoint publishEndpoint, ILogger<OutboxMessagePublisher> logger)
        {
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Khởi tạo mapping, dễ dàng mở rộng bằng cách thêm dòng mới
            _eventMapping = new Dictionary<Type, Func<object, object>>
            {
                { typeof(ProductCreatedDomainEvent), domainEvent =>
                    {
                        var evt = (ProductCreatedDomainEvent)domainEvent;
                        return new ProductCreatedIntegrationEvent(evt.ProductId, evt.Name, evt.Price);
                    }
                },
                { typeof(ProductUpdatedDomainEvent), domainEvent =>
                    {
                        var evt = (ProductUpdatedDomainEvent)domainEvent;
                        return new ProductUpdatedIntegrationEvent(evt.ProductId, evt.Name, evt.Price);
                    }
                },
                { typeof(ProductDeletedDomainEvent), domainEvent =>
                    {
                        var evt = (ProductDeletedDomainEvent)domainEvent;
                        return new ProductDeletedIntegrationEvent(evt.ProductId);
                    }
                }
                // Bạn có thể tiếp tục đăng ký các event khác tại đây
            };
        }

        public async Task PublishOutboxMessagesAsync(OutboxMessage message)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    _logger.LogWarning("Event type not found: {Type}", message.Type);
                    return;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                if (domainEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize event content: {Type}", message.Type);
                    return;
                }

                if (!_eventMapping.TryGetValue(eventType, out var integrationEventFactory))
                {
                    _logger.LogWarning("No integration event mapping found for event type: {Type}", message.Type);
                    return;
                }

                var integrationEvent = integrationEventFactory(domainEvent);
                await _publishEndpoint.Publish(integrationEvent);

                message.Processed = true;
                message.ProcessedOn = DateTimeOffset.UtcNow;
                _logger.LogInformation("Published outbox message: {Type}", message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message: {Type}", message.Type);
                message.RetryCount++;
            }
        }
    }
}
