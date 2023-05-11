using Planner.Implementation;
using Core.Implementation.Services;
using Planner.Abstraction;
using Core.Abstraction.Services;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;
using Controller.Abstraction;
using Controller.Implementation;

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
    }
});

IController controller = new SimulationController(operations, machines, planner, new Simulator(123, DateTime.Now));
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

//var operationsByMachine = plan.Operations.GroupBy(o => o.Machine);
//foreach (var machineGroup in operationsByMachine)
//{
//    var operationsOnMachine = machineGroup.OrderBy(o => o.EarliestStart).ToList();

//    for (int i = 0; i < operationsOnMachine.Count; i++)
//    {
//        for (int j = i + 1; j < operationsOnMachine.Count; j++)
//        {
//            var operation1 = operationsOnMachine[i];
//            var operation2 = operationsOnMachine[j];

//            if (operation1.EarliestFinish > operation2.EarliestStart && operation1.EarliestStart < operation2.EarliestFinish)
//            {
//                throw new Exception($"Operation {operation1} overlaps with operation {operation2}");
//            }
//        }
//    }
//}

//var operationsByOrder = plan.Operations.GroupBy(o => o.WorkOrder);
//foreach (var orderGroup in operationsByOrder)
//{
//    var operationsOfOrder = orderGroup.OrderBy(o => o.EarliestStart).ToList();

//    for (int i = 0; i < operationsOfOrder.Count; i++)
//    {
//        for (int j = i + 1; j < operationsOfOrder.Count; j++)
//        {
//            var operation1 = operationsOfOrder[i];
//            var operation2 = operationsOfOrder[j];

//            if (operation1.EarliestFinish > operation2.EarliestStart && operation1.EarliestStart < operation2.EarliestFinish)
//            {
//                throw new Exception($"Operation {operation1} overlaps with operation {operation2}");
//            }
//        }
//    }
//}

