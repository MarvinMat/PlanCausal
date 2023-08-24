import os
import argparse
import random
from itertools import dropwhile

from pythonnet import load

load("coreclr")
import clr

# Parse the script arguments, if -s is provided, use it as the root directory
# If no -s is provided, use the current directory as the root directory

parser = argparse.ArgumentParser(description='Process some integers.')
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
from System.Collections.Generic import List
from System import DateTime
from System import Func
from System.Linq import Enumerable
from System.Collections.Generic import IEnumerable
from SimSharp import Distributions
from SimSharp import ActiveObject
from SimSharp import Simulation
from SimSharp import Event

from Core.Abstraction.Domain.Processes import ProductionOrder
from Core.Abstraction.Domain.Processes import WorkOperation
from Core.Abstraction.Domain.Resources import Machine
from Core.Abstraction.Domain.Enums import OperationState
from Core.Abstraction.Domain.Enums import MachineState
from Core.Abstraction.Services import PythonGeneratorAdapter
from Core.Implementation.Events import ReplanningEvent
from Core.Implementation.Events import OperationCompletedEvent
from Core.Implementation.Events import InterruptionEvent
from Core.Implementation.Events import InterruptionHandledEvent
from Core.Implementation.Services import MachineProviderJson
from Core.Implementation.Services import WorkPlanProviderJson
from Core.Implementation.Services import ToolProviderJson
from Core.Implementation.Services.Reporting import ProductionStats
from Core.Implementation.Domain import ModelUtil

from Planner.Implementation import GifflerThompsonPlanner

from ProcessSim.Implementation import Simulator
from ProcessSim.Implementation.Core.SimulationModels import MachineModel
from Controller.Implementation import SimulationController

path_machines = os.path.join(root_dir, 'Machines.json')
path_workplans = os.path.join(root_dir, 'Workplans.json')
path_tools = os.path.join(root_dir, 'Tools.json')

machines = MachineProviderJson(path_machines).Load()
plans = WorkPlanProviderJson(path_workplans).Load()
tools = ToolProviderJson(path_tools).Load()

orders = List[ProductionOrder]()

for plan in plans:
    order = ProductionOrder()
    order.Name = f"Order {plan.Name}"
    order.Quantity = 50
    order.WorkPlan = plan
    orders.Add(order)

operations = ModelUtil.GetWorkOperationsFromOrders(orders)

seed = random.randint(1, 10000000)
print(f"Seed: {seed}")
simulator = Simulator(seed, DateTime.Now)

simulator.ReplanningInterval = TimeSpan.FromHours(1)

def interrupt_action(sim_process):
    if isinstance(sim_process, MachineModel):
        waitFor = 2
        start = simulator.CurrentSimulationTime
        print(F"Interrupted machine {sim_process.Machine.Description} at {start}: Waiting {waitFor} hours")
        yield simulator.Timeout(TimeSpan.FromHours(waitFor))
        print(F"Machine {sim_process.Machine.Description} waited {simulator.CurrentSimulationTime - start} (done at {simulator.CurrentSimulationTime}).")


class PythonEnumerator():
    def __init__(self, generator, arg):
        self.generator = generator(arg)
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

    
simulator.AddInterrupt(
    predicate = Func[ActiveObject[Simulation], bool](lambda process: True),
    distribution = Distributions.EXP(TimeSpan.FromHours(5)),
    interruptAction = Func[ActiveObject[Simulation], IEnumerable[Event]](
            lambda arg: PythonGeneratorAdapter[Event](PythonEnumerator(interrupt_action, arg))
    )
)

gt_planner = GifflerThompsonPlanner()

controller = SimulationController(operations, machines, gt_planner, simulator)


def right_shift_successors(operation, operations_to_simulate):
    #queued_operations_on_delayed_machine = Enumerable.OrderBy[WorkOperation, DateTime](
    #   Enumerable.Where[WorkOperation](operations_to_simulate, Func[WorkOperation, bool](lambda op: op.Machine == operation.Machine)),
    #   Func[WorkOperation, DateTime](lambda op: op.EarliestStart)
    # )

    queued_operations_on_delayed_machine = filter(lambda op: op.Machine == operation.Machine, operations_to_simulate)
    queued_operations_on_delayed_machine = sorted(queued_operations_on_delayed_machine, key=lambda op: op.EarliestStart)

    #Skip list till you find the current delayed operation, go one further and get the successor
    #successor_on_machine = Enumerable.FirstOrDefault[WorkOperation](
    #   Enumerable.Skip[WorkOperation](
    #       Enumerable.SkipWhile[WorkOperation](
    #           queued_operations_on_delayed_machine,
    #           Func[WorkOperation, bool](lambda op: not op.Equals(operation))),
    #      1
    #   )
    #)

    successors = list(dropwhile(lambda op: not op.Equals(operation), queued_operations_on_delayed_machine))
    successor_on_machine = successors[1] if len(successors) > 1 else None

    update_successor_times(operation, successor_on_machine, operations_to_simulate)
    update_successor_times(operation, operation.Successor, operations_to_simulate)


def update_successor_times(operation, successor, operations_to_simulate):
    if successor is None:
        return

    delay = operation.LatestFinish - successor.EarliestStart

    if delay > TimeSpan.Zero:
        successor.EarliestStart = successor.EarliestStart.Add(delay)
        successor.LatestStart = successor.LatestStart.Add(delay)
        successor.EarliestFinish = successor.EarliestFinish.Add(delay)
        successor.LatestFinish = successor.LatestFinish.Add(delay)

        right_shift_successors(successor, operations_to_simulate)


def event_handler(e, planner, simulation, current_plan, operations_to_simulate, finished_operations):
    if isinstance(e, ReplanningEvent) and operations_to_simulate.Count > 0:
        print(F"Replanning at: {e.CurrentDate}")
        operations_to_plan = list(filter(
            lambda op: (not op.State.Equals(OperationState.InProgress)) and (not op.State.Equals(OperationState.Completed)),
            operations_to_simulate
        ))
        operations_to_plan_list = List[WorkOperation]()
        for op in operations_to_plan:
            operations_to_plan_list.Add(op)
            
        working_machines = list(filter(lambda m: m.State.Equals(MachineState.Working), machines))
        working_machines_list = List[Machine]()
        for m in working_machines:
            working_machines_list.Add(m)
            
        new_plan = planner.Schedule(
            operations_to_plan_list,
            working_machines_list,
            e.CurrentDate
        )
        controller.CurrentPlan = new_plan
        simulation.SetCurrentPlan(new_plan.Operations)

    if isinstance(e, OperationCompletedEvent):
        completed_operation = e.CompletedOperation

        # if it is too late, reschedule the current plan (right shift)
        late = e.CurrentDate - completed_operation.LatestFinish
        if late > TimeSpan.Zero:
            completed_operation.LatestFinish = e.CurrentDate
            right_shift_successors(completed_operation, operations_to_simulate)

        if not operations_to_simulate.Remove(completed_operation):
            raise Exception(
                F"Operation {completed_operation.WorkPlanPosition.Name} ({completed_operation.WorkOrder.Name}) " +
                F"was just completed but not found in the list of operations to simulate. This should not happen.")

        finished_operations.Add(completed_operation)
        controller.FinishedOperations = finished_operations
        
    if isinstance(e, InterruptionEvent):
        # replan without the machines that just got interrupted
        operations_to_plan = list(filter(
            lambda op: (not op.State.Equals(OperationState.InProgress)) and (not op.State.Equals(OperationState.Completed)),
            operations_to_simulate
        ))
        operations_to_plan_list = List[WorkOperation]()
        for op in operations_to_plan:
            operations_to_plan_list.Add(op)
            
        working_machines = list(filter(lambda m: m.State.Equals(MachineState.Working), machines))
        working_machines_list = List[Machine]()
        for m in working_machines:
            working_machines_list.Add(m)
            
        new_plan = planner.Schedule(
            operations_to_plan_list,
            working_machines_list,
            e.CurrentDate
        )
        controller.CurrentPlan = new_plan
        simulation.SetCurrentPlan(new_plan.Operations)
        
    if isinstance(e, InterruptionHandledEvent):
        # replan with the machine included that just finished its interruption
        operations_to_plan = list(filter(
            lambda op: (not op.State.Equals(OperationState.InProgress)) and (not op.State.Equals(OperationState.Completed)),
            operations_to_simulate
        ))
        operations_to_plan_list = List[WorkOperation]()
        for op in operations_to_plan:
            operations_to_plan_list.Add(op)
            
        working_machines = list(filter(lambda m: m.State.Equals(MachineState.Working), machines))
        working_machines_list = List[Machine]()
        for m in working_machines:
            working_machines_list.Add(m)
            
        new_plan = planner.Schedule(
            operations_to_plan_list,
            working_machines_list,
            e.CurrentDate
        )
        controller.CurrentPlan = new_plan
        simulation.SetCurrentPlan(new_plan.Operations)


controller.HandleEvent = SimulationController.HandleSimulationEvent(event_handler)
controller.Execute(TimeSpan.FromDays(7))

print(simulator.GetResourceSummary())

stats = ProductionStats(orders, controller.Feedbacks)

meanLeadTime = stats.CalculateMeanLeadTimeInMinutes()
print(f"Mean lead time: {meanLeadTime} minutes")

meanLeadTimeMachine1 = stats.CalculateMeanLeadTimeOfAGivenMachineTypeInMinutes(1)
print(f"Mean lead time of machine 1: {meanLeadTimeMachine1} minutes")