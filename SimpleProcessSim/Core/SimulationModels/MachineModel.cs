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
        private readonly Process _process;
        private readonly List<WorkOperation> _operationQueue;
        private readonly ManualResetEventSlim _continueEvent;

        public Guid Id => _machine.Id;
        public string Name => _machine.Name;

        public int CurrentToolId { get; set; }
        public event EventHandler? InterruptEvent;
        private bool isWorking;
        private bool isProcessRunning;
        public MachineModel(Simulation environment, Machine machine, ManualResetEventSlim continueEvent) : base(environment)
        {
            _machine = machine;
            _process = environment.Process(Work());
			      _operationQueue = new();
            _continueEvent = continueEvent;
            isWorking = false;
            isProcessRunning = false;
            CurrentToolId = _machine.AllowedToolIds.FirstOrDefault();
        }

        public void EnqueueOperation(WorkOperation operation)
        {
            var previousFirstOperation = _operationQueue.FirstOrDefault();
            _operationQueue.Add(operation);
            _operationQueue.Sort((a, b) => a.EarliestStart.CompareTo(b.EarliestStart));

            // interrupt if current operation has changed
            if (isProcessRunning && !isWorking && _operationQueue.First() != previousFirstOperation)
            {
                _process.Interrupt();
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

            _operationQueue.Sort((a, b) => a.EarliestStart.CompareTo(b.EarliestStart));

            // interrupt if current operation has changed
            if (isProcessRunning && !isWorking && _operationQueue.First() != previousFirstOperation)
            {
                _process.Interrupt();
            }
        }

        public bool IsQueued(WorkOperation operation)
        {
            return _operationQueue.Contains(operation);
        }

        private IEnumerable<Event> Work()
        {
            isProcessRunning = true;
            while (true)
            {
                while (_operationQueue.Count == 0)
                {
                    yield return Environment.Timeout(TimeSpan.FromDays(1000));
                    Environment.ActiveProcess.HandleFault();
                }

                var currentOperation = _operationQueue.First();

                if (CurrentToolId != currentOperation.WorkPlanPosition.ToolId)
                {
                    Console.WriteLine($"On Machine {_machine.Name}: Changing from Tool Id {CurrentToolId} to {currentOperation.WorkPlanPosition.ToolId}");
                }

                var rowIndex = _machine.AllowedToolIds.ToList().IndexOf(CurrentToolId);
                var colIndex = _machine.AllowedToolIds.ToList().IndexOf(currentOperation.WorkPlanPosition.ToolId);

                var changeoverTimeMean = TimeSpan.FromMinutes(_machine.ChangeoverTimes[rowIndex][colIndex]);
                var changeoverTime = TimeSpan.Zero;
                if (changeoverTimeMean > TimeSpan.Zero)
                {
                    var changeoverTimeDistribution = N(changeoverTimeMean, TimeSpan.FromMinutes(0.05 * changeoverTimeMean.TotalMinutes));
                    changeoverTime = Environment.Rand(POS(changeoverTimeDistribution));
                }


                var waitTime = currentOperation.EarliestStart - Environment.Now - changeoverTime;

                while (waitTime + changeoverTime > TimeSpan.Zero)
                {
                    while (waitTime > TimeSpan.Zero)
                    {
                        yield return Environment.Timeout(waitTime);
                        if (Environment.ActiveProcess.HandleFault())
                        {
                            currentOperation = _operationQueue.First();
                        }
                        rowIndex = _machine.AllowedToolIds.ToList().IndexOf(CurrentToolId);
                        colIndex = _machine.AllowedToolIds.ToList().IndexOf(currentOperation.WorkPlanPosition.ToolId);
                        changeoverTime = TimeSpan.FromMinutes(_machine.ChangeoverTimes[rowIndex][colIndex]);
                        waitTime = currentOperation.EarliestStart - Environment.Now - changeoverTime;
                    }

                    while (changeoverTime > TimeSpan.Zero)
                    {
                        yield return Environment.Timeout(changeoverTime);
                        if (Environment.ActiveProcess.HandleFault())
                        {
                            currentOperation = _operationQueue.First();
                        }
                        else
                        {
                            CurrentToolId = currentOperation.WorkPlanPosition.ToolId;
                        }
                        rowIndex = _machine.AllowedToolIds.ToList().IndexOf(CurrentToolId);
                        colIndex = _machine.AllowedToolIds.ToList().IndexOf(currentOperation.WorkPlanPosition.ToolId);
                        changeoverTime = TimeSpan.FromMinutes(_machine.ChangeoverTimes[rowIndex][colIndex]);
                        waitTime = currentOperation.EarliestStart - Environment.Now - changeoverTime;
                    }
                }

                currentOperation.State = OperationState.InProgress;
                currentOperation.WorkOrder.State = OrderState.InProgress;
                currentOperation.WorkOrder.ProductionOrder.State = OrderState.InProgress;

                isWorking = true;

                var durationDistribution = N(currentOperation.Duration, TimeSpan.FromMinutes(0.1 * currentOperation.Duration.TotalMinutes));
                var doneIn = Environment.Rand(POS(durationDistribution));
                var startTime = Environment.Now;

                Console.WriteLine($"Started {currentOperation.WorkPlanPosition.Name} on machine {_machine.Id} at {startTime} (should have been at {currentOperation.EarliestStart}).");
                
                yield return Environment.Timeout(doneIn);
                
                Console.WriteLine($"Completed {currentOperation.WorkPlanPosition.Name} at {Environment.Now} (lasted {Environment.Now - startTime:hh\\:mm\\:ss} - was planned {currentOperation.WorkPlanPosition.Duration:hh\\:mm\\:ss})");

                currentOperation.State = OperationState.Completed;
                if (currentOperation.Successor is null)
                {
                    currentOperation.WorkOrder.State = OrderState.Completed;
                    if (currentOperation.WorkOrder.ProductionOrder.WorkOrders.All(workOrder => workOrder.State.Equals(OrderState.Completed)))
                        currentOperation.WorkOrder.ProductionOrder.State = OrderState.Completed;
                }
                
                InterruptEvent?.Invoke(this, new OperationCompletedEvent(Environment.Now, currentOperation));

                _continueEvent.Wait();
                _continueEvent.Reset();

                isWorking = false;
                _operationQueue.Remove(currentOperation);
            }
        }
    }
}
