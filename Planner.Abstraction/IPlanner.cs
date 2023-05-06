using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Planner.Abstraction;
public interface IPlanner
{
    /// <summary>
    /// Schedule the execution of the given operations on the given machines by setting the start and end time of each operation and assigning a machine.
    /// The given datetime is the current time of the simulation and will be the start time of the plan.
    /// </summary>
    /// <param name="workOperations">The operations to schedule. This should NOT include operations that have already begun execution or have already been completed.</param>
    /// <param name="machines">The machines to schedule the given operations on. Can include multiple machines of a single type.</param>
    /// <param name="currentTime">The current time of the simulation. This will be the start time of the plan.</param>
    /// <returns>The given work operations as a plan. They are now scheduled with certain start and end times on a specific machine.</returns>
    Plan Schedule(List<WorkOperation> workOperations, List<Machine> machines/*, List<IResource> resources*/, DateTime currentTime);
}