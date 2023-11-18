using Core.Abstraction.Domain.Enums;

namespace Core.Abstraction.Domain.Processes
{
    public class ProductionOrder 
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = "Unnamed Product";
        public int Quantity { get; init; } = 0;
        public DateTime DeliveryDate { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime CompletedDate { get; set; }
        public OrderState State { get; set; } = OrderState.Created;

        public WorkPlan WorkPlan { get; init; } = new();
        public List<WorkOrder> WorkOrders { get; set; } = new();
    }
}
