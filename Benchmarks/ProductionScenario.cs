using Controller.Implementation;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Domain;
using Core.Implementation.Events;
using Core.Implementation.Services.Reporting;
using Generators.Abstraction;
using Generators.Implementation;
using MathNet.Numerics.Distributions;
using Planner.Implementation;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.SimulationModels;
using Serilog;
using SimSharp;
using static SimSharp.Distributions;

namespace Benchmarks;

public class ProductionScenario
{
    private readonly List<Machine> _machines;
    public SimulationController SimulationController { get; set; }
    private List<ProductionOrder>? _orders;
    private readonly IDataGenerator<ProductionOrder> _orderGenerator;
    private readonly ILogger _logger;

    /// <summary>
    ///     Generates a basic production scenario with a given quantity of products.
    /// </summary>
    /// <param name="machines">A list of available machines.</param>
    /// <param name="workPlans">A list of available work plans.</param>
    /// <param name="handleSimulationEvent">A Delegate of <see cref="SimulationEventHandler"/> that represents the logic for the controller component. Use this handler to react to several events emitted by the simulation. Consider the Core.Implementation.Events namespace for all relevant events. </param>
    /// <param name="sim">The instance of a simulator.</param>
    /// <param name="dataGenerator">A generator for data used while the simulation is running.</param>
    /// <param name="simulationController"></param>
    public ProductionScenario(List<Machine> machines, IEnumerable<WorkPlan> workPlans,
        SimulationController.HandleSimulationEvent? handleSimulationEvent = null, Simulator? sim = null,
        IDataGenerator<ProductionOrder>? dataGenerator = null, SimulationController? simulationController = null)
    {
        var rnd = new Random();
        var seed = rnd.Next();
        _logger = Log.ForContext<ProductionScenario>();
        _machines = machines;
        var simulator = sim ?? new Simulator(seed, DateTime.Now);

        WorkPlan ProductDistribution()
        {
            var workPlanList = workPlans.ToList();
            if (workPlanList.Count == 0)
            {
                // Handle the case where the list is empty
                throw new InvalidOperationException("The list of work plans is empty.");
            }

            return workPlanList[rnd.Next(workPlanList.Count)];
        }

        _orderGenerator = dataGenerator ?? new OrderGenerator(ProductDistribution, QuantityDistribution);
        
        _logger.Information("Seed: {Seed}", seed);
        _logger.Information("Simulation started at: {SimulationStartTime}", DateTime.Now);
        
        var planner = new GifflerThompsonPlanner();
        
        var workOperations = ModelUtil.GetWorkOperationsFromOrders(_orders?.ToList() ?? new List<ProductionOrder>());
        
        SimulationController = simulationController ?? new SimulationController(workOperations.ToList(), _machines, planner, simulator);
        
        // apply default behaviour
        if (handleSimulationEvent is null)
            SimulationController.HandleEvent += SimulationEventHandler;
        SimulationController.HandleEvent += handleSimulationEvent;
        return;

        int QuantityDistribution()
        {
            var distribution = new Geometric(1.0 / 2.5);
            return distribution.Sample();
        }
    }


    public void Run(TimeSpan simulationDuration)
    {
        SimulationController.Execute(simulationDuration);
        _logger.Information("Simulation finished at: {SimulationEndTime}", DateTime.Now);
        _logger.Information(" Generated {FeedbacksCount} feedbacks", SimulationController.Feedbacks.Count);
    }
    
    public void CollectStats()
    {
        // TODO: change parameter
        var stats = new ProductionStats(new List<ProductionOrder>(), SimulationController.Feedbacks);

        var meanLeadTime = stats.CalculateMeanLeadTimeInMinutes();
        _logger.Information("Mean lead time: {MeanLeadTime:F2} minutes", meanLeadTime);

        var meanLeadTimeMachine1 = stats.CalculateMeanLeadTimeOfAGivenMachineTypeInMinutes(1);
        _logger.Information("Mean lead time of machine 1: {MeanLeadTimeMachine1:F2} minutes", meanLeadTimeMachine1);
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
                SimulationController.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
            case OperationCompletedEvent operationCompletedEvent:
            {
                var completedOperation = operationCompletedEvent.CompletedOperation;

                if (!operationsToSimulate.Remove(completedOperation))
                    throw new Exception(
                        $"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
                        $"was just completed but not found in the list of operations to simulate. This should not happen.");
                finishedOperations.Add(completedOperation);
                SimulationController.FinishedOperations = finishedOperations;
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
                SimulationController.CurrentPlan = newPlan;
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
                SimulationController.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
            case OrderGenerationEvent orderGenerationEvent:
            {
                var newOrder = _orderGenerator.Generate(1);
                var newOperations = ModelUtil.GetWorkOperationsFromOrders(newOrder);
                Log.Logger.Information("A new order was generated for {Quantity} of {Product}. It contains {Amount} new operations", newOrder[0].Quantity, newOrder[0].WorkPlan.Name, newOperations.Count);
                operationsToSimulate.AddRange(newOperations);
                SimulationController.OperationsToSimulate = operationsToSimulate;

                var newPlan = planner.Schedule(
                    operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    orderGenerationEvent.CurrentDate);
                SimulationController.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
        }
    }
}


