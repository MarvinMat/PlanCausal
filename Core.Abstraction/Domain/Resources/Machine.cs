using Core.Abstraction.Domain.Models;

namespace Core.Abstraction.Domain.Resources
{
    public class Machine : IResource
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public int PartsMade { get; set; }

        public int MachineType { get; init; }
        public int [] AllowedToolIds { get; init; }
        
        public double [][] ChangeoverTimes { get; set; }

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
