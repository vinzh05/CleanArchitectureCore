using Application.Abstractions.Common;
using Domain.Events.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.EventHandlers
{
    public class ProductCreatedDomainEventHandler // implement an interface of your choosing
    {
        private readonly IUnitOfWork _uow;
        public ProductCreatedDomainEventHandler(IUnitOfWork uow) { _uow = uow; }

        public async Task Handle(ProductCreatedDomainEvent evt)
        {
            var integration = new ProductCreatedIntegrationEvent(evt.ProductId, evt.Name, evt.Price);
            await _uow.AddIntegrationEventToOutboxAsync(integration);
        }
    }
}
