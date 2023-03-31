using ProcessSim.Abstraction.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSimImplementation.Domain
{
    public class WorkPlan 
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public List<WorkOrder> WorkOrders { get; set; }

        WorkPlan() 
        {
            Id = Guid.NewGuid();
            Name = "Unnamed work plan";
            WorkOrders = new List<WorkOrder>();
        }
    }
}
