using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain.Processes
{
    public class WorkOrder
    {
        public Guid Id { get; init; }
        public string Name => ProductionOrder.Name;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public OrderState State { get; set; }
        public List<WorkOperation> WorkOperations { get; set; }
        public readonly ProductionOrder ProductionOrder;
        public WorkOrder(ProductionOrder productionOrder)
        {
            Id = Guid.NewGuid();
            State = OrderState.Created;
            ProductionOrder = productionOrder;
            WorkOperations = new List<WorkOperation>();
        }
    }
}
