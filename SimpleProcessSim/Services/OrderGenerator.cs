//using Core.Abstraction.Domain.Resources;
//using ProcessSim.Implementation.Core.SimulationModels;
//using SimSharp;
//using static SimSharp.Distributions;

//namespace Core.Abstraction.Domain.Processes.Services
//{
//    public class OrderGenerator : ActiveObject<Simulation>
//    {
//        public List<WorkPlan> WorkPlans { get; set; }
//        public OrderGenerator(Simulation environment) : base(environment)
//        {
//            WorkPlans = new List<WorkPlan>();
//            environment.Process(GenerateProductionOrders());
//        }

//        private IEnumerable<Event> GenerateProductionOrders()
//        {
//            var productionOrders = new List<ProductionOrderModel>();
//            while (true)
//            {
//                var wait = Environment.Rand(EXP(TimeSpan.FromMinutes(60)));
//                yield return Environment.Timeout(wait);

//                if (!WorkPlans.Any()) continue;

//                var rand = new Random();
//                var idx = rand.Next(0, WorkPlans.Count);
//                var plan = WorkPlans.ElementAt(idx);
//                var quantity = (int)Math.Ceiling(Environment.Rand(EXP(3)));

//                var order = new ProductionOrder() { WorkPlan = plan, Quantity = quantity };
//                Environment.Log($"Received an order with id {order.Id} for product {plan.Name} with quantity {quantity} at {Environment.Now}.");

//                productionOrders.Add(new ProductionOrderModel(Environment, order));
//            }
//        }
//    }
//}
