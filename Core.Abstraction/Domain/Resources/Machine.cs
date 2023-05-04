namespace Core.Abstraction.Domain.Resources
{
    public class Machine : IResource
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PartsMade { get; set; }

        public int MachineType { get; set; }

        //public double Cost { get; set; }

        //public double ProbabilityToBreak { get; set; }

        public Machine()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Machine";
            Description = "No Description";
            PartsMade = 0;
            //Cost = 0;
            //ProbabilityToBreak = 0.05;

        }

    }
}
