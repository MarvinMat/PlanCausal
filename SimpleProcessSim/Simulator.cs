using CoreAbstraction = Core.Abstraction;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation.Core.SimulationModels;
using SimSharp;
using System.Diagnostics;
using System.Text;
using ProcessSim.Abstraction;

namespace ProcessSim.Implementation
{
    public class Simulator : ISimulator
    {
        private readonly Simulation _sim;
        private readonly Dictionary<IResource, ActiveObject<Simulation>> _simResources;
        public TimeSpan ReplanningInterval { get; init; }
        private List<WorkOperation> _currentPlan;
        private readonly ManualResetEventSlim _currentPlanChangedEvent = new(false);
        public DateTime CurrentSimulationTime => _sim.Now;
        public event EventHandler? SimulationEventHandler;
        public int CountOfMachines => _simResources.Keys.OfType<Machine>().Count();

        public IEnumerable<IFactor> InfluencingFactors { get; set; }
        public Func<Dictionary<string, IFactor>, double> CalculateOperationDurationFactor;

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
            ReplanningInterval = TimeSpan.FromHours(12);
            InfluencingFactors = new HashSet<IFactor>();
            CalculateOperationDurationFactor = _ => 1;
        }

        public void Start(TimeSpan duration)
        {
            _sim.Process(Replanning());

            foreach (var factor in InfluencingFactors)
            {
                _sim.Process(factor.SimulateFactor());
            }

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
                    QueueLength = new TimeSeriesMonitor(_sim, $"QueueLength of Machine {machine.Description}", true),
                    InfluencingFactors = InfluencingFactors,
                    CalculateOperationDurationFactor = CalculateOperationDurationFactor,
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
                yield return _sim.Timeout(ReplanningInterval);

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
                if (successor?.Machine != null)
                    ExecuteOperation(successor);
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
                var isPredecessorCompleted = operation.Predecessor is not null &&
                                             operation.Predecessor.State.Equals(OperationState.Completed);

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
        public void AddInterrupt(Func<ActiveObject<Simulation>, bool> predicate,
            CoreAbstraction.Distribution<TimeSpan> distribution,
            Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
        {
            _sim.Process(InterruptProcess(predicate, distribution, interruptAction));
        }

        private IEnumerable<Event> InterruptProcess(Func<ActiveObject<Simulation>, bool> predicate,
            CoreAbstraction.Distribution<TimeSpan> interruptTimeDistribution,
            Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
        {
            while (true)
            {
                var interruptTime = interruptTimeDistribution();
                yield return _sim.Timeout(interruptTime);
                var resourcesToInterrupt = _simResources
                    .Where(resource =>
                        predicate.Invoke(resource.Value)).ToList();

                var interruptedResources = new List<IResource>();
                resourcesToInterrupt.ForEach(resource =>
                {
                    if (resource.Value is not MachineModel machineModel) return;
                    if (machineModel.State.Equals(MachineState.Interrupted)) return;
                    
                    machineModel.Machine.State = MachineState.Interrupted;
                    machineModel.Process.Interrupt(interruptAction);
                    interruptedResources.Add(resource.Key);
                });

                InvokeSimulationEvent(this, new InterruptionEvent(_sim.Now, interruptedResources));

                _currentPlanChangedEvent.Wait();
                _currentPlanChangedEvent.Reset();
            }
        }

        public SimSharp.Timeout Timeout(Distribution<TimeSpan> distribution) => _sim.Timeout(distribution);
        public SimSharp.Timeout Timeout(TimeSpan duration) => _sim.Timeout(duration);
        public T SampleRandomDistribution<T>(IDistribution<T> distribution) => _sim.Rand(distribution);

        /// <summary>
        /// Add an order generation process that invokes the <see cref="OrderGenerationEvent"/> regularly, according to the given distribution.
        /// </summary>
        /// <param name="orderFrequencyDistribution">The distribution of the time between two occurrences of an order generation event.</param>
        public void AddOrderGeneration(CoreAbstraction.Distribution<TimeSpan> orderFrequencyDistribution)
        {
            _sim.Process(GenerateOrder(orderFrequencyDistribution));
        }

        private IEnumerable<Event> GenerateOrder(CoreAbstraction.Distribution<TimeSpan> orderFrequencyDistribution)
        {
            while (true)
            {
                var nextOrderTime = orderFrequencyDistribution();
                yield return _sim.Timeout(nextOrderTime);

                InvokeSimulationEvent(this, new OrderGenerationEvent(_sim.Now));

                _currentPlanChangedEvent.Wait();
                _currentPlanChangedEvent.Reset();
            }
        }

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

        public double GetWaitingTimeSummaryOfMachines()
        {
            var totalIdleTime = 0.0;
            foreach (var resource in _simResources)
            {
                if (resource.Value is MachineModel { WaitingTime: not null } machineModel)
                    totalIdleTime += machineModel.WaitingTime.Sum;
            }

            return totalIdleTime;
        }

        public double GetWaitingTimeByMachineType(int machineType)
        {
            var totalIdleTime = 0.0;
            _simResources.Values.OfType<MachineModel>().Where(machineModel => machineModel.Machine.MachineType == machineType)
                .ToList().ForEach(machineModel =>
                {
                    if (machineModel.WaitingTime != null) totalIdleTime += machineModel.WaitingTime.Sum;
                });
            return totalIdleTime;
        }
    }
}
