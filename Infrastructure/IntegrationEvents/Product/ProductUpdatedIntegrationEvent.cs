using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IntegrationEvents.Product
{
    public record ProductUpdatedIntegrationEvent(Guid ProductId, string Name, decimal Price);
}
