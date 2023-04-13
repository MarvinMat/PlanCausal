using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Abstraction.Domain.Resources
{
    public class ProductionOrder
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime DateOfProductionStart { get; set; }

        public OrderState State { get; set; }

        public WorkPlan WorkPlan { get; init; }

        public ProductionOrder()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Product";
            Quantity = 0;
            State = OrderState.Created;
            WorkPlan = new WorkPlan();
        }
    }
}
