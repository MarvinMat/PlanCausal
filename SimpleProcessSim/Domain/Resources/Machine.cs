using ProcessSim.Abstraction.Domain.Interfaces;
using SimSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ProcessSimImplementation.Domain
{
    internal class Machine : IResource
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
