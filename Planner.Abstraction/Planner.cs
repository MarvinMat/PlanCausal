using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Planner.Abstraction
{
    public abstract class Planner
    {
        /// <summary>
        /// Schedule the execution of the given operations on the given machines by setting the start and end time of each operation and assigning a machine.
        /// The given datetime is the current time of the simulation and will be the start time of the plan.
        /// </summary>
        /// <param name="workOperations">The operations to schedule. This should NOT include operations that have already begun execution or have already been completed.</param>
        /// <param name="machines">The machines to schedule the given operations on. Can include multiple machines of a single type.</param>
        /// <param name="currentTime">The current time of the simulation. This will be the start time of the plan.</param>
        /// <returns>The given work operations as a plan. They are now scheduled with certain start and end times on a specific machine.</returns>
        public Plan Schedule(List<WorkOperation> workOperations, List<Machine> machines, DateTime currentTime)
        {
            var plan = ScheduleInternal(workOperations, machines, currentTime);

            ValidatePlan(plan);
            return plan;
        }

        protected abstract Plan ScheduleInternal(List<WorkOperation> workOperations, List<Machine> machines, DateTime currentTime);

        /// <summary>
        /// A utility method that does some basic validation of the given plan. It checks that no two operations on the same machine overlap
        /// and that no two operations of the same order overlap. If an error in the plan is found, an exception is thrown.
        /// </summary>
        /// <param name="plan">The plan to validate.</param>
        /// <exception cref="Exception">The exception being thrown when the plan is incorrect. 
        /// It has a message containing the two operations that overlap.</exception>
        public static void ValidatePlan(Plan plan)
        {
            var operationsByMachine = plan.Operations.GroupBy(o => o.Machine);
            foreach (var machineGroup in operationsByMachine)
            {
                var operationsOnMachine = machineGroup.OrderBy(o => o.PlannedStart).ToList();

                for (int i = 0; i < operationsOnMachine.Count; i++)
                {
                    for (int j = i + 1; j < operationsOnMachine.Count; j++)
                    {
                        var operation1 = operationsOnMachine[i];
                        var operation2 = operationsOnMachine[j];

                        if (operation1.PlannedFinish > operation2.PlannedStart && operation1.PlannedStart < operation2.PlannedFinish)
                        {
                            throw new Exception($"Operation {operation1} overlaps with operation {operation2}");
                        }
                    }
                }
            }

            var operationsByOrder = plan.Operations.GroupBy(o => o.WorkOrder);
            foreach (var orderGroup in operationsByOrder)
            {
                var operationsOfOrder = orderGroup.OrderBy(o => o.PlannedStart).ToList();

                for (int i = 0; i < operationsOfOrder.Count; i++)
                {
                    for (int j = i + 1; j < operationsOfOrder.Count; j++)
                    {
                        var operation1 = operationsOfOrder[i];
                        var operation2 = operationsOfOrder[j];

                        if (operation1.PlannedFinish > operation2.PlannedStart && operation1.PlannedStart < operation2.PlannedFinish)
                        {
                            throw new Exception($"Operation {operation1} overlaps with operation {operation2}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A utility method that can be used to sort operations by their duration in ascending order.
        /// </summary>
        /// <returns></returns>
        protected static Comparison<WorkOperation> ShortestProcessingTimeFirst()
        {
            return (a, b) => a.MeanDuration.CompareTo(b.MeanDuration);
        }
    }
}
