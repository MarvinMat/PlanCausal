using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Resources;
using SimSharp;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    public class ProductionOrderModel : ActiveObject<Simulator>
    {
        public ProductionOrderModel(Simulator environment, ProductionOrder order) : base(environment)
        {
            Order = order;
            Environment.Process(Producing());
        }
        public int Quantity => Order.Quantity;

        public OrderState OrderState => Order.State;

        public ProductionOrder Order { get; init; }

        public string Name => Order.Name;

        private IEnumerable<Event> Producing()
        {
            var models = new List<WorkOrderModel>();
            Order.State = OrderState.InProgress;
            Order.DateOfProductionStart = Environment.Now;

            var store = new Store(Environment, Quantity);
            for (int i = 0; i < Quantity; i++)
                models.Add(new WorkOrderModel(Environment, store) { WorkPlan = Order.WorkPlan });

            yield return store.WhenFull();

            Order.State = OrderState.Completed;
            Environment.Log($"Completed an order with ID {Order.Id} for product {Order.WorkPlan.Name} with quantity {Order.Quantity} at {Environment.Now} and lasted {Environment.Now - Order.DateOfProductionStart}");
        }
    }
}
