using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Planner.Implementation
{
    public class GifflerThompsonPlanner : Abstraction.Planner
    {
        public GifflerThompsonPlanner() : base()
        {
        }

        // see https://docplayer.org/docview/24/2940604/#file=/storage/24/2940604/2940604.pdf
        protected override Plan ScheduleInternal(List<WorkOperation> workOperations, List<Machine> machines, DateTime currentTime)
        {
            var plannedOperations = new List<WorkOperation>();
            var isPlanComplete = true;

            var machineTypes = machines.Select(m => m.MachineType).Distinct().ToList();
            var machinesByType = new Dictionary<int, List<Machine>>();
            foreach (var machineType in machineTypes)
            {
                machinesByType.Add(machineType, machines.Where(m => m.MachineType == machineType).ToList());
            }

            // 1. initialise
            var earliestStartTimeByMachine = new Dictionary<Machine, DateTime>();
            foreach (var machine in machines)
            {
                earliestStartTimeByMachine.Add(machine, currentTime);
            }

            var earliestStartTimeByMachineType = new Dictionary<int, DateTime>();
            foreach (var machineType in machineTypes)
            {
                earliestStartTimeByMachineType.Add(machineType, currentTime);
            }

            // find all operations that are first to be scheduled (operations without predecessors or with predecessors already running or completed )
            var S0 = workOperations.AsParallel().Where(o => o.Predecessor is null || o.Predecessor.State == OperationState.InProgress || o.Predecessor.State == OperationState.Completed).ToList();
            var S = new HashSet<WorkOperation>(S0);

            Parallel.ForEach(S, op =>
            {
                if (op.Predecessor?.State == OperationState.InProgress)
                {
                    op.PlannedStart = op.Predecessor.PlannedFinish;
                }
                else
                {
                    op.PlannedStart = currentTime;
                }
            });

            var R = new Dictionary<int, WorkOperation>();

            // 2. run Giffler-Thompson algorithm (schedule every operation)
            while (S.Count > 0)
            {
                // 2.1 find the operation(s) in S that have the earliest finish time considering their current planned start and their duration
                var earliestFinishOfS = S.AsParallel().Select(o => o.PlannedStart + o.MeanDuration).Min();
                var O = S.AsParallel().Where(o => earliestFinishOfS.Equals(o.PlannedStart + o.MeanDuration));

                // put all needed machine types of these operations O into R together with the operation
                foreach (var op in O)
                {
                    R.TryAdd(op.WorkPlanPosition.MachineType, op);
                }

                // 2.2 schedule an operation on each of these machine types
                while (R.Count > 0)
                {
                    // 2.2.1 select a random machine type from R
                    var rnd = new Random();
                    var selectedMachineType = R.Keys.ElementAt(rnd.Next(R.Count));
                    var OpR = R[selectedMachineType];
                    R.Remove(selectedMachineType);

                    // 2.2.2 find an operation to schedule on the selected machine type by using some priority rule
                    var operationsOnMachineType = S.AsParallel().Where(o => o.WorkPlanPosition.MachineType == selectedMachineType);
                    var validOperationsToSchedule = operationsOnMachineType.AsParallel().Where(o => o.PlannedStart < OpR.PlannedStart + OpR.MeanDuration).ToList();
                    var operationToSchedule = SelectOperationToSchedule(validOperationsToSchedule);

                    // 2.2.3 schedule the selected operation on a machine of the selected machine type

                    S.Remove(operationToSchedule);

                    // select the machine with the earliest possible start time
                    var areMachinesOfTypeAvailable = machinesByType.TryGetValue(selectedMachineType, out var machinesOfSelectedType);
                    if (!areMachinesOfTypeAvailable)
                    {
                        isPlanComplete = false;
                        continue;
                    }
                    var earliestStartTimeByMachineOfSelectedType = earliestStartTimeByMachine.AsParallel().Where(pair => machinesOfSelectedType.Contains(pair.Key)).ToList();
                    earliestStartTimeByMachineOfSelectedType.Sort((a, b) => a.Value.CompareTo(b.Value));
                    var machineToSchedule = earliestStartTimeByMachineOfSelectedType.First();

                    // schedule the operation
                    operationToSchedule.PlannedFinish = operationToSchedule.PlannedStart + operationToSchedule.MeanDuration;
                    if (operationToSchedule.State.Equals(OperationState.Created)) // don't change the state if the operation is already pending on a machine
                        operationToSchedule.State = OperationState.Scheduled;
                    plannedOperations.Add(operationToSchedule);

                    // schedule the operation on the machine
                    operationToSchedule.Machine = machineToSchedule.Key;

                    // set the earliest start time of the machine to the finish time of the scheduled operation and update the earliest start time of the machine type
                    earliestStartTimeByMachine[machineToSchedule.Key] = operationToSchedule.PlannedStart + operationToSchedule.MeanDuration;

                    earliestStartTimeByMachineOfSelectedType = earliestStartTimeByMachine.AsParallel().Where(pair => machinesOfSelectedType.Contains(pair.Key)).ToList();
                    earliestStartTimeByMachineOfSelectedType.Sort((a, b) => a.Value.CompareTo(b.Value));
                    var newEarliestStartTimeOfMachineType = earliestStartTimeByMachineOfSelectedType.First().Value;
                    earliestStartTimeByMachineType[selectedMachineType] = newEarliestStartTimeOfMachineType;

                    // update the start time of all other operations on this machine type
                    var otherOperationsOnSelectedMachineType = workOperations
                        .AsParallel()
                        .Where(op => op.WorkPlanPosition.MachineType == selectedMachineType && !plannedOperations.Contains(op))
                        .ToList();

                    Parallel.ForEach(otherOperationsOnSelectedMachineType, op =>
                    {
                        if (newEarliestStartTimeOfMachineType > op.PlannedStart)
                            op.PlannedStart = newEarliestStartTimeOfMachineType;
                    });


                    // 2.2.4 update the start time of the successor of the scheduled operation and add it to S (the operations to schedule next)
                    var successor = operationToSchedule.Successor;
                    if (successor != null)
                    {
                        if (operationToSchedule.PlannedFinish > successor.PlannedStart)
                            successor.PlannedStart = operationToSchedule.PlannedFinish;

                        S.Add(successor);
                    }
                }
            }

            return new Plan(plannedOperations, isPlanComplete);
        }

        private static WorkOperation SelectOperationToSchedule(List<WorkOperation> operations)
        {
            operations.Sort(ShortestProcessingTimeFirst());
            return operations.First();
        }
    }
}
