
namespace ProcessSimImplementation.Domain
{
    public class Machine
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PartsMade { get; set; }

        public Machine()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Machine";
            Description = "No Description";
            PartsMade = 0;

        }

    }
}
