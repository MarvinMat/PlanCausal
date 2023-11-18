using Controller.Abstraction;
using Controller.Implementation;
using Core.Abstraction;
using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Customers;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;
using Core.Implementation.Domain;
using Core.Implementation.Events;
using Core.Implementation.Services.Reporting;
using Generators.Abstraction;
using Generators.Implementation;
using Planner.Implementation;
using ProcessSim.Abstraction;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.Interrupts;
using Serilog;
using ActiveObject = SimSharp.ActiveObject<SimSharp.Simulation>;
using Event = SimSharp.Event;

namespace ProcessSimulator.Scenarios;

public class ProductionScenario : IScenario
{
    private readonly ILogger _logger;
    private readonly List<IEntityLoader>? _entityLoaders;
    private readonly List<IResource> _resources;
    private readonly List<InterruptInfo> _interrupts = new();
    private readonly List<Distribution<TimeSpan>> _orderGenerationFrequencies = new();
    private readonly HashSet<IDataGenerator> _generators = new();
    public List<string> ReportingFolderPaths = new();

    private Planner.Abstraction.Planner? _planner;
    private List<WorkPlan> _workPlans;
    private List<Tool> _tools;
    private Dictionary<Guid, Customer> _customers;

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get;  }
    public string Description { get;  }
    public int Seed { get; set; }
    public DateTime? StartTime { get; set; }
    public TimeSpan Duration { get; init; }
    public TimeSpan RePlanningInterval { get; init; }
    
    /// <summary>
    /// Defines the amount of customer orders that are generated at the beginning of the simulation
    /// </summary>
    public int InitialCustomerOrdersGenerated { get; init; }

    public IController? Controller { get; private set; }

    public ISimulator? Simulator { get; private set; }

    /// <summary>
    /// Represents a simulation configuration. 
    /// </summary>
    /// <param name="name">The name of the scenario</param>
    /// <param name="description">Describes the scenario</param>
    /// <param name="entityLoaders">List of loaders that a necessary for the scenario</param>
    public ProductionScenario(string name, string description, List<IEntityLoader>? entityLoaders = null)
    {
        Name = name;
        Description = description;
        _logger = Log.ForContext<ProductionScenario>();
        _entityLoaders = _entityLoaders is null ? new List<IEntityLoader>() : entityLoaders;
        
        _resources = new List<IResource>();
        _workPlans = new List<WorkPlan>();
        _tools = new List<Tool>();
        _customers = new Dictionary<Guid, Customer>();
    }
    
    public void Run()
    {
        _logger.Debug("Trying to load necessary entities");
        LoadEntities();
        _logger.Debug("Loading entities done");
        
        if (Simulator is null)
        {
            _logger.Warning("The simulator is not set");
            
            if (Seed is 0) Seed = 42;
            StartTime ??= DateTime.Now;
            Simulator = new Simulator(Seed, DateTime.Now)
                { ReplanningInterval = RePlanningInterval};
            _logger.Information("Using the default simulator with Seed: {Seed} and date: {Date}", Seed, DateTime.Now);
        }
        
        if (_planner is null)
        {
            _logger.Warning("The planner is not set");
            _planner = new GifflerThompsonPlanner();
            _logger.Information("Using the default planner");
        }
        
        if (Controller is null)
        {
            _logger.Warning("The controller is not set");
            var machines = _resources.OfType<Machine>().ToList();
            if (!machines.Any()) throw new InvalidOperationException("The list of machines is empty.");
            var simulationController = new SimulationController(new List<WorkOperation>(), machines, _planner, Simulator);
            simulationController.HandleEvent += SimulationEventHandler;
            Controller = simulationController;
            _logger.Information("Using the default controller");
        }

        if (!_generators.OfType<IDataGenerator<ProductionOrder>>().Any())
        {
            _logger.Warning("The production generator is not set");

            var probabilities = new List<double>
                {
                    0.3,
                    0.1,
                    0.2,
                    0.1,
                    0.3
                };
            var productDistribution = Distributions.DiscreteDistribution(_workPlans, probabilities);

            var quantityDistribution = Distributions.ConstantDistribution(1);

            _generators.Add(new OrderGenerator
            {
                ProductDistribution = productDistribution,
                QuantityDistribution = quantityDistribution
            });
            
            _logger.Information("Using the default generator");
        }

        if (!_generators.OfType<IDataGenerator<CustomerOrder>>().Any())
        {
            var probabilities = new List<double>
                {
                    0.17, 0.06, 0.14, 0.16, 0.09, 0.12, 0.01, 0.01, 0.09, 0.15
                };
            var customerDistribution = Distributions.DiscreteDistribution(_customers.Values.ToList(), probabilities);
            
            _generators.Add(new CustomerOrderGenerator
            {
                CustomerDistribution = customerDistribution, 
                OrderGenerator = _generators.OfType<IDataGenerator<ProductionOrder>>().FirstOrDefault(),
                AmountDistribution = Distributions.ConstantDistribution(1)
            });

            var customerOrders = _generators.OfType<IDataGenerator<CustomerOrder>>().FirstOrDefault()?.Generate(InitialCustomerOrdersGenerated);
            customerOrders?.ForEach(order =>
            {
                _customers.TryGetValue(order.CustomerId, out var customer);
                customer?.Orders.Add(order);
                order.OrderReceivedDate = StartTime ?? DateTime.Now;
            });

            var initialOperationsToSimulate = ModelUtil.GetWorkOperationsFromOrders(customerOrders.SelectMany(order => order.ProductionOrders).ToList());
            if (Controller is SimulationController simulationController)
            {
                simulationController.OperationsToSimulate = initialOperationsToSimulate;
            }
            
             _logger.Debug("Generated {Amount} of customer orders", 
                 _customers.Select(customer => customer.Value.Orders.Count).Sum());
        }
        
        if (Simulator is Simulator simulator)
        {
            foreach (var orderGenerationFrequency in _orderGenerationFrequencies)
            {
               simulator.AddOrderGeneration(orderGenerationFrequency);
            }
            
            foreach (var interrupt in _interrupts)
            {
                simulator.AddInterrupt(interrupt.Predicate, interrupt.Distribution, interrupt.InterruptAction);
            }
        }

        Controller.Execute(Duration);

        if (Controller is SimulationController simController)
        {
            foreach (var folderPath in ReportingFolderPaths)
            {
                FeedbackWriter.WriteFeedbacksToJson(simController.Feedbacks.OfType<ProductionFeedback>().ToList(), Path.Combine(folderPath, "feedbacks.json"));
                FeedbackWriter.WriteFeedbacksToCSV(simController.Feedbacks.OfType<ProductionFeedback>().ToList(), Path.Combine(folderPath, "feedbacks.csv"));
                FeedbackWriter.WriteCustomerOrdersToCSV(_customers.Values.ToList(), Path.Combine(folderPath, "customerOrders.csv"));
            }
        }
    }

    private void LoadEntities()
    {
        if (_entityLoaders == null) return;
        if (!_entityLoaders.Any()) return;

        foreach (var entityLoader in _entityLoaders)
        {
            switch (entityLoader)
            {
                case IEntityLoader<Machine> machineLoader:
                    _resources.AddRange(machineLoader.Load());
                    break;
                case IEntityLoader<WorkPlan> workPlanLoader:
                    _workPlans = workPlanLoader.Load().ToList();
                    break;
                case IEntityLoader<Tool> toolLoader:
                    _tools = toolLoader.Load().ToList();
                    break;
                case IEntityLoader<Customer> customerLoader:
                    _customers = customerLoader.Load().ToDictionary(key => key.Id, value => value);
                    break;
            }
        }
    }

    public ProductionScenario WithController(IController controller)
    {
        if ( controller is null) throw new ArgumentNullException(nameof(controller));
        Controller = controller;
        return this;
    }

    public ProductionScenario WithPlanner(Planner.Abstraction.Planner planner)
    {
        if( planner is null) throw new ArgumentNullException(nameof(planner));
        _planner = planner;
        return this;
    }

    public ProductionScenario WithSimulator(ISimulator simulator)
    {
        if (simulator is null) throw new ArgumentNullException(nameof(simulator));
        Simulator = simulator;
        return this;
    }
    
    public ProductionScenario WithGenerator(IDataGenerator generator)
    {
        if( generator is null) throw new ArgumentNullException(nameof(generator));
        _generators.Add(generator);
        return this;
    }
    
    public ProductionScenario WithResource(IResource resource)
    {
        if ( resource is null) throw new ArgumentNullException(nameof(resource));
        _resources.Add(resource);
        return this;
    }

    public ProductionScenario WithEntityLoader(IEntityLoader entityLoader)
    {
        if ( entityLoader is null) throw new ArgumentNullException(nameof(entityLoader));
        
        _entityLoaders?.Add(entityLoader);
        return this; 
    }
    
    public ProductionScenario WithInterrupt(
        Func<ActiveObject, bool> predicate,
        Distribution<TimeSpan> distribution,
        Func<ActiveObject, IScenario, IEnumerable<Event>> interruptAction
    )
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));   
        if (distribution is null) throw new ArgumentNullException(nameof(distribution));
        if (interruptAction is null) throw new ArgumentNullException(nameof(interruptAction));

        var interruptInfo = new InterruptInfo(
            predicate,
            distribution,
            (obj) => interruptAction(obj, this)
        );
        
        _interrupts.Add(interruptInfo);
        return this;
    }
    
    public ProductionScenario WithOrderGenerationFrequency(Distribution<TimeSpan> timeSpan)
    {
        if (timeSpan is null) throw new ArgumentNullException(nameof(timeSpan));
        _orderGenerationFrequencies.Add(timeSpan);
     
        return this;
    }

    /// <summary>
    /// Enable reporting. All report files will be written to the given folder.
    /// </summary>
    /// <param name="folderPath">The folder where the report files will be written.</param>
    /// <returns>The scenario itself.</returns>
    public ProductionScenario WithReporting(string folderPath)
    {
        if (folderPath is null) throw new ArgumentNullException(nameof(folderPath));

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
        
        ReportingFolderPaths.Add(folderPath);

        return this;
    }
    
    public void CollectStats()
    {
        if (Simulator is not Simulator simulator) return;
        var sumOfWaitingTime = simulator.GetWaitingTimeSummaryOfMachines();

        var utilization = ( (Duration * simulator.CountOfMachines) - TimeSpan.FromSeconds(sumOfWaitingTime) ) / (Duration * simulator.CountOfMachines);
        Log.Logger.Information("Utilization: {Utilization:F2} %", utilization * 100);
        for (var i = 1; i < simulator.CountOfMachines + 1; i++)
        {
            var utilizationOfMachineType = (Duration - TimeSpan.FromSeconds(simulator.GetWaitingTimeByMachineType(i))) / Duration;
            Log.Logger.Information("Utilization of Machine {Machine} is: {Utilization:F2} %", i, utilizationOfMachineType * 100);
        }
    }
    
     private void SimulationEventHandler(EventArgs e, 
        Planner.Abstraction.Planner planner, 
        ISimulator simulator, 
        Plan currentPlan, 
        List<WorkOperation> operationsToSimulate, 
        List<WorkOperation> finishedOperations)
    {
        if (Controller is null) throw new InvalidOperationException("The controller is not set");
        if (Controller is not SimulationController simulationController) return;

        var machines = _resources.OfType<Machine>().ToList();
        
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
            {
                _logger.Information("Replanning started at: {ReplanningStartTime}", replanningEvent.CurrentDate);
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    replanningEvent.CurrentDate);
                
                simulationController.CurrentPlan = newPlan;
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
                simulationController.FinishedOperations = finishedOperations;
                break;
            }
            case InterruptionEvent interruptionEvent: {
                // replan without the machines that just got interrupted
                var newPlan = planner.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionEvent.CurrentDate);
                simulationController.CurrentPlan = newPlan;
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
                    machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionHandledEvent.CurrentDate);
                simulationController.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
            case OrderGenerationEvent orderGenerationEvent:
            {
                var customerOrder = _generators.OfType<IDataGenerator<CustomerOrder>>().FirstOrDefault()?.Generate(1);
                if (customerOrder is null || customerOrder.Count < 1)
                {
                    Log.Logger.Warning("No customer order was generated");
                    return;
                }

                var order = customerOrder.First();
                _customers.TryGetValue(order.CustomerId, out var customer);
                customer?.Orders.Add(order);
                order.OrderReceivedDate = orderGenerationEvent.CurrentDate;
                
                var newOperations = ModelUtil.GetWorkOperationsFromOrders(customerOrder.SelectMany(order => order.ProductionOrders).ToList());
                //TODO: add list of new products to logging
                Log.Logger.Information("Generated {Amount} of new operations for customer {Customer}", newOperations.Count, customer.Name);
                
                operationsToSimulate.AddRange(newOperations);
                simulationController.OperationsToSimulate = operationsToSimulate;

                var newPlan = planner.Schedule(
                    operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    orderGenerationEvent.CurrentDate);
                simulationController.CurrentPlan = newPlan;
                simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
        }
    }
}