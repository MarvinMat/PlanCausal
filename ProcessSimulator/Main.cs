using Controller.Implementation;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using Core.Implementation.Domain;
using Core.Implementation.Events;
using Core.Implementation.Services;
using Planner.Implementation;
using ProcessSim.Implementation;

var rnd = new Random();

Planner.Abstraction.Planner planner = new GifflerThompsonPlanner();

IMachineProvider machineProvider = new MachineProviderJson("../../../../Machines.json");
var machines = machineProvider.Load();

ToolProviderJson toolProvider = new ToolProviderJson("../../../../Tools.json");
var tools = toolProvider.Load();

IWorkPlanProvider workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
var plans = workPlanProvider.Load();

// create 1 order for each plan
var orders = plans.Select(plan => new ProductionOrder()
{
    Name = $"Order {plan.Name}",
    Quantity = 1,
    WorkPlan = plan,
}).ToList();

//orders.ForEach(order => Console.WriteLine($"{order.Name} for {order.Quantity} of {order.WorkPlan.Name}"));

var operations = ModelUtil.GetWorkOperationsFromOrders(orders);


/// <summary>
/// Shifts the start and finish times of the given operation's successors on the same machine and in the global sequence to the right.
/// </summary>
/// <param name="operation">The WorkOperation whose successors' times need to be adjusted.</param>
void RightShiftSuccessors(WorkOperation operation, List<WorkOperation> operationsToSimulate)
{
    var QueuedOperationsOnDelayedMachine = operationsToSimulate.Where(op => op.Machine == operation.Machine).OrderBy(op => op.EarliestStart).ToList();
    // Skip list till you find the current delayed operation, go one further and get the successor
    var successorOnMachine = QueuedOperationsOnDelayedMachine.SkipWhile(op => !op.Equals(operation)).Skip(1).FirstOrDefault();

    UpdateSuccessorTimes(operation, successorOnMachine, operationsToSimulate);
    UpdateSuccessorTimes(operation, operation.Successor, operationsToSimulate);
}

/// <summary>
/// Updates the start and finish times of the successor operation based on the delay caused by the completion of the current operation.
/// </summary>
/// <param name="operation">The current operation which has just been completed.</param>
/// <param name="successor">The successor operation which is dependent on the completion of the current operation.</param>
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


var controller = new SimulationController(operations, machines, planner, new Simulator(rnd.Next(), DateTime.Now));

SimulationController.HandleInterruptEvent eHandler = (e,
                                                      planner,
                                                      simulation,
                                                      currentPlan,
                                                      operationsToSimulate,
                                                      finishedOperations) =>
{
    if (e is ReplanningEvent replanningEvent)
    {
        var newPlan = planner.Schedule(operationsToSimulate, machines, replanningEvent.CurrentDate);
        controller.CurrentPlan = newPlan;
        simulation.SetCurrentPlan(newPlan.Operations);
    }
    if (e is OperationCompletedEvent operationCompletedEvent)
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
            throw new Exception($"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
                $"was just completed but not found in the list of operations to simulate. This should not happen.");

        controller.OperationsToSimulate = operationsToSimulate;

        finishedOperations.Add(completedOperation);
        controller.FinishedOperations = finishedOperations;
    }
};

controller.HandleEvent = eHandler;
controller.Execute(TimeSpan.FromDays(7));

//var plan = planner.Schedule(operations, machines, DateTime.Now);

//Console.WriteLine(plan.ToString());

//ISimulator simulator = new Simulator(123, DateTime.Now);
//simulator.CreateSimulationResources(machines);
//simulator.SetCurrentPlan(plan.Operations);
//simulator.InterruptEvent += (sender, args) =>
//{
//    simulator.Continue();
//};
//simulator.Start(TimeSpan.FromDays(7));

