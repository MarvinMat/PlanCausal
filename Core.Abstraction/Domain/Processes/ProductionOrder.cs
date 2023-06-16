using Core.Abstraction.Domain.Enums;

namespace Core.Abstraction.Domain.Processes
{
    public class ProductionOrder
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime CompletedDate { get; set; }
        public OrderState State { get; set; }

        public WorkPlan WorkPlan { get; init; }
        public List<WorkOrder> WorkOrders { get; set; }

        public ProductionOrder()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Product";
            Quantity = 0;
            State = OrderState.Created;
            WorkPlan = new WorkPlan();
            WorkOrders = new List<WorkOrder>();
        }
    }
}
