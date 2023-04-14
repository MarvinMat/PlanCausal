using Controller.Abstraction;
using Core.Abstraction.Domain.Processes;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Implementation;

namespace Controller.Implementation;

public class SimulationController : IController
{
    private readonly ISimulation _simulation;
    public IEnumerable<WorkOperation> WorkOperations { get; set; }

    public SimulationController(IEnumerable<WorkOperation> workOperations)
    {
        WorkOperations = workOperations;
        _simulation = new Simulation(42, DateTime.Now);
    }

    public void Execute()
    {
        //TODO: 

        _simulation.Start(TimeSpan.FromHours(24));
    }
}