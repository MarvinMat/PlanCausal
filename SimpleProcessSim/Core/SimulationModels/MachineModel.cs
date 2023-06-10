using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using SimSharp;
using static SimSharp.Distributions;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    public class MachineModel : ActiveObject<Simulation>
    {
        private readonly Machine _machine;
        private readonly List<WorkOperation> _operationQueue;
        private readonly ManualResetEventSlim _continueEvent;
        public WorkOperation? _currentOperation;
        private bool _isProcessRunning;
        public WorkOperation? CurrentOperation => _currentOperation;
        public MachineState State => _machine.State;
        public Process Process { get; init; }
        public ITimeSeriesMonitor? Utilization { get; set; }
        public ITimeSeriesMonitor? QueueLength { get; set; }
        public ISampleMonitor? LeadTime { get; set; }
        public ISampleMonitor? WaitingTime { get; set; }
        public Guid Id => _machine.Id;
        public string Name => _machine.Name;
        public int CurrentToolId { get; set; }
        public event EventHandler? SimulationEventHandler;
        public MachineModel(Simulation environment, Machine machine, ManualResetEventSlim continueEvent) : base(environment)
        {
            _machine = machine;
            Process = environment.Process(Work());
            _operationQueue = new();
            _continueEvent = continueEvent;
            _machine.State = MachineState.Idle;
            _isProcessRunning = false;
            CurrentToolId = _machine.AllowedToolIds.FirstOrDefault();
        }

        public void EnqueueOperation(WorkOperation operation)
        {
            var previousFirstOperation = _operationQueue.FirstOrDefault();
            _operationQueue.Add(operation);
            QueueLength?.UpdateTo(_operationQueue.Count);
            _operationQueue.Sort((a, b) => a.EarliestStart.CompareTo(b.EarliestStart));

            // interrupt if current operation has changed
            if (_isProcessRunning && State.Equals(MachineState.Idle) && _operationQueue.First() != previousFirstOperation)
            {
                Process.Interrupt();
            }
        }

        public void RemoveOperation(WorkOperation operation)
        {
            var previousFirstOperation = _operationQueue.FirstOrDefault();
            if (!_operationQueue.Remove(operation))
            {
                throw new Exception($"Tried to remove operation {operation.WorkPlanPosition.Name} from " +
                    $"{_machine.Name} ({_machine.Id}) queue, but was not found.");
            }
            QueueLength?.UpdateTo(_operationQueue.Count);
            _operationQueue.Sort((a, b) => a.EarliestStart.CompareTo(b.EarliestStart));

            // interrupt if current operation has changed
            if (_isProcessRunning && State.Equals(MachineState.Idle) && _operationQueue.First() != previousFirstOperation)
            {
                Process.Interrupt();
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
                    if (!Environment.ActiveProcess.IsOk)
                    {
                        if (Environment.ActiveProcess.Value is Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
                        {
                            Environment.ActiveProcess.HandleFault();
                            var previousState = State;
                            _machine.State = MachineState.Interrupted;

							SimulationEventHandler?.Invoke(this, new InterruptionEvent(Environment.Now, _machine));
							_continueEvent.Wait();
							_continueEvent.Reset();

							foreach (var interruptEvent in interruptAction.Invoke(this))
                                yield return interruptEvent;

                            _machine.State = previousState;

                            SimulationEventHandler?.Invoke(this, new InterruptionHandledEvent(Environment.Now, _machine));
                            _continueEvent.Wait();
                            _continueEvent.Reset();
                        }
                    }
                }
                if (Environment.Now - idleTime > TimeSpan.Zero)
                    WaitingTime?.Add((Environment.Now - idleTime).TotalMinutes);
                foreach (var waitingEvent in Changeover())
                {
                    yield return waitingEvent;
					if (!Environment.ActiveProcess.IsOk)
					{
						if (Environment.ActiveProcess.Value is Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
						{
							Environment.ActiveProcess.HandleFault();
							var previousState = State;
							_machine.State = MachineState.Interrupted;

							SimulationEventHandler?.Invoke(this, new InterruptionEvent(Environment.Now, _machine));
							_continueEvent.Wait();
							_continueEvent.Reset();

							foreach (var interruptEvent in interruptAction.Invoke(this))
								yield return interruptEvent;

							_machine.State = previousState;

							SimulationEventHandler?.Invoke(this, new InterruptionHandledEvent(Environment.Now, _machine));
							_continueEvent.Wait();
							_continueEvent.Reset();
						}
					}
				}
                var processStartTime = Environment.Now;
                foreach (var processingEvent in ProcessOrder())
                {
                    yield return processingEvent;
					if (!Environment.ActiveProcess.IsOk)
					{
						if (Environment.ActiveProcess.Value is Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
						{
							Environment.ActiveProcess.HandleFault();
							var previousState = State;
							_machine.State = MachineState.Interrupted;
							
							SimulationEventHandler?.Invoke(this, new InterruptionEvent(Environment.Now, _machine));
							_continueEvent.Wait();
							_continueEvent.Reset();

							foreach (var interruptEvent in interruptAction.Invoke(this))
                                yield return interruptEvent;

                            _machine.State = previousState;

                            SimulationEventHandler?.Invoke(this, new InterruptionHandledEvent(Environment.Now, _machine));
                            _continueEvent.Wait();
                            _continueEvent.Reset();
                        }
					}
				}

                AssessOrderCompletion();
                SimulationEventHandler?.Invoke(this, new OperationCompletedEvent(Environment.Now, _currentOperation));
                
                _continueEvent.Wait();
                _continueEvent.Reset();

                LeadTime?.Add((Environment.Now - processStartTime).TotalMinutes);

                _machine.State = MachineState.Idle;
                _operationQueue.Remove(_currentOperation);

            }
        }
        private IEnumerable<Event> ProcessOrder()
        {
            _currentOperation.State = OperationState.InProgress;
            _currentOperation.WorkOrder.State = OrderState.InProgress;
            _currentOperation.WorkOrder.ProductionOrder.State = OrderState.InProgress;

            _machine.State = MachineState.Working;

            var durationDistribution = N(_currentOperation.Duration,
                TimeSpan.FromMinutes(0.1 * _currentOperation.Duration.TotalMinutes));
            var doneIn = Environment.Rand(POS(durationDistribution));
            var startTime = Environment.Now;

            Console.WriteLine(
                $"Started {_currentOperation.WorkPlanPosition.Name} on machine {_machine.Id} at {startTime} (should have been at {_currentOperation.EarliestStart}).");

            yield return Environment.Timeout(doneIn);

            Console.WriteLine(
                $"Completed {_currentOperation.WorkPlanPosition.Name} at {Environment.Now} (lasted {Environment.Now - startTime:hh\\:mm\\:ss} - was planned {_currentOperation.WorkPlanPosition.Duration:hh\\:mm\\:ss})");

            _currentOperation.State = OperationState.Completed;
        }

        private void AssessOrderCompletion()
        {
            if (_currentOperation?.Successor is not null) return;

            _currentOperation.WorkOrder.State = OrderState.Completed;

            if (_currentOperation.WorkOrder.ProductionOrder.WorkOrders.All(workOrder =>
                    workOrder.State.Equals(OrderState.Completed)))
                _currentOperation.WorkOrder.ProductionOrder.State = OrderState.Completed;
        }

        private IEnumerable<Event> Changeover()
        {
            if (CurrentToolId != _currentOperation?.WorkPlanPosition.ToolId)
            {
                Console.WriteLine(
                  $"On Machine {_machine.Name}: Changing from Tool Id {CurrentToolId} to {_currentOperation.WorkPlanPosition.ToolId} at {Environment.Now}");
            }

            var changeoverTime = GenerateChangeoverTime();
            var waitTime = _currentOperation.EarliestStart - Environment.Now - changeoverTime;

            while (waitTime + changeoverTime > TimeSpan.Zero)
            {
                while (waitTime > TimeSpan.Zero)
                {
                    yield return Environment.Timeout(waitTime);
                    if (Environment.ActiveProcess.HandleFault())
                    {
                        _currentOperation = _operationQueue.First();
                    }
                    changeoverTime = GenerateChangeoverTime();
                    waitTime = _currentOperation.EarliestStart - Environment.Now - changeoverTime;
                }

                while (changeoverTime > TimeSpan.Zero)
                {
                    yield return Environment.Timeout(changeoverTime);
                    if (Environment.ActiveProcess.HandleFault())
                    {
                        _currentOperation = _operationQueue.First();
                    }
                    else
                    {
                        CurrentToolId = _currentOperation.WorkPlanPosition.ToolId;
                    }

                    changeoverTime = GenerateChangeoverTime();
                    waitTime = _currentOperation.EarliestStart - Environment.Now - changeoverTime;
                }
            }
        }

        private TimeSpan GenerateChangeoverTime()
        {
            var rowIndex = _machine.AllowedToolIds.ToList().IndexOf(CurrentToolId);
            var colIndex = _machine.AllowedToolIds.ToList().IndexOf(_currentOperation.WorkPlanPosition.ToolId);

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
               
                Environment.ActiveProcess.HandleFault();
            }
            _currentOperation = _operationQueue.First();
        }
    }
}
