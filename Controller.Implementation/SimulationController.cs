using Controller.Abstraction;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using Planner.Abstraction;
using ProcessSim.Abstraction.Domain.Interfaces;

namespace Controller.Implementation;

public class SimulationController : IController
{
    private readonly ISimulator _simulation;
    private readonly IPlanner _planner;
    private readonly List<Machine> _machines;
    private List<WorkOperation> OperationsToSimulate { get; set; }
    private List<WorkOperation> FinishedOperations { get; set; }
    private Plan CurrentPlan { get; set; }

    public SimulationController(
        List<WorkOperation> operationsToSimulate,
        List<Machine> machines,
        IPlanner planner,
        ISimulator simulator
        )
    {
        OperationsToSimulate = operationsToSimulate;
        FinishedOperations = new List<WorkOperation>();

        _machines = machines;

        _planner = planner;
        CurrentPlan = _planner.Schedule(OperationsToSimulate, machines, DateTime.Now);

        _simulation = simulator;
        _simulation.InterruptEvent += InterruptHandler;
        _simulation.CreateSimulationResources(machines);
    }

    public void Execute(TimeSpan duration)
    {
        _simulation.SetCurrentPlan(CurrentPlan.Operations);
        _simulation.Start(duration);
    }

    protected virtual void OnReschedule(EventArgs eventArgs)
    {
        CurrentPlan = _planner.Schedule(OperationsToSimulate, _machines, DateTime.Now);
        _simulation.SetCurrentPlan(CurrentPlan.Operations);
    }

    /// <summary>
    /// Handles events that interrupt the simulation process, such as replanning or completion of an operation.
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
            var completedOperation = operationCompletedEvent.CompletedOperation;

            // if it is too late, reschedule the current plan (right shift)
            var late = operationCompletedEvent.CurrentDate - completedOperation.LatestFinish;
            if (late > TimeSpan.Zero)
            {
                completedOperation.LatestFinish = operationCompletedEvent.CurrentDate;
                RightShiftSuccessors(completedOperation);
            }

            if (!OperationsToSimulate.Remove(completedOperation))
                throw new Exception($"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
                    $"was just completed but not found in the list of operations to simulate. This should not happen.");

            FinishedOperations.Add(completedOperation);
        }

        _simulation.Continue();
    }
    /// <summary>
    /// Shifts the start and finish times of the given operation's successors on the same machine and in the global sequence to the right.
    /// </summary>
    /// <param name="operation">The WorkOperation whose successors' times need to be adjusted.</param>
    private void RightShiftSuccessors(WorkOperation operation)
    {
        var QueuedOperationsOnDelayedMachine = OperationsToSimulate.Where(op => op.Machine == operation.Machine).OrderBy(op => op.EarliestStart).ToList();
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

        var delay =  operation.LatestFinish - successor.EarliestStart;

        if (delay > TimeSpan.Zero)
        {
            successor.EarliestStart = successor.EarliestStart.Add(delay);
            successor.LatestStart = successor.LatestStart.Add(delay);
            successor.EarliestFinish = successor.EarliestFinish.Add(delay);
            successor.LatestFinish = successor.LatestFinish.Add(delay);

            RightShiftSuccessors(successor);
        }
    }
}

