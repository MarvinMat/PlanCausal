namespace Core.Abstraction.Domain.Resources
{
    public class Machine : IResource
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int MachineType { get; set; }

        public Machine()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Machine";
            Description = "No Description";
        }
    }
}
