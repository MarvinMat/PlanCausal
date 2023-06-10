using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Models;

namespace Core.Abstraction.Domain.Resources
{
    public class Machine : IResource
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public MachineState State { get; set; }

        public int MachineType { get; init; }
        public int [] AllowedToolIds { get; init; }
        
        public double [][] ChangeoverTimes { get; set; }

        public Machine()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Machine";
            Description = "No Description";
            State = MachineState.Idle;
        }
    }
}
