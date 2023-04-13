using Core.Abstraction.Domain.Processes;

namespace ProcessSim.Abstraction.Services
{
    public interface IWorkPlanProvider
    {
        List<WorkPlan> Load();
    }
}
