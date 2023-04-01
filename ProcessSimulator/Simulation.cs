using ProcessSim.Implementation.Core;
using ProcessSim.Implementation.Core.SimulationModels;
using ProcessSim.Implementation.Services;
using ProcessSimImplementation.Domain;
using SimSharp;

var start = DateTime.Now;

var simulation = new Simulation(start);

var workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
var workPlanVOs = workPlanProvider.Load();

var machinesById = new Dictionary<int, MachineModel>();
var workPlans = new List<WorkPlan>();
workPlanVOs.ForEach(plan =>
{
    var operations = new List<WorkOperation>();
    plan.ForEach(operation =>
    {
        if (!machinesById.TryGetValue(operation.MachineId, out var machineModel))
        {
            var machine = new Machine() { Name = $"Maschine {operation.MachineId}" };
            machineModel = new MachineModel(simulation, machine);
            machinesById.TryAdd(operation.MachineId, machineModel);
            SimWorkShop.Instance.Resources.TryAdd(machineModel.Id, machineModel.simMachine);
        }

        operations.Add(new WorkOperation()
        {
            Name = operation.Name,
            Duration = TimeSpan.FromMinutes(operation.Duration),
            Resources = new List<Guid> { machineModel.Id }
        });
    });

    workPlans.Add(new WorkPlan()
    {
        Name = $"Produkt {workPlans.Count + 1}",
        WorkOperations = operations
    });
});

var gen = new OrderGenerator(simulation) 
{
    WorkPlans = workPlans
};

simulation.Run(TimeSpan.FromHours(24));

machinesById.ToList().ForEach(machine =>
{
    machine.Value.Monitors.ForEach(monitor => { 
        //simulation.Log(monitor.Summarize());

        File.AppendAllText("../../../../MachineOutput.log",monitor.Summarize());

    });

});
