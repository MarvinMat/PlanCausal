using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation.Core.SimulationModels;
using ProcessSimImplementation.Domain;
using SimSharp;
using static SimSharp.Distributions;

namespace ProcessSim.Implementation.Services
{
    public class OrderGenerator : ActiveObject<Simulation>
    {
        public List<WorkPlan> WorkPlans { get; set; }
        public OrderGenerator(Simulation environment) : base(environment)
        {
            WorkPlans = new List<WorkPlan>();
            environment.Process(GenerateWorkOperations());
        }

        private IEnumerable<Event> GenerateWorkOperations ()
        {
            var productionOrders = new List<ProductionOrderModel>();
            while (true) 
            {
                var wait = Environment.Rand(EXP(TimeSpan.FromMinutes(10)));
                yield return Environment.Timeout(wait);

                if (!WorkPlans.Any()) continue;

                var rand = new Random();
                var idx = rand.Next(0, WorkPlans.Count);
                var plan = WorkPlans.ElementAt(idx);
                var quantity = (int)Environment.Rand(EXP(5));

                productionOrders.Add(new ProductionOrderModel(Environment, new ProductionOrder() { WorkPlan = plan, Quantity = quantity }));
                
            }
        }

        public IEnumerable<Event> GenerateWorkPlans()
        {
            throw new NotImplementedException();
        }

    }
}
