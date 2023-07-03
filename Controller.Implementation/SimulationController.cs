using Controller.Abstraction;
using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Events;
using ProcessSim.Abstraction.Domain.Interfaces;
using System.Text;

namespace Controller.Implementation;

public class SimulationController : IController
{
    private readonly ISimulator _simulation;
    private readonly Planner.Abstraction.Planner _planner;
    private readonly List<Machine> _machines;
    public List<IFeedback> Feedbacks { get; set; }
    private List<WorkOperation> OperationsToSimulate { get; set; }
    public List<WorkOperation> FinishedOperations { get; set; }
    public Plan CurrentPlan { get; set; }

    public delegate void HandleSimulationEvent(EventArgs e, Planner.Abstraction.Planner planner, ISimulator simulator, Plan currentPlan, List<WorkOperation> OperationsToSimulate, List<WorkOperation> FinishedOperations);
    public HandleSimulationEvent? HandleEvent { get; set; }

    public SimulationController(
        List<WorkOperation> operationsToSimulate,
        List<Machine> machines,
        Planner.Abstraction.Planner planner,
        ISimulator simulator
        )
    {
        OperationsToSimulate = operationsToSimulate;
        FinishedOperations = new List<WorkOperation>();

        _machines = machines;

        _planner = planner;
        CurrentPlan = _planner.Schedule(OperationsToSimulate, machines, DateTime.Now);

        _simulation = simulator;
        _simulation.SimulationEventHandler += InterruptHandler;
        _simulation.CreateSimulationResources(machines);
        Feedbacks = new List<IFeedback>();
    }

    public void Execute(TimeSpan duration)
    {
        _simulation.SetCurrentPlan(CurrentPlan.Operations);
        _simulation.Start(duration);
    }

    /// <summary>
    /// Handles events that interrupt the simulation process, such as replanning or completion of an operation.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private void InterruptHandler(object? sender, EventArgs e)
    {
        //TODO: possibly refactor this to use a separate method as it collides with the purpose of this method
        if (e is OperationCompletedEvent operationCompletedEvent)
        {
            var productionFeedback = new ProductionFeedback(operationCompletedEvent.CompletedOperation)
            {
                CreatedAt = operationCompletedEvent.CurrentDate,
                IsFinished = true,
                DoneTotal = 1,
                DoneInPercent = 100,
                Resources = new List<IResource>(){operationCompletedEvent.CompletedOperation.Machine ??
                                                  throw new NullReferenceException("Machine is null.")},
            };
            operationCompletedEvent.CompletedOperation.Feedbacks.Add(productionFeedback);
            Feedbacks.Add(productionFeedback);
            //Console.WriteLine($"The production order (ID: {productionFeedback.Id}) {productionFeedback.WorkOperation.WorkOrder.ProductionOrder.Name} is now: {productionFeedback.WorkOperation.WorkOrder.ProductionOrder.State}");
        }
        HandleEvent?.Invoke(e, _planner, _simulation, CurrentPlan, OperationsToSimulate, FinishedOperations);

        _simulation.Continue();
    }

    public string Summerize()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Simulation Summary");
        sb.AppendLine("------------------");
        sb.AppendLine($"Planned Operations: {OperationsToSimulate.Count}");
        sb.AppendLine($"Finished Operations: {FinishedOperations.Count}");
        sb.AppendLine($"Remaining Operations: {OperationsToSimulate.Count - FinishedOperations.Count}");

        return sb.ToString();
    }

}

