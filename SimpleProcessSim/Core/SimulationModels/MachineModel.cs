using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSimImplementation.Domain;
using SimSharp;
using static SimSharp.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    public class MachineModel : ActiveObject<Simulation>
    {

        private Machine Machine { get; init; }

        public string Name => Machine.Name;
        public int PartsMade => Machine.PartsMade;

        public bool isWorking { get; set; }

        public double Variance => 2; //TODO: get proper formula for adding variances of different sources (machine, worker, ...)

        private Process Process;
        private Queue<WorkOperation> operationQueue;
        
        public MachineModel(Simulation environment, string name, string description) : base(environment)
        {
            Machine = new Machine()
            {
                Name = name,
                Description = description,
                PartsMade = 0
            };
            isWorking = false;
            operationQueue = new Queue<WorkOperation>();
            Process = environment.Process(Working());
        }

        public void QueueOperation(WorkOperation op)
        {
            operationQueue.Enqueue(op);
            if(!isWorking)
                Process.Interrupt();
        }

        private IEnumerable<Event> Working()
        {
            while (true)
            {
                
                yield return Environment.Timeout(TimeSpan.FromDays(1000));
                if (Environment.ActiveProcess.HandleFault())
                {
                    isWorking = true;
                    while (operationQueue.Any())
                    {
                        var op = operationQueue.Dequeue();
                        var durationDistribution = N(op.Duration, TimeSpan.FromMinutes(Variance));

                        var doneIn = Environment.Rand(POS(durationDistribution));

                        yield return Environment.Timeout(doneIn);

                        Machine.PartsMade++;
                    }
                    isWorking = false;
                }
            }
        }

    }
}
