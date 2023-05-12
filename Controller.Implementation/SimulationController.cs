using Controller.Abstraction;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Planner.Abstraction;
using ProcessSim.Abstraction.Domain.Interfaces;

namespace Controller.Implementation;

public class SimulationController : IController
{
    private readonly ISimulator _simulation;
    private readonly IPlanner _planner;
    private readonly List<Machine> _machines;
    public List<WorkOperation> OperationsToSimulate { get; set; }
    public List<WorkOperation> FinishedOperations { get; set; }
    public Plan CurrentPlan { get; set; }

    public delegate void HandleInterruptEvent(EventArgs e, IPlanner planner, ISimulator simulator, Plan currentPlan, List<WorkOperation> OperationsToSimulate, List<WorkOperation> FinishedOperations);
    public HandleInterruptEvent? HandleEvent { get; set; }

    public SimulationController(
        List<WorkOperation> operationsToSimulate,
        List<Machine> machines,
        IPlanner planner,
        ISimulator simulator
        )
    {
        OperationsToSimulate = operationsToSimulate;
        FinishedOperations = new List<WorkOperation>();

        _machines = machines;

        _planner = planner;
        CurrentPlan = _planner.Schedule(OperationsToSimulate, machines, DateTime.Now);

        _simulation = simulator;
        _simulation.InterruptEvent += InterruptHandler;
        _simulation.CreateSimulationResources(machines);
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
        HandleEvent?.Invoke(e, _planner, _simulation, CurrentPlan, OperationsToSimulate, FinishedOperations);

        _simulation.Continue();
    }

}

