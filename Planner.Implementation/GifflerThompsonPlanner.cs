using Planner.Abstraction;
using ProcessSim.Abstraction.Services;

namespace Planner.Implementation
{
    public class GifflerThompsonPlanner : PlannerBase, IPlanner
    {
        public GifflerThompsonPlanner(IWorkPlanProvider workPlanProvider) : base(workPlanProvider)
        {
        }

        public override void Plan()
        {
            throw new NotImplementedException();
        }
    }
}
