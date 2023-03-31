using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessSim.Abstraction.Domain.Interfaces;


namespace ProcessSimImplementation.Domain
{
    public class WorkOperation 
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }

        public List<IResource> Resources { get; set; }


        public WorkOperation()
        { 
            Id = Guid.NewGuid();
            Name = "Unnamed Operation";
            Description = "No description";
            Resources = new List<IResource>();
        }
    }
}
