﻿using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;

namespace Planner.Implementation
{
    public class GifflerThompsonPlanner : PlannerBase
    {

        public GifflerThompsonPlanner() : base()
        {
        }

        //public void Plan()
        //{
        //    List<WorkOperation> plan = InitializePlan();

        //    _controller = new SimulationController(plan, _machines);
        //    SubscribeToControllerEvents(_controller);
        //    _controller.Execute(TimeSpan.FromDays(1));
        //}

        //private List<WorkOperation> InitializePlan(IEnumerable<WorkOperation>? workOperations = null)
        //{
        //    var rnd = new Random();
        //    List<WorkOperation> plan;

        //    if (workOperations is null)
        //    {
        //        plan = new List<WorkOperation>();
        //    }
        //    else
        //    {
        //        plan = workOperations.ToList();
        //    }

        //    _productionOrders.ToList().ForEach(order =>
        //    {
        //        WorkOperation? prevOperation = null;
        //        order.WorkPlan.WorkPlanPositions.ForEach(operation =>
        //        {
        //            var workOperation = new WorkOperation(operation);
        //            var matchingMachines = _machines.Where(machine => machine.MachineType == operation.MachineType).ToArray();
        //            workOperation.Machine = matchingMachines[rnd.Next(0, matchingMachines.Length)];
        //            workOperation.State = OperationState.Created;

        //            if (prevOperation is not null)
        //            {
        //                prevOperation.Successor = workOperation;
        //                workOperation.Predecessor = prevOperation;
        //            }
        //            prevOperation = workOperation;

        //            Schedule(workOperation);
        //            plan.Add(workOperation);
        //        });
        //    });
        //    return plan;
        //}

        //private static void Schedule(WorkOperation workOperation)
        //{
        //    //TODO: Rework - Context of all Work Operations and resources needed
        //    workOperation.EarliestStart = DateTime.Now;
        //    workOperation.EarliestFinish = DateTime.Now + workOperation.Duration;
        //    workOperation.LatestStart = DateTime.Now;
        //    workOperation.LatestFinish = DateTime.Now + workOperation.Duration;
        //}

        //protected override void SubscribeToControllerEvents(IController controller)
        //{
        //    controller.RescheduleEvent += RescheduleHandler;
        //}

        //private void RescheduleHandler(object? sender, EventArgs e)
        //{
        //    InitializePlan();
        //}

        // see https://docplayer.org/docview/24/2940604/#file=/storage/24/2940604/2940604.pdf
        public override Plan Schedule(List<WorkOperation> workOperations, List<Machine> machines, DateTime currentTime)
        {
            var plan = new Plan(workOperations);

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
            var S0 = workOperations.Where(o => o.Predecessor is null || o.Predecessor.State == OperationState.InProgress || o.Predecessor.State == OperationState.Completed).ToList();
            var S = new HashSet<WorkOperation>(S0);

            foreach (var op in S)
            {
                if (op.Predecessor?.State == OperationState.InProgress)
                {
                    op.EarliestStart = op.Predecessor.EarliestFinish;
                    op.LatestStart = op.Predecessor.LatestFinish;
                }
                else
                {
                    op.EarliestStart = currentTime;
                    op.LatestStart = currentTime;
                }
            }

            var R = new Dictionary<int, WorkOperation>();

            // 2. run Giffler-Thompson algorithm (schedule every operation)
            while (S.Count > 0)
            {
                // 2.1 find the operation(s) in S that have the earliest finish time considering their current start and their duration
                var earliestFinishOfS = S.Select(o => o.EarliestStart + o.Duration).Min();
                var O = S.Where(o => earliestFinishOfS.Equals(o.EarliestStart + o.Duration));

                // put all needed machine types of these operations O into R together with the operation
                foreach (var op in O)
                {
                    if (!R.ContainsKey(op.WorkPlanPosition.MachineType))
                    {
                        R.Add(op.WorkPlanPosition.MachineType, op);
                    }
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
                    var operationsOnMachineType = S.Where(o => o.WorkPlanPosition.MachineType == selectedMachineType);
                    var validOperationsToSchedule = operationsOnMachineType.Where(o => o.EarliestStart < OpR.EarliestStart + OpR.Duration).ToList();
                    var operationToSchedule = SelectOperationToSchedule(validOperationsToSchedule);

                    // 2.2.3 schedule the selected operation on a machine of the selected machine type
                    S.Remove(operationToSchedule);
                    operationToSchedule.EarliestFinish = operationToSchedule.EarliestStart + operationToSchedule.Duration;
                    operationToSchedule.LatestFinish = operationToSchedule.LatestStart + operationToSchedule.Duration;
                    operationToSchedule.State = OperationState.Scheduled;

                    // select the machine with the earliest possible start time
                    var machinesOfSelectedType = machinesByType[selectedMachineType];
                    var earliestStartTimeByMachineOfSelectedType = earliestStartTimeByMachine.Where(pair => machinesOfSelectedType.Contains(pair.Key)).ToList();
                    earliestStartTimeByMachineOfSelectedType.Sort((a, b) => a.Value.CompareTo(b.Value));
                    var machineToSchedule = earliestStartTimeByMachineOfSelectedType.First();

                    // schedule the operation on the machine
                    operationToSchedule.Machine = machineToSchedule.Key;

                    // set the earliest start time of the machine to the finish time of the scheduled operation and update the earliest start time of the machine type
                    earliestStartTimeByMachine[machineToSchedule.Key] = operationToSchedule.EarliestStart + operationToSchedule.Duration;

                    earliestStartTimeByMachineOfSelectedType = earliestStartTimeByMachine.Where(pair => machinesOfSelectedType.Contains(pair.Key)).ToList();
                    earliestStartTimeByMachineOfSelectedType.Sort((a, b) => a.Value.CompareTo(b.Value));
                    var newEarliestStartTimeOfMachineType = earliestStartTimeByMachineOfSelectedType.First().Value;
                    earliestStartTimeByMachineType[selectedMachineType] = newEarliestStartTimeOfMachineType;

                    // update the start time of all other operations on this machine type
                    var otherOperationsOnSelectedMachineType = workOperations.Where(op =>
                        op.WorkPlanPosition.MachineType == selectedMachineType &&
                        op.State != OperationState.Scheduled
                        ).ToList();
                    otherOperationsOnSelectedMachineType.ForEach(op =>
                    {
                        if (newEarliestStartTimeOfMachineType > op.EarliestStart)
                            op.EarliestStart = newEarliestStartTimeOfMachineType;

                        if (newEarliestStartTimeOfMachineType > op.LatestStart)
                            op.LatestStart = newEarliestStartTimeOfMachineType;
                    });

                    // 2.2.4 update the start time of the successor of the scheduled operation and add it to S (the operations to schedule next)
                    var successor = operationToSchedule.Successor;
                    if (successor != null)
                    {
                        if (operationToSchedule.EarliestFinish > successor.EarliestStart)
                            successor.EarliestStart = operationToSchedule.EarliestFinish;

                        if (operationToSchedule.LatestFinish > successor.LatestStart)
                            successor.LatestStart = operationToSchedule.LatestFinish;

                        S.Add(successor);
                    }
                }
            }

            return plan;
        }

        private WorkOperation SelectOperationToSchedule(List<WorkOperation> operations)
        {
            operations.Sort(ShortestProcessingTimeFirst());
            return operations.First();
        }

        // Code for validating a plan
        //var operationsByMachine = plan.Operations.GroupBy(o => o.Machine);
        //foreach (var machineGroup in operationsByMachine)
        //{
        //    var operationsOnMachine = machineGroup.OrderBy(o => o.EarliestStart).ToList();

        //    for (int i = 0; i < operationsOnMachine.Count; i++)
        //    {
        //        for (int j = i + 1; j < operationsOnMachine.Count; j++)
        //        {
        //            var operation1 = operationsOnMachine[i];
        //            var operation2 = operationsOnMachine[j];

        //            if (operation1.EarliestFinish > operation2.EarliestStart && operation1.EarliestStart < operation2.EarliestFinish)
        //            {
        //                throw new Exception($"Operation {operation1} overlaps with operation {operation2}");
        //            }
        //        }
        //    }
        //}

        //var operationsByOrder = plan.Operations.GroupBy(o => o.WorkOrder);
        //foreach (var orderGroup in operationsByOrder)
        //{
        //    var operationsOfOrder = orderGroup.OrderBy(o => o.EarliestStart).ToList();

        //    for (int i = 0; i < operationsOfOrder.Count; i++)
        //    {
        //        for (int j = i + 1; j < operationsOfOrder.Count; j++)
        //        {
        //            var operation1 = operationsOfOrder[i];
        //            var operation2 = operationsOfOrder[j];

        //            if (operation1.EarliestFinish > operation2.EarliestStart && operation1.EarliestStart < operation2.EarliestFinish)
        //            {
        //                throw new Exception($"Operation {operation1} overlaps with operation {operation2}");
        //            }
        //        }
        //    }
        //}
    }
}
