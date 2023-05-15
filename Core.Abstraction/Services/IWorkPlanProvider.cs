using Core.Abstraction.Domain.Processes;

namespace Core.Abstraction.Services
{
    public interface IWorkPlanProvider
    {
        List<WorkPlan> Load();
    }
}
