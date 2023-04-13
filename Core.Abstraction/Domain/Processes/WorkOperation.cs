namespace Core.Abstraction.Domain.Processes
{
    public class WorkOperation
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }

        public List<Guid> Resources { get; set; }
        //TODO: Add type of machines
        public WorkOperation()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Operation";
            Description = "No description";
            Resources = new List<Guid>();
        }
    }
}
