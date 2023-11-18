using Benchmarks;
using Controller.Implementation;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using Core.Implementation.Domain;
using Core.Implementation.Events;
using Core.Implementation.Services;
using Generators.Implementation;
using MathNet.Numerics.Distributions;
using Planner.Implementation;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.SimulationModels;
using Serilog;
using SimSharp;
using static SimSharp.Distributions;

Log.Logger = new LoggerConfiguration()
 .WriteTo.Console()
 .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
 .MinimumLevel.Information()
 .Enrich.FromLogContext()
 .CreateLogger();


// var productionScenario = new ProductionScenario(
//     quantity: 5,
//     new MachineProviderJson($"../../../../Machines.json").Load(),
//     new WorkPlanProviderJson($"../../../../WorkPlans.json").Load());
//
// productionScenario.Run(TimeSpan.FromDays(30));

// var benchmark = BenchmarkRunner.Run<InterruptHandlerBenchmark>();

// #region 11-machines-problem
//
// var machines = new MachineProviderJson("../../../../Machines_11Machines.json").Load();
// var workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans_11Machines.json");
// var workPlans = workPlanProvider.Load();
// var simulator = new Simulator(42, DateTime.Now);
//
// //TODO Extract this into a helper method
// Core.Abstraction.Distribution<WorkPlan> productDistribution = () => {
//     var rnd = new Random().NextDouble();
//     var probabilities = new List<double> { 0.3, 0.1, 0.2, 0.1, 0.3};
// 	var sum = 0.0;
// 	foreach (var (product, probability) in workPlans.Zip(probabilities))
// 	{
// 		sum += probability;
// 		if (rnd < sum)
// 		{
// 			return product;
// 		}
// 	}
// 	throw new ArgumentException("Given ProductDistribution does not sum up to 1");
// };
//
// Core.Abstraction.Distribution<int> quantityDistribution = () => 1;
// var orderGenerator = new OrderGenerator(productDistribution, quantityDistribution);
//
// // var operationsToSimulate = ModelUtil
// //     .GetWorkOperationsFromOrders(new ElevenMachinesProblemOrderGenerator(workPlanProvider).Generate(1))
// //     .ToList();
// var operationsToSimulate = new List<WorkOperation>();
// // adding interrupts for order generation based on 11-machines-problem
//
// IEnumerable<Event> InterruptAction(ActiveObject<Simulation> simProcess)
// {
//     if (simProcess is MachineModel machineModel)
//     {
//         var waitFor = POS(N(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)));
//         var start = simulator.CurrentSimulationTime;
//
//         Log.Logger.Debug("Interrupted {Machine} at {Time}",
//             machineModel.Machine.Description, simulator.CurrentSimulationTime);
//         yield return simulator.Timeout(waitFor);
//         Log.Logger.Debug("{Machine} waited {Waited} hours (done at {Time})",
//             machineModel.Machine.Description, simulator.CurrentSimulationTime - start, simulator.CurrentSimulationTime);
//     }
// }
//
// simulator.AddOrderGeneration(() =>
// {
//     var rnd = new Random().NextDouble();
//     var probabilities = new List<double> { 0.25, 0.60, 0.15 };
//     var interArrivalTimes = new List<TimeSpan> { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30) };
//     var sum = 0.0;
//     foreach (var (time, probability) in interArrivalTimes.Zip(probabilities))
//     {
//         sum += probability;
//         if (rnd < sum)
//         {
//             return time;
//         }
//     }
//
//     throw new ArgumentException("Given interarrival time distribution does not sum up to 1");
// });
//
// // simulator.AddInterrupt(
// //     predicate: (process) => ((MachineModel)process).Machine.MachineType == 1,
// //     distribution: () => TimeSpan.FromHours(new MathNet.Numerics.Distributions.Exponential(1.0 / 5.0).Sample()),
// //     interruptAction: InterruptAction
// // );
//
//
//
// var controller = new SimulationController(operationsToSimulate,
//     machines,
//     new GifflerThompsonPlanner(),
//     simulator);
//
//
// var productionScenario = new ProductionScenario(
//     machines,
//     workPlanProvider.Load(),
//     simulationController: controller,
//     handleSimulationEvent: (e, planningAlgo, simulation, currentPlan, workOperations, finishedOperations) =>
//     {
//         switch (e)
//         {
//             case ReplanningEvent replanningEvent when operationsToSimulate.Any():
//             {
//                 Log.Logger.Debug("Re-planning started at: {CurrentDate}", replanningEvent.CurrentDate);
//                 var newPlan = planningAlgo.Schedule(operationsToSimulate
//                         .Where(op => !op.State.Equals(OperationState.InProgress)
//                                      && !op.State.Equals(OperationState.Completed))
//                         .ToList(),
//                     machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
//                     replanningEvent.CurrentDate);
//                 controller.CurrentPlan = newPlan;
//                 simulation.SetCurrentPlan(newPlan.Operations);
//                 break;
//             }
//             case OperationCompletedEvent operationCompletedEvent:
//             {
//                 var completedOperation = operationCompletedEvent.CompletedOperation;
//
//                 // if it is too late, reschedule the current plan (right shift)
//                 var late = operationCompletedEvent.CurrentDate - completedOperation.PlannedFinish;
//                 if (late > TimeSpan.Zero)
//                 {
//                     completedOperation.PlannedFinish = operationCompletedEvent.CurrentDate;
//                 }
//
//                 if (!operationsToSimulate.Remove(completedOperation))
//                     throw new Exception(
//                         $"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
//                         $"was just completed but not found in the list of operations to simulate. This should not happen.");
//                 finishedOperations.Add(completedOperation);
//                 controller.FinishedOperations = finishedOperations;
//                 break;
//             }
//             case InterruptionEvent interruptionEvent:
//             {
//                 break;
//             }
//             case InterruptionHandledEvent interruptionHandledEvent:
//             {
//                 break;
//             }
//             case OrderGenerationEvent orderGenerationEvent:
//             {
//                 var newOrder = orderGenerator.Generate(1);
//                 var newOperations = ModelUtil.GetWorkOperationsFromOrders(newOrder);
//                 Log.Logger.Information("A new order was generated for {Quantity} of {Product}. It contains {Amount} new operations", newOrder[0].Quantity, newOrder[0].WorkPlan.Name, newOperations.Count);
//                 operationsToSimulate.AddRange(newOperations);
//                 controller.OperationsToSimulate = operationsToSimulate;
//
//                 var newPlan = planningAlgo.Schedule(
//                     operationsToSimulate
//                         .Where(op => !op.State.Equals(OperationState.InProgress)
//                                      && !op.State.Equals(OperationState.Completed))
//                         .ToList(),
//                     machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
//                     orderGenerationEvent.CurrentDate);
//                 controller.CurrentPlan = newPlan;
//                 simulation.SetCurrentPlan(newPlan.Operations);
//                 break;
//             }
//
//         }
//     });
// var simulationDuration = TimeSpan.FromDays(365);
// productionScenario.SimulationController = controller;
// productionScenario.Run(simulationDuration);
// productionScenario.CollectStats();
// var sumOfWaitingTime = simulator.GetWaitingTimeSummaryOfMachines();
//
// var utilization = ( (simulationDuration * simulator.CountOfMachines) - TimeSpan.FromSeconds(sumOfWaitingTime) ) / (simulationDuration * simulator.CountOfMachines);
// Log.Logger.Information("Utilization: {Utilization:F2} %", utilization * 100);
// for (int i = 1; i < simulator.CountOfMachines + 1; i++)
// {
//     var utilizationOfMachineType = (simulationDuration - TimeSpan.FromSeconds(simulator.GetWaitingTimeByMachineType(i))) / simulationDuration;
//     Log.Logger.Information("Utilization of Machine {Machine} is: {Utilization:F2} %", i, utilizationOfMachineType * 100);
// }
//
// #endregion

var scenario = new ProcessSimulator.Scenarios.ProductionScenario("ElevenMachinesProblem", "Test")
 {
  Duration = TimeSpan.FromDays(30),
  Seed = 42,
  StartTime = DateTime.Now
 }
 .WithEntityLoader(new MachineProviderJson($"../../../../Machines_11Machines.json"))
 .WithEntityLoader(new WorkPlanProviderJson($"../../../../Workplans_11Machines.json"));


scenario.Run();
scenario.CollectStats();

Log.CloseAndFlush();