using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Product
{
    public record ProductCreatedIntegrationEvent(Guid ProductId, string Name, decimal Price);
}
