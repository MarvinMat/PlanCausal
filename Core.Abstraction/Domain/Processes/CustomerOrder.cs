using Core.Abstraction.Domain.Customers;
using Core.Abstraction.Domain.Enums;

namespace Core.Abstraction.Domain.Processes;

public class CustomerOrder
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Unnamed Order";
    public DateTime DeliveryDate => ProductionOrders.Max(order => order.DeliveryDate);
    public DateTime OrderReceivedDate { get; set; }
    public DateTime StartedDate => ProductionOrders.Min(order => order.StartedDate);
    public DateTime CompletedDate => ProductionOrders.Max(order => order.CompletedDate);
    public OrderState State { get; set; } = OrderState.Created;
    public Guid CustomerId { get; init; }
    public List<ProductionOrder> ProductionOrders { get; set; } = new();
}