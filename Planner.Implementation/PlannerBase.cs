using Controller.Abstraction;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;
using Planner.Abstraction;

namespace Planner.Implementation
{
    public abstract class PlannerBase : IPlanner
    {

        protected Queue<ProductionOrder> _productionOrders;
        protected IEnumerable<Machine> _machines;
        protected IController? _controller;
        private IWorkPlanProvider workPlanProvider;


        protected PlannerBase(IWorkPlanProvider workPlanProvider, IMachineProvider machineProvider)
        {
            //TODO: Clean up 
            var rnd = new Random();
            var workPlans = workPlanProvider.Load();
            var productionOrders = new List<ProductionOrder>() {
                new ProductionOrder() {
                    Name = "Order 1",
                    Quantity = rnd.Next(1, 100),
                    WorkPlan = workPlans[rnd.Next(0, workPlans.Count)]
                },
                new ProductionOrder()
                {
                    Name = "Order 2",
                    Quantity = rnd.Next(1, 100),
                    WorkPlan = workPlans[rnd.Next(0, workPlans.Count)]
                },
                 new ProductionOrder()
                {
                    Name = "Order 3",
                    Quantity = rnd.Next(1, 100),
                    WorkPlan = workPlans[rnd.Next(0, workPlans.Count)]
                },
            };
            _productionOrders = new Queue<ProductionOrder>(productionOrders);

            _machines = machineProvider.Load();

        }
        public abstract void Plan();
    }
}
