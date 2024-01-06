using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction;
using ProcessSim.Implementation.Core.InfluencingFactors;
using Serilog;
using SimSharp;
using static SimSharp.Distributions;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    public class MachineModel : ActiveObject<Simulation>
    {
        private readonly Machine _machine;
        private readonly List<WorkOperation> _operationQueue;
        private readonly ManualResetEventSlim _continueEvent;
        private readonly ILogger _logger;

        private WorkOperation? _currentOperation;
        private bool _isProcessRunning;
        private bool _isProcessInterrupted;
        public List<WorkOperation> OperationQueue => _operationQueue;
        public WorkOperation? CurrentOperation => _currentOperation;
        public MachineState State => _machine.State;
        public Guid Id => _machine.Id;
        public Machine Machine => _machine;
        public Process Process { get; init; }
        public event EventHandler? SimulationEventHandler;
        public IEnumerable<IFactor> InfluencingFactors { get; set; }
        private Dictionary<string, object> LastObservedValuesOfInfluencingFactors { get; set; }
        public Func<Dictionary<string, IFactor>, double> CalculateOperationDurationFactor { get; set; }

        public int CurrentToolId { get; set; }

        private bool operationNeededChangeover = false;

        // monitoring
        public ITimeSeriesMonitor? Utilization { get; set; }
        public ITimeSeriesMonitor? QueueLength { get; set; }
        public ISampleMonitor? LeadTime { get; set; }
        public ISampleMonitor? WaitingTime { get; set; }

        public MachineModel(Simulation environment, Machine machine, ManualResetEventSlim continueEvent) : base(environment)
        {
            _logger = Log.ForContext<MachineModel>();
            _machine = machine;
            Process = environment.Process(Work());
            _operationQueue = new();
            _continueEvent = continueEvent;
            _machine.State = MachineState.Idle;
            _isProcessRunning = false;
            _isProcessInterrupted = false;
            InfluencingFactors = new HashSet<IFactor>();
            LastObservedValuesOfInfluencingFactors = new();
            CalculateOperationDurationFactor = _ => 1.0;
            if (_machine.AllowedToolIds != null) CurrentToolId = _machine.AllowedToolIds.FirstOrDefault();
        }

        public void EnqueueOperation(WorkOperation operation)
        {
            var previousFirstOperation = _operationQueue.FirstOrDefault();
            _operationQueue.Add(operation);
            QueueLength?.UpdateTo(_operationQueue.Count);
            _operationQueue.Sort((a, b) => a.PlannedStart.CompareTo(b.PlannedStart));

            if (_isProcessRunning && // don't interrupt if the process is not even running yet
                !_isProcessInterrupted &&  // don't interrupt if the process has already been interrupted
                Process != Environment.ActiveProcess && // the active process can't interrupt itself
                State.Equals(MachineState.Idle) && // only interrupt the infinite timeout when being idle and waiting for an operation
                _operationQueue.First() != previousFirstOperation) // only interrupt if current operation has changed
            {
                Process.Interrupt("Enqueued Operation");
                _isProcessInterrupted = true;
            }
        }

        public void RemoveOperation(WorkOperation operation)
        {
            var previousFirstOperation = _operationQueue.FirstOrDefault();
            if (!_operationQueue.Remove(operation))
            {
                throw new Exception($"Tried to remove operation {operation.WorkPlanPosition.Name} from " +
                    $"{_machine.Description} queue, but was not found.");
            }
            QueueLength?.UpdateTo(_operationQueue.Count);
            _operationQueue.Sort((a, b) => a.PlannedStart.CompareTo(b.PlannedStart));

            // interrupt if current operation has changed
            if (_isProcessRunning &&
                !_isProcessInterrupted &&
                Process != Environment.ActiveProcess &&
                State.Equals(MachineState.Idle) &&
                (_operationQueue.Count == 0 || _operationQueue.First() != previousFirstOperation))
            {
                Process.Interrupt("Removed operation");
                _isProcessInterrupted = true;
            }
        }

        public bool IsQueued(WorkOperation operation)
        {
            return _operationQueue.Contains(operation);
        }

        private IEnumerable<Event> Work()
        {
            _isProcessRunning = true;
            while (true)
            {
                var idleTime = Environment.Now;
                foreach (var idleEvent in Idle())
                {
                    yield return idleEvent;
                }

                if (Environment.Now - idleTime > TimeSpan.Zero)
                    WaitingTime?.Add((Environment.Now - idleTime).TotalSeconds);

                foreach (var waitingOrChangeoverEvent in Changeover())
                {
                    yield return waitingOrChangeoverEvent;
                }

                if (_currentOperation == null)
                {
                    // the operation that was supposed to be processed, got removed during wait and changeover and no other operation is queued now, so go back to idle
                    continue;
                }

                var processStartTime = Environment.Now;
                foreach (var processingEvent in ProcessOrder())
                {
                    yield return processingEvent;
                }

                AssessOrderCompletion();
                SimulationEventHandler?.Invoke(this, new OperationCompletedEvent(Environment.Now, _currentOperation, LastObservedValuesOfInfluencingFactors));

                _continueEvent.Wait();
                _continueEvent.Reset();

                LeadTime?.Add((Environment.Now - processStartTime).TotalMinutes);

                _machine.State = MachineState.Idle;
                _operationQueue.Remove(_currentOperation);
                operationNeededChangeover = false;
            }
        }
        private IEnumerable<Event> ProcessOrder()
        {
            if (_currentOperation == null) yield break;
            _currentOperation.State = OperationState.InProgress;
            if (_currentOperation.WorkOrder.State.Equals(OrderState.Created))
                _currentOperation.WorkOrder.StartTime = Environment.Now;
            _currentOperation.WorkOrder.State = OrderState.InProgress;
            if (_currentOperation.WorkOrder.ProductionOrder.State.Equals(OrderState.Created))
                _currentOperation.WorkOrder.ProductionOrder.StartedDate = Environment.Now;
            _currentOperation.WorkOrder.ProductionOrder.State = OrderState.InProgress;

            _machine.State = MachineState.Working;

            var influencingFactors = InfluencingFactors.ToDictionary(factor => factor.Name);
            influencingFactors.Add(InternalInfluenceFactorName.NeededChangeover.ToString(), new InfluencingFactor<bool>(InternalInfluenceFactorName.NeededChangeover.ToString(), null, operationNeededChangeover));
            influencingFactors.Add(InternalInfluenceFactorName.CurrentTime.ToString(), new InfluencingFactor<DateTime>(InternalInfluenceFactorName.CurrentTime.ToString(), null, Environment.Now));

            LastObservedValuesOfInfluencingFactors = new Dictionary<string, object>();
            foreach (var factor in influencingFactors)
            {
                LastObservedValuesOfInfluencingFactors.Add(factor.Key, factor.Value.GetCurrentValue());
            }

            var durationFactor = CalculateOperationDurationFactor(influencingFactors);
            var meanDuration = _currentOperation.MeanDuration * durationFactor;
            var standardDeviation = _currentOperation.VariationCoefficient * meanDuration;

            var durationDistribution = N(meanDuration, standardDeviation);
            var processingDuration = Environment.Rand(POS(durationDistribution));

            var startTime = Environment.Now;
            _currentOperation.ActualStart = startTime;

            _logger.Debug(
                "On {MachineDescription}: Started {Name} at {StartTime} (should have been at {CurrentOperationPlannedStart}). " +
                "ETA is {ProcessingDuration}",
                _machine.Description, _currentOperation.WorkPlanPosition.Name, startTime,
                _currentOperation.PlannedStart, startTime + processingDuration);

            var processingTimeDone = TimeSpan.Zero;
            while (processingDuration - processingTimeDone > TimeSpan.Zero)
            {
                var startedProcessingAt = Environment.Now;
                yield return Environment.Timeout(processingDuration - processingTimeDone);
                processingTimeDone += Environment.Now - startedProcessingAt;
                if (Environment.ActiveProcess.HandleFault())
                {
                    _isProcessInterrupted = false;

                    if (Environment.ActiveProcess.Value is
                        Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
                    {
                        foreach (var interruptEvent in HandleInterrupt(interruptAction))
                            yield return interruptEvent;

                        _machine.State = MachineState.Working;
                    }
                    else
                        throw new Exception(
                            "Process got interrupted during operation processing. This should not happen.");
                }
            }

            _logger.Debug(
                "On {MachineDescription}: Completed {Name} at {EndTime} (lasted {Duration} - supposed to {SupposedDuration} - mean is {MeanDuration})",
                _machine.Description, _currentOperation.WorkPlanPosition.Name, Environment.Now,
                Environment.Now - startTime, processingDuration, _currentOperation.WorkPlanPosition.Duration);

            var endTime = Environment.Now;
            _currentOperation.ActualFinish = endTime;
            _currentOperation.State = OperationState.Completed;
        }

        private void AssessOrderCompletion()
        {
            if (_currentOperation?.Successor is not null || _currentOperation is null) return;

            _currentOperation.WorkOrder.State = OrderState.Completed;
            _currentOperation.WorkOrder.EndTime = Environment.Now;

            if (!_currentOperation.WorkOrder.ProductionOrder.WorkOrders.All(workOrder =>
                    workOrder.State.Equals(OrderState.Completed))) return;
            
            _currentOperation.WorkOrder.ProductionOrder.State = OrderState.Completed;
            _currentOperation.WorkOrder.ProductionOrder.CompletedDate = Environment.Now;
            _logger.Information("Finished production order {ProductionOrderId} of product {Product} at {EndTime}",
                _currentOperation.WorkOrder.ProductionOrder.Id, _currentOperation.WorkOrder.Name ,Environment.Now);

        }

        private IEnumerable<Event> Changeover()
        {
            if (_currentOperation == null) yield break;

            var changeoverTime = GenerateChangeoverTime(_currentOperation.WorkPlanPosition.ToolId);
            var waitTime = _currentOperation.PlannedStart - Environment.Now - changeoverTime;
            if (waitTime < TimeSpan.Zero) waitTime = TimeSpan.Zero;

            _logger.Debug("On {MachineDescription}: Starting preparation at {StartTime} for next operation scheduled at {CurrentOperationPlannedStart}, taking {WaitTime} to wait and {ChangeoverTime} to changeover",
                _machine.Description, Environment.Now, _currentOperation.PlannedStart, waitTime, changeoverTime);

            while (waitTime + changeoverTime > TimeSpan.Zero)
            {
                while (waitTime > TimeSpan.Zero)
                {
                    yield return Environment.Timeout(waitTime);
                    if (Environment.ActiveProcess.HandleFault())
                    {
                        _isProcessInterrupted = false;

                        if (Environment.ActiveProcess.Value is Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
                        {
                            foreach (var interruptEvent in HandleInterrupt(interruptAction))
                                yield return interruptEvent;

                            _machine.State = MachineState.Idle;
                        }

                        if (_operationQueue.Count > 0)
                        {
                            if (_currentOperation.WorkPlanPosition.ToolId != _operationQueue.First().WorkPlanPosition.ToolId)
                            {
                                changeoverTime = GenerateChangeoverTime(_operationQueue.First().WorkPlanPosition.ToolId);

                                _logger.Debug("On {MachineDescription}: Got new operation to process at {StartTime} ,scheduled at {CurrentOperationPlannedStart}, taking {ChangeoverTime} to changeover",
                                    _machine.Description, Environment.Now, _operationQueue.First().PlannedStart, changeoverTime);
                            }
                            _currentOperation = _operationQueue.First();
                        }
                        else
                        {
                            _logger.Debug("On {MachineDescription}: Got no operation to process anymore, at {StartTime}, leaving waiting state",
                                _machine.Description, Environment.Now);

                            _currentOperation = null;
                            yield break;
                        }
                    }
                    waitTime = _currentOperation.PlannedStart - Environment.Now - changeoverTime;
                }

                var changeoverTimeDone = TimeSpan.Zero;
                if (changeoverTime > TimeSpan.Zero)
                {
                    _logger.Debug("On {MachineDescription}: Changing from Tool Id {CurrentToolId} to {NewToolId} at {StartTime}, taking {ChangeoverTime}",
                        _machine.Description, CurrentToolId, _currentOperation.WorkPlanPosition.ToolId, Environment.Now, changeoverTime);
                }
                while (changeoverTime - changeoverTimeDone > TimeSpan.Zero)
                {
                    var changeoverStartedAt = Environment.Now;
                    var timeout = changeoverTime - changeoverTimeDone;

                    operationNeededChangeover = true;
                    yield return Environment.Timeout(timeout);

                    changeoverTimeDone += Environment.Now - changeoverStartedAt;

                    if (Environment.ActiveProcess.HandleFault())
                    {
                        _isProcessInterrupted = false;

                        if (Environment.ActiveProcess.Value is Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
                        {
                            foreach (var interruptEvent in HandleInterrupt(interruptAction))
                                yield return interruptEvent;

                            _machine.State = MachineState.Idle;
                        }

                        if (_operationQueue.Count > 0)
                        {
                            if (_currentOperation.WorkPlanPosition.ToolId != _operationQueue.First().WorkPlanPosition.ToolId)
                            {
                                //changeover target tool is now a different one, so restart the changeover completely
                                changeoverTime = GenerateChangeoverTime(_operationQueue.First().WorkPlanPosition.ToolId);
                                changeoverTimeDone = TimeSpan.Zero;

                                _logger.Debug("On {MachineDescription}: Got new operation to process at {StartTime} ,scheduled at {CurrentOperationPlannedStart}, reset changeover progress, now taking {ChangeoverTime} to changeover",
                                    _machine.Description, Environment.Now, _operationQueue.First().PlannedStart, changeoverTime);
                            }
                            _currentOperation = _operationQueue.First();
                        }
                        else
                        {
                            _logger.Debug("On {MachineDescription}: Got no operation to process anymore, at {StartTime}, leaving changeover state",
                                _machine.Description, Environment.Now);

                            _currentOperation = null;
                            yield break;
                        }
                    }
                    else
                    {
                        if (changeoverTime - changeoverTimeDone > TimeSpan.Zero)
                            throw new Exception("Completed changeover without being interrupted but there is still time left. This should not happen.");

                        _logger.Debug("On {MachineDescription}: Done changing from Tool Id {CurrentToolId} to {NewToolId} at {StartTime}",
                            _machine.Description, CurrentToolId, _currentOperation.WorkPlanPosition.ToolId, Environment.Now);

                        CurrentToolId = _currentOperation.WorkPlanPosition.ToolId;
                        changeoverTime = TimeSpan.Zero;
                    }
                }

                waitTime = _currentOperation.PlannedStart - Environment.Now - changeoverTime;
            }
        }

        private TimeSpan GenerateChangeoverTime(int nextToolId)
        {
            if (_machine.AllowedToolIds == null) throw new Exception("Machine has no allowed tool ids.");
            var rowIndex = _machine.AllowedToolIds.ToList().IndexOf(CurrentToolId);
            var colIndex = _machine.AllowedToolIds.ToList().IndexOf(nextToolId);

            var changeoverTimeMean = TimeSpan.FromMinutes(_machine.ChangeoverTimes[rowIndex][colIndex]);
            var changeoverTime = TimeSpan.Zero;
            if (changeoverTimeMean > TimeSpan.Zero)
            {
                var changeoverTimeDistribution =
                    N(changeoverTimeMean, TimeSpan.FromMinutes(0.05 * changeoverTimeMean.TotalMinutes));
                changeoverTime = Environment.Rand(POS(changeoverTimeDistribution));
            }

            return changeoverTime;
        }

        private IEnumerable<Event> Idle()
        {
            while (_operationQueue.Count == 0)
            {
                yield return Environment.Timeout(TimeSpan.FromDays(1000));

                if (Environment.ActiveProcess.HandleFault())
                {
                    _isProcessInterrupted = false;

                    if (Environment.ActiveProcess.Value is Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
                    {
                        foreach (var interruptEvent in HandleInterrupt(interruptAction))
                            yield return interruptEvent;

                        _machine.State = MachineState.Idle;
                    }
                }
            }
            _currentOperation = _operationQueue.First();
        }

        private IEnumerable<Event> HandleInterrupt(Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
        {
            foreach (var interruptEvent in interruptAction.Invoke(this))
                yield return interruptEvent;

            SimulationEventHandler?.Invoke(this, new InterruptionHandledEvent(Environment.Now, _machine));
            _continueEvent.Wait();
            _continueEvent.Reset();
        }
    }
}
