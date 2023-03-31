using ProcessSim.Implementation.Core;
using ProcessSim.Implementation.Core.SimulationModels;
using ProcessSim.Implementation.Services;

using SimSharp;

var start = DateTime.Now;

var simulation = new SimulatorBuilder().Build();

var machine1 = new MachineModel(simulation, "Maschine 1", "Testbeschreibung");

var gen = new OrderGenerator(simulation);
gen.Machine = machine1;

simulation.Run(TimeSpan.FromHours(9));



simulation.Log("{0} made {1} parts.", machine1.Name, machine1.PartsMade);
