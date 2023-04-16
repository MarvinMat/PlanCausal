using Controller.Abstraction;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;

namespace Controller.Implementation;

public class SimulationController : IController
{
    private readonly ISimulator _simulation;
    private readonly IEnumerable<Machine> _machines;
    public event EventHandler? RescheduleEvent;
    public List<WorkOperation> PlannedOperations { get; set; }
    public List<WorkOperation> FinishedOperations { get; set; }

    public SimulationController(
        List<WorkOperation> plannedOperations,
        IEnumerable<Machine> machines
        )
    {
        PlannedOperations = plannedOperations;
        FinishedOperations = new List<WorkOperation>();
        _machines = machines;
        _simulation = new Simulator(42, DateTime.Now);
        _simulation.InterruptEvent += InterruptHandler;
        _simulation.CreateSimulationResources(machines);
    }

    public void Execute(TimeSpan duration)
    {
        _simulation.Start(duration);
    }

    protected virtual void OnReschedule(EventArgs eventArgs)
    {
        RescheduleEvent?.Invoke(this, eventArgs);
    }
    /// <summary>
    /// Handles events that interrupt the scheduling process, such as replanning or completion of an operation.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private void InterruptHandler(object? sender, EventArgs e)
    {
        if (e is ReplanningEvent replanningEvent)
        {
            OnReschedule(replanningEvent);
        }
        if (e is OperationCompletedEvent operationCompletedEvent)
        {
            FinishedOperations.Add(operationCompletedEvent.CompletedOperation);

            //TODO if this is too late, reschedule the current plan (right shift)
            var late = operationCompletedEvent.CurrentDate - operationCompletedEvent.CompletedOperation.LatestFinish;
            if (late > TimeSpan.Zero)
            {
                operationCompletedEvent.CompletedOperation.LatestFinish = operationCompletedEvent.CurrentDate;
                RightShift(operationCompletedEvent.CompletedOperation);
            }

            PlannedOperations.Remove(operationCompletedEvent.CompletedOperation);
            _simulation.Continue();
        }
    }
    /// <summary>
    /// Shifts the start and finish times of the given operation's successors on the same machine and in the global sequence.
    /// </summary>
    /// <param name="operation">The WorkOperation whose successors' times need to be adjusted.</param>
    private void RightShift(WorkOperation operation)
    {
        var QueuedOperationsOnDelayedMachine = PlannedOperations.Where(op => op.Machine == operation.Machine && !op.Equals(operation)).OrderBy(op => op.LatestStart).ToList();
        // Skip list till you find the current delayed operation, go one further and get the successor
        var successorOnMachine = QueuedOperationsOnDelayedMachine.SkipWhile(op => !op.Equals(operation)).Skip(1).FirstOrDefault();

        UpdateSuccessorTimes(operation, successorOnMachine);
        UpdateSuccessorTimes(operation, operation.Successor);
    }

    /// <summary>
    /// Updates the start and finish times of the successor operation based on the delay caused by the completion of the current operation.
    /// </summary>
    /// <param name="operation">The current operation which has just been completed.</param>
    /// <param name="successor">The successor operation which is dependent on the completion of the current operation.</param>
    private void UpdateSuccessorTimes(WorkOperation operation, WorkOperation? successor)
    {
        if (successor == null) return;

        var delay = successor.EarliestStart - operation.LatestFinish;

        if (delay > TimeSpan.Zero)
        {
            successor.EarliestStart = successor.EarliestStart.Add(delay);
            successor.LatestStart = successor.LatestStart.Add(delay);
            successor.EarliestFinish = successor.EarliestFinish.Add(delay);
            successor.LatestFinish = successor.LatestFinish.Add(delay);

            RightShift(successor);
        }
    }
}

