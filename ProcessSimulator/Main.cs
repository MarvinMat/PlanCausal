using Controller.Implementation;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;
using Core.Implementation.Events;
using Core.Implementation.Services;
using Planner.Abstraction;
using Planner.Implementation;
using ProcessSim.Implementation;

IPlanner planner = new GifflerThompsonPlanner();

IWorkPlanProvider workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
IMachineProvider machineProvider = new MachineProviderJson("../../../../Machines.json");

var plans = workPlanProvider.Load();
var rnd = new Random();
// create 1 order for each plan
var orders = plans.Select(plan => new ProductionOrder()
{
    Name = $"Order {plan.Name}",
    Quantity = 1,
    WorkPlan = plan,
}).ToList();
//var orders = new List<ProductionOrder>();
//orders.Add(new ProductionOrder()
//{
//    Name = $"Order {plans[0].Name}",
//    Quantity = 1,
//    WorkPlan = plans[0],
//});

//orders.ForEach(order => Console.WriteLine($"{order.Name} for {order.Quantity} of {order.WorkPlan.Name}"));

var machines = machineProvider.Load();

var operations = new List<WorkOperation>();

orders.ForEach(productionOrder =>
{
    var workOrders = new List<WorkOrder>();
    for (var i = 0; i < productionOrder.Quantity; i++)
    {
        var workOrder = new WorkOrder(productionOrder);
        var workOrderOperations = new List<WorkOperation>();

        WorkOperation? prevOperation = null;
        productionOrder.WorkPlan.WorkPlanPositions.ForEach(planPosition =>
        {
            var workOperation = new WorkOperation(planPosition, workOrder);

            if (prevOperation is not null)
            {
                prevOperation.Successor = workOperation;
                workOperation.Predecessor = prevOperation;
            }
            prevOperation = workOperation;
            operations.Add(workOperation);
            workOrderOperations.Add(workOperation);
        });
        workOrder.WorkOperations = workOrderOperations;

        workOrders.Add(workOrder);
    }
    productionOrder.WorkOrders = workOrders;
});



/// <summary>
/// Shifts the start and finish times of the given operation's successors on the same machine and in the global sequence to the right.
/// </summary>
/// <param name="operation">The WorkOperation whose successors' times need to be adjusted.</param>
void RightShiftSuccessors(WorkOperation operation, List<WorkOperation> operationsToSimulate)
{
    var QueuedOperationsOnDelayedMachine = Enumerable.OrderBy(operationsToSimulate, op => op.Machine == operation.Machine).OrderBy(op => op.EarliestStart).ToList();
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
void UpdateSuccessorTimes(WorkOperation operation, WorkOperation? successor)
{
    if (successor == null) return;

    var delay = operation.LatestFinish - successor.EarliestStart;

    if (delay > TimeSpan.Zero)
    {
        successor.EarliestStart = successor.EarliestStart.Add(delay);
        successor.LatestStart = successor.LatestStart.Add(delay);
        successor.EarliestFinish = successor.EarliestFinish.Add(delay);
        successor.LatestFinish = successor.LatestFinish.Add(delay);

        RightShiftSuccessors(successor, operations);
    }
}


var controller = new SimulationController(operations, machines, planner, new Simulator(123, DateTime.Now));

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

