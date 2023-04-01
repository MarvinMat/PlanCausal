using ProcessSim.Implementation.Core;
using ProcessSim.Implementation.Core.SimulationModels;
using ProcessSim.Implementation.Services;

using SimSharp;

var start = DateTime.Now;

var simulation = new SimulatorBuilder().Build();

//var machine1 = new MachineModel(simulation, "Maschine 1", "Testbeschreibung");

var machines = new ResourcePool(simulation, );
var machine2 = new PreemptiveResource(simulation, 1);
var machine3 = new Resource(simulation, 2);


var gen = new OrderGenerator(simulation);

simulation.Run(TimeSpan.FromHours(9));

simulation.Log($"{machine1.Name} made {machine1.PartsMade} parts.");
