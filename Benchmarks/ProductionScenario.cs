using System.ComponentModel;
using Controller.Implementation;
using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Domain;
using Core.Implementation.Events;
using Core.Implementation.Services;
using Core.Implementation.Services.Reporting;
using Planner.Implementation;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.SimulationModels;
using Serilog;
using Serilog.Core;
using SimSharp;
using static SimSharp.Distributions;

namespace Benchmarks;

public class ProductionScenario
{
    private readonly List<Machine> _machines;
    private readonly SimulationController _controller;
    private readonly IEnumerable<ProductionOrder>? _orders;
    private readonly Simulator _simulator;
    private readonly ILogger _logger;

    /// <summary>
    ///     Generates a basic production scenario with a given quantity of products.
    /// </summary>
    /// <param name="quantity"> The amount of each product to be produced.</param>
    /// <param name="machines"></param>
    /// <param name="workPlans"></param>
    public ProductionScenario(int quantity, List<Machine> machines, IEnumerable<WorkPlan> workPlans)
    {
        _logger = Log.ForContext<ProductionScenario>();
        
        var rnd = new Random();
        var seed = rnd.Next();
        _logger.Information("Seed: {Seed}", seed);
        
        _simulator = new Simulator(seed, DateTime.Now);
        _logger.Information("Simulation started at: {SimulationStartTime}", DateTime.Now);
        
        var planner = new GifflerThompsonPlanner();
        _machines = machines;
        
        _orders = workPlans?.Select(plan => new ProductionOrder()
        {
            Name = $"Order {plan.Name}",
            Quantity = quantity,
            WorkPlan = plan,
        }).ToList();

        var workOperations = ModelUtil.GetWorkOperationsFromOrders(_orders?.ToList() ?? new List<ProductionOrder>());
        
        _controller = new SimulationController(workOperations.ToList(), _machines, planner, _simulator);
        _controller.HandleEvent += SimulationEventHandler;
        
        _simulator.AddInterrupt(
            predicate: (process) => true,
            distribution: EXP(TimeSpan.FromHours(5)),
            interruptAction: InterruptAction
        );
        
    }
    public void Run(TimeSpan simulationDuration)
    {
        _controller.Execute(simulationDuration);
        _logger.Information("Simulation finished at: {SimulationEndTime}", DateTime.Now);
        _logger.Information(" Generated {FeedbacksCount} feedbacks", _controller.Feedbacks.Count);
    }
    
    public void CollectStats()
    {
        if (_orders == null) return;
        var stats = new ProductionStats(_orders.ToList(), _controller.Feedbacks);

        var meanLeadTime = stats.CalculateMeanLeadTimeInMinutes();
        _logger.Information("Mean lead time: {MeanLeadTime} minutes", meanLeadTime);

        var meanLeadTimeMachine1 = stats.CalculateMeanLeadTimeOfAGivenMachineTypeInMinutes(1);
        _logger.Information("Mean lead time of machine 1: {MeanLeadTimeMachine1} minutes", meanLeadTimeMachine1);
    }

    private void SimulationEventHandler(EventArgs e, 
        Planner.Abstraction.Planner planner, 
        ISimulator simulator, 
        Plan currentPlan, 
        List<WorkOperation> operationsToSimulate, 
        List<WorkOperation> finishedOperations)
    {
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
            {
                _logger.Information("Replanning started at: {ReplanningStartTime}", replanningEvent.CurrentDate);
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    replanningEvent.CurrentDate);
                _controller.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
            case OperationCompletedEvent operationCompletedEvent:
            {
                var completedOperation = operationCompletedEvent.CompletedOperation;

                // if it is too late, reschedule the current plan (right shift)
                var late = operationCompletedEvent.CurrentDate - completedOperation.LatestFinish;
                if (late > TimeSpan.Zero)
                {
                    completedOperation.LatestFinish = operationCompletedEvent.CurrentDate;
                    RightShiftSuccessors(completedOperation, operationsToSimulate);
                }

                if (!operationsToSimulate.Remove(completedOperation))
                    throw new Exception(
                        $"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
                        $"was just completed but not found in the list of operations to simulate. This should not happen.");
                finishedOperations.Add(completedOperation);
                _controller.FinishedOperations = finishedOperations;
                break;
            }
            case InterruptionEvent interruptionEvent:
            {
                // replan without the machines that just got interrupted
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionEvent.CurrentDate);
                _controller.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
            case InterruptionHandledEvent interruptionHandledEvent:
            {
                // replan with the machine included that just finished its interruption
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionHandledEvent.CurrentDate);
                _controller.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
        }
    }
    
    /// <summary>
    ///     Shifts the start and finish times of the given operation's successors on the same machine and in the global sequence to the right.
    /// </summary>
    /// <param name="operation">The WorkOperation whose successors' times need to be adjusted.</param>
    /// <param name="operationsToSimulate">The list of all WorkOperations that are yet to be simulated.</param>
    private void RightShiftSuccessors(WorkOperation operation, List<WorkOperation> operationsToSimulate)
    {
        var queuedOperationsOnDelayedMachine = operationsToSimulate.Where(op => op.Machine == operation.Machine)
            .OrderBy(op => op.EarliestStart).ToList();
        // Skip list till you find the current delayed operation, go one further and get the successor
        // var successorOnMachine = queuedOperationsOnDelayedMachine.SkipWhile(op => !op.Equals(operation)).Skip(1)
        //    .FirstOrDefault();
        WorkOperation? successorOnMachine = null;
        
        var currentIndex = queuedOperationsOnDelayedMachine.FindIndex(op => op.Equals(operation));
        if (currentIndex >= 0 && currentIndex + 1 < queuedOperationsOnDelayedMachine.Count)
        {
            successorOnMachine = queuedOperationsOnDelayedMachine[currentIndex + 1];
            UpdateSuccessorTimes(operation, successorOnMachine, operationsToSimulate);
        }

        if (operation.Successor == null) return;
        
        UpdateSuccessorTimes(operation, successorOnMachine, operationsToSimulate);
        UpdateSuccessorTimes(operation, operation.Successor, operationsToSimulate);
    }

    /// <summary>
    ///     Updates the start and finish times of the successor operation based on the delay caused by the completion of the current operation.
    /// </summary>
    /// <param name="operation">The current operation which has just been completed.</param>
    /// <param name="successor">The successor operation which is dependent on the completion of the current operation.</param>
    /// <param name="operationsToSimulate">The list of all WorkOperations that are yet to be simulated.</param>
    private void UpdateSuccessorTimes(WorkOperation operation, WorkOperation? successor, List<WorkOperation> operationsToSimulate)
    {
        if (successor == null) return;

        var delay = operation.LatestFinish - successor.EarliestStart;

        if (delay <= TimeSpan.Zero) return;
        successor.EarliestStart = successor.EarliestStart.Add(delay);
        successor.LatestStart = successor.LatestStart.Add(delay);
        successor.EarliestFinish = successor.EarliestFinish.Add(delay);
        successor.LatestFinish = successor.LatestFinish.Add(delay);

        RightShiftSuccessors(successor, operationsToSimulate);
    }
    
    private IEnumerable<Event> InterruptAction(ActiveObject<Simulation> simProcess)
    {
        if (simProcess is MachineModel machineModel)
        {
            var waitFor = 2;
            var start = _simulator.CurrentSimulationTime;

            _logger.Information("Interrupted machine {Machine} at {CurrentSimulationTime}: Waiting {WaitFor} hours",
                machineModel.Machine.Description, _simulator.CurrentSimulationTime, waitFor);
            yield return _simulator.Timeout(TimeSpan.FromHours(waitFor));

            _logger.Information(
                "Machine {Machine} waited {Waited} (done at {CurrentSimulationTime})",
                machineModel.Machine.Description, _simulator.CurrentSimulationTime - start, _simulator.CurrentSimulationTime);
        }
    }
    
}


