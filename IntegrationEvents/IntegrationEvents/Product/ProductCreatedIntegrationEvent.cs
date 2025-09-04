using System;

namespace IntegrationEvents.Product
{
    public record ProductCreatedIntegrationEvent(Guid ProductId, string Name, decimal Price);
}
