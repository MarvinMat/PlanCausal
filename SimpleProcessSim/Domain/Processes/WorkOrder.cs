using ProcessSim.Abstraction.Domain.Enums;
using ProcessSim.Abstraction.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSimImplementation.Domain
{
    public class WorkOrder 
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public OrderState State { get; set; }
        public List<WorkOperation> WorkOperations { get; set; }

        WorkOrder() 
        {
            Id = Guid.NewGuid();
            Name = "Unnamed work order";
            Description = "No description";
            State = OrderState.Created;
            WorkOperations = new List<WorkOperation>();
        }
    }
}
