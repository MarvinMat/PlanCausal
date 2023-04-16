using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation.Core.SimulationModels;
using SimSharp;
using System.Diagnostics;

namespace ProcessSim.Implementation
{
    public class Simulator : ISimulator
    {
        private readonly Simulation _sim;
        private readonly Dictionary<IResource, ActiveObject<Simulation>> _simResources;
        private List<WorkOperation> _currentPlan;
        private ManualResetEventSlim _currentPlanChangedEvent = new(false);
        public event EventHandler? InterruptEvent;

        public List<WorkOperation> CurrentPlan
        {
            get { return _currentPlan; }
            set
            {
                _currentPlan = value;
                // set the plan, start all operations that can be started and notify the simulation thread to continue
                _currentPlan.Where(operation =>
                {
                    var isNotStarted = operation.State.Equals(OperationState.Created);
                    var hasPredecessor = operation.Predecessor is not null;
                    var isPredecessorCompleted = operation.Predecessor is not null && operation.Predecessor.State.Equals(OperationState.Completed);

                    return isNotStarted && (!hasPredecessor || isPredecessorCompleted);
                }).ToList().ForEach(operation => ExecuteOperation(operation));

                //notifiy the simulation thread to continue
                Continue();
            }
        }
        public Simulator(int seed, DateTime initialDateTime)
        {
            _sim = new Simulation(randomSeed: seed, initialDateTime: initialDateTime);
            _simResources = new();
            _currentPlan = new();

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
                var model = new MachineModel(_sim, machine, _currentPlanChangedEvent);
                model.InterruptEvent += InterruptHandler;

                if (_simResources.TryAdd(resource, model))
                    Debug.WriteLine($"Machine {machine.Name} with ID {machine.Id} already added.");
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

        private void ExecuteOperation(WorkOperation operation)
        {
            var machine = operation.Machine ?? throw new ArgumentNullException(nameof(operation));
            _simResources.TryGetValue(machine, out var activeObject);
            if (activeObject is MachineModel machineModel)
            {
                operation.State = OperationState.Pending;
                machineModel.EnqueueOperation(operation);
            }

        }

        protected virtual void OnInterrupt(EventArgs e)
        {
            InterruptEvent?.Invoke(this, e);
        }

        public void Continue()
        {
            //potential issues
            _currentPlanChangedEvent.Set();
        }

        private void InterruptHandler(object? sender, EventArgs e)
        {
            if (e is OperationCompletedEvent operationCompletedEvent)
            {
                var successor = operationCompletedEvent.CompletedOperation.Successor;
                if (successor is not null)
                {
                    ExecuteOperation(successor);
                }
            }
            InterruptEvent?.Invoke(sender, e);
        }
    }
}
