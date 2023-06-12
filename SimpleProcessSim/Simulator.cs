using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation.Core.SimulationModels;
using SimSharp;
using System.Diagnostics;
using System.Text;

namespace ProcessSim.Implementation
{
    public class Simulator : ISimulator
    {
        private readonly Simulation _sim;
        private readonly Dictionary<IResource, ActiveObject<Simulation>> _simResources;
        private List<WorkOperation> _currentPlan;
        private ManualResetEventSlim _currentPlanChangedEvent = new(false);
        public DateTime CurrentSimulationTime => _sim.Now;
        public event EventHandler? SimulationEventHandler;

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
                CreateSimulationResource(resource);
            }
        }

        public void CreateSimulationResource(IResource resource)
        {
            if (resource is Machine machine)
            {
                var model = new MachineModel(_sim, machine, _currentPlanChangedEvent)
                {
                    WaitingTime = new SampleMonitor($"WaitingTime of Machine {machine.Description}", true),
                    LeadTime = new SampleMonitor($"LeadTime of Machine {machine.Description}", true),
                    QueueLength = new TimeSeriesMonitor(_sim, $"QueueLength of Machine {machine.Description}", true)
                };
                model.SimulationEventHandler += InvokeSimulationEvent;

                if (!_simResources.TryAdd(resource, model))
                    Debug.WriteLine($"Machine {machine.Description} with ID {machine.Id} already added.");
            }
        }
        private IEnumerable<Event> Replanning()
        {
            while (true)
            {
                yield return _sim.Timeout(TimeSpan.FromHours(8));

                InvokeSimulationEvent(this, new ReplanningEvent(_sim.Now));

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

        private void InvokeSimulationEvent(object? sender, EventArgs e)
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

            SimulationEventHandler?.Invoke(sender, e);
        }

        public void SetCurrentPlan(List<WorkOperation> modifiedPlan)
        {
            _currentPlan = modifiedPlan;

            MoveQueuedOperationsToNewMachine(modifiedPlan);
            RemoveOperationsNotInNewPlan(modifiedPlan);

            // start all operations that can be started
            _currentPlan.Where(operation =>
            {
                var isNotStarted = operation.State.Equals(OperationState.Scheduled);
                var hasPredecessor = operation.Predecessor is not null;
                var isPredecessorCompleted = operation.Predecessor is not null && operation.Predecessor.State.Equals(OperationState.Completed);

                return isNotStarted && (!hasPredecessor || isPredecessorCompleted);
            }).ToList().ForEach(ExecuteOperation);
        }

        private void RemoveOperationsNotInNewPlan(List<WorkOperation> modifiedPlan)
        {
            // if any operation that is already queued on a machine, is not in the new plan, remove it from the machine
             _simResources.ToList().ForEach(resource =>
             {
                if (resource.Value is MachineModel machineModel)
                {
                     var operationsToRemove = new List<WorkOperation>();
                     machineModel.OperationQueue.Where(operation => operation.State.Equals(OperationState.Pending))
                     .ToList().ForEach(operation =>
                     {
                         if (!modifiedPlan.Contains(operation))
                             operationsToRemove.Add(operation);
                     });
                     operationsToRemove.ForEach(operation =>
                     {
                         machineModel.RemoveOperation(operation);
                         operation.State = OperationState.Scheduled;
                     });
                }
            });
        }

        private void MoveQueuedOperationsToNewMachine(IEnumerable<WorkOperation> modifiedPlan)
        {
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
        }

        /// <summary>
        /// Add an interrupt that occurs randomly on specific processes and is automatically handled after running the given action.
        /// </summary>
        /// <param name="predicate">The function specifying whether this interrupt is supposed to interrupt the given process. It will be evaluated for all simulation 
        /// processes. Should return true if the given process is supposed to be interrupted and false otherwise.</param>
        /// <param name="distribution">The distribution of the time between two occurrences of this interrupt.</param>
        /// <param name="interruptAction">The function to be run by each affected process when the interrupt occurs. For example, it can contain handling the interrupt. 
        /// The process will continue execution after this function has run.</param>
        public void AddInterrupt(Func<ActiveObject<Simulation>, bool> predicate, Distribution<TimeSpan> distribution, Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
        {
            _sim.Process(InterruptProcess(predicate, distribution, interruptAction));
        }

        private IEnumerable<Event> InterruptProcess(Func<ActiveObject<Simulation>, bool> predicate, Distribution<TimeSpan> interruptTime, Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
        {
            while (true)
            {
                yield return _sim.Timeout(interruptTime);
                var resourcesToInterrupt = _simResources.Where(resource => {
                    return predicate.Invoke(resource.Value);
                }).ToList();

                var interruptedResources = new List<IResource>();
                resourcesToInterrupt.ForEach(resource =>
                {
                    if (resource.Value is MachineModel machineModel)
                        if (!machineModel.State.Equals(MachineState.Interrupted))
                        {
                            machineModel.Machine.State = MachineState.Interrupted;
                            machineModel.Process.Interrupt(interruptAction);
                            interruptedResources.Add(resource.Key);
                        }
                });

                InvokeSimulationEvent(this, new InterruptionEvent(_sim.Now, interruptedResources));

                _currentPlanChangedEvent.Wait();
                _currentPlanChangedEvent.Reset();
            }
        }
        public SimSharp.Timeout Timeout(Distribution<TimeSpan> distribution) => _sim.Timeout(distribution);
        public SimSharp.Timeout Timeout(TimeSpan duration) => _sim.Timeout(duration);

        public string GetResourceSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("#############################################");
            foreach (var resource in _simResources)
            {

                //TODO: use reflection to detect all set monitors and summarize them
                if (resource.Value is MachineModel machineModel)
                {
                    sb.AppendLine(machineModel.WaitingTime?.Summarize());
                    sb.AppendLine("#############################################");
                    sb.AppendLine(machineModel.LeadTime?.Summarize());
                    sb.AppendLine("#############################################");
                    sb.AppendLine(machineModel.QueueLength?.Summarize());

                }
            }
            return sb.ToString();
        }
    }
}
