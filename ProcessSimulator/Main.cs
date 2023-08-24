﻿using Controller.Implementation;
using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using Core.Implementation.Domain;
using Core.Implementation.Events;
using Core.Implementation.Services;
using Core.Implementation.Services.Reporting;
using Planner.Implementation;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.SimulationModels;
using Serilog;
using SimSharp;
using System.Text.Json;
using static SimSharp.Distributions;

Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Information().CreateLogger();

var rnd = new Random();

IMachineProvider machineProvider = new MachineProviderJson("../../../../Machines.json");
var machines = machineProvider.Load();

IToolProvider toolProvider = new ToolProviderJson("../../../../Tools.json");
var tools = toolProvider.Load();

IWorkPlanProvider workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
var plans = workPlanProvider.Load();

// create 1 order for each plan
// var orders = plans.Select(plan => new ProductionOrder()
// {
//     Name = $"Order {plan.Name}",
//     Quantity = 20,
//     WorkPlan = plan,
// }).ToList();

var orders = new List<ProductionOrder>()
{
    new ()
    {
        Name = "Order: Tisch bauen",
        Quantity = 20,
        WorkPlan = plans[0]
    },
    new ()
    {
        Name = "Order: Stuhl bauen",
        Quantity = 80,
        WorkPlan = plans[1]

    }
};

var operations = ModelUtil.GetWorkOperationsFromOrders(orders);

var seed = rnd.Next();
Log.Logger.Information("Seed: {Seed}", seed);
var simulator = new Simulator(seed, DateTime.Now);

simulator.ReplanningInterval = TimeSpan.FromHours(8);

IEnumerable<Event> InterruptAction(ActiveObject<Simulation> simProcess)
{
    if (simProcess is MachineModel machineModel)
    {
        var waitFor = POS(N(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)));
        var start = simulator.CurrentSimulationTime;

        Log.Logger.Information("Interrupted {Machine} at {Time}",
            machineModel.Machine.Description, simulator.CurrentSimulationTime);
        yield return simulator.Timeout(waitFor);
        Log.Logger.Information("{Machine} waited {Waited} hours (done at {Time})",
            machineModel.Machine.Description, simulator.CurrentSimulationTime - start, simulator.CurrentSimulationTime);
    }
}

simulator.AddInterrupt(
    predicate: (process) => ((MachineModel)process).Machine.MachineType == 1,
    distribution: EXP(TimeSpan.FromHours(5)),
    interruptAction: InterruptAction
);

Planner.Abstraction.Planner planner = new GifflerThompsonPlanner();

var controller = new SimulationController(operations, machines, planner, simulator);


/// <summary>
/// Shifts the start and finish times of the given operation's successors on the same machine and in the global sequence to the right.
/// </summary>
/// <param name="operation">The WorkOperation whose successors' times need to be adjusted.</param>
/// <param name="operationsToSimulate">The list of all WorkOperations that are yet to be simulated.</param>
void RightShiftSuccessors(WorkOperation operation, List<WorkOperation> operationsToSimulate)
{
    var QueuedOperationsOnDelayedMachine = operationsToSimulate
        .Where(op => op.Machine == operation.Machine)
        .OrderBy(op => op.EarliestStart)
        .ToList();
    // Skip list till you find the current delayed operation, go one further and get the successor
    var successorOnMachine = QueuedOperationsOnDelayedMachine
        .SkipWhile(op => !op.Equals(operation))
        .Skip(1)
        .FirstOrDefault();

    UpdateSuccessorTimes(operation, successorOnMachine, operationsToSimulate);
    UpdateSuccessorTimes(operation, operation.Successor, operationsToSimulate);
}

/// <summary>
/// Updates the start and finish times of the successor operation based on the delay caused by the completion of the current operation.
/// </summary>
/// <param name="operation">The current operation which has just been completed.</param>
/// <param name="successor">The successor operation which is dependent on the completion of the current operation.</param>
/// <param name="operationsToSimulate">The list of all WorkOperations that are yet to be simulated.</param>
void UpdateSuccessorTimes(WorkOperation operation, WorkOperation? successor, List<WorkOperation> operationsToSimulate)
{
    if (successor == null) return;

    var delay = operation.LatestFinish - successor.EarliestStart;

    if (delay > TimeSpan.Zero)
    {
        successor.EarliestStart = successor.EarliestStart.Add(delay);
        successor.LatestStart = successor.LatestStart.Add(delay);
        successor.EarliestFinish = successor.EarliestFinish.Add(delay);
        successor.LatestFinish = successor.LatestFinish.Add(delay);

        RightShiftSuccessors(successor, operationsToSimulate);
    }
}

SimulationController.HandleSimulationEvent eHandler = (e,
    planner,
    simulation,
    currentPlan,
    operationsToSimulate,
    finishedOperations) =>
{
    switch (e)
    {
        case ReplanningEvent replanningEvent when operationsToSimulate.Any():
            {
                Log.Logger.Information("Replanning started at: {CurrentDate}", replanningEvent.CurrentDate);
                var newPlan = planner.Schedule(operationsToSimulate
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                replanningEvent.CurrentDate);
                controller.CurrentPlan = newPlan;
                simulation.SetCurrentPlan(newPlan.Operations);
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
                controller.FinishedOperations = finishedOperations;
                break;
            }
        case InterruptionEvent interruptionEvent:
            {
                // replan without the machines that just got interrupted
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionEvent.CurrentDate);
                controller.CurrentPlan = newPlan;
                simulation.SetCurrentPlan(newPlan.Operations);
                break;
            }
        case InterruptionHandledEvent interruptionHandledEvent:
            {
                // replan with the machine included that just finished its interruption
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionHandledEvent.CurrentDate);
                controller.CurrentPlan = newPlan;
                simulation.SetCurrentPlan(newPlan.Operations);
                break;
            }
    }
};

controller.HandleEvent = eHandler;

controller.Execute(TimeSpan.FromDays(30));

// Console.WriteLine(simulator.GetResourceSummary());
//
// var incompleteOps = operations.Where(op => !op.State.Equals(OperationState.Completed)).ToList().Count;
// var incompleteOrders = orders.Where(order => !order.State.Equals(OrderState.Completed)).ToList().Count;
// Console.WriteLine($"{incompleteOps} operations were not completed.");
// Console.WriteLine($"{incompleteOrders} orders were not completed.");
//
// controller.Feedbacks
//     .OfType<ProductionFeedback>()
//     .ToList()
//     .ForEach(feedback => Console.WriteLine(
//         $"Feedback for Work Order: {feedback.Id} - Created at: {feedback.CreatedAt}\n" +
//         $"Resources: {string.Join(", ", feedback.Resources.Select(r => r.Name))}\n" +
//         $"Is Finished: {feedback.IsFinished}\n" +
//         $"Lead Time: {feedback.LeadTime.TotalMinutes:##.###} minutes - planned: {feedback.WorkOperation.Duration}\n" +
//         $"Produced Parts Count: {feedback.DoneTotal}\n" +
//         $"Associated Work Order: {feedback.WorkOperation.WorkOrder.Name}\n" +
//         $"Associated Production Order State: {feedback.WorkOperation.WorkOrder.ProductionOrder.State}"));
//
// Console.WriteLine("===================================================");
//
// controller.FinishedOperations.ForEach(operation =>
// {
//     Console.WriteLine($"Operation: {operation.WorkPlanPosition.Name}");
//     Console.WriteLine($"Feedbacks: ");
//     operation.Feedbacks.ForEach(feedback =>
//     {
//         if(feedback is ProductionFeedback productionFeedback)
//             Console.WriteLine($"\tId: {productionFeedback.Id}\n" +
//                           $"\tCreated at: {productionFeedback.CreatedAt}\n" +
//                           $"\tMachine used: {productionFeedback.Resources.FirstOrDefault().Name}\n" + //TODO: Needs to be fixed if more than one resource exist
//                           $"\tLead time: {productionFeedback.LeadTime.TotalMinutes:##.##} minutes");
//     });
// });

Log.Logger.Information("Simulation finished at: {SimulationFinishedAt}", DateTime.Now);
Log.Logger.Information("Generated {FeedbacksCount} Feedbacks", controller.Feedbacks.Count);
var stats = new ProductionStats(orders, controller.Feedbacks);

var meanLeadTime = stats.CalculateMeanLeadTimeInMinutes();
Log.Logger.Information("Mean lead time: {MeanLeadTime} minutes", meanLeadTime);

var meanLeadTimeMachine1 = stats.CalculateMeanLeadTimeOfAGivenMachineTypeInMinutes(1);
Log.Logger.Information("Mean lead time of {MachineDescription}: {MeanLeadTimeMachine1} minutes", machines[0].Description, meanLeadTimeMachine1);

Log.CloseAndFlush();


//TODO: refactor this to a separate class and file
var writer = new FeedbackWriter();
await writer.WriteFeedbackToJsonAsync(controller.Feedbacks.OfType<ProductionFeedback>().ToList(), "feedbacks.json");

public class FeedbackWriter
{
    public async Task WriteFeedbackToJsonAsync(List<ProductionFeedback> feedbackList, string filePath)
    {
        string jsonData = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, jsonData);
    }
}