namespace Core.Abstraction.Domain.Processes
{
    public class WorkPlanPosition

    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }

        public int ToolId { get; set; }
        public int MachineType { get; set; }

        public WorkPlanPosition()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Operation";
            Description = "No description";
        }
    }
}
