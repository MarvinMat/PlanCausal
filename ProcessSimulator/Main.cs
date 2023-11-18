using Controller.Implementation;
using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Customers;
using CoreAbstraction = Core.Abstraction;
using Core.Implementation.Services;
using Core.Implementation.Services.Reporting;
using Generators.Implementation;
using ProcessSim.Abstraction;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.Interrupts;
using ProcessSim.Implementation.Core.SimulationModels;
using ProcessSimulator;
using ProcessSimulator.Scenarios;
using Serilog;
using SimSharp;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Resources;
using Generators.Implementation;
using static SimSharp.Distributions;
using MathNet.Numerics.Distributions;

// Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Information().CreateLogger();
//
// var rnd = new Random();
//
// IEntityLoader<Machine> machineProvider = new MachineProviderJson("../../../../Machines.json");
// var machines = machineProvider.Load();
//
// IEntityLoader<Tool> toolProvider = new ToolProviderJson("../../../../Tools.json");
// var tools = toolProvider.Load();
//
// IEntityLoader<WorkPlan> workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
// var workPlans = workPlanProvider.Load();
//
// Core.Abstraction.Distribution<WorkPlan> productDistribution = () => workPlans[rnd.Next(workPlans.Count)];
//
// Core.Abstraction.Distribution<int> quantityDistribution = () => {
//     var distribution = new Geometric(1.0/2.5);
// 	return distribution.Sample();
// };
//
// var orderGenerator = new OrderGenerator(productDistribution, quantityDistribution);
//
// var orders = orderGenerator.Generate(5);
//
// var operations = ModelUtil.GetWorkOperationsFromOrders(orders);
//
// Log.Logger.Information("Simulation started at: {SimulationStartedAt}", DateTime.Now);
// Log.Logger.Information("Operations count: {OperationsCount}", operations.Count);
// Log.Logger.Debug("Following orders were generated:");
// Log.Logger.Debug("{Orders}", JsonSerializer.Serialize(orders, new JsonSerializerOptions
// {
//     WriteIndented = true,
//     ReferenceHandler = ReferenceHandler.Preserve
// }));
//
// var seed = rnd.Next();
// Log.Logger.Debug("Seed: {Seed}", seed);
// var simulator = new Simulator(seed, DateTime.Now);
//
// simulator.ReplanningInterval = TimeSpan.FromHours(8);
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
// simulator.AddInterrupt(
//     predicate: (process) => ((MachineModel)process).Machine.MachineType == 1,
//     distribution: () => TimeSpan.FromHours(new MathNet.Numerics.Distributions.Exponential(1.0 / 10.0).Sample()),
//     interruptAction: InterruptAction
// );
//
// simulator.AddOrderGeneration(() => TimeSpan.FromHours(new MathNet.Numerics.Distributions.Exponential(1.0 / 24.0).Sample()));
//
// Planner.Abstraction.Planner planner = new GifflerThompsonPlanner();
//
// var controller = new SimulationController(operations, machines, planner, simulator);
//
// SimulationController.HandleSimulationEvent eHandler = (e,
//     planningAlgo,
//     simulation,
//     currentPlan,
//     operationsToSimulate,
//     finishedOperations) =>
// {
//     switch (e)
//     {
//         case ReplanningEvent replanningEvent when operationsToSimulate.Any():
//         {
//              Log.Logger.Debug("Replanning started at: {CurrentDate}", replanningEvent.CurrentDate);
//              var newPlan = planningAlgo.Schedule(
//                  operationsToSimulate
//                      .Where(op => !op.State.Equals(OperationState.InProgress)
//                                   && !op.State.Equals(OperationState.Completed))
//                      .ToList(),
//                  machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
//                  replanningEvent.CurrentDate);
//              controller.CurrentPlan = newPlan;
//              simulation.SetCurrentPlan(newPlan.Operations);
//              break;
//         }
//         case OperationCompletedEvent operationCompletedEvent:
//         {
//             var completedOperation = operationCompletedEvent.CompletedOperation;
//
//             if (!operationsToSimulate.Remove(completedOperation))
//                 throw new Exception(
//                     $"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
//                     $"was just completed but not found in the list of operations to simulate. This should not happen.");
//             finishedOperations.Add(completedOperation);
//             controller.FinishedOperations = finishedOperations;
//             break;
//         }
//         case InterruptionEvent interruptionEvent:
//         {
//             // replan without the machines that just got interrupted
//             var newPlan = planningAlgo.Schedule(
//                  operationsToSimulate
//                      .Where(op => !op.State.Equals(OperationState.InProgress)
//                                   && !op.State.Equals(OperationState.Completed))
//                      .ToList(),
//                  machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
//                  interruptionEvent.CurrentDate);
//             controller.CurrentPlan = newPlan;
//             simulation.SetCurrentPlan(newPlan.Operations);
//             break;
//         }
//         case InterruptionHandledEvent interruptionHandledEvent:
//         {
//             // replan with the machine included that just finished its interruption
//             Log.Logger.Debug("Replanning started at: {CurrentDate}", interruptionHandledEvent.CurrentDate);
//             var newPlan = planningAlgo.Schedule(
//                 operationsToSimulate
//                     .Where(op => !op.State.Equals(OperationState.InProgress)
//                                  && !op.State.Equals(OperationState.Completed))
//                     .ToList(),
//                 machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
//                 interruptionHandledEvent.CurrentDate);
//             controller.CurrentPlan = newPlan;
//             simulation.SetCurrentPlan(newPlan.Operations);
//             break;
//         }
//         case OrderGenerationEvent orderGenerationEvent:
//         {
//             var newOrder = orderGenerator.Generate(1);
//             var newOperations = ModelUtil.GetWorkOperationsFromOrders(newOrder);
//             Log.Logger.Information("A new order was generated for {Quantity} of {Product}. It contains {Amount} new operations.", newOrder[0].Quantity, newOrder[0].WorkPlan.Name, newOperations.Count);
//             operationsToSimulate.AddRange(newOperations);
//             controller.OperationsToSimulate = operationsToSimulate;
//
//             var newPlan = planningAlgo.Schedule(
//                 operationsToSimulate
//                     .Where(op => !op.State.Equals(OperationState.InProgress)
//                               && !op.State.Equals(OperationState.Completed))
//                     .ToList(),
//                 machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
//                 orderGenerationEvent.CurrentDate);
//             controller.CurrentPlan = newPlan;
//             simulation.SetCurrentPlan(newPlan.Operations);
//             break;
//         }
//     }
// };
//
// controller.HandleEvent = eHandler;
//
// controller.Execute(TimeSpan.FromDays(30));
//
// Log.Logger.Information("Simulation finished at: {SimulationFinishedAt}", DateTime.Now);
// Log.Logger.Information("Generated {FeedbacksCount} Feedbacks", controller.Feedbacks.Count);
// var stats = new ProductionStats(orders, controller.Feedbacks);
//
// var meanLeadTime = stats.CalculateMeanLeadTimeInMinutes();
// Log.Logger.Information("Mean lead time: {MeanLeadTime} minutes", meanLeadTime);
//
// var meanLeadTimeMachine1 = stats.CalculateMeanLeadTimeOfAGivenMachineTypeInMinutes(1);
// Log.Logger.Information("Mean lead time of {MachineDescription}: {MeanLeadTimeMachine1} minutes",
//     machines[0].Description, meanLeadTimeMachine1);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .CreateLogger();


var scenario = new ProductionScenario("ElevenMachinesProblem", "Test")
{
    Duration = TimeSpan.FromDays(30),
    Seed = 42,
    RePlanningInterval = TimeSpan.FromHours(8),
    StartTime = DateTime.Now,
    InitialCustomerOrdersGenerated = 5
}
    .WithEntityLoader(new MachineProviderJson($"../../../../Machines_11Machines.json"))
    .WithEntityLoader(new WorkPlanProviderJson($"../../../../Workplans_11Machines.json"))
    .WithEntityLoader(new CustomerProviderJson("../../../../Customers.json"))
    .WithInterrupt(predicate: process => ((MachineModel)process).Machine.MachineType is 1 or 11, distribution:
        CoreAbstraction.Distributions.ConstantDistribution(TimeSpan.FromHours(4)), interruptAction: InterruptAction)
    .WithOrderGenerationFrequency(
        CoreAbstraction.Distributions.DiscreteDistribution(
            new List<TimeSpan> { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30) }, 
            new List<double> { 0.25, 0.60, 0.15 }
        )
    )
    .WithReporting(".");

IEnumerable<Event> InterruptAction(ActiveObject<Simulation> simProcess, IScenario productionScenario)
{
    if (productionScenario is not ProductionScenario prodScenario)
        throw new NullReferenceException("Scenario is null.");
    if (prodScenario.Simulator is not Simulator simulator)
        throw new NullReferenceException("Simulator is null.");
    
    if (simProcess is MachineModel machineModel)
    {
        var waitFor = POS(N(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)));
        var start = simulator.CurrentSimulationTime;

        Log.Logger.Warning("Interrupted {Machine} at {Time}",
            machineModel.Machine.Description, simulator.CurrentSimulationTime);
        yield return simulator.Timeout(waitFor);
        Log.Logger.Warning("{Machine} waited {Waited} hours (done at {Time})",
            machineModel.Machine.Description, simulator.CurrentSimulationTime - start, simulator.CurrentSimulationTime);
    }
}

scenario.Run();
scenario.CollectStats();


Log.CloseAndFlush();
