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
        private List<WorkOperation> _operationQueue;
        private readonly ManualResetEventSlim _planChangedEvent;

        public Guid Id => _machine.Id;
        public string Name => _machine.Name;
        public int PartsMade => _machine.PartsMade;
        public int Capacity { get; set; } = 1;
        public event EventHandler? InterruptEvent;
        private bool isWorking;
        private bool isProcessRunning;
        public MachineModel(Simulation environment, Machine machine, ManualResetEventSlim planChangedEvent) : base(environment)
        {
            _machine = machine;
            _process = environment.Process(Work());
			_operationQueue = new();
            _planChangedEvent = planChangedEvent;
            isWorking = false;
            isProcessRunning = false;
        }

        public void EnqueueOperation(WorkOperation operation)
        {
            _operationQueue.Add(operation);
            _operationQueue.Sort((a,b) => a.EarliestStart.CompareTo(b.EarliestStart));

            if (isProcessRunning && !isWorking)
            {
                // machine is idle or new
                _process.Interrupt();
            }
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

                var waitTime = currentOperation.EarliestStart - Environment.Now;

                while (waitTime > TimeSpan.Zero)
                {
                    yield return Environment.Timeout(waitTime);
                    if (Environment.ActiveProcess.HandleFault() )
                    {
                        currentOperation = _operationQueue.First();
					}
                    waitTime = currentOperation.EarliestStart - Environment.Now;
                }

                currentOperation.State = OperationState.InProgress;
                var durationDistribution = N(currentOperation.Duration, TimeSpan.FromMinutes(0.1 * currentOperation.Duration.TotalMinutes));
                var doneIn = Environment.Rand(POS(durationDistribution));
                var startTime = Environment.Now;
                Console.WriteLine($"Started {currentOperation.WorkPlanPosition.Name} on machine {_machine.Id} at {startTime}.");
				isWorking = true;
				yield return Environment.Timeout(doneIn);

                isWorking = false;
                currentOperation.State = OperationState.Completed;
                Console.WriteLine($"Completed {currentOperation.WorkPlanPosition.Name} at {Environment.Now} (lasted {Environment.Now - startTime}).");
                InterruptEvent?.Invoke(this, new OperationCompletedEvent(Environment.Now, currentOperation));
                _planChangedEvent.Wait();
                _planChangedEvent.Reset();

                _operationQueue.Remove(currentOperation);
            }
        }
    }
}
