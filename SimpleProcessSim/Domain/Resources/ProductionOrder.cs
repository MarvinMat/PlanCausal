using ProcessSim.Abstraction.Domain.Enums;
using ProcessSim.Abstraction.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSimImplementation.Domain
{
    public class ProductionOrder : IResource
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime DateOfProductionStart { get; set; }

        public OrderState State { get; set; }

        public List<WorkOrder> WorkOrders { get; set; }

        ProductionOrder()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed Product";
            Quantity = 0;
            State = OrderState.Created;
            WorkOrders = new List<WorkOrder>();
        }
    }
}
