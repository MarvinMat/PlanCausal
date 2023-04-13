using Core.Abstraction.Domain.Resources;
using ProcessSim.Abstraction.Services;

namespace Planner.Implementation
{
    public abstract class PlannerBase
    {

        protected Queue<ProductionOrder> _productionOrders;

        public PlannerBase(IWorkPlanProvider workPlanProvider)
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
        }

        public abstract void Plan();
    }
}
