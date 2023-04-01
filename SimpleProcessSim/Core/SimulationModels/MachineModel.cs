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
        private PreemptiveResource simMachine;
        public string Name => _machine.Name;
        public int PartsMade => _machine.PartsMade;
        public int Capacity { get; set; } = 1;

        public MachineModel(Simulation environment, Machine machine) : base(environment)
        {
            _machine = machine;
            simMachine = new PreemptiveResource(environment, Capacity);
        }
    }
}
