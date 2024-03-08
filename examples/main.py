import os
import argparse
import random

from pythonnet import load

load("coreclr")
import clr

# Parse the script arguments, if -s is provided, use it as the root directory
# If no -s is provided, use the current directory as the root directory

parser = argparse.ArgumentParser(description='Run the Process Simulation with Python')
parser.add_argument('-s', '--source', type=str, default=os.getcwd(), help='The root directory of the source code')
args = parser.parse_args()

# Set the root directory
root_dir = args.source

bin_dir = os.path.join(root_dir, 'ProcessSimulator\\bin\\Debug\\net6.0')

# Read all .dll files into a list
dll_files = []
for file in os.listdir(bin_dir):
    if file.endswith('.dll'):
        dll_files.append(os.path.join(bin_dir, file))

for lib in dll_files:
    clr.AddReference(lib)

from System import TimeSpan
from System import DateTime
from System import Double
from System.Collections.Generic import List 
from System.Collections.Generic import *

from System import *

from Serilog import *

from SimSharp import ActiveObject, Simulation, Event

from Core.Implementation.Services import MachineProviderJson
from Core.Implementation.Services import WorkPlanProviderJson
from Core.Implementation.Services import CustomerProviderJson
from Core.Abstraction.Services import PythonGeneratorAdapter
from Core.Abstraction.Domain.Processes import Plan, WorkOperation
from Core.Abstraction.Domain.Resources import Machine

from Core.Abstraction import Distributions, Distribution

from Planner.Implementation import PythonDelegatePlanner

from ProcessSim.Implementation import Simulator
from ProcessSim.Implementation.Core.SimulationModels import MachineModel
from ProcessSim.Implementation.Core.InfluencingFactors import InternalInfluenceFactorName

from ProcessSim.Abstraction import IFactor

from ProcessSimulator.Scenarios import ProductionScenario


# Configure Serilog
logger_configuration = LoggerConfiguration() \
    .MinimumLevel.Information() \
    .Enrich.FromLogContext() \

logger_configuration = ConsoleLoggerConfigurationExtensions.Console(logger_configuration.WriteTo)
# logger_configuration = ConsoleLoggerConfigurationExtensions.File(logger_configuration.WriteTo, "log.txt", rollingInterval=RollingInterval.Day)

# Assign the configured logger to Serilog's static Log class
Log.Logger = logger_configuration.CreateLogger()

path_machines = os.path.join(root_dir, 'Machines_11Machines.json')
path_workplans = os.path.join(root_dir, 'Workplans_11Machines.json')
path_customers = os.path.join(root_dir, 'customers.json')

timespans_py = [TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30)]
timespans_net = List[TimeSpan]()
prob_py = [0.25, 0.60, 0.15]
prob_net = List[Double]()

for item in prob_py:
    prob_net.Add(item)

for item in timespans_py:
    timespans_net.Add(item)

    
def schedule_internal(work_operations : List[WorkOperation], machines : List[Machine], current_time : DateTime):
    Log.Logger.Information(F"Scheduling: {work_operations.Count} operations on {machines.Count} machines at {current_time}.")
    return Plan(List[WorkOperation](), False)

def calculate_duration_factor(influencing_factors):
    # temperature = influencing_factors["Temperature"]
    # shift = influencing_factors["Shift"]
    current_time = influencing_factors[InternalInfluenceFactorName.CurrentTime.ToString()]
    time_since_last_interrupt = influencing_factors[InternalInfluenceFactorName.TimeSinceLastInterrupt.ToString()]
    
    # print(temperature.CurrentValue)
    # print(shift.CurrentValue)
    print(current_time.GetCurrentValue())
    print(time_since_last_interrupt.GetCurrentValue())
    
    return 1

scenario = ProductionScenario("Python-11-Machines-Problem", "Simulating the 11-machine problem using python and .NET")\
    .WithEntityLoader(MachineProviderJson(path_machines))\
    .WithEntityLoader(WorkPlanProviderJson(path_workplans))\
    .WithEntityLoader(CustomerProviderJson(path_customers))\
    .WithInterrupt(
        predicate= Func[ActiveObject[Simulation], bool](lambda process: random.random() < 0.7), 
        distribution= Distribution[TimeSpan] (lambda: TimeSpan.FromHours(random.expovariate(1/20))),
        interruptAction= Func[ActiveObject[Simulation], ProductionScenario, IEnumerable[Event]](
            lambda simObject, scenario: \
                PythonGeneratorAdapter[Event](PythonEnumerator(interrupt_action, simObject, scenario))
            ))\
    .WithOrderGenerationFrequency(Distributions.DiscreteDistribution[TimeSpan](
        timespans_net, prob_net))\
    .WithReporting(".")\
    .WithAdjustedOperationTime(Func[Dictionary[str, IFactor], Double](calculate_duration_factor))
    # .WithPlanner(PythonDelegatePlanner(schedule_internal))

scenario.Duration = TimeSpan.FromDays(1)
scenario.Seed = 42
scenario.RePlanningInterval = TimeSpan.FromHours(8)
scenario.StartTime = DateTime.Now
scenario.InitialCustomerOrdersGenerated = 10


def interrupt_action(sim_process, prod_scenario):
    if isinstance(prod_scenario.Simulator, Simulator):
        simulator = prod_scenario.Simulator
    else:
        raise Exception("Simulator is not of type Simulator")
    
    if isinstance(sim_process, MachineModel):
        waitFor = max(random.normalvariate(1, 25/60), 0.01)
        start = simulator.CurrentSimulationTime
        Log.Logger.Warning(F"Interrupted Machine {sim_process.Machine.Description} at {simulator.CurrentSimulationTime}.")
        yield simulator.Timeout(TimeSpan.FromHours(waitFor))
        print(F"Machine {sim_process.Machine.Description} waited {simulator.CurrentSimulationTime - start} (done at {simulator.CurrentSimulationTime}).")


class PythonEnumerator():
    def __init__(self, generator, *args):
        self.generator = generator(*args)
        self.current = None

    def MoveNext(self):
        try:
            self.current = next(self.generator)
            return True
        except StopIteration:
            return False
        
    def Current(self):
        return self.current

    def Dispose(self):
        pass

    
scenario.Run()
scenario.CollectStats()