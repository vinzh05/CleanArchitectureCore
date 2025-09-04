using System;

namespace Shared.Contracts.Product
{
    public record ProductCreatedIntegrationEvent(Guid ProductId, string Name, decimal Price);
}
