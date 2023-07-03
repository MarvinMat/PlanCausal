using Core.Abstraction.Domain.Enums;
using System.Diagnostics;

namespace Core.Abstraction.Domain.Resources
{
    [DebuggerDisplay("{Description}")]
    public class Machine : IResource
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Description => $"{Name}.{Id.ToString()[..3]}";
        public MachineState State { get; set; }

        public int MachineType { get; init; }
        public int [] AllowedToolIds { get; init; }
        
        public double [][] ChangeoverTimes { get; set; }

        public Machine()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Machine";
            //Description = "No Description";
            State = MachineState.Idle;
        }
    }
}
