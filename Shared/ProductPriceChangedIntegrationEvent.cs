using Base.EventBus;

namespace Shared;

public class ProductPriceChangedIntegrationEvent : BaseIntegrationEvent
{
    public int ProductId { get; private set; }
    public decimal NewPrice { get; private set; }
    public decimal OldPrice { get; private set; }

    public ProductPriceChangedIntegrationEvent(int productId, decimal newPrice,
        decimal oldPrice) : base(Guid.NewGuid(), DateTime.UtcNow)
    {
        ProductId = productId;
        NewPrice = newPrice;
        OldPrice = oldPrice;
    }
}