
namespace Application.EventHandlers
{
    internal class ProductCreatedIntegrationEvent
    {
        private Guid productId;
        private string name;
        private decimal price;

        public ProductCreatedIntegrationEvent(Guid productId, string name, decimal price)
        {
            this.productId = productId;
            this.name = name;
            this.price = price;
        }
    }
}