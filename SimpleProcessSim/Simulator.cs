using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation.Core.SimulationModels;
using SimSharp;

namespace ProcessSim.Implementation
{
    public class Simulator : ISimulator
    {
        private readonly Simulation _sim;
        private readonly List<ActiveObject<Simulation>> _simResources;
        private List<WorkOperation> _currentPlan;
        private ManualResetEventSlim _currentPlanChangedEvent = new(false);
        public event EventHandler? InterruptEvent;

        public List<WorkOperation> CurrentPlan
        {
            get { return _currentPlan; }
            set
            {
                _currentPlan = value;
                _currentPlanChangedEvent.Set();
            }
        }
        public Simulator(int seed, DateTime initialDateTime)
        {
            _sim = new Simulation(randomSeed: seed, initialDateTime: initialDateTime);
            _simResources = new List<ActiveObject<Simulation>>();
            _currentPlan = new List<WorkOperation>();

        }
        public void Start(TimeSpan duration)
        {
            _sim.Process(Replanning());

            _currentPlanChangedEvent.Wait();
            _currentPlanChangedEvent.Reset();

            _sim.Run(duration);
        }
        public void CreateSimulationResources(IEnumerable<IResource> resources)
        {
            foreach (var resource in resources)
            {
                if (resource != null)
                {
                    CreateSimulationResource(resource);
                }
            }
        }
        public void CreateSimulationResource(IResource resource)
        {
            if (resource is Machine machine)
            {
                _simResources.Add(new MachineModel(_sim, machine));
            }
        }
        private IEnumerable<Event> Replanning()
        {
            while (true)
            {
                yield return _sim.Timeout(TimeSpan.FromHours(8));
                OnInterrupt(new ReplanningEvent());

                _currentPlanChangedEvent.Wait();
                _currentPlanChangedEvent.Reset();
            }
        }
        protected virtual void OnInterrupt(EventArgs e)
        {
            InterruptEvent?.Invoke(this, e);
        }
    }
}
