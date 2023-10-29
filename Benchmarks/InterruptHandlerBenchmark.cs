using BenchmarkDotNet.Attributes;
using Controller.Implementation;
using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using Core.Implementation.Services;
using Planner.Implementation;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;
using Serilog;

namespace Benchmarks;

[RPlotExporter, SimpleJob(launchCount: 1, warmupCount: 5, iterationCount: 15, invocationCount: 32)]
public class InterruptHandlerBenchmark
{
    private readonly ILogger _logger = Log.ForContext<InterruptHandlerBenchmark>();
    private readonly GifflerThompsonPlanner _planner = new();
    private readonly Simulator _simulator;
    private readonly SimulationController _controller;
    private readonly List<Machine> _machines;
    private readonly List<WorkPlan> _workPlans;
    private ProductionScenario? _productionScenario;
    private const int DaysToSimulate = 90;
    private ProductionScenario? _productionScenarioWithoutAnyPlanning;
    private ProductionScenario? _productionScenarioWithReplanningEventOnly;
    private ProductionScenario? _productionScenarioWithInterruptEventOnly;
    private ProductionScenario? _productionScenarioWithInterruptionHandledEventOnly;
    private ProductionScenario? _productionScenarioWithFullPlanning;
    private ProductionScenario? _productionScenarioWithFullPlanningAndPerformanceOptimization;
    private int _quantity = 20;
    public int Seed { get; set; }
    public DateTime DateNow { get; set; }
    
    
    public InterruptHandlerBenchmark()
    {
        _machines = new MachineProviderJson("../../../../../../../../Machines.json").Load();
        _workPlans = new WorkPlanProviderJson("../../../../../../../../WorkPlans.json").Load();
        _simulator= new Simulator(Seed, DateNow);
        _controller = new SimulationController(
            new List<WorkOperation>(),
            _machines, 
            _planner, 
            _simulator);
    }
    
    [GlobalSetup]
    public void Init()
    {
        _logger.Information("Starting benchmark...");
        
    _productionScenarioWithoutAnyPlanning = new ProductionScenario( _machines, _workPlans, HandleSimulationEventWithoutAnyPlanning);
    _productionScenarioWithReplanningEventOnly = new ProductionScenario( _machines, _workPlans, HandleSimulationEventOnlyWithReplanningEvent);
    _productionScenarioWithInterruptEventOnly = new ProductionScenario( _machines, _workPlans, HandleSimulationEventOnlyWithInterruptEvent);
    _productionScenarioWithInterruptionHandledEventOnly = new ProductionScenario( _machines, _workPlans, HandleSimulationEventOnlyWithInterruptionHandledEvent);
    _productionScenarioWithFullPlanning = new ProductionScenario( _machines, _workPlans, HandleSimulationEventWithFullPlanning);
    _productionScenarioWithFullPlanningAndPerformanceOptimization = new ProductionScenario( _machines, _workPlans, HandleSimulationEventWithFullPlanningAndPerformanceOptimization);
        
    }
    
    [Benchmark]
    public void BenchWithoutAnyPlanning()
    {
        if (_productionScenarioWithoutAnyPlanning == null) throw new Exception("ProductionScenario is null.");
        _productionScenarioWithoutAnyPlanning.Run(TimeSpan.FromDays(DaysToSimulate));
    }
    [Benchmark]
    public void BenchWithReplanningEventOnly()
    {
        if(_productionScenarioWithReplanningEventOnly == null) throw new Exception("ProductionScenario is null.");
        _productionScenarioWithReplanningEventOnly.Run(TimeSpan.FromDays(DaysToSimulate));
    }
    [Benchmark]
    public void BenchWithInterruptEventOnly()
    {
        if(_productionScenarioWithInterruptEventOnly == null) throw new Exception("ProductionScenario is null.");
        _productionScenarioWithInterruptEventOnly.Run(TimeSpan.FromDays(DaysToSimulate));
    }
    [Benchmark]
    public void BenchWithInterruptionHandledEventOnly()
    {
        if(_productionScenarioWithInterruptionHandledEventOnly == null) throw new Exception("ProductionScenario is null.");
        _productionScenarioWithInterruptionHandledEventOnly.Run(TimeSpan.FromDays(DaysToSimulate));
    }
    [Benchmark]
    public void BenchWithFullPlanning()
    {
        if(_productionScenarioWithFullPlanning == null) throw new Exception("ProductionScenario is null.");
        _productionScenarioWithFullPlanning.Run(TimeSpan.FromDays(DaysToSimulate));
    }
    [Benchmark]
    public void BenchWithFullPlanningAndPerformanceOptimizations()
    {
        if(_productionScenarioWithFullPlanningAndPerformanceOptimization == null) throw new Exception("ProductionScenario is null.");
        _productionScenarioWithFullPlanningAndPerformanceOptimization.Run(TimeSpan.FromDays(DaysToSimulate));
    }

    private void HandleSimulationEventWithoutAnyPlanning(
        EventArgs e,
        Planner.Abstraction.Planner planningAlgo,  
        ISimulator simulation,      
        Plan currentPlan,           
        List<WorkOperation> operationsToSimulate,
        List<WorkOperation> finishedOperations
    )
    {
        switch (e)
    {
        case ReplanningEvent replanningEvent when operationsToSimulate.Any():
        {
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
            _controller.FinishedOperations = finishedOperations;
            break;
        }
        case InterruptionEvent interruptionEvent:
        {
            break;
        }
        case InterruptionHandledEvent interruptionHandledEvent:
        {
            break;
        }
    }
    }

    private void HandleSimulationEventOnlyWithReplanningEvent(
        EventArgs e,
        Planner.Abstraction.Planner planningAlgo,  
        ISimulator simulation,      
        Plan currentPlan,           
        List<WorkOperation> operationsToSimulate,
        List<WorkOperation> finishedOperations
        )
    {
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
            {
                Log.Logger.Debug("Replanning started at: {CurrentDate}", replanningEvent.CurrentDate);
                var newPlan = planningAlgo.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    replanningEvent.CurrentDate);
                _controller.CurrentPlan = newPlan;
                simulation.SetCurrentPlan(newPlan.Operations);
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
                _controller.FinishedOperations = finishedOperations;
                break;
            }
            case InterruptionEvent interruptionEvent:
            {
                break;
            }
            case InterruptionHandledEvent interruptionHandledEvent:
            {
                break;
            }
        }
    }

    private void HandleSimulationEventOnlyWithInterruptEvent(
        EventArgs e,
        Planner.Abstraction.Planner planningAlgo,  
        ISimulator simulation,      
        Plan currentPlan,           
        List<WorkOperation> operationsToSimulate,
        List<WorkOperation> finishedOperations 
    )
    {
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
            {
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
                _controller.FinishedOperations = finishedOperations;
                break;
            }
            case InterruptionEvent interruptionEvent:
            {
                // replan without the machines that just got interrupted
                var newPlan = planningAlgo.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionEvent.CurrentDate);
                _controller.CurrentPlan = newPlan;
                _simulator.SetCurrentPlan(newPlan.Operations);
                break;
            }
            case InterruptionHandledEvent interruptionHandledEvent:
            {
                break;
            }
        }
    }

    private void HandleSimulationEventOnlyWithInterruptionHandledEvent(
        EventArgs e,
        Planner.Abstraction.Planner planningAlgo,  
        ISimulator simulation,      
        Plan currentPlan,           
        List<WorkOperation> operationsToSimulate,
        List<WorkOperation> finishedOperations
    )
    {
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
            {
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
                _controller.FinishedOperations = finishedOperations;
                break;
            }
            case InterruptionEvent interruptionEvent:
            {
                break;
            }
            case InterruptionHandledEvent interruptionHandledEvent:
            {
                // replan with the machine included that just finished its interruption
                Log.Logger.Debug("Replanning started at: {CurrentDate}", interruptionHandledEvent.CurrentDate);
                var newPlan = planningAlgo.Schedule(operationsToSimulate
                        .Where(op => !op.State.Equals(OperationState.InProgress)
                                     && !op.State.Equals(OperationState.Completed))
                        .ToList(),
                    _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                    interruptionHandledEvent.CurrentDate);
                _controller.CurrentPlan = newPlan;
                simulation.SetCurrentPlan(newPlan.Operations);
                break;
            }
            
        }
    }

    private void HandleSimulationEventWithFullPlanning(
        EventArgs e,
        Planner.Abstraction.Planner planningAlgo,  
        ISimulator simulation,      
        Plan currentPlan,           
        List<WorkOperation> operationsToSimulate,
        List<WorkOperation> finishedOperations  
    )
    {
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
        {
            Log.Logger.Debug("Replanning started at: {CurrentDate}", replanningEvent.CurrentDate);
            var newPlan = planningAlgo.Schedule(operationsToSimulate
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                replanningEvent.CurrentDate);
            _controller.CurrentPlan = newPlan;
            simulation.SetCurrentPlan(newPlan.Operations);
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
            _controller.FinishedOperations = finishedOperations;
            break;
        }
        case InterruptionEvent interruptionEvent:
        {
            //replan without the machines that just got interrupted
            var newPlan = planningAlgo.Schedule(operationsToSimulate
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                interruptionEvent.CurrentDate);
            _controller.CurrentPlan = newPlan;
            simulation.SetCurrentPlan(newPlan.Operations);
            break;
        }
        case InterruptionHandledEvent interruptionHandledEvent:
        {
            // replan with the machine included that just finished its interruption
            Log.Logger.Debug("Replanning started at: {CurrentDate}", interruptionHandledEvent.CurrentDate);
            var newPlan = planningAlgo.Schedule(operationsToSimulate
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                _machines.Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                interruptionHandledEvent.CurrentDate);
            _controller.CurrentPlan = newPlan;
            simulation.SetCurrentPlan(newPlan.Operations);
            break;
        }
            
        }
    }
       
    private void HandleSimulationEventWithFullPlanningAndPerformanceOptimization(
        EventArgs e,
        Planner.Abstraction.Planner planningAlgo,  
        ISimulator simulation,      
        Plan currentPlan,           
        List<WorkOperation> operationsToSimulate,
        List<WorkOperation> finishedOperations
    )
    {
        switch (e)
        {
            case ReplanningEvent replanningEvent when operationsToSimulate.Any():
        {
            Log.Logger.Debug("Replanning started at: {CurrentDate}", replanningEvent.CurrentDate);
            var newPlan = planningAlgo.Schedule(operationsToSimulate
                    .AsParallel()
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                _machines
                    .AsParallel()
                    .Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                replanningEvent.CurrentDate);
            _controller.CurrentPlan = newPlan;
            simulation.SetCurrentPlan(newPlan.Operations);
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
            _controller.FinishedOperations = finishedOperations;
            break;
        }
        case InterruptionEvent interruptionEvent:
        {
            //replan without the machines that just got interrupted
            var newPlan = planningAlgo.Schedule(operationsToSimulate
                    .AsParallel()
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                _machines
                    .AsParallel()
                    .Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                interruptionEvent.CurrentDate);
            _controller.CurrentPlan = newPlan;
            simulation.SetCurrentPlan(newPlan.Operations);
            break;
        }
        case InterruptionHandledEvent interruptionHandledEvent:
        {
            // replan with the machine included that just finished its interruption
            Log.Logger.Debug("Replanning started at: {CurrentDate}", interruptionHandledEvent.CurrentDate);
            var newPlan = planningAlgo.Schedule(operationsToSimulate
                    .AsParallel()
                    .Where(op => !op.State.Equals(OperationState.InProgress)
                                 && !op.State.Equals(OperationState.Completed))
                    .ToList(),
                _machines
                    .AsParallel()
                    .Where(m => !m.State.Equals(MachineState.Interrupted)).ToList(),
                interruptionHandledEvent.CurrentDate);
            _controller.CurrentPlan = newPlan;
            simulation.SetCurrentPlan(newPlan.Operations);
            break;
        }
            
        }
    }
} 