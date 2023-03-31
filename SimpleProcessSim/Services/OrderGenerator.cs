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
        //public List<WorkPlan> WorkPlans { get; set; }
        public MachineModel? Machine { get; set; }
        public OrderGenerator(Simulation environment) : base(environment)
        {
            //  WorkPlans = new List<WorkPlan>();
            environment.Process(GenerateWorkOperations());
            
        }

        public IEnumerable<Event> GenerateWorkOperations ()
        {
            while (true) 
            {
                var wait = Environment.Rand(EXP(TimeSpan.FromMinutes(10)));
                yield return Environment.Timeout(wait);
                Machine?.QueueOperation(new WorkOperation()
                {
                    Name = "Op1",
                    Description = "Test",
                    Duration = TimeSpan.FromMinutes(30)
                });
            }
        }

    }
}
