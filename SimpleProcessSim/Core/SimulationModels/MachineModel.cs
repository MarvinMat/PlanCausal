using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSimImplementation.Domain;
using SimSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    public class MachineModel : ActiveObject<Simulation>
    {
        private readonly Machine _machine;
        public PreemptiveResource simMachine;
        public Guid Id => _machine.Id;
        public string Name => _machine.Name;
        public int PartsMade => _machine.PartsMade;
        public int Capacity { get; set; } = 1;
        public List<IMonitor> Monitors { get; set; }

        public MachineModel(Simulation environment, Machine machine) : base(environment)
        {
            _machine = machine;
            Monitors = new List<IMonitor>();
            var utilization = new TimeSeriesMonitor(environment, name: "Utilization", collect: true);
            var queueLength = new TimeSeriesMonitor(environment, name: "Queue Length", collect: true);
            Monitors.Add(utilization);
            Monitors.Add(queueLength);

            simMachine = new PreemptiveResource(environment, Capacity) 
            {
                Utilization = utilization,
                QueueLength = queueLength
                
            };
        }
    }
}
