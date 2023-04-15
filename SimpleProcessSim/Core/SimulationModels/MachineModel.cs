using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using SimSharp;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    public class MachineModel : ActiveObject<Simulation>
    {
        private readonly Machine _machine;
        //public PreemptiveResource simMachine;
        public Guid Id => _machine.Id;
        public string Name => _machine.Name;
        public int PartsMade => _machine.PartsMade;
        public int Capacity { get; set; } = 1;
        //public List<IMonitor> Monitors { get; set; }
        public event EventHandler? InterruptEvent;
        public Queue<WorkOperation> Queue { get; set; } = new();

        public MachineModel(Simulation environment, Machine machine) : base(environment)
        {
            _machine = machine;
            environment.Process(Work());

            //Monitors = new List<IMonitor>();
            //var utilization = new TimeSeriesMonitor(environment, name: "Utilization", collect: true);
            //var queueLength = new TimeSeriesMonitor(environment, name: "Queue Length", collect: true);
            //Monitors.Add(utilization);
            //Monitors.Add(queueLength);

            //simMachine = new PreemptiveResource(environment, Capacity)
            //{
            //    Utilization = utilization,
            //    QueueLength = queueLength

            //};
        }

        private IEnumerable<Event> Work()
        {
            while (true)
            {
                //TODO: Implementent idle time

                var order = Queue.Dequeue();
                yield return Environment.Timeout(order.Duration);




            }
        }
    }
}
