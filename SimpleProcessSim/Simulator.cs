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

        /// <summary>
        /// Construct a new SimSharp simulation environment with the given seed and start date.
        /// </summary>
        /// <param name="seed">The seed for the generation of random numbers during the simulation.</param>
        /// <param name="initialDateTime">The starting time of the simulation.</param>
        public Simulator(int seed, DateTime initialDateTime)
        {
            _sim = new Simulation(randomSeed: seed, initialDateTime: initialDateTime);
            _simResources = new();
            _currentPlan = new();
        }

        public void Start(TimeSpan duration)
        {
            _sim.Process(Replanning());

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

                if (!_simResources.TryAdd(resource, model))
                    Debug.WriteLine($"Machine {machine.Name} with ID {machine.Id} already added.");
            }
        }
        private IEnumerable<Event> Replanning()
        {
            while (true)
            {
                yield return _sim.Timeout(TimeSpan.FromHours(8));
                InterruptHandler(this, new ReplanningEvent(_sim.Now));

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

        public void Continue()
        {
            _currentPlanChangedEvent.Set();
        }

        private void InterruptHandler(object? sender, EventArgs e)
        {
            if (e is OperationCompletedEvent operationCompletedEvent)
            {
                var completedOperation = operationCompletedEvent.CompletedOperation;
                var successor = completedOperation.Successor;
                if (successor is not null)
                {
                    ExecuteOperation(successor);
                }
            }
            InterruptEvent?.Invoke(sender, e);
        }

        public void SetCurrentPlan(List<WorkOperation> modifiedPlan)
        {
            _currentPlan = modifiedPlan;

            // if the machine of any operation (that is already queued) changed, remove that operation from that machine and enqueue it on the new machine
            var queuedOperations = modifiedPlan.Where(op => op.State.Equals(OperationState.Pending));
            queuedOperations.ToList().ForEach(op =>
            {
                _simResources.ToList().ForEach(resource =>
                {
                    if (resource.Value is MachineModel machineModel &&
                    machineModel.IsQueued(op) &&
                    op.Machine != resource.Key)
                    {
                        machineModel.RemoveOperation(op);
                        ExecuteOperation(op);
                    }
                });
            });

            // start all operations that can be started
            _currentPlan.Where(operation =>
            {
                var isNotStarted = operation.State.Equals(OperationState.Scheduled);
                var hasPredecessor = operation.Predecessor is not null;
                var isPredecessorCompleted = operation.Predecessor is not null && operation.Predecessor.State.Equals(OperationState.Completed);

                return isNotStarted && (!hasPredecessor || isPredecessorCompleted);
            }).ToList().ForEach(operation => ExecuteOperation(operation));
        }
    }
}
