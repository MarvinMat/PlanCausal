using Core.Abstraction.Domain.Enums;

namespace Core.Abstraction.Domain.Processes
{
    public class WorkOrder
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public OrderState State { get; set; }
        public List<WorkPlanPosition> WorkOperations { get; set; }

        WorkOrder()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed work order";
            Description = "No description";
            State = OrderState.Created;
            WorkOperations = new List<WorkPlanPosition>();
        }
    }
}
