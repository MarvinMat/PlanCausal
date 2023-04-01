using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessSim.Abstraction.Domain.Interfaces;
using SimSharp;

namespace ProcessSim.Implementation.Core
{
    public class SimulatorBuilder 
    {
        private Simulation simulation;
        private List<IResource> resources;
        public DateTime StartTime { get; set; }

        public SimulatorBuilder(DateTime startTime) {
            StartTime = startTime;
            simulation = new Simulation(startTime);
            resources = new List<IResource>();
        }

        public SimulatorBuilder AddProcess(IProcess process)
        {
            simulation.Process(process, 0);
            return this;
        }

        public SimulatorBuilder AddResource(IResource? resource)
        {
            if (resource != null)
                resources.Add(resource);
            return this;
        }

        public SimulatorBuilder AddPreemptiveResource(int capacity)
        {
            var resource = new PreemptiveResource(simulation, capacity);
            return this;
        }

        public Simulation Build()
        {
            return simulation;
        }
    }
}


